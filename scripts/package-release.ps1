[CmdletBinding()]
param(
    [ValidateSet("Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "ClearClock.csproj"
$releaseDirectory = Join-Path $root "release"
$readme = Join-Path $root "README.txt"
$stagingDirectory = Join-Path $root ".package"
$distributionDirectory = Join-Path $root "dist"

[xml]$projectFile = Get-Content -Raw $project
$version = [string]($projectFile.Project.PropertyGroup.Version | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "ClearClock.csproj に Version を設定してください。"
}

& dotnet publish $project -c $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$packageName = "ClearClock-v$version-windows-x64"
$packageFolder = Join-Path $stagingDirectory $packageName
$archive = Join-Path $distributionDirectory "$packageName.zip"

Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $packageFolder -Force | Out-Null
New-Item -ItemType Directory -Path $distributionDirectory -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $releaseDirectory "ClearClock.exe") -Destination $packageFolder
Copy-Item -LiteralPath $readme -Destination $packageFolder
Compress-Archive -Path $packageFolder -DestinationPath $archive

Write-Host "配布用 ZIP を生成しました: $archive"
