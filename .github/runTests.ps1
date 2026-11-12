# Runs test projects listed in SolTechnology.Core.slnx with --no-build; build first.
#
#   ./.github/runTests.ps1                    # core: every project under tests/
#   ./.github/runTests.ps1 -Filter '<regex>'  # every slnx project whose path matches

param(
    [string] $Filter = '^tests[\\/]'
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$solution = Join-Path $repoRoot 'SolTechnology.Core.slnx'

$solutionProjects = dotnet sln $solution list
if ($LASTEXITCODE -ne 0) {
    Write-Host "Listing projects of $solution failed (exit code $LASTEXITCODE)."
    exit 1
}

$testProjects = @($solutionProjects | Where-Object { $_ -match $Filter })
if ($testProjects.Count -eq 0) {
    Write-Host "No test projects matching '$Filter' found in $solution."
    exit 1
}

$failed = @()

foreach ($project in $testProjects) {
    dotnet test --no-build (Join-Path $repoRoot $project)
    if ($LASTEXITCODE -ne 0) {
        $failed += $project
    }
}

if ($failed.Count -gt 0) {
    Write-Host "Failed test projects ($($failed.Count)):"
    $failed | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "All $($testProjects.Count) test projects passed."
exit 0
