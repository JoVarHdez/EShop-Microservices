# .github/hooks/scripts/invoke-reflector.ps1
#
# Stop hook — enforce that the reflector skill runs before the session ends.
# Uses a sentinel file to prevent an infinite loop:
#   1st Stop  → create sentinel + return continue:true + inject system message
#   2nd Stop  → sentinel exists → delete it + exit 0 (session ends normally)

$sentinelPath = Join-Path $PSScriptRoot ".." ".." ".reflector-sentinel"
$sentinelPath = [System.IO.Path]::GetFullPath($sentinelPath)

if (Test-Path $sentinelPath) {
    # Reflector already ran this session — allow the session to end cleanly.
    Remove-Item $sentinelPath -Force
    exit 0
}

# First stop: create sentinel and ask the agent to invoke the reflector skill.
New-Item -Path $sentinelPath -ItemType File -Force | Out-Null

$message = @(
    "MANDATORY SESSION CLOSE POLICY: Before this session ends you MUST invoke the ``reflector`` skill.",
    "The reflector skill must: (1) review code diffs in /src and update README, API docs, and ADRs;",
    "(2) append new rules to .github/skills/ instruction files for any repeated failures encountered;",
    "(3) evaluate and explain session outcomes to the user.",
    "Do NOT end the session without completing the reflector skill."
) -join " "

@{
    "continue"      = $true
    "systemMessage" = $message
} | ConvertTo-Json -Compress
