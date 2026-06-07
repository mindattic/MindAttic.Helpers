<#
  SessionStart hook: inject docs/BIBLE.digest.md as authoritative context.
  Emits Claude Code hook JSON on stdout. If the digest is missing/empty, emits {}.
  Windows PowerShell 5.1 / Win-1252 safe: all non-ASCII escaped to \uXXXX.
#>
$ErrorActionPreference = 'Stop'

$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$digestPath = Join-Path $repoRoot 'docs\BIBLE.digest.md'

if (-not (Test-Path $digestPath)) { Write-Output '{}'; return }

$digest = Get-Content -Raw -Path $digestPath
if ([string]::IsNullOrWhiteSpace($digest)) { Write-Output '{}'; return }

$preamble = @"
[MindAttic.Helpers / Codex] The following is the AUTHORITATIVE project digest, generated
from docs/BIBLE.md. Treat it as the source of truth for what this project IS, is NOT, and
the Laws that govern it. Full detail and stable IDs live in docs/BIBLE.md, docs/USER_STORIES.md,
and docs/AMENDMENTS.md (an amendment wins over the bible). Do not contradict it.

"@

$context = $preamble + $digest

# JSON-encode with all non-ASCII escaped to \uXXXX (avoid code-page corruption).
$sb = New-Object System.Text.StringBuilder
foreach ($ch in $context.ToCharArray()) {
    $code = [int]$ch
    switch ($ch) {
        '"'  { [void]$sb.Append('\"') }
        '\'  { [void]$sb.Append('\\') }
        "`b" { [void]$sb.Append('\b') }
        "`f" { [void]$sb.Append('\f') }
        "`n" { [void]$sb.Append('\n') }
        "`r" { [void]$sb.Append('\r') }
        "`t" { [void]$sb.Append('\t') }
        default {
            if ($code -lt 32 -or $code -gt 126) {
                [void]$sb.Append(('\u{0:x4}' -f $code))
            } else {
                [void]$sb.Append($ch)
            }
        }
    }
}

$json = '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"' + $sb.ToString() + '"}}'
Write-Output $json
