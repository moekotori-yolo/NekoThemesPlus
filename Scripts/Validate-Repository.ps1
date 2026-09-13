[CmdletBinding()]
param(
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$packageRoot = Join-Path $repoRoot "Packages\com.neko.themesplus"
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
}

function Test-RequiredFile([string]$RelativePath) {
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Failure "缺少必要文件：$RelativePath"
    }
}

Write-Host "==> 校验仓库结构" -ForegroundColor Cyan

$requiredFiles = @(
    "README.md",
    "README.en.md",
    "LICENSE",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "SECURITY.md",
    "Packages\com.neko.themesplus\package.json",
    "Packages\com.neko.themesplus\LICENSE.md",
    "Packages\com.neko.themesplus\README.md",
    "Packages\com.neko.themesplus\CHANGELOG.md",
    "Packages\com.neko.themesplus\THIRD_PARTY_NOTICES.md",
    "Packages\com.neko.themesplus\Editor\NekoThemesPlus.asmdef",
    "Packages\com.neko.themesplus\Editor\Core\NekoThemesPlusConstants.cs",
    "Packages\com.neko.themesplus\Editor\Reflection\HostViewBridge.cs",
    "Packages\com.neko.themesplus\Editor\Windows\HostViewHookManager.cs",
    "Packages\com.neko.themesplus\Editor\Shaders\NekoBackground.shader",
    "Packages\com.neko.themesplus\Editor\Shaders\NekoColorAdjust.shader",
    "Packages\com.neko.themesplus\Editor\Shaders\NekoBlur.shader"
)
$requiredFiles | ForEach-Object { Test-RequiredFile $_ }

try {
    $package = Get-Content -LiteralPath (Join-Path $packageRoot "package.json") -Raw -Encoding UTF8 | ConvertFrom-Json
}
catch {
    Add-Failure "package.json 不是有效 JSON：$($_.Exception.Message)"
    $package = $null
}

if ($null -ne $package) {
    if ($package.name -ne "com.neko.themesplus") {
        Add-Failure "Package ID 必须是 com.neko.themesplus。"
    }

    $version = [string]$package.version
    if ($version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
        Add-Failure "版本号不符合 SemVer：$version"
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion) -and $version -ne $ExpectedVersion) {
        Add-Failure "标签/期望版本 $ExpectedVersion 与 package.json 版本 $version 不一致。"
    }

    if ([string]$package.unity -ne "2022.3") {
        Add-Failure "package.json 的 unity 必须保持为 2022.3。"
    }

    if ([string]$package.license -ne "MIT") {
        Add-Failure "package.json 的 license 必须是 MIT。"
    }

    $constantsPath = Join-Path $packageRoot "Editor\Core\NekoThemesPlusConstants.cs"
    if (Test-Path -LiteralPath $constantsPath) {
        $constants = Get-Content -LiteralPath $constantsPath -Raw -Encoding UTF8
        if ($constants -notmatch ('FallbackVersion\s*=\s*"' + [regex]::Escape($version) + '"')) {
            Add-Failure "NekoThemesPlusConstants.FallbackVersion 未同步到 $version。"
        }
    }

    $uxmlPath = Join-Path $packageRoot "Editor\UI\NekoThemesPlusWindow.uxml"
    if (Test-Path -LiteralPath $uxmlPath) {
        $uxml = Get-Content -LiteralPath $uxmlPath -Raw -Encoding UTF8
        if ($uxml -notmatch ('name="version-label"\s+text="v' + [regex]::Escape($version) + '"')) {
            Add-Failure "设置窗口的静态 fallback 版本未同步到 v$version。"
        }
    }

    $changeLogPath = Join-Path $packageRoot "CHANGELOG.md"
    if (Test-Path -LiteralPath $changeLogPath) {
        $changeLog = Get-Content -LiteralPath $changeLogPath -Raw -Encoding UTF8
        if ($changeLog -notmatch ('(?m)^## \[' + [regex]::Escape($version) + '\]')) {
            Add-Failure "CHANGELOG.md 缺少 $version 版本章节。"
        }
    }
}

Write-Host "==> 校验 JSON 与 Editor-only 程序集" -ForegroundColor Cyan
$jsonFiles = @(
    "Packages\manifest.json",
    "Packages\com.neko.themesplus\package.json",
    "Packages\com.neko.themesplus\Editor\NekoThemesPlus.asmdef",
    "Assets\Tests\Editor\NekoThemesPlus.Editor.Tests.asmdef"
)
foreach ($relativePath in $jsonFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        try { $null = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
        catch { Add-Failure "$relativePath 不是有效 JSON：$($_.Exception.Message)" }
    }
}

