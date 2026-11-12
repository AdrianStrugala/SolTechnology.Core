# Checks the C# lines a change ADDS against the docs/ClaudeCodingGuide.md rules that no compiler
# analyzer covers. Existing code is not scanned: section 15 debt is reported, never auto-fixed.
# Exit code 1 when any added line breaks a rule.
#
#   ./.github/checkConventions.ps1                      # working tree vs origin/master
#   ./.github/checkConventions.ps1 -Base origin/main    # another base

param([string] $Base = 'origin/master')

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repoRoot
try {
    $mergeBase = (git merge-base $Base HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $mergeBase) {
        Write-Host "Cannot find a merge base with '$Base'. Fetch it or pass -Base."
        exit 1
    }
    $diff = @(git diff --unified=0 --no-color $mergeBase -- '*.cs')
    if ($LASTEXITCODE -ne 0) {
        Write-Host "git diff against $mergeBase failed (exit code $LASTEXITCODE)."
        exit 1
    }
    # Untracked files are invisible to git diff; every line of a new file counts as added.
    foreach ($untracked in @(git ls-files --others --exclude-standard -- '*.cs')) {
        $diff += "+++ b/$untracked"
        $diff += '@@ -0,0 +1 @@'
        $diff += @(Get-Content -LiteralPath $untracked | ForEach-Object { "+$_" })
    }
}
finally {
    Pop-Location
}

$frameworkAsync = '^(Dispose|Start|Stop|Execute|Invoke|CheckHealth|Initialize|On\w+)Async$'
$logCall = '\.Log(Trace|Debug|Information|Warning|Error|Critical)?\(|\[LoggerMessage\('
$logCallEnd = ';\s*(//.*)?$|\)\]\s*$'
$interpolated = '\$@?"|@\$"'
$stringLiteral = '"(?:[^"\\]|\\.)*"'
# A placeholder not preceded by '[' ('{{' is an escaped brace, not a placeholder).
$bareHolder = '(?<![\[{])\{(?!\{)@?[A-Za-z_]\w*[^}]*\}'
$asyncDeclaration = '^\s*[\w\s]*\b(?:Task|ValueTask)(?:<.*>)?\s+(\w+Async)\s*(?:<[^>]*>)?\('

$problems = New-Object System.Collections.Generic.List[string]
$file = $null
$lineNumber = 0
# A log call spans every added line from the call to its ';' (or ')]' of [LoggerMessage]), so
# a template wrapped onto the next line is still checked.
$inLogCall = $false

foreach ($line in $diff) {
    if ($line -match '^\+\+\+ (?:b/(.+)|/dev/null)$') {
        $file = $Matches[1]
        $inLogCall = $false
        continue
    }
    if ($line -match '^@@ -\d+(?:,\d+)? \+(\d+)') {
        $lineNumber = [int] $Matches[1]
        $inLogCall = $false
        continue
    }
    if (-not $file -or $line -notmatch '^\+' -or $line -match '^\+\+\+') {
        continue
    }

    $text = $line.Substring(1)
    $where = "${file}:${lineNumber}"
    $lineNumber++

    if ($text -match '^\s*#region\b') {
        $problems.Add("${where}: #region (Guide section 9.9)")
    }
    if ($text -match $logCall) {
        $inLogCall = $true
    }
    if ($inLogCall) {
        $literals = @([regex]::Matches($text, $stringLiteral) | ForEach-Object { $_.Value })
        if ($text -match $interpolated -or @($literals | Where-Object { $_ -match $bareHolder }).Count -gt 0) {
            $problems.Add("${where}: log placeholder without [] brackets, or an interpolated template (Guide section 11)")
        }
        if ($text -match $logCallEnd) {
            $inLogCall = $false
        }
    }
    if ($text -notmatch '\boverride\b' -and $text -match $asyncDeclaration -and $Matches[1] -notmatch $frameworkAsync) {
        $problems.Add("${where}: 'Async' suffix on project-owned method $($Matches[1]) (Guide section 10)")
    }
    if ($text -match '\bNotImplementedException\b|\b(TODO|FIXME)\b') {
        $problems.Add("${where}: stub code (Guide section 16)")
    }
}

if ($problems.Count -gt 0) {
    Write-Host "Convention check failed ($($problems.Count)):"
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "Convention check passed."
exit 0
