#requires -Version 3

# Conditionally gets nuget, then conditionally gets psake, then runs build.psake.ps1

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

if ($(Get-PackageProvider | where { $_.Name -match 'NuGet'}).Count -eq 0){
    Write-Information "Installing NuGet provider..."
    Install-PackageProvider -Name NuGet -Scope AllUsers -Force
}

if ($(Get-PackageSource | where { $_.Name -match 'NuGet_v2'}).Count -eq 0){
    Write-Information "Installing NuGet_v2 source..."
    Register-PackageSource -Name NuGet_v2 -Location http://nuget.org/api/v2/ -ProviderName NuGet
}


if (Get-Module -ListAvailable -Name 'PSake') {
    Write-Information "Module PSake exists. Continue"
} else {
    Write-Information "Installing PSake..."
    Install-Module -Name PSake -RequiredVersion 4.7.0 -Force
}

Import-Module psake -MaximumVersion 4.7.0
$ErrorActionPreference = "Stop"

Install-Module VSSetup

Invoke-Psake "$($PSScriptRoot)\initworkspace.psake.ps1" -task Invoke-FullChain -ErrorAction $ErrorActionPreference
