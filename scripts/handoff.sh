#!/usr/bin/env bash
# The handoff written BEFORE compaction, so the session that comes out the far
# side is not guessing.
#
# THIS FILE IS NOT THE RECORD AND MUST NEVER BECOME IT. The record is the last
# commit message, the rows under `readings/`, and the tests. What compaction
# actually destroys is the stuff that has not reached any of those yet: the
# uncommitted diff, which commits are unpushed, which reading was mid-flight.
# So that is what this collects, and it deliberately collects nothing else.
#
# IT MUST ALSO BE CHEAP AND MUST NOT TOUCH THE GPU. It runs while the model is
# blocked; a hook that fired `pytest` would put a second job on a card the rules
# say is only ever allowed one. Git and `ls`, nothing more.
#
# Run it by hand any time:  bash scripts/handoff.sh
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT" || exit 0

payload=""
[ -t 0 ] || payload="$(cat)"
trigger="$(printf '%s' "$payload" |
  sed -n 's/.*"trigger"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
[ -n "$trigger" ] || trigger="by hand"

mkdir -p state
out="state/handoff.md"

{
  echo "# Session handoff, written $(date -u +%Y-%m-%dT%H:%M:%SZ) (compaction: $trigger)"
  echo
  echo "## Do these before anything else"
  echo
  echo "- Read \`CLAUDE.md\` and \`docs/unfused.md\` whole. THE ORDER lives in the doc,"
  echo "  not here, and this file deliberately carries no second copy of it."
  echo "- Re-arm the five-minute \`Monitor\` heartbeat. Compaction does not carry it,"
  echo "  and a session without it stops being a session that continues itself."
  echo "- Findings go in \`readings/\` and in the commit message. Never in the doc."
  echo
  echo "## Where the branch is"
  echo
  echo "    branch $(git branch --show-current 2>/dev/null)"
  echo "    HEAD   $(git log -1 --format='%h  %ad  %s' --date=short 2>/dev/null)"
  echo
  unpushed="$(git log --oneline '@{u}..HEAD' 2>/dev/null)"
  if [ -n "$unpushed" ]; then
    echo "Unpushed commits:"
    echo
    printf '%s\n' "$unpushed" | sed 's/^/    /'
  else
    echo "Nothing unpushed (or no upstream set)."
  fi
  echo
  echo "## The last commit message, which carries the last handoff"
  echo
  git log -1 --format=%B 2>/dev/null | sed 's/^/    /'
  echo
  echo "## Uncommitted work -- the part compaction would otherwise lose"
  echo
  dirty="$(git status --short 2>/dev/null)"
  if [ -n "$dirty" ]; then
    printf '%s\n' "$dirty" | sed 's/^/    /'
    echo
    git diff --stat 2>/dev/null | tail -25 | sed 's/^/    /'
  else
    echo "    working tree clean"
  fi
  echo
  echo "## The last readings taken"
  echo
  ls -t readings/*.json 2>/dev/null | head -8 | sed 's/^/    /' || true
  echo
  echo "## The red set, which is intent surviving a session"
  echo
  grep -rhn '^def test_' tests/outstanding 2>/dev/null | sed 's/^/    /' || true
  echo
  echo "-- end of handoff. Anything not above was never written down. --"
} > "$out" 2>/dev/null

python - "$out" <<'PY' 2>/dev/null || echo '{"systemMessage":"handoff written to state/handoff.md"}'
import json, sys
body = open(sys.argv[1], encoding="utf-8", errors="replace").read()
if len(body) > 8000:
    body = body[:8000] + "\n... (truncated; read state/handoff.md in full)\n"
print(json.dumps({
    "systemMessage": "Session handoff written to state/handoff.md",
    "suppressOutput": True,
    "hookSpecificOutput": {
        "hookEventName": "PreCompact",
        "additionalContext":
            "A pre-compaction handoff was written to state/handoff.md. Its "
            "contents follow. Re-read CLAUDE.md and docs/unfused.md, re-arm "
            "the five-minute Monitor heartbeat, and carry on from here.\n\n"
            + body,
    },
}))
PY
