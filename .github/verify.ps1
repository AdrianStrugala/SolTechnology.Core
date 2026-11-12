# One command that answers "can this change ship?": build, core tests, sample tests,
# conventions, docs. Exit code 0 is the only accepted evidence that a code change is done
# (CLAUDE.md section 3). The tests start containers through Testcontainers, so Docker must run.
#
#   ./.github/verify.ps1                 # all gates, conventions vs origin/master
#   ./.github/verify.ps1 -Base <ref>     # conventions vs another base
#   ./.github/verify.ps1 -SkipTests      # build + conventions + docs

param(
    [string] $Base = 'origin/master',
    [switch] $SkipTests
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$results = New-Object System.Collections.Generic.List[object]

# Sample-app test projects in the solution: Unit and Component. EndToEnd needs a deployed
# environment and stays out of the gate.
$sampleTests = '^sample-tale-code-apps[\\/].+[\\/]tests[\\/](Unit|Component)[\\/]'

function Invoke-Gate([string] $name, [scriptblock] $action) {
    Write-Host ""
    Write-Host "== $name"
    $global:LASTEXITCODE = 0
    & $action
    $passed = ($LASTEXITCODE -eq 0)
    $results.Add([pscustomobject]@{ Gate = $name; Passed = $passed })
    return $passed
}

Push-Location $repoRoot
try {
    $built = Invoke-Gate 'build' { dotnet build SolTechnology.Core.slnx -nologo -v q | Out-Host }

    if ($SkipTests) {
        Write-Host ""
        Write-Host "== tests: skipped by -SkipTests"
    }
    elseif ($built) {
        Invoke-Gate 'core tests' { & (Join-Path $PSScriptRoot 'runTests.ps1') | Out-Host } | Out-Null
        Invoke-Gate 'sample tests' { & (Join-Path $PSScriptRoot 'runTests.ps1') -Filter $sampleTests | Out-Host } | Out-Null
    }
    else {
        $results.Add([pscustomobject]@{ Gate = 'core tests'; Passed = $false })
        $results.Add([pscustomobject]@{ Gate = 'sample tests'; Passed = $false })
        Write-Host ""
        Write-Host "== tests: not run, the build failed"
    }

    Invoke-Gate 'conventions' { & (Join-Path $PSScriptRoot 'checkConventions.ps1') -Base $Base | Out-Host } | Out-Null
    Invoke-Gate 'docs' { & (Join-Path $PSScriptRoot 'checkDocs.ps1') | Out-Host } | Out-Null
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "== summary"
foreach ($result in $results) {
    if ($result.Passed) { $state = 'PASS' } else { $state = 'FAIL' }
    Write-Host ("  {0,-12} {1}" -f $result.Gate, $state)
}

if (@($results | Where-Object { -not $_.Passed }).Count -gt 0) {
    exit 1
}

exit 0
