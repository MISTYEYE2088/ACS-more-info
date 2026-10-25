param(
    [string]$Configuration = "Release",
    [string]$Dotnet = "dotnet",
    [switch]$NoBuild,
    [string]$Deploy
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\MoreInfo\MoreInfo.csproj"
$dll = Join-Path $root "src\MoreInfo\bin\$Configuration\MoreInfo.dll"
$dist = Join-Path $root "dist\MoreInfo"
$zip = Join-Path $root "dist\MoreInfo.zip"

if (-not $NoBuild) {
    & $Dotnet build $project -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Build output not found: $dll"
}

Remove-Item -LiteralPath $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $dist | Out-Null

Copy-Item -LiteralPath $dll -Destination $dist
Copy-Item -LiteralPath (Join-Path $root "mod\Info.json") -Destination $dist
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination $dist
Copy-Item -LiteralPath (Join-Path $root "LICENSE") -Destination $dist

Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $dist "*") -DestinationPath $zip

if ($Deploy) {
    $target = Join-Path $Deploy "MoreInfo"
    Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item -Path (Join-Path $dist "*") -Destination $target
    Write-Host "Deployed to $target"
}

Write-Host "Package: $zip"
