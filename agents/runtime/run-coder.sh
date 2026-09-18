#!/usr/bin/env bash
set -euo pipefail

PROMPT_PATH="${1:?prompt path required}"
AUDIT_PATH="${2:?audit path required}"

DEEPSEEK_MODEL="${DEEPSEEK_MODEL:-deepseek-flash}"
DEEPSEEK_TIMEOUT_MINUTES="${DEEPSEEK_TIMEOUT_MINUTES:-30}"
GEMINI_MODEL="${GEMINI_MODEL:-gemini-3.8-flash}"
OPENROUTER_MODEL="${OPENROUTER_MODEL:-nvidia/nemotron-3-ultra-550b-a55b:free}"

DEEPSEEK_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-deepseek.jsonl"
DEEPSEEK_ERROR="${RUNNER_TEMP:-/tmp}/coder-deepseek.stderr.log"
GEMINI_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-gemini.jsonl"
GEMINI_ERROR="${RUNNER_TEMP:-/tmp}/coder-gemini.stderr.log"
OPENROUTER_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-openrouter.jsonl"
OPENROUTER_ERROR="${RUNNER_TEMP:-/tmp}/coder-openrouter.stderr.log"

if [[ ! -f "$PROMPT_PATH" ]]; then
  echo "Prompt file not found: $PROMPT_PATH" >&2
  exit 2
fi

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

DEEPSEEK_STATUS=0
DEEPSEEK_OUTCOME="not_attempted"
DEEPSEEK_REASON="not_configured"
DEEPSEEK_USAGE='{"step_finishes":0,"input_tokens":0,"output_tokens":0,"reasoning_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"reported_cost_usd":0,"usage_complete":false}'

GEMINI_STATUS=0
GEMINI_OUTCOME="not_attempted"
GEMINI_INITIALIZED="unknown"
GEMINI_ACTUAL='{}'
GEMINI_REASON="not_configured"

OPENROUTER_STATUS=0
OPENROUTER_OUTCOME="not_attempted"
OPENROUTER_REASON="not_configured"
OPENROUTER_USAGE='{"step_finishes":0,"input_tokens":0,"output_tokens":0,"reasoning_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"reported_cost_usd":0,"usage_complete":false}'

