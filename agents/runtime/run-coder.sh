#!/usr/bin/env bash
set -euo pipefail

PROMPT_PATH="${1:?prompt path required}"
AUDIT_PATH="${2:?audit path required}"

OPENROUTER_MODEL="${OPENROUTER_MODEL:-nvidia/nemotron-3-ultra-550b-a55b:free}"
OPENROUTER_TIMEOUT_MINUTES="${OPENROUTER_TIMEOUT_MINUTES:-30}"
DEEPSEEK_MODEL="${DEEPSEEK_MODEL:-deepseek-flash}"
DEEPSEEK_TIMEOUT_MINUTES="${DEEPSEEK_TIMEOUT_MINUTES:-30}"
CODER_TRUSTED_GIT_REF="${CODER_TRUSTED_GIT_REF:-origin/main}"

OPENROUTER_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-openrouter.jsonl"
OPENROUTER_ERROR="${RUNNER_TEMP:-/tmp}/coder-openrouter.stderr.log"
DEEPSEEK_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-deepseek.jsonl"
DEEPSEEK_ERROR="${RUNNER_TEMP:-/tmp}/coder-deepseek.stderr.log"
TRUSTED_CONTEXT_PROMPT="${RUNNER_TEMP:-/tmp}/coder-trusted-context.md"
TRUSTED_CONTEXT_MANIFEST="${RUNNER_TEMP:-/tmp}/coder-trusted-context.json"

if [[ ! -f "$PROMPT_PATH" ]]; then
  echo "Prompt file not found: $PROMPT_PATH" >&2
  exit 2
fi

python3 agents/runtime/build-coder-context.py \
  --git-ref "$CODER_TRUSTED_GIT_REF" \
  --prompt-input "$PROMPT_PATH" \
  --output "$TRUSTED_CONTEXT_PROMPT" \
  --manifest "$TRUSTED_CONTEXT_MANIFEST"
PROMPT_PATH="$TRUSTED_CONTEXT_PROMPT"

echo "Trusted coding context manifest:"
cat "$TRUSTED_CONTEXT_MANIFEST"

