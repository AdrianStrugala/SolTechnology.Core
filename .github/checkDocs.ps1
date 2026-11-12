# Validates the AI-facing and architecture docs: relative links, heading anchors, section
# citations (section N / N.M of CLAUDE.md and docs/ClaudeCodingGuide.md), agent / skill index
# parity, and the shape of feature briefs and active plans. Exit code 1 when any check fails.
# ASCII-only so Windows PowerShell 5.1 parses it.

# Any error (an unreadable file, a bad regex) must fail the check, never fall through to "passed".
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$problems = New-Object System.Collections.Generic.List[string]
$sec = [string][char]0xA7

function Get-RepoFiles([string[]] $patterns) {
    $found = @()
    foreach ($pattern in $patterns) {
        $found += @(Get-ChildItem -Path (Join-Path $repoRoot $pattern) -File -ErrorAction SilentlyContinue)
    }
    return $found | Sort-Object FullName -Unique
}

function Get-RelativePath([string] $fullPath) {
    return $fullPath.Substring($repoRoot.Length + 1).Replace('\', '/')
}

# Lines outside fenced code blocks, as objects with Number and Text.
function Get-ProseLines([string] $path) {
    $inFence = $false
    $number = 0
    foreach ($line in [System.IO.File]::ReadAllLines($path)) {
        $number++
        if ($line -match '^\s*(```|~~~)') {
            $inFence = -not $inFence
            continue
        }
        if (-not $inFence) {
            [pscustomobject]@{ Number = $number; Text = $line }
        }
    }
}

function Get-Anchors([string] $path) {
    $anchors = @{}
    foreach ($line in Get-ProseLines $path) {
        if ($line.Text -match '^#{1,6}\s+(.+?)\s*$') {
            $slug = $Matches[1].ToLowerInvariant()
            $slug = [regex]::Replace($slug, '[^\w\- ]', '')
            $slug = $slug.Replace(' ', '-')
            $anchors[$slug] = $true
        }
    }
    return $anchors
}

# Section number -> list of numbered items found in that section.
function Get-SectionIndex([string] $path, [string] $headingPattern) {
    $index = @{}
    $current = $null
    foreach ($line in Get-ProseLines $path) {
        if ($line.Text -match $headingPattern) {
            $current = $Matches[1]
            $index[$current] = New-Object System.Collections.Generic.List[string]
        }
        elseif ($null -ne $current -and $line.Text -match '^\s{0,4}(\d+)\.\s') {
            $index[$current].Add($Matches[1])
        }
    }
    return $index
}

$claudePath = Join-Path $repoRoot 'CLAUDE.md'
$guidePath = Join-Path $repoRoot 'docs/ClaudeCodingGuide.md'
$sections = @{
    'CLAUDE' = Get-SectionIndex $claudePath "^## ${sec}(\d+)\."
    'Guide'  = Get-SectionIndex $guidePath '^## (\d+)\.'
}

$linkFiles = Get-RepoFiles @(
    'CLAUDE.md', 'docs/*.md', 'docs/architecture/*.md', 'docs/diagrams/*.md',
    '.github/*.md', '.github/agents/*.md', '.github/skills/*.md', '.github/skills/*/*.md',
    '.github/skills/*/references/*.md')
$citationFiles = Get-RepoFiles @(
    'CLAUDE.md', 'docs/ClaudeCodingGuide.md', 'docs/AIDocsGuide.md', 'docs/architecture/*.md',
    '.github/*.md', '.github/agents/*.md', '.github/skills/*.md', '.github/skills/*/*.md',
    '.github/skills/*/references/*.md')

# 1. Relative links and anchors.
$anchorCache = @{}
foreach ($file in $linkFiles) {
    $relative = Get-RelativePath $file.FullName
    foreach ($line in Get-ProseLines $file.FullName) {
        $withoutCode = [regex]::Replace($line.Text, '`[^`]*\]\([^`]*`', '')
        foreach ($match in [regex]::Matches($withoutCode, '\]\(<?([^)>\s]+)>?\)')) {
            $target = $match.Groups[1].Value
            if ($target -match '^(https?:|mailto:)' -or $target -match '[<>]|YYYY') {
                continue
            }
            $pathPart, $anchor = $target -split '#', 2
            if ($pathPart -eq '') {
                $targetPath = $file.FullName
            }
            else {
                $targetPath = Join-Path $file.DirectoryName ([uri]::UnescapeDataString($pathPart))
            }
            if (-not (Test-Path -LiteralPath $targetPath)) {
                $problems.Add("${relative}:$($line.Number): link target not found: $target")
                continue
            }
            if ($anchor -and $targetPath -like '*.md' -and (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
                $resolved = (Resolve-Path -LiteralPath $targetPath).Path
                if (-not $anchorCache.ContainsKey($resolved)) {
                    $anchorCache[$resolved] = Get-Anchors $resolved
                }
                if (-not $anchorCache[$resolved].ContainsKey($anchor.ToLowerInvariant())) {
                    $problems.Add("${relative}:$($line.Number): heading anchor not found: $target")
                }
            }
        }
    }
}

# 2. Section citations. The owner is the nearest "CLAUDE.md" / "Guide" mention within 30
#    characters before the citation; a citation chained to the previous one (3/4) inherits its
#    owner; otherwise CLAUDE.md and the Coding Guide cite themselves and every other file cites
#    the Coding Guide.
foreach ($file in $citationFiles) {
    $relative = Get-RelativePath $file.FullName
    if ($relative -eq 'CLAUDE.md') { $defaultOwner = 'CLAUDE' } else { $defaultOwner = 'Guide' }
    foreach ($line in Get-ProseLines $file.FullName) {
        $previousEnd = -1
        $previousOwner = $null
        foreach ($match in [regex]::Matches($line.Text, "${sec}(\d+)(?:\.(\d+))?")) {
            $start = [Math]::Max(0, $match.Index - 30)
            $before = $line.Text.Substring($start, $match.Index - $start)
            $between = ''
            if ($previousEnd -ge 0) {
                $between = $line.Text.Substring($previousEnd, $match.Index - $previousEnd)
            }
            $claudeAt = $before.LastIndexOf('CLAUDE.md')
            $guideAt = $before.LastIndexOf('Guide')
            if ($null -ne $previousOwner -and $between -match '^[\s/,&-]*(and|or)?[\s/,&-]*$') {
                $owner = $previousOwner
            }
            elseif ($claudeAt -ge 0 -and $claudeAt -gt $guideAt) {
                $owner = 'CLAUDE'
            }
            elseif ($guideAt -ge 0) {
                $owner = 'Guide'
            }
            else {
                $owner = $defaultOwner
            }
            $previousEnd = $match.Index + $match.Length
            $previousOwner = $owner

            $section = $match.Groups[1].Value
            $item = $match.Groups[2].Value
            $index = $sections[$owner]
            if (-not $index.ContainsKey($section)) {
                $problems.Add("${relative}:$($line.Number): section $section does not exist in $owner")
            }
            elseif ($item -and -not $index[$section].Contains($item)) {
                $problems.Add("${relative}:$($line.Number): section $section of $owner has no item $item")
            }
        }
    }
}

# 3. Every agent and skill on disk is listed in CLAUDE.md section 4, the only index.
$claudeText = [System.IO.File]::ReadAllText($claudePath)
foreach ($agent in Get-RepoFiles @('.github/agents/*.agent.md')) {
    if (-not $claudeText.Contains(".github/agents/$($agent.Name)")) {
        $problems.Add("CLAUDE.md: agent $($agent.Name) is missing from section 4")
    }
}
foreach ($skill in Get-RepoFiles @('.github/skills/*/SKILL.md')) {
    $name = $skill.Directory.Name
    if (-not $claudeText.Contains(".github/skills/$name/SKILL.md")) {
        $problems.Add("CLAUDE.md: skill $name is missing from section 4")
    }
}

# 4. Feature briefs and active plans (docs/architecture/delivery-workflow.md). Every brief:
#    frontmatter on line 1, status in the vocabulary, no working folder once closed. An open
#    brief: three gate fields with one value each, plan rows match step files by number, step
#    files carry no status, no link into the working folder, retrospective not overdue.
function Get-Frontmatter([string] $path) {
    $lines = [System.IO.File]::ReadAllLines($path)
    if ($lines.Count -eq 0 -or $lines[0].Trim() -ne '---') {
        return $null
    }
    $fields = @{}
    for ($i = 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '---') {
            return $fields
        }
        if ($lines[$i] -match '^([A-Za-z-]+):\s*(.*)$') {
            $fields[$Matches[1]] = $Matches[2].Trim()
        }
    }
    return $null
}

$featureStatuses = @('planning', 'planned', 'in-progress', 'blocked', 'completed', 'abandoned')
$closedStatuses = @('completed', 'abandoned')
$stepStatus = '(to-do|blocked|in-progress|done)'
$gateValues = @{
    'plan-review' = '^(pending|done \(.+\)|waived \(.+\)|skipped \(.+\))$'
    'premortem'   = '^(pending|go \(.+\)|go-with-mitigations \(.+\)|no-go \(.+\)|waived \(.+\)|skipped \(.+\))$'
    'code-review' = '^(pending|done \(.+\)|skipped \(.+\))$'
}
$planFiles = @()
foreach ($brief in Get-RepoFiles @('docs/features/*.md')) {
    $relative = Get-RelativePath $brief.FullName
    $front = Get-Frontmatter $brief.FullName
    if ($null -eq $front) {
        $problems.Add("${relative}: frontmatter is missing or does not start on line 1")
        continue
    }
    if ($featureStatuses -notcontains $front['status']) {
        $problems.Add("${relative}: status '$($front['status'])' is not in the vocabulary")
    }
    $closed = $closedStatuses -contains $front['status']
    if ($closed -and -not $front['completed']) {
        $problems.Add("${relative}: status $($front['status']) requires a completed date")
    }
    $isNew = [string]::CompareOrdinal([string]$front['created'], '2026-11-12') -ge 0
    if ($front['status'] -eq 'in-progress' -and -not $front['approved'] -and $isNew) {
        $problems.Add("${relative}: status in-progress requires approved")
    }

    $workingFolder = Join-Path $brief.DirectoryName $brief.BaseName
    $stepsFolder = Join-Path $workingFolder 'steps'
    $hasFolder = Test-Path -LiteralPath $workingFolder
    if ($closed) {
        if ($hasFolder) {
            $problems.Add("${relative}: status $($front['status']) but the working folder still exists")
        }
        continue
    }
    if (-not $hasFolder -and -not $isNew) {
        continue
    }

    # An open brief: three gate fields, each holding one value.
    $planFiles += $brief
    foreach ($gate in $gateValues.Keys) {
        $value = [string]$front[$gate]
        if (-not $value) {
            $problems.Add("${relative}: gate field '$gate' is missing or empty")
        }
        elseif ($value -notmatch $gateValues[$gate] -or $value.Length -gt 200) {
            $problems.Add("${relative}: gate field '$gate' must hold one allowed value, at most 200 characters")
        }
    }

    $briefLines = [System.IO.File]::ReadAllLines($brief.FullName)
    $rows = @{}
    foreach ($line in $briefLines) {
        if ($line -match ('^\|\s*(\d{2})\s*\|.*\|\s*' + $stepStatus + '\s*\|\s*$')) {
            $rows[$Matches[1]] = $Matches[2]
        }
        if ($line.Contains('](' + $brief.BaseName + '/')) {
            $problems.Add("${relative}: the brief links into its working folder")
        }
    }

    $stepNumbers = @()
    if (Test-Path -LiteralPath $stepsFolder) {
        foreach ($step in @(Get-ChildItem -LiteralPath $stepsFolder -Filter '*.md' -File | Sort-Object Name)) {
            $planFiles += $step
            $stepRelative = Get-RelativePath $step.FullName
            if ($step.Name -notmatch '^(\d{2})-' -or $Matches[1] -eq '00' -or $step.Name -match 'retrospective') {
                $problems.Add("${stepRelative}: not an implementation step; premortem and retrospective are not step files")
                continue
            }
            $number = $Matches[1]
            if ($stepNumbers -contains $number) {
                $problems.Add("${stepRelative}: a second step file with number $number")
            }
            $stepNumbers += $number
            $stepFront = Get-Frontmatter $step.FullName
            if ($null -eq $stepFront) {
                $problems.Add("${stepRelative}: frontmatter is missing or does not start on line 1")
            }
            elseif ($stepFront.ContainsKey('status')) {
                $problems.Add("${stepRelative}: a step file carries no status; the brief's plan table does")
            }
            if (-not $rows.ContainsKey($number)) {
                $problems.Add("${relative}: no plan row with a valid status for step $number")
            }
        }
        foreach ($number in $rows.Keys) {
            if ($stepNumbers -notcontains $number) {
                $problems.Add("${relative}: plan row $number has no step file")
            }
        }
    }

    $open = @($rows.Values | Where-Object { $_ -ne 'done' }).Count
    if ($rows.Count -gt 0 -and $open -eq 0 -and [string]$front['code-review'] -match '^(done|skipped) ') {
        $problems.Add("${relative}: retrospective overdue - every step is done and the code review is resolved")
    }
}

foreach ($file in $planFiles) {
    $relative = Get-RelativePath $file.FullName
    foreach ($line in Get-ProseLines $file.FullName) {
        $withoutCode = [regex]::Replace($line.Text, '`[^`]*\]\([^`]*`', '')
        foreach ($match in [regex]::Matches($withoutCode, '\]\(<?([^)>\s#]+)[^)>\s]*>?\)')) {
            $target = $match.Groups[1].Value
            if ($target -match '^(https?:|mailto:)' -or $target -match '[<>]|YYYY') {
                continue
            }
            $targetPath = Join-Path $file.DirectoryName ([uri]::UnescapeDataString($target))
            if (-not (Test-Path -LiteralPath $targetPath)) {
                $problems.Add("${relative}:$($line.Number): link target not found: $target")
            }
        }
    }
}

if ($problems.Count -gt 0) {
    Write-Host "Documentation check failed ($($problems.Count)):"
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "Documentation check passed ($($linkFiles.Count) files)."
exit 0
