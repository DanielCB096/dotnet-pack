param(
    [string]$Version = "0.5.0"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) {
    throw "Falta NUGET_API_KEY. Ejecuta: `$env:NUGET_API_KEY = 'TU_API_KEY'"
}

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    dotnet pack PipelineKit.slnx -c Release -o artifacts
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

    dotnet nuget push "artifacts/*.$Version.nupkg" `
        --api-key $env:NUGET_API_KEY `
        --source "https://api.nuget.org/v3/index.json" `
        --skip-duplicate

    if ($LASTEXITCODE -ne 0) { throw "dotnet nuget push failed" }

    Write-Host "Published PipelineKit $Version to NuGet.org."
}
finally {
    Pop-Location
}
