$ErrorActionPreference = "Stop"

$projectDirectory = Join-Path $PSScriptRoot "../backend/AwsServerlessChatroom"

$artifactsDirectory = Join-Path $PSScriptRoot "../artifacts"
New-Item -ItemType Directory -Force -Path $artifactsDirectory | Out-Null

dotnet lambda package --project-location "$projectDirectory" `
    --configuration Release `
    --framework net6.0 `
    --output-package "$(Join-Path $artifactsDirectory "AwsServerlessChatroomBackend.zip")"
