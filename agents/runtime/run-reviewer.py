#!/usr/bin/env python3
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

if len(sys.argv) != 4:
    raise SystemExit("Usage: run-reviewer.py <prompt.md> <result.json> <audit.json>")

prompt_path = Path(sys.argv[1])
result_path = Path(sys.argv[2])
audit_path = Path(sys.argv[3])
prompt = prompt_path.read_text()

openrouter_model = os.environ.get(
    "OPENROUTER_REVIEW_MODEL",
    "nvidia/nemotron-3-ultra-550b-a55b:free",
)
gemini_model = os.environ.get("GEMINI_REVIEW_MODEL", "gemini-3.8-flash")
attempts = []


def extract_result(text: str) -> dict:
    text = text.strip()
    text = re.sub(r"^```(?:json)?\s*", "", text, flags=re.I)
    text = re.sub(r"\s*```$", "", text)
    start, end = text.find("{"), text.rfind("}")
    if start < 0 or end < start:
        raise ValueError("Reviewer did not return a JSON object")
    result = json.loads(text[start : end + 1])
    if result.get("status") not in {"PASS", "CHANGES_REQUIRED", "BLOCKED"}:
        raise ValueError("Invalid reviewer status")
    return result


def write_success(provider: str, requested: str, actual: str, response: str):
    result = extract_result(response)
    result_path.write_text(json.dumps(result, indent=2))
    attempts.append(
        {
            "provider": provider,
            "requested_model": requested,
            "actual_model": actual,
            "status": "success",
        }
    )
    audit_path.write_text(
        json.dumps(
            {
                "selected_provider": provider,
                "selected_model": actual or requested,
                "attempts": attempts,
            },
            indent=2,
        )
    )
    print(result_path.read_text())
    print(audit_path.read_text())
    raise SystemExit(0)


openrouter_key = os.environ.get("OPENROUTER_API_KEY", "").strip()
if openrouter_key:
    payload = json.dumps(
        {
            "model": openrouter_model,
            "messages": [{"role": "user", "content": prompt}],
            "temperature": 0.1,
            "max_tokens": 4096,
        }
    ).encode()
    request = urllib.request.Request(
        "https://openrouter.ai/api/v1/chat/completions",
        data=payload,
        method="POST",
        headers={
            "Authorization": f"Bearer {openrouter_key}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://github.com/al-gri/TIA-Automation-Factory",
            "X-Title": "TIA Automation Factory Reviewer",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=180) as response:
            envelope = json.loads(response.read().decode())
        text = envelope["choices"][0]["message"]["content"]
        actual_model = envelope.get("model") or openrouter_model
        write_success("openrouter", openrouter_model, actual_model, text)
    except Exception as exc:
        attempts.append(
            {
                "provider": "openrouter",
                "requested_model": openrouter_model,
                "status": "failed",
                "error": str(exc)[:1000],
            }
        )
        print(f"OpenRouter reviewer failed: {exc}", file=sys.stderr)
else:
    attempts.append(
        {
            "provider": "openrouter",
            "requested_model": openrouter_model,
            "status": "skipped_not_configured",
        }
    )


gemini_key = os.environ.get("GEMINI_API_KEY", "").strip()
if gemini_key:
    env = dict(os.environ)
    env["GEMINI_CLI_TRUST_WORKSPACE"] = "true"
    proc = subprocess.run(
        [
            "gemini",
            "--model",
            gemini_model,
            "--prompt",
            prompt,
            "--approval-mode=plan",
            "--skip-trust",
            "--extensions",
            "none",
            "--output-format",
            "json",
        ],
        text=True,
        capture_output=True,
        env=env,
        timeout=240,
    )
    if proc.stderr:
        print(proc.stderr, file=sys.stderr)
    if proc.returncode == 0:
        envelope = json.loads(proc.stdout)
        text = (envelope.get("response") or "").strip()
        models = (envelope.get("stats") or {}).get("models", {})
        actual_model = ",".join(models.keys()) if models else gemini_model
        write_success("gemini", gemini_model, actual_model, text)
    attempts.append(
        {
            "provider": "gemini",
            "requested_model": gemini_model,
            "status": "failed",
            "returncode": proc.returncode,
            "error": (proc.stderr or proc.stdout)[-1000:],
        }
    )
else:
    attempts.append(
        {
            "provider": "gemini",
            "requested_model": gemini_model,
            "status": "skipped_not_configured",
        }
    )


audit_path.write_text(
    json.dumps(
        {
            "selected_provider": None,
            "selected_model": None,
            "attempts": attempts,
        },
        indent=2,
    )
)
print(audit_path.read_text(), file=sys.stderr)
raise SystemExit("No reviewer provider completed successfully")
