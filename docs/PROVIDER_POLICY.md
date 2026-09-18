# Coding Provider Policy

Status: active repository policy.

GitHub is authoritative for provider order and role boundaries. Chat history is not.

## Autonomous coding order

The trusted coder runtime `agents/runtime/run-coder.sh` must use providers in this order:

1. **OpenRouter first** using the configured free coding model (`nvidia/nemotron-3-ultra-550b-a55b:free` unless changed by a versioned repository decision).
2. **DeepSeek second** using `deepseek-flash` when OpenRouter is unavailable, errors, or reaches its daily quota/rate limit.
3. **Gemini is not a coding fallback.** Gemini is reserved for independent verification / red-team review.

The purpose is to consume the available OpenRouter daily free resource first and spend DeepSeek balance only after that resource is unavailable.

## Handoff behavior

If OpenRouter fails before producing repository changes, the disposable coding workspace is reset before DeepSeek starts.

If OpenRouter reaches quota/rate limit after already producing repository changes, those changes are preserved and DeepSeek continues from the same disposable workspace. The handoff is recorded in provider audit as `partial_candidate_handoff`.

If DeepSeek reaches a quota/rate/balance limit after producing repository changes, the candidate may be preserved for deterministic validation using the existing bounded partial-candidate policy.

A provider handoff never bypasses:

- protected-path checks;
- deterministic Linux build/tests/generator checks;
- external ChatGPT/Gemini review policy;
- trusted Windows/TIA boundary;
- TIA Portal V21 compile;
- bounded repair attempts;
- human merge decision.

## Reviewer providers

Coding-provider priority is separate from review policy.

- ChatGPT: Senior Architect and primary connected external reviewer.
- Gemini: independent review / red-team escalation when required by risk policy.
- Repository reviewer runtimes may use their own configured model fallback for deterministic reviewer jobs; this does not change the autonomous coder order above.

## Secrets

Secret values must never be committed.

Expected GitHub Actions secret names:

- `OPENROUTER_API_KEY` — primary coding resource and existing reviewer runtime where configured.
- `DEEPSEEK_API_KEY` — paid coding fallback for `deepseek-flash`.
- `GEMINI_API_KEY` — independent reviewer/runtime fallback where explicitly used by trusted review workflows.

## Audit requirement

Every autonomous coding run must persist provider/model audit in its workflow/PR evidence. At minimum the audit must identify:

- provider order;
- attempted providers;
- selected provider/model;
- failure/fallback reason;
- token/cost data when reported by the provider/runtime.

Current required coding provider order is exactly:

`OpenRouter -> DeepSeek`
