#!/usr/bin/env bash
# .github/hooks/scripts/invoke-reflector.sh
#
# Stop hook — enforce that the reflector skill runs before the session ends.
# Uses a sentinel file to prevent an infinite loop:
#   1st Stop  → create sentinel + return continue:true + inject system message
#   2nd Stop  → sentinel exists → delete it + exit 0 (session ends normally)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SENTINEL="$SCRIPT_DIR/../../.reflector-sentinel"

if [ -f "$SENTINEL" ]; then
    # Reflector already ran this session — allow the session to end cleanly.
    rm -f "$SENTINEL"
    exit 0
fi

# First stop: create sentinel and ask the agent to invoke the reflector skill.
touch "$SENTINEL"

printf '%s\n' '{
  "continue": true,
  "systemMessage": "MANDATORY SESSION CLOSE POLICY: Before this session ends you MUST invoke the `reflector` skill. The reflector skill must: (1) review code diffs in /src and update README, API docs, and ADRs; (2) append new rules to .github/skills/ instruction files for any repeated failures encountered; (3) evaluate and explain session outcomes to the user. Do NOT end the session without completing the reflector skill."
}'
