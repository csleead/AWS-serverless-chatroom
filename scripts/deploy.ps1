$ErrorActionPreference = "Stop"

Push-Location
try {
    & (Join-Path $PSScriptRoot "package-backend.ps1")

    $infraDirectory = Join-Path $PSScriptRoot "../infrastructure"
    Set-Location $infraDirectory
    cdk deploy
}
finally {
    Pop-Location
}
