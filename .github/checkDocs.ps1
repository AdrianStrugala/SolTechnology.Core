# Validates the AI-facing and architecture docs: relative links, heading anchors, section
# citations (section N / N.M of CLAUDE.md and docs/ClaudeCodingGuide.md), agent / skill index
# parity, and the shape of feature briefs and active plans. Exit code 1 when any check fails.
# ASCII-only so Windows PowerShell 5.1 parses it.

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
#    frontmatter on line 1, status in the vocabulary. A brief with a working folder: gate fields,
#    step statuses, the Steps table mirrors every step file, bracket steps, links.
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
        if ($lines[$i] -match '^([A-Za-z]+):\s*(.*)$') {
            $fields[$Matches[1]] = $Matches[2].Trim()
        }
    }
    return $null
}

$featureStatuses = @('planning', 'planned', 'in-progress', 'blocked', 'completed', 'abandoned')
$stepStatuses = @('to-do', 'blocked', 'in-progress', 'done')
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
    if (@('completed', 'abandoned') -contains $front['status'] -and -not $front['completed']) {
        $problems.Add("${relative}: status $($front['status']) requires a completed date")
    }
    if ($front['status'] -eq 'in-progress' -and -not $front['approved'] -and
        [string]::CompareOrdinal([string]$front['created'], '2026-11-12') -ge 0) {
        $problems.Add("${relative}: status in-progress requires approved")
    }

    $stepsFolder = Join-Path (Join-Path $brief.DirectoryName $brief.BaseName) 'steps'
    if (-not (Test-Path -LiteralPath $stepsFolder)) {
        continue
    }
    $planFiles += $brief
    foreach ($gate in @('review', 'premortem')) {
        if (-not $front[$gate]) {
            $problems.Add("${relative}: gate field '$gate' is missing or empty")
        }
    }
    $briefLines = [System.IO.File]::ReadAllLines($brief.FullName)
    $steps = @(Get-ChildItem -LiteralPath $stepsFolder -Filter '*.md' -File | Sort-Object Name)
    foreach ($step in $steps) {
        $planFiles += $step
        $stepRelative = Get-RelativePath $step.FullName
        $stepFront = Get-Frontmatter $step.FullName
        if ($null -eq $stepFront) {
            $problems.Add("${stepRelative}: frontmatter is missing or does not start on line 1")
            continue
        }
        if ($stepStatuses -notcontains $stepFront['status']) {
            $problems.Add("${stepRelative}: status '$($stepFront['status'])' is not in the vocabulary")
            continue
        }
        $row = @($briefLines | Where-Object { $_.StartsWith('|') -and $_.Contains($step.Name) })
        if ($row.Count -eq 0) {
            $problems.Add("${relative}: the Steps table does not list $($step.Name)")
        }
        elseif ($row[0] -notmatch ('[ |]' + [regex]::Escape($stepFront['status']) + '\s*\|\s*$')) {
            $problems.Add("${relative}: the Steps row of $($step.Name) does not mirror status '$($stepFront['status'])'")
        }
    }
    if ($steps.Count -gt 0 -and $steps[$steps.Count - 1].Name -notmatch 'retrospective') {
        $problems.Add("${relative}: the highest-numbered step is not the retrospective")
    }
    $hasPremortemStep = @($steps | Where-Object { $_.Name -like '00-*' }).Count -gt 0
    if ($front['premortem'] -eq 'pending' -and -not $hasPremortemStep) {
        $problems.Add("${relative}: premortem is pending but step 00 does not exist")
    }
    if ([string]$front['premortem'] -like 'waived*' -and $hasPremortemStep) {
        $problems.Add("${relative}: premortem is waived but step 00 exists")
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
