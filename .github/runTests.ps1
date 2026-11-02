$testProjects = dotnet sln SolTechnology.Core.slnx list | Where-Object { $_ -like 'tests/*' }
$failed = @()

foreach ($project in $testProjects) {
    dotnet test --no-build $project
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
