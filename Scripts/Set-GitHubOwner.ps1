[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?$')]
    [string]$Owner,

    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$Repository = "NekoThemesPlus"
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$files = @(
    "README.md",
    "README.en.md",
    "docs\GITHUB_RELEASE.zh-CN.md",
    ".github\ISSUE_TEMPLATE\config.yml"
)

foreach ($relativePath in $files) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "找不到文件：$relativePath"
    }

    $content = [System.IO.File]::ReadAllText($path)
    $content = $content.Replace("github.com/OWNER/NekoThemesPlus", "github.com/$Owner/$Repository")
    $content = $content.Replace("OWNER/NekoThemesPlus", "$Owner/$Repository")
    [System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "GitHub 地址已设置为：https://github.com/$Owner/$Repository" -ForegroundColor Green
Write-Host "请运行 ./Scripts/Validate-Repository.ps1，然后检查 git diff。"