summarize_opencode_usage() {
  local path="$1"
  if [[ ! -s "$path" ]]; then
    printf '%s\n' '{"step_finishes":0,"input_tokens":0,"output_tokens":0,"reasoning_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"reported_cost_usd":0,"usage_complete":false}'
    return
  fi

  jq -cs '
    [ .[] | select(.type == "step_finish" and (.part | type) == "object") ] as $steps |
    {
      step_finishes: ($steps | length),
      input_tokens: (($steps | map(.part.tokens.input // 0) | add) // 0),
      output_tokens: (($steps | map(.part.tokens.output // 0) | add) // 0),
      reasoning_tokens: (($steps | map(.part.tokens.reasoning // 0) | add) // 0),
      cache_read_tokens: (($steps | map(.part.tokens.cache.read // 0) | add) // 0),
      cache_write_tokens: (($steps | map(.part.tokens.cache.write // 0) | add) // 0),
      reported_cost_usd: (($steps | map(.part.cost // 0) | add) // 0),
      usage_complete: (($steps | length) > 0)
    }
  ' "$path" 2>/dev/null || printf '%s\n' '{"step_finishes":0,"input_tokens":0,"output_tokens":0,"reasoning_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"reported_cost_usd":0,"usage_complete":false}'
}

OPENROUTER_STATUS=0
OPENROUTER_OUTCOME="not_attempted"
OPENROUTER_REASON="not_configured"
OPENROUTER_USAGE='{"step_finishes":0,"input_tokens":0,"output_tokens":0,"reasoning_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"reported_cost_usd":0,"usage_complete":false}'

DEEPSEEK_STATUS=0
DEEPSEEK_OUTCOME="not_attempted"
DEEPSEEK_REASON="not_configured"
DEEPSEEK_USAGE='{"step_finishes":0,"input_tokens":0,"output_tokens":0,"reasoning_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"reported_cost_usd":0,"usage_complete":false}'

write_audit() {
  local selected_provider="$1"
  local selected_model="$2"
  local final_outcome="$3"
  local context_bundle
  context_bundle=$(cat "$TRUSTED_CONTEXT_MANIFEST")

  jq -n \
    --arg selected_provider "$selected_provider" \
    --arg selected_model "$selected_model" \
    --arg final_outcome "$final_outcome" \
    --arg openrouter_requested "openrouter/$OPENROUTER_MODEL" \
    --arg openrouter_outcome "$OPENROUTER_OUTCOME" \
    --arg openrouter_reason "$OPENROUTER_REASON" \
    --argjson openrouter_status "$OPENROUTER_STATUS" \
    --argjson openrouter_usage "$OPENROUTER_USAGE" \
    --arg deepseek_requested "deepseek/$DEEPSEEK_MODEL" \
    --arg deepseek_outcome "$DEEPSEEK_OUTCOME" \
    --arg deepseek_reason "$DEEPSEEK_REASON" \
    --argjson deepseek_status "$DEEPSEEK_STATUS" \
    --argjson deepseek_usage "$DEEPSEEK_USAGE" \
    --argjson context_bundle "$context_bundle" \
    '{
      selected_provider:$selected_provider,
      selected_model:$selected_model,
      final_outcome:$final_outcome,
      provider_order:["openrouter","deepseek"],
      policy:"Use OpenRouter while available; on quota/rate exhaustion continue with DeepSeek. A fresh secondary ChatGPT is reserved for independent review when the primary ChatGPT is not independent; it is not a coding fallback.",
      context_bundle:$context_bundle,
      openrouter:{requested:$openrouter_requested,outcome:$openrouter_outcome,reason:$openrouter_reason,exit_status:$openrouter_status,usage:$openrouter_usage},
      deepseek:{requested:$deepseek_requested,outcome:$deepseek_outcome,reason:$deepseek_reason,exit_status:$deepseek_status,usage:$deepseek_usage},
      secondary_chatgpt:{role:"independent_review_only",outcome:"not_used_by_coder"}
    }' > "$AUDIT_PATH"
}

has_workspace_changes() {
  [[ -n "$(git status --porcelain --untracked-files=normal)" ]]
}

reset_workspace() {
  echo "Resetting disposable workspace before fallback provider."
  git reset --hard HEAD
  git clean -fd
}

# Primary coding provider: OpenRouter free model while its daily allowance is available.
if [[ -n "${OPENROUTER_API_KEY:-}" ]]; then
  echo "Trying OpenRouter/OpenCode coding provider: openrouter/$OPENROUTER_MODEL"
  set +e
  timeout "${OPENROUTER_TIMEOUT_MINUTES}m" \
    opencode run \
      --pure \
      --auto \
      --agent build \
      --format json \
      --model "openrouter/$OPENROUTER_MODEL" \
      "$(cat "$PROMPT_PATH")" \
      2> >(tee "$OPENROUTER_ERROR" >&2) \
      | tee "$OPENROUTER_OUTPUT"
  OPENROUTER_STATUS=${PIPESTATUS[0]}
  set -e
  OPENROUTER_USAGE=$(summarize_opencode_usage "$OPENROUTER_OUTPUT")

  if [[ "$OPENROUTER_STATUS" -eq 0 ]]; then
    OPENROUTER_OUTCOME="success"
    OPENROUTER_REASON=""
    write_audit "openrouter" "openrouter/$OPENROUTER_MODEL" "success"
    exit 0
  fi

  OPENROUTER_OUTCOME="failed"
  if [[ "$OPENROUTER_STATUS" -eq 124 ]]; then
    OPENROUTER_REASON="timeout"
  elif grep -Eqi 'quota|429|exhausted|rate.?limit|free-models-per-day' "$OPENROUTER_OUTPUT" "$OPENROUTER_ERROR" 2>/dev/null; then
    OPENROUTER_REASON="quota_or_rate_limit"
  else
    OPENROUTER_REASON="provider_or_runtime_error"
  fi

  echo "OpenRouter coding provider failed with status $OPENROUTER_STATUS ($OPENROUTER_REASON)."

  if [[ "$OPENROUTER_REASON" = "quota_or_rate_limit" ]] && has_workspace_changes; then
    OPENROUTER_OUTCOME="partial_candidate_after_rate_limit"
    echo "OpenRouter exhausted its allowance after producing repository changes. Keeping those changes and asking DeepSeek to continue the same bounded task."
  else
    reset_workspace
  fi
else
  echo "OPENROUTER_API_KEY is not configured; skipping OpenRouter."
  reset_workspace
fi

# Paid fallback: official DeepSeek API / deepseek-flash.
if [[ -n "${DEEPSEEK_API_KEY:-}" ]]; then
  echo "Trying DeepSeek coding fallback: deepseek/$DEEPSEEK_MODEL"
  set +e
  timeout "${DEEPSEEK_TIMEOUT_MINUTES}m" \
    opencode run \
      --pure \
      --auto \
      --agent build \
      --format json \
      --model "deepseek/$DEEPSEEK_MODEL" \
      "$(cat "$PROMPT_PATH")" \
      2> >(tee "$DEEPSEEK_ERROR" >&2) \
      | tee "$DEEPSEEK_OUTPUT"
  DEEPSEEK_STATUS=${PIPESTATUS[0]}
  set -e
  DEEPSEEK_USAGE=$(summarize_opencode_usage "$DEEPSEEK_OUTPUT")

  if [[ "$DEEPSEEK_STATUS" -eq 0 ]]; then
    DEEPSEEK_OUTCOME="success"
    DEEPSEEK_REASON=""
    write_audit "deepseek" "deepseek/$DEEPSEEK_MODEL" "success"
    exit 0
  fi

  DEEPSEEK_OUTCOME="failed"
  if [[ "$DEEPSEEK_STATUS" -eq 124 ]]; then
    DEEPSEEK_REASON="timeout"
  elif grep -Eqi 'quota|429|rate.?limit|insufficient.?balance|balance.?insufficient|payment|required|(^|[^0-9])402([^0-9]|$)' "$DEEPSEEK_OUTPUT" "$DEEPSEEK_ERROR" 2>/dev/null; then
    DEEPSEEK_REASON="quota_rate_or_balance_limit"
  else
    DEEPSEEK_REASON="provider_or_runtime_error"
  fi

  echo "DeepSeek coding provider failed with status $DEEPSEEK_STATUS ($DEEPSEEK_REASON)."

  if [[ "$DEEPSEEK_REASON" = "quota_rate_or_balance_limit" ]] && has_workspace_changes; then
    DEEPSEEK_OUTCOME="partial_candidate_after_limit"
    write_audit "deepseek" "deepseek/$DEEPSEEK_MODEL" "partial_candidate_after_limit"
    echo "DeepSeek hit a late quota/rate/balance limit after producing repository changes. Preserving candidate for deterministic validation."
    exit 0
  fi
else
  echo "DEEPSEEK_API_KEY is not configured; skipping DeepSeek."
fi

write_audit "none" "" "all_coding_providers_failed"

if [[ "$DEEPSEEK_STATUS" -ne 0 ]]; then
  exit "$DEEPSEEK_STATUS"
fi
if [[ "$OPENROUTER_STATUS" -ne 0 ]]; then
  exit "$OPENROUTER_STATUS"
fi
exit 2