write_audit() {
  local selected_provider="$1"
  local selected_model="$2"
  local final_outcome="$3"

  jq -n \
    --arg selected_provider "$selected_provider" \
    --arg selected_model "$selected_model" \
    --arg final_outcome "$final_outcome" \
    --arg deepseek_requested "deepseek/$DEEPSEEK_MODEL" \
    --arg deepseek_outcome "$DEEPSEEK_OUTCOME" \
    --arg deepseek_reason "$DEEPSEEK_REASON" \
    --argjson deepseek_status "$DEEPSEEK_STATUS" \
    --argjson deepseek_usage "$DEEPSEEK_USAGE" \
    --arg gemini_requested "$GEMINI_MODEL" \
    --arg gemini_initialized "$GEMINI_INITIALIZED" \
    --arg gemini_outcome "$GEMINI_OUTCOME" \
    --arg gemini_reason "$GEMINI_REASON" \
    --argjson gemini_status "$GEMINI_STATUS" \
    --argjson gemini_actual "$GEMINI_ACTUAL" \
    --arg openrouter_requested "openrouter/$OPENROUTER_MODEL" \
    --arg openrouter_outcome "$OPENROUTER_OUTCOME" \
    --arg openrouter_reason "$OPENROUTER_REASON" \
    --argjson openrouter_status "$OPENROUTER_STATUS" \
    --argjson openrouter_usage "$OPENROUTER_USAGE" \
    '{
      selected_provider:$selected_provider,
      selected_model:$selected_model,
      final_outcome:$final_outcome,
      provider_order:["deepseek","gemini","openrouter"],
      deepseek:{requested:$deepseek_requested,outcome:$deepseek_outcome,reason:$deepseek_reason,exit_status:$deepseek_status,usage:$deepseek_usage},
      gemini:{requested:$gemini_requested,initialized:$gemini_initialized,outcome:$gemini_outcome,reason:$gemini_reason,exit_status:$gemini_status,actual_usage:$gemini_actual},
      openrouter:{requested:$openrouter_requested,outcome:$openrouter_outcome,reason:$openrouter_reason,exit_status:$openrouter_status,usage:$openrouter_usage}
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

# Primary provider: DeepSeek V4.1 Flash through OpenCode's native DeepSeek provider.
if [[ -n "${DEEPSEEK_API_KEY:-}" ]]; then
  echo "Trying DeepSeek coding provider: deepseek/$DEEPSEEK_MODEL"
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

reset_workspace

# First fallback: Gemini CLI.
if [[ -n "${GEMINI_API_KEY:-}" ]]; then
  echo "Trying Gemini coding fallback: $GEMINI_MODEL"
  set +e
  gemini \
    --model "$GEMINI_MODEL" \
    --prompt "$(cat "$PROMPT_PATH")" \
    --approval-mode=yolo \
    --skip-trust \
    --extensions none \
    --output-format stream-json \
    2> >(tee "$GEMINI_ERROR" >&2) \
    | tee "$GEMINI_OUTPUT"
  GEMINI_STATUS=${PIPESTATUS[0]}
  set -e

  if [[ -f "$GEMINI_OUTPUT" ]]; then
    FOUND_INIT=$(jq -r 'select(.type == "init") | .model // empty' "$GEMINI_OUTPUT" | head -n 1 || true)
    FOUND_ACTUAL=$(jq -c 'select(.type == "result") | .stats.models // {}' "$GEMINI_OUTPUT" | tail -n 1 || true)
    [[ -n "$FOUND_INIT" ]] && GEMINI_INITIALIZED="$FOUND_INIT"
    [[ -n "$FOUND_ACTUAL" ]] && GEMINI_ACTUAL="$FOUND_ACTUAL"
  fi

  if [[ "$GEMINI_STATUS" -eq 0 ]]; then
    GEMINI_OUTCOME="success"
    GEMINI_REASON=""
    write_audit "gemini" "$GEMINI_MODEL" "success"
    exit 0
  fi

  GEMINI_OUTCOME="failed"
  if grep -Eqi 'quota|429|exhausted|rate.?limit' "$GEMINI_OUTPUT" "$GEMINI_ERROR" 2>/dev/null; then
    GEMINI_REASON="quota_or_rate_limit"
  else
    GEMINI_REASON="provider_or_runtime_error"
  fi

  echo "Gemini coding fallback failed with status $GEMINI_STATUS ($GEMINI_REASON)."

  if [[ "$GEMINI_REASON" = "quota_or_rate_limit" ]] && has_workspace_changes; then
    GEMINI_OUTCOME="partial_candidate_after_rate_limit"
    write_audit "gemini" "$GEMINI_MODEL" "partial_candidate_after_rate_limit"
    echo "Gemini hit a late quota/rate limit after producing repository changes. Preserving candidate for deterministic validation."
    exit 0
  fi
else
  echo "GEMINI_API_KEY is not configured; skipping Gemini."
fi

reset_workspace

# Final fallback: OpenRouter/OpenCode free model.
if [[ -n "${OPENROUTER_API_KEY:-}" ]]; then
  echo "Trying OpenRouter/OpenCode coding fallback: $OPENROUTER_MODEL"
  set +e
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
  if grep -Eqi 'quota|429|exhausted|rate.?limit|free-models-per-day' "$OPENROUTER_OUTPUT" "$OPENROUTER_ERROR" 2>/dev/null; then
    OPENROUTER_REASON="quota_or_rate_limit"
  else
    OPENROUTER_REASON="provider_or_runtime_error"
  fi

  if [[ "$OPENROUTER_REASON" = "quota_or_rate_limit" ]] && has_workspace_changes; then
    OPENROUTER_OUTCOME="partial_candidate_after_rate_limit"
    write_audit "openrouter" "openrouter/$OPENROUTER_MODEL" "partial_candidate_after_rate_limit"
    echo "OpenRouter hit a late quota/rate limit after producing repository changes. Preserving candidate for deterministic validation."
    exit 0
  fi

  echo "OpenRouter/OpenCode fallback failed with status $OPENROUTER_STATUS ($OPENROUTER_REASON)." >&2
else
  echo "OPENROUTER_API_KEY is not configured; skipping OpenRouter."
fi

write_audit "none" "" "all_providers_failed"

if [[ "$OPENROUTER_STATUS" -ne 0 ]]; then
  exit "$OPENROUTER_STATUS"
fi
if [[ "$GEMINI_STATUS" -ne 0 ]]; then
  exit "$GEMINI_STATUS"
fi
if [[ "$DEEPSEEK_STATUS" -ne 0 ]]; then
  exit "$DEEPSEEK_STATUS"
fi
exit 2
