[CmdletBinding()]
param(
    [string]$UnityPath = "",
    [switch]$SkipUnityTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Utf8NoBom([string]$Path, [string]$Content) {
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Find-UnityEditor([string]$RequestedPath) {
    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = [System.IO.Path]::GetFullPath($RequestedPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "找不到指定的 Unity.exe：$resolved"
        }

        return $resolved
    }

    $hubRoot = Join-Path $env:ProgramFiles "Unity\Hub\Editor"
    if (-not (Test-Path -LiteralPath $hubRoot -PathType Container)) {
        return $null
    }

    $editors = Get-ChildItem -LiteralPath $hubRoot -Directory |
        Where-Object { $_.Name -like "2022.3.*" } |
        Sort-Object @{ Expression = {
            if ($_.Name -match '^2022\.3\.(\d+)') { [int]$Matches[1] } else { -1 }
        }; Descending = $true }

    $exact = $editors | Where-Object { $_.Name -eq "2022.3.42f1" } | Select-Object -First 1
    $selected = if ($null -ne $exact) { $exact } else { $editors | Select-Object -First 1 }
    if ($null -eq $selected) {
        return $null
    }

    $candidate = Join-Path $selected.FullName "Editor\Unity.exe"
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        return $candidate
    }

    return $null
}

function Assert-Package([string]$PackageDirectory) {
    Write-Step "检查 UPM 包结构"

    $required = @(
        "package.json",
        "LICENSE.md",
        "README.md",
        "CHANGELOG.md",
        "THIRD_PARTY_NOTICES.md",
        "Editor\NekoThemesPlus.asmdef",
        "Editor\Core\NekoThemesPlusBootstrap.cs",
        "Editor\UI\NekoThemesPlusWindow.cs",
        "Editor\Shaders\NekoBackground.shader",
        "Editor\Shaders\NekoBlur.shader",
        "Editor\Shaders\NekoColorAdjust.shader"
    )

    foreach ($relativePath in $required) {
        $fullPath = Join-Path $PackageDirectory $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "缺少必要文件：$relativePath"
        }
    }

    $missingMeta = @(Get-ChildItem -LiteralPath $PackageDirectory -Recurse -File |
        Where-Object {
            $_.Extension -ne ".meta" -and
            $_.FullName -notlike "*\Documentation~\*" -and
            -not (Test-Path -LiteralPath ($_.FullName + ".meta") -PathType Leaf)
        })

    if ($missingMeta.Count -gt 0) {
        $paths = ($missingMeta | ForEach-Object { $_.FullName }) -join [Environment]::NewLine
        throw "以下 Unity 资源缺少 .meta：$([Environment]::NewLine)$paths"
    }

    Write-Host "包结构检查通过。" -ForegroundColor Green
}

