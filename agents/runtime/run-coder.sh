#!/usr/bin/env bash
set -euo pipefail

PROMPT_PATH="${1:?prompt path required}"
AUDIT_PATH="${2:?audit path required}"

GEMINI_MODEL="${GEMINI_MODEL:-gemini-3.8-flash}"
OPENROUTER_MODEL="${OPENROUTER_MODEL:-nvidia/nemotron-3-ultra-550b-a55b:free}"
GEMINI_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-gemini.jsonl"
GEMINI_ERROR="${RUNNER_TEMP:-/tmp}/coder-gemini.stderr.log"
OPENROUTER_OUTPUT="${RUNNER_TEMP:-/tmp}/coder-openrouter.jsonl"

if [[ ! -f "$PROMPT_PATH" ]]; then
  echo "Prompt file not found: $PROMPT_PATH" >&2
  exit 2
fi

write_audit() {
  local selected_provider="$1"
  local selected_model="$2"
  local fallback_from="$3"
  local fallback_reason="$4"
  local gemini_initialized="$5"
  local gemini_actual="$6"
  local openrouter_outcome="${7:-not_attempted}"
  local openrouter_status="${8:-0}"

  jq -n \
    --arg selected_provider "$selected_provider" \
    --arg selected_model "$selected_model" \
    --arg fallback_from "$fallback_from" \
    --arg fallback_reason "$fallback_reason" \
    --arg gemini_requested "$GEMINI_MODEL" \
    --arg gemini_initialized "$gemini_initialized" \
    --argjson gemini_actual "$gemini_actual" \
    --arg openrouter_requested "openrouter/$OPENROUTER_MODEL" \
    --arg openrouter_outcome "$openrouter_outcome" \
    --argjson openrouter_status "$openrouter_status" \
    '{selected_provider:$selected_provider,selected_model:$selected_model,fallback_from:$fallback_from,fallback_reason:$fallback_reason,gemini:{requested:$gemini_requested,initialized:$gemini_initialized,actual_usage:$gemini_actual},openrouter:{requested:$openrouter_requested,outcome:$openrouter_outcome,exit_status:$openrouter_status}}' \
    > "$AUDIT_PATH"
}

GEMINI_STATUS=127
GEMINI_INITIALIZED="unknown"
GEMINI_ACTUAL='{}'
GEMINI_REASON="not_configured"

if [[ -n "${GEMINI_API_KEY:-}" ]]; then
  echo "Trying Gemini coding provider: $GEMINI_MODEL"
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
    write_audit "gemini" "$GEMINI_MODEL" "" "" "$GEMINI_INITIALIZED" "$GEMINI_ACTUAL" "not_attempted" 0
    exit 0
  fi

  if grep -Eqi 'quota|429|exhausted|rate.?limit' "$GEMINI_OUTPUT" "$GEMINI_ERROR" 2>/dev/null; then
    GEMINI_REASON="quota_or_rate_limit"
  else
    GEMINI_REASON="provider_or_runtime_error"
  fi

  echo "Gemini coding provider failed with status $GEMINI_STATUS ($GEMINI_REASON)."
else
  echo "GEMINI_API_KEY is not configured; skipping Gemini."
fi

if [[ -z "${OPENROUTER_API_KEY:-}" ]]; then
  echo "No OPENROUTER_API_KEY fallback is configured."
  write_audit "none" "" "gemini" "$GEMINI_REASON" "$GEMINI_INITIALIZED" "$GEMINI_ACTUAL" "not_configured" 0
  exit "$GEMINI_STATUS"
fi

# Never mix partial edits from a failed provider attempt with fallback output.
echo "Resetting disposable workspace before OpenRouter fallback."
git reset --hard HEAD
git clean -fd

echo "Trying OpenRouter/OpenCode coding provider: $OPENROUTER_MODEL"
set +e
opencode run \
  --pure \
  --auto \
  --agent build \
  --format json \
  --model "openrouter/$OPENROUTER_MODEL" \
  "$(cat "$PROMPT_PATH")" \
  | tee "$OPENROUTER_OUTPUT"
OPENROUTER_STATUS=${PIPESTATUS[0]}
set -e

if [[ "$OPENROUTER_STATUS" -ne 0 ]]; then
  OPENROUTER_REASON="provider_or_runtime_error"
  if grep -Eqi 'quota|429|exhausted|rate.?limit|free-models-per-day' "$OPENROUTER_OUTPUT" 2>/dev/null; then
    OPENROUTER_REASON="quota_or_rate_limit"
  fi

  # A provider can hit its quota after it has already produced a coherent candidate.
  # Preserve such edits and let deterministic acceptance + independent reviewers decide.
  if [[ "$OPENROUTER_REASON" = "quota_or_rate_limit" ]] && [[ -n "$(git status --porcelain --untracked-files=normal)" ]]; then
    write_audit "openrouter" "openrouter/$OPENROUTER_MODEL" "gemini" "$GEMINI_REASON" "$GEMINI_INITIALIZED" "$GEMINI_ACTUAL" "partial_candidate_after_rate_limit" "$OPENROUTER_STATUS"
    echo "OpenRouter hit a late quota/rate limit after producing repository changes. Preserving candidate for deterministic validation."
    exit 0
  fi

  write_audit "openrouter" "openrouter/$OPENROUTER_MODEL" "gemini" "$GEMINI_REASON" "$GEMINI_INITIALIZED" "$GEMINI_ACTUAL" "failed" "$OPENROUTER_STATUS"
  echo "OpenRouter/OpenCode fallback failed with status $OPENROUTER_STATUS ($OPENROUTER_REASON)." >&2
  exit "$OPENROUTER_STATUS"
fi

write_audit "openrouter" "openrouter/$OPENROUTER_MODEL" "gemini" "$GEMINI_REASON" "$GEMINI_INITIALIZED" "$GEMINI_ACTUAL" "success" 0