$asmdefPath = Join-Path $packageRoot "Editor\NekoThemesPlus.asmdef"
if (Test-Path -LiteralPath $asmdefPath) {
    $asmdef = Get-Content -LiteralPath $asmdefPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($asmdef.includePlatforms) -notcontains "Editor") {
        Add-Failure "NekoThemesPlus.Editor.asmdef 必须只包含 Editor 平台。"
    }
}

Write-Host "==> 校验 Unity .meta 与 GUID" -ForegroundColor Cyan
$assetRoots = @((Join-Path $repoRoot "Assets"), $packageRoot)
$missingMeta = foreach ($root in $assetRoots) {
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
    Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
        $_.Extension -ne ".meta" -and
        $_.FullName -notmatch '[\\/]Documentation~[\\/]' -and
        -not (Test-Path -LiteralPath ($_.FullName + ".meta") -PathType Leaf)
    }
}
foreach ($file in $missingMeta) {
    Add-Failure "Unity 资源缺少 .meta：$($file.FullName.Substring($repoRoot.Length + 1))"
}

$guidOwners = @{}
$metaFiles = Get-ChildItem -LiteralPath (Join-Path $repoRoot "Assets"), $packageRoot -Recurse -Filter "*.meta" -File -ErrorAction SilentlyContinue
foreach ($meta in $metaFiles) {
    $match = Select-String -LiteralPath $meta.FullName -Pattern '^guid:\s*([0-9a-fA-F]{32})\s*$' | Select-Object -First 1
    if ($null -eq $match) {
        Add-Failure "无有效 GUID：$($meta.FullName.Substring($repoRoot.Length + 1))"
        continue
    }

    $guid = $match.Matches[0].Groups[1].Value.ToLowerInvariant()
    if ($guidOwners.ContainsKey($guid)) {
        Add-Failure "重复 Unity GUID $guid：$($guidOwners[$guid]) 与 $($meta.FullName.Substring($repoRoot.Length + 1))"
    }
    else {
        $guidOwners[$guid] = $meta.FullName.Substring($repoRoot.Length + 1)
    }
}

Write-Host "==> 校验第三方声明和仓库卫生" -ForegroundColor Cyan
$noticePath = Join-Path $packageRoot "THIRD_PARTY_NOTICES.md"
if (Test-Path -LiteralPath $noticePath) {
    $notice = Get-Content -LiteralPath $noticePath -Raw -Encoding UTF8
    foreach ($requiredText in @("System32X-code/UniPrism", "MIT License", "b8f2caec2426df3b158ff37608621e563417b16d")) {
        if ($notice -notmatch [regex]::Escape($requiredText)) {
            Add-Failure "THIRD_PARTY_NOTICES.md 缺少：$requiredText"
        }
    }
}

foreach ($forbidden in @("Library", "Temp", "Logs", "UserSettings", "Dist")) {
    $tracked = @(& git -C $repoRoot ls-files -- $forbidden 2>$null)
    if ($LASTEXITCODE -eq 0 -and $tracked.Count -gt 0) {
        Add-Failure "生成目录不应被 Git 跟踪：$forbidden"
    }
}

$placeholderCandidates = @(
    (Join-Path $repoRoot "README.md"),
    (Join-Path $repoRoot "README.en.md"),
    (Join-Path $repoRoot "docs\GITHUB_RELEASE.zh-CN.md"),
    (Join-Path $repoRoot ".github\ISSUE_TEMPLATE\config.yml")
)
$placeholderFiles = @(Select-String -LiteralPath $placeholderCandidates -Pattern 'github.com/OWNER/NekoThemesPlus' -SimpleMatch -ErrorAction SilentlyContinue)
if ($placeholderFiles.Count -gt 0) {
    Write-Warning "GitHub OWNER 尚未设置。上传前运行：./Scripts/Set-GitHubOwner.ps1 -Owner 你的用户名"
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "仓库校验失败（$($failures.Count) 项）：" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" -ForegroundColor Red }
    exit 1
}

Write-Host "仓库校验通过。Version=$($package.version)，GUID=$($guidOwners.Count)，缺失 .meta=0。" -ForegroundColor Green
exit 0
