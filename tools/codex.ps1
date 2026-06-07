<#
.SYNOPSIS
  Codex documentation CLI for MindAttic.Helpers.
.DESCRIPTION
  Subcommands:
    doctor  - validate the Codex canon (front-matter, stable IDs, cross-refs,
              data schemas, story test citations, cited paths, digest freshness).
              Exits non-zero on any hard error.
    digest  - regenerate docs/BIBLE.digest.md from BIBLE.md (§1, §3, §5, §9) plus a
              status index and the latest amendment head.
  Windows PowerShell 5.1 compatible. No build step, no external modules.
.EXAMPLE
  powershell -File tools/codex.ps1 doctor
  powershell -File tools/codex.ps1 digest
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('doctor', 'digest')]
    [string]$Command = 'doctor'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DocsDir  = Join-Path $RepoRoot 'docs'
$BiblePath  = Join-Path $DocsDir 'BIBLE.md'
$DigestPath = Join-Path $DocsDir 'BIBLE.digest.md'
$StoriesPath = Join-Path $DocsDir 'USER_STORIES.md'
$AmendPath   = Join-Path $DocsDir 'AMENDMENTS.md'

# ----------------------------------------------------------------------------- helpers
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Utf8 {
    # Read a file as UTF-8 regardless of the PS 5.1 default code page.
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Read-Utf8Lines {
    param([string]$Path)
    return [System.IO.File]::ReadAllLines($Path, [System.Text.Encoding]::UTF8)
}

function Get-FrontMatter {
    param([string]$Text)
    if ($Text -notmatch "(?s)^\s*---\r?\n(.*?)\r?\n---\r?\n") { return $null }
    $block = $Matches[1]
    $map = @{}
    foreach ($line in ($block -split "\r?\n")) {
        if ($line -match '^\s*([A-Za-z0-9_]+)\s*:\s*(.*?)\s*$') {
            $map[$Matches[1]] = $Matches[2]
        }
    }
    return $map
}

# Status emoji as surrogate-safe strings (some are > U+FFFF, so build from code points).
$script:Emoji = @{
    Done    = [char]::ConvertFromUtf32(0x2705)    # white heavy check mark
    Partial = [char]::ConvertFromUtf32(0x1F7E1)   # yellow circle
    Planned = [char]::ConvertFromUtf32(0x2B1C)    # white large square
    Cut     = [char]::ConvertFromUtf32(0x1F5D1)   # wastebasket
}

function Get-StoryStatusCounts {
    $done = 0; $partial = 0; $planned = 0; $cut = 0
    if (Test-Path $StoriesPath) {
        foreach ($line in ((Read-Utf8Lines $StoriesPath))) {
            if ($line -notmatch 'HLP-US-') { continue }
            if     ($line.Contains($script:Emoji.Done))    { $done++ }
            elseif ($line.Contains($script:Emoji.Partial)) { $partial++ }
            elseif ($line.Contains($script:Emoji.Planned)) { $planned++ }
            elseif ($line.Contains($script:Emoji.Cut))     { $cut++ }
        }
    }
    return [pscustomobject]@{ Done = $done; Partial = $partial; Planned = $planned; Cut = $cut }
}

$script:Section = [char]::ConvertFromUtf32(0xA7)   # § (avoid a literal in source)

function Get-BibleSection {
    # Returns the body text of the "## N. Title {#HLP-§N}" bible section, by number.
    param([string]$Text, [int]$Number)
    $anchor = "HLP-$($script:Section)$Number"
    $lines = $Text -split "\r?\n"
    $out = New-Object System.Collections.Generic.List[string]
    $inSection = $false
    foreach ($line in $lines) {
        if ($line -match '^##\s') {
            if ($inSection) { break }
            if ($line -match [regex]::Escape("{#$anchor}")) { $inSection = $true; continue }
        }
        elseif ($inSection) {
            $out.Add($line)
        }
    }
    return ($out -join "`n").Trim()
}

# ----------------------------------------------------------------------------- doctor
function Invoke-Doctor {
    $errors = New-Object System.Collections.Generic.List[string]
    $warns  = New-Object System.Collections.Generic.List[string]
    $checks = New-Object System.Collections.Generic.List[string]

    $expectFm = @{
        $BiblePath   = 'bible'
        $StoriesPath = 'stories'
        $AmendPath   = 'amendments'
    }

    # rfc + data files
    $rfcDir  = Join-Path $DocsDir 'rfc'
    $dataDir = Join-Path $DocsDir 'data'
    if (Test-Path $rfcDir) {
        Get-ChildItem -Path $rfcDir -Filter '*.md' -File | ForEach-Object { $expectFm[$_.FullName] = 'rfc' }
    }
    if (Test-Path $dataDir) {
        Get-ChildItem -Path $dataDir -Filter '*.json' -File -Recurse |
            Where-Object { $_.FullName -notmatch '\\_schema\\' } |
            ForEach-Object { $expectFm[$_.FullName] = 'data' }
    }

    # 1. front-matter validity
    foreach ($path in $expectFm.Keys) {
        if (-not (Test-Path $path)) { $errors.Add("missing file: $path"); continue }
        if ($path -like '*.json') {
            try { (Read-Utf8 $path) | ConvertFrom-Json | Out-Null }
            catch { $errors.Add("invalid JSON: $path ($($_.Exception.Message))") }
            continue
        }
        $text = (Read-Utf8 $path)
        $fm = Get-FrontMatter $text
        if ($null -eq $fm) { $errors.Add("no front-matter: $path"); continue }
        foreach ($key in @('codex', 'project', 'code', 'layer', 'status', 'updated')) {
            if (-not $fm.ContainsKey($key)) { $errors.Add("front-matter missing '$key': $path") }
        }
        if ($fm.ContainsKey('layer') -and $fm['layer'] -ne $expectFm[$path]) {
            $errors.Add("front-matter layer '$($fm['layer'])' != expected '$($expectFm[$path])': $path")
        }
        if ($fm.ContainsKey('updated') -and $fm['updated'] -notmatch '^\d{4}-\d{2}-\d{2}$') {
            $errors.Add("front-matter 'updated' not YYYY-MM-DD: $path")
        }
    }
    $checks.Add("front-matter checked on $($expectFm.Count) file(s)")

    # 2. stable IDs unique + cross-refs resolve (across all canon markdown)
    $mdFiles = New-Object System.Collections.Generic.List[string]
    foreach ($p in @($BiblePath, $StoriesPath, $AmendPath)) { if (Test-Path $p) { $mdFiles.Add($p) } }
    if (Test-Path $rfcDir) { Get-ChildItem $rfcDir -Filter '*.md' -File | ForEach-Object { $mdFiles.Add($_.FullName) } }

    $anchors = @{}
    $refs = New-Object System.Collections.Generic.List[object]
    foreach ($p in $mdFiles) {
        $text = (Read-Utf8 $p)
        foreach ($m in [regex]::Matches($text, '\{#([A-Za-z0-9§\-]+)\}')) {
            $id = $m.Groups[1].Value
            if ($anchors.ContainsKey($id)) { $errors.Add("duplicate anchor {#$id} (in $p and $($anchors[$id]))") }
            else { $anchors[$id] = $p }
        }
    }
    # collect link refs of form (...#anchor)
    foreach ($p in $mdFiles) {
        $text = (Read-Utf8 $p)
        foreach ($m in [regex]::Matches($text, '\]\(([^)]*?)#([A-Za-z0-9§\-]+)\)')) {
            $refs.Add([pscustomobject]@{ File = $p; Target = $m.Groups[1].Value; Anchor = $m.Groups[2].Value })
        }
    }
    foreach ($r in $refs) {
        # skip house-rules anchors (external file, owned elsewhere)
        if ($r.Anchor -like 'HOUSE-*') { continue }
        if (-not $anchors.ContainsKey($r.Anchor)) {
            $errors.Add("unresolved cross-ref #$($r.Anchor) in $($r.File)")
        }
    }
    $checks.Add("$($anchors.Count) unique anchor(s); $($refs.Count) cross-ref(s) checked")

    # 3. data JSON validates against schema + ids unique  (none expected for a library)
    $dataEntities = 0
    if (Test-Path $dataDir) {
        $ids = @{}
        Get-ChildItem -Path $dataDir -Filter '*.json' -File |
            Where-Object { $_.Directory.Name -ne '_schema' } | ForEach-Object {
                try {
                    $json = (Read-Utf8 $_.FullName) | ConvertFrom-Json
                    foreach ($e in @($json)) {
                        if ($e.PSObject.Properties.Name -contains 'id') {
                            $dataEntities++
                            if ($ids.ContainsKey($e.id)) { $errors.Add("duplicate data id '$($e.id)'") }
                            else { $ids[$e.id] = $true }
                        }
                    }
                } catch { $errors.Add("invalid data JSON: $($_.FullName)") }
            }
    }
    $checks.Add("$dataEntities data entit(y/ies) checked")

    # 4. every checkmark story names a test token; best-effort, test exists in tree
    $testTokens = @{}
    Get-ChildItem -Path $RepoRoot -Filter '*Tests.cs' -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | ForEach-Object {
            $t = (Read-Utf8 $_.FullName)
            foreach ($m in [regex]::Matches($t, '(?m)public\s+(?:async\s+)?(?:void|Task)\s+([A-Za-z0-9_]+)\s*\(')) {
                $testTokens[$m.Groups[1].Value] = $true
            }
        }
    $counts = Get-StoryStatusCounts
    $doneCount = $counts.Done; $partial = $counts.Partial; $planned = $counts.Planned; $cut = $counts.Cut
    if (Test-Path $StoriesPath) {
        $raw = (Read-Utf8 $StoriesPath)
        # Per-story bullets only: a list item beginning "- **HLP-US-XX ..." extending to
        # the next such bullet, a heading, or EOF. Numbered backlog items (which may
        # mention a story id) are intentionally excluded.
        $bullets = [regex]::Matches($raw, "(?sm)^-\s+\*\*(HLP-US-[A-Za-z0-9]+)\b.*?(?=(\r?\n-\s+\*\*HLP-US-)|(\r?\n#)|\z)")
        foreach ($b in $bullets) {
            $sid = $b.Groups[1].Value
            $body = $b.Groups[0].Value
            if ($body.Contains($script:Emoji.Done)) {
                $cites = [regex]::Matches($body, '(?s)verified by\s+((?:[^)]*?\`[^`]+\`)+)[^)]*\)')
                if ($cites.Count -eq 0) {
                    $errors.Add("done story $sid cites no test")
                } else {
                    foreach ($c in $cites) {
                        foreach ($tm in [regex]::Matches($c.Groups[1].Value, '`([^`]+)`')) {
                            $tok = $tm.Groups[1].Value.Trim()
                            if ($tok -and -not $testTokens.ContainsKey($tok)) {
                                $warns.Add("story $sid cites test '$tok' not found in test tree")
                            }
                        }
                    }
                }
            }
        }
    }
    $checks.Add("stories: $doneCount done / $partial partial / $planned planned / $cut cut; $($testTokens.Count) test method(s) indexed")

    # 5. every code path/file cited in the bible exists on disk
    if (Test-Path $BiblePath) {
        $btext = (Read-Utf8 $BiblePath)
        $bibleDir = Split-Path -Parent $BiblePath
        $parentDir = Split-Path -Parent $RepoRoot
        foreach ($m in [regex]::Matches($btext, '`([A-Za-z0-9_./\\\-]+\.(?:cs|csproj|slnx|md|json|ps1))`')) {
            $cited = $m.Groups[1].Value
            $leaf = Split-Path $cited -Leaf
            # Accept the citation if it resolves relative to the repo root, the bible
            # dir, or the parent dir (org-wide files like MindAttic.HouseRules.md), or
            # if a file with that leaf name exists anywhere in the repo tree.
            $ok = (Test-Path (Join-Path $RepoRoot $cited)) -or
                  (Test-Path (Join-Path $bibleDir $cited)) -or
                  (Test-Path (Join-Path $parentDir $leaf)) -or
                  [bool](Get-ChildItem -Path $RepoRoot -Filter $leaf -File -Recurse -ErrorAction SilentlyContinue |
                         Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' })
            if (-not $ok) { $errors.Add("bible cites missing file: $cited") }
        }
    }
    $checks.Add("bible file citations checked")

    # 6. digest freshness (generatedFrom source mtime <= artifact mtime)
    if (Test-Path $DigestPath) {
        if ((Get-Item $BiblePath).LastWriteTimeUtc -gt (Get-Item $DigestPath).LastWriteTimeUtc) {
            $warns.Add("BIBLE.digest.md is stale (BIBLE.md changed after it) - run: codex.ps1 digest")
        }
    } else {
        $warns.Add("BIBLE.digest.md missing - run: codex.ps1 digest")
    }
    $checks.Add("digest freshness checked")

    # ---- report
    Write-Host "Codex doctor - MindAttic.Helpers" -ForegroundColor Cyan
    foreach ($c in $checks) { Write-Host ("  [check] {0}" -f $c) }
    foreach ($w in $warns)  { Write-Host ("  [warn]  {0}" -f $w) -ForegroundColor Yellow }
    foreach ($e in $errors) { Write-Host ("  [FAIL]  {0}" -f $e) -ForegroundColor Red }

    if ($errors.Count -gt 0) {
        Write-Host ("doctor: FAIL ({0} error(s), {1} warning(s))" -f $errors.Count, $warns.Count) -ForegroundColor Red
        exit 1
    }
    Write-Host ("doctor: OK ({0} warning(s))" -f $warns.Count) -ForegroundColor Green
    exit 0
}

# ----------------------------------------------------------------------------- digest
function Invoke-Digest {
    if (-not (Test-Path $BiblePath)) { throw "BIBLE.md not found at $BiblePath" }
    $text = (Read-Utf8 $BiblePath)

    $s1 = Get-BibleSection $text 1
    $s3 = Get-BibleSection $text 3
    $s5 = Get-BibleSection $text 5
    $s9 = Get-BibleSection $text 9

    # status index from stories
    $counts = Get-StoryStatusCounts
    $done = $counts.Done; $partial = $counts.Partial; $planned = $counts.Planned; $cut = $counts.Cut

    # latest amendment head
    $amendHead = ''
    if (Test-Path $AmendPath) {
        $alines = (Read-Utf8Lines $AmendPath)
        $heads = @($alines | Where-Object { $_ -match '^##\s+HLP-A' })
        if ($heads.Count -gt 0) { $amendHead = ($heads[-1] -replace '^##\s+', '').Trim() }
    }

    $nl = "`n"
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("AUTHORITATIVE - full detail in docs/BIBLE.md" + $nl + $nl)
    [void]$sb.Append("# MindAttic.Helpers - Bible Digest (generated)" + $nl)
    [void]$sb.Append("<!-- generatedFrom: HLP-BIBLE. Do not hand-edit; run tools/codex.ps1 digest. -->" + $nl + $nl)
    [void]$sb.Append("## 1. The one sentence" + $nl + $s1 + $nl + $nl)
    [void]$sb.Append("## 3. What it is NOT" + $nl + $s3 + $nl + $nl)
    [void]$sb.Append("## 5. The Laws" + $nl + $s5 + $nl + $nl)
    [void]$sb.Append("## 9. Glossary" + $nl + $s9 + $nl + $nl)
    [void]$sb.Append("## Status index (stories)" + $nl)
    [void]$sb.Append(("- done: {0}  partial: {1}  planned: {2}  cut: {3}" -f $done, $partial, $planned, $cut) + $nl + $nl)
    [void]$sb.Append("## Latest amendment" + $nl)
    [void]$sb.Append(($(if ($amendHead) { "- $amendHead" } else { "- (none)" })) + $nl)

    [System.IO.File]::WriteAllText($DigestPath, $sb.ToString(), $script:Utf8NoBom)
    Write-Host "digest: wrote docs/BIBLE.digest.md ($done done / $partial partial / $planned planned / $cut cut)" -ForegroundColor Green
}

switch ($Command) {
    'doctor' { Invoke-Doctor }
    'digest' { Invoke-Digest }
}
