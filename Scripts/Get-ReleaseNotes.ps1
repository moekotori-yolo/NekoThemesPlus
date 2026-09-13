[CmdletBinding()]
param(
    [string]$Version = "",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$packageRoot = Join-Path $repoRoot "Packages\com.neko.themesplus"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $package = Get-Content -LiteralPath (Join-Path $packageRoot "package.json") -Raw -Encoding UTF8 | ConvertFrom-Json
    $Version = [string]$package.version
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "Dist\RELEASE_NOTES.md"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$changeLog = Get-Content -LiteralPath (Join-Path $packageRoot "CHANGELOG.md") -Raw -Encoding UTF8
$pattern = '(?ms)^## \[' + [regex]::Escape($Version) + '\].*?(?=^## \[|\z)'
$match = [regex]::Match($changeLog, $pattern)
if (-not $match.Success) {
    throw "CHANGELOG.md 中找不到版本 $Version。"
}

$directory = Split-Path $OutputPath -Parent
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$content = "# NekoThemesPlus $Version`r`n`r`n" + $match.Value.Trim() + "`r`n"
[System.IO.File]::WriteAllText($OutputPath, $content, [System.Text.UTF8Encoding]::new($false))
Write-Host "Release Notes：$OutputPath"