function New-UpmTarball(
    [string]$PackageDirectory,
    [string]$Destination,
    [string]$ExpectedFileName,
    [string]$TemporaryRoot
) {
    Write-Step "生成 UPM tarball"

    $expectedPath = Join-Path $Destination $ExpectedFileName
    if (Test-Path -LiteralPath $expectedPath) {
        Remove-Item -LiteralPath $expectedPath -Force
    }

    $npm = Get-Command npm.cmd -ErrorAction SilentlyContinue
    if ($null -eq $npm) {
        $npm = Get-Command npm -ErrorAction SilentlyContinue
    }

    if ($null -ne $npm) {
        $previousErrorActionPreference = $ErrorActionPreference
        try {
            # Windows PowerShell 5 wraps native stderr (npm notices) as ErrorRecord.
            # It is informational, so capture it without turning it into a terminating error.
            $ErrorActionPreference = "Continue"
            $npmOutput = @(& $npm.Source pack $PackageDirectory --pack-destination $Destination 2>&1)
            $npmExitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        $npmOutput | ForEach-Object { Write-Host $_ }
        if ($npmExitCode -ne 0) {
            throw "npm pack 执行失败，退出码：$npmExitCode"
        }
    }
    else {
        $tar = Get-Command tar.exe -ErrorAction SilentlyContinue
        if ($null -eq $tar) {
            $tar = Get-Command tar -ErrorAction SilentlyContinue
        }

        if ($null -eq $tar) {
            throw "未找到 npm 或 tar，无法生成 .tgz。"
        }

        $stageRoot = Join-Path $TemporaryRoot "tar-stage"
        $stagePackage = Join-Path $stageRoot "package"
        New-Item -ItemType Directory -Path $stagePackage -Force | Out-Null
        Copy-Item -Path (Join-Path $PackageDirectory "*") -Destination $stagePackage -Recurse -Force
        $null = & $tar.Source -czf $expectedPath -C $stageRoot package
        $tarExitCode = $LASTEXITCODE
        if ($tarExitCode -ne 0) {
            throw "tar 执行失败，退出码：$tarExitCode"
        }
    }

    if (-not (Test-Path -LiteralPath $expectedPath -PathType Leaf)) {
        throw "没有生成预期的成品包：$expectedPath"
    }

    $tarCommand = Get-Command tar.exe -ErrorAction SilentlyContinue
    if ($null -eq $tarCommand) {
        $tarCommand = Get-Command tar -ErrorAction SilentlyContinue
    }

    if ($null -ne $tarCommand) {
        $entries = @(& $tarCommand.Source -tzf $expectedPath)
        if ($LASTEXITCODE -ne 0 -or $entries.Count -eq 0) {
            throw "成品包无法读取或内容为空。"
        }

        foreach ($entry in @("package/package.json", "package/Editor/NekoThemesPlus.asmdef")) {
            if ($entries -notcontains $entry) {
                throw "成品包缺少：$entry"
            }
        }

        Write-Host "归档检查通过，共 $($entries.Count) 个文件。" -ForegroundColor Green
    }

    return [string]$expectedPath
}

function Test-WithUnity(
    [string]$UnityExecutable,
    [string]$TarballPath,
    [string]$PackageName,
    [string]$Version,
    [string]$TemporaryRoot,
    [string]$LogDestination
) {
    if ([string]::IsNullOrWhiteSpace($UnityExecutable)) {
        Write-Warning "未检测到 Unity 2022.3，已跳过安装编译测试。成品包仍然可以分享。"
        return "未执行（未检测到 Unity 2022.3）"
    }

    Write-Step "使用 Unity 做隔离安装编译测试"
    Write-Host "Unity：$UnityExecutable"

    $project = Join-Path $TemporaryRoot "smoke-project"
    $assets = Join-Path $project "Assets"
    $editorAssets = Join-Path $assets "Editor"
    $packages = Join-Path $project "Packages"
    $projectSettings = Join-Path $project "ProjectSettings"
    New-Item -ItemType Directory -Path $assets, $editorAssets, $packages, $projectSettings -Force | Out-Null

    $localTarball = [System.IO.Path]::GetFullPath($TarballPath).Replace('\', '/')
    $dependencies = [ordered]@{}
    $dependencies[$PackageName] = "file:$localTarball"
    $manifestObject = [ordered]@{ dependencies = $dependencies }
    Write-Utf8NoBom (Join-Path $packages "manifest.json") ($manifestObject | ConvertTo-Json -Depth 4)

    $unityFolderVersion = Split-Path (Split-Path (Split-Path $UnityExecutable -Parent) -Parent) -Leaf
    $projectVersion = "m_EditorVersion: $unityFolderVersion`r`n"
    Write-Utf8NoBom (Join-Path $projectSettings "ProjectVersion.txt") $projectVersion
    Write-Utf8NoBom (Join-Path $assets ".gitkeep") ""

    $validationSource = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class NekoThemesPlusReleaseSmoke
{
    public static void Validate()
    {
        var failures = new List<string>();
        var hostView = typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView", false);
        if (hostView == null)
        {
            failures.Add("UnityEditor.HostView was not found");
        }
        else
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (hostView.GetField("m_OnGUI", BindingFlags.Instance | BindingFlags.NonPublic) == null)
                failures.Add("HostView.m_OnGUI was not found");
            if (hostView.GetProperty("actualView", flags) == null)
                failures.Add("HostView.actualView was not found");
        }

        foreach (var shaderName in new[]
        {
            "Hidden/NekoThemesPlus/Background",
            "Hidden/NekoThemesPlus/ColorAdjust",
            "Hidden/NekoThemesPlus/Blur"
        })
        {
            if (Shader.Find(shaderName) == null)
                failures.Add(shaderName + " was not found");
        }

        var themePath = Path.Combine(Path.GetTempPath(), "NekoThemesPlusReleaseSmoke.nekotheme");
        var imagePath = Path.Combine(Path.GetTempPath(), "NekoThemesPlusReleaseSmoke.png");
        string importedWindowPath = null;
        try
        {
            var themeType = Type.GetType("NekoThemesPlus.Theme.NekoThemeImportExport, NekoThemesPlus.Editor", false);
            var export = themeType == null ? null : themeType.GetMethod("Export", BindingFlags.Static | BindingFlags.Public);
            var import = themeType == null ? null : themeType.GetMethod("Import", BindingFlags.Static | BindingFlags.Public);
            var backgroundType = Type.GetType("NekoThemesPlus.Background.BackgroundManager, NekoThemesPlus.Editor", false);
            var windowKindType = Type.GetType("NekoThemesPlus.Windows.WindowKind, NekoThemesPlus.Editor", false);
            var setWindow = backgroundType == null ? null : backgroundType.GetMethod("SetWindowBackground", BindingFlags.Static | BindingFlags.Public);
            var clearWindow = backgroundType == null ? null : backgroundType.GetMethod("ClearWindowBackground", BindingFlags.Static | BindingFlags.Public);
            var getWindowPath = backgroundType == null ? null : backgroundType.GetMethod("GetBackgroundPath", BindingFlags.Static | BindingFlags.Public);
            var getWindowTexture = backgroundType == null ? null : backgroundType.GetMethod("GetProcessedTexture", BindingFlags.Static | BindingFlags.Public);
            var releaseWindowCache = backgroundType == null ? null : backgroundType.GetMethod("ReleaseWindowCache", BindingFlags.Static | BindingFlags.Public);
            if (export == null || import == null || windowKindType == null ||
                setWindow == null || clearWindow == null || getWindowPath == null ||
                getWindowTexture == null || releaseWindowCache == null)
            {
                failures.Add("theme/per-window background API was not found");
            }
            else
            {
                File.WriteAllBytes(imagePath, Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
                object hierarchy = Enum.Parse(windowKindType, "Hierarchy");
                if (!(bool)setWindow.Invoke(null, new[] { hierarchy, (object)imagePath }))
                    failures.Add("per-window background could not be set");

                var textureArgs = new object[] { hierarchy, -9137, new Rect(0f, 0f, 320f, 240f), null };
                var windowTexture = getWindowTexture.Invoke(null, textureArgs) as Texture;
                Rect windowUv = textureArgs[3] is Rect ? (Rect)textureArgs[3] : new Rect();
                if (windowTexture == null || windowTexture.width != 320 || windowTexture.height != 240)
                    failures.Add("per-window GPU texture was not built at the requested size");
                if (windowUv != new Rect(0f, 0f, 1f, 1f))
                    failures.Add("per-window GPU texture did not use local full-image UVs");
                releaseWindowCache.Invoke(null, new object[] { -9137 });

                var exportArgs = new object[] { themePath, null };
                var exported = (bool)export.Invoke(null, exportArgs);
                if (!exported || !File.Exists(themePath))
                    failures.Add("theme export failed: " + (exportArgs[1] ?? "no message"));
                else
                {
                    string json = File.ReadAllText(themePath);
                    if (!json.Contains("\"schemaVersion\": 3") ||
                        !json.Contains("\"hierarchyBackgroundBase64\"") ||
                        !json.Contains("\"enableTextColors\""))
                        failures.Add("theme did not contain schema 3 window and text-theme data");

                    clearWindow.Invoke(null, new[] { hierarchy });
                    var importArgs = new object[] { themePath, null };
                    if (!(bool)import.Invoke(null, importArgs))
                        failures.Add("theme import failed: " + (importArgs[1] ?? "no message"));
                    else
                    {
                        importedWindowPath = getWindowPath.Invoke(null, new[] { hierarchy }) as string;
                        if (string.IsNullOrEmpty(importedWindowPath) || !File.Exists(importedWindowPath))
                            failures.Add("theme import did not restore the per-window image");
                    }

                    clearWindow.Invoke(null, new[] { hierarchy });
                }
            }
        }
        catch (Exception exception)
        {
            failures.Add("theme round-trip threw " + exception.GetBaseException().Message);
        }
        finally
        {
            if (File.Exists(themePath)) File.Delete(themePath);
            if (File.Exists(imagePath)) File.Delete(imagePath);
            if (!string.IsNullOrEmpty(importedWindowPath) && File.Exists(importedWindowPath))
                File.Delete(importedWindowPath);
        }

        if (failures.Count > 0)
            Debug.LogError("NEKO_RELEASE_VALIDATION_FAILED: " + string.Join("; ", failures));
        else
            Debug.Log("NEKO_RELEASE_VALIDATION_PASSED");
    }
}
'@
    Write-Utf8NoBom (Join-Path $editorAssets "NekoThemesPlusReleaseSmoke.cs") $validationSource

    if (Test-Path -LiteralPath $LogDestination) {
        Remove-Item -LiteralPath $LogDestination -Force
    }

    $unityArguments = @(
        "-batchmode",
        "-nographics",
        "-accept-apiupdate",
        "-executeMethod",
        "NekoThemesPlusReleaseSmoke.Validate",
        "-quit",
        "-projectPath",
        ('"' + $project + '"'),
        "-logFile",
        ('"' + $LogDestination + '"')
    )
    $unityProcess = Start-Process `
        -FilePath $UnityExecutable `
        -ArgumentList $unityArguments `
        -WindowStyle Hidden `
        -Wait `
        -PassThru

    $unityExitCode = $unityProcess.ExitCode
    if ($unityExitCode -ne 0) {
        throw "Unity 安装编译测试失败，退出码：$unityExitCode。日志：$LogDestination"
    }

    $errorPattern = 'error CS\d+|Shader error in|Compilation failed|Scripts have compiler errors|Aborting batchmode due to failure|Unhandled Exception|NEKO_RELEASE_VALIDATION_FAILED'
    $errors = @(Select-String -LiteralPath $LogDestination -Pattern $errorPattern)
    if ($errors.Count -gt 0) {
        $preview = ($errors | Select-Object -First 8 | ForEach-Object { $_.Line }) -join [Environment]::NewLine
        throw "Unity 日志中发现编译错误：$([Environment]::NewLine)$preview"
    }

    if (-not (Select-String -LiteralPath $LogDestination -Pattern 'NEKO_RELEASE_VALIDATION_PASSED' -Quiet)) {
        throw "Unity 编译完成，但没有收到 HostView/Shader 自检通过标记。"
    }

    $assembly = Join-Path $project "Library\ScriptAssemblies\NekoThemesPlus.Editor.dll"
    if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) {
        throw "Unity 返回成功，但没有生成 NekoThemesPlus.Editor.dll。"
    }

    $lockFile = Join-Path $packages "packages-lock.json"
    if (-not (Test-Path -LiteralPath $lockFile -PathType Leaf)) {
        throw "Package Manager 没有生成 packages-lock.json。"
    }

    $lockText = Get-Content -LiteralPath $lockFile -Raw
    if ($lockText -notmatch '"source"\s*:\s*"local-tarball"') {
        throw "Package Manager 没有把成品识别为 local-tarball。"
    }

    Write-Host "Unity 安装编译测试通过。" -ForegroundColor Green
    return "通过（Unity $unityFolderVersion，退出码 0）"
}

$repoRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$packageDirectory = Join-Path $repoRoot "Packages\com.neko.themesplus"
$packageJsonPath = Join-Path $packageDirectory "package.json"
$distDirectory = Join-Path $repoRoot "Dist"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("NekoThemesPlus-Release-" + [Guid]::NewGuid().ToString("N"))

try {
    $repositoryValidator = Join-Path $repoRoot "Scripts\Validate-Repository.ps1"
    if (Test-Path -LiteralPath $repositoryValidator -PathType Leaf) {
        Write-Step "执行仓库发布前校验"
        & $repositoryValidator
        if ($LASTEXITCODE -ne 0) {
            throw "仓库发布前校验失败。"
        }
    }

    if (-not (Test-Path -LiteralPath $packageJsonPath -PathType Leaf)) {
        throw "找不到 package.json：$packageJsonPath"
    }

    $package = Get-Content -LiteralPath $packageJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($package.name -ne "com.neko.themesplus") {
        throw "Package ID 必须是 com.neko.themesplus，当前为：$($package.name)"
    }

    if ([string]::IsNullOrWhiteSpace([string]$package.version)) {
        throw "package.json 缺少 version。"
    }

    $version = [string]$package.version
    $tarballName = "$($package.name)-$version.tgz"
    $zipName = "NekoThemesPlus-$version-Windows.zip"
    New-Item -ItemType Directory -Path $distDirectory, $temporaryRoot -Force | Out-Null

    Assert-Package $packageDirectory
    $tarballPath = New-UpmTarball $packageDirectory $distDirectory $tarballName $temporaryRoot
    $hash = (Get-FileHash -LiteralPath $tarballPath -Algorithm SHA256).Hash
    $checksumPath = Join-Path $distDirectory "SHA256SUMS.txt"
    Write-Utf8NoBom $checksumPath "$hash  $tarballName`r`n"

    $unityStatus = "未执行（使用 -SkipUnityTest）"
    $smokeLog = Join-Path $distDirectory "NekoThemesPlus-$version-SmokeTest.log"
    if (-not $SkipUnityTest) {
        $detectedUnity = Find-UnityEditor $UnityPath
        $unityStatus = Test-WithUnity $detectedUnity $tarballPath $package.name $version $temporaryRoot $smokeLog
    }

    Write-Step "生成可分享 ZIP"
    $shareDirectory = Join-Path $temporaryRoot "share"
    New-Item -ItemType Directory -Path $shareDirectory -Force | Out-Null
    Copy-Item -LiteralPath $tarballPath -Destination $shareDirectory
    Copy-Item -LiteralPath $checksumPath -Destination $shareDirectory
    $installTemplatePath = Join-Path $repoRoot "Share\安装说明.txt"
    $installText = (Get-Content -LiteralPath $installTemplatePath -Raw -Encoding UTF8).Replace("{{VERSION}}", $version)
    Write-Utf8NoBom (Join-Path $shareDirectory "安装说明.txt") $installText

    $buildInfo = @"
Neko Themes Plus 发布信息

Package: $($package.name)
Version: $version
Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')
Unity test: $unityStatus
SHA-256: $hash
"@
    Write-Utf8NoBom (Join-Path $shareDirectory "发布信息.txt") $buildInfo

    $zipPath = Join-Path $distDirectory $zipName
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $shareDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal
    $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    Write-Utf8NoBom $checksumPath "$hash  $tarballName`r`n$zipHash  $zipName`r`n"

    Write-Step "发布完成"
    Write-Host "给朋友发送：$zipPath" -ForegroundColor Green
    Write-Host "UPM 成品包：$tarballPath"
    Write-Host "TGZ SHA-256：$hash"
    Write-Host "ZIP SHA-256：$zipHash"
    Write-Host "Unity 测试：$unityStatus"
}
catch {
    Write-Host ""
    Write-Host "发布失败：$($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot -PathType Container) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

exit 0
