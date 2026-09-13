# NekoThemesPlus

[![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-000000?logo=unity)](ProjectSettings/ProjectVersion.txt)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![CI](https://github.com/moekotori-yolo/NekoThemesPlus/actions/workflows/ci.yml/badge.svg)](https://github.com/moekotori-yolo/NekoThemesPlus/actions/workflows/ci.yml)

NekoThemesPlus is an editor-only wallpaper and glass-theme extension for Unity. It provides a continuous background across docked windows, per-window opacity, GPU color processing and blur, dock chrome tinting, bilingual UI, diagnostics, and portable `.nekotheme` files.

> Before publishing, run `./Scripts/Set-GitHubOwner.ps1 -Owner YOUR_NAME` to replace the `OWNER` placeholder in repository links.

[简体中文](README.md) · [User guide](Packages/com.neko.themesplus/Documentation~/README.zh-CN.md) · [Changelog](Packages/com.neko.themesplus/CHANGELOG.md) · [Contributing](CONTRIBUTING.md)

## Highlights

- Continuous wallpaper coordinates across the Unity main window, with sensible fallback behavior for floating windows.
- Fill, Fit, Stretch, Center, and Tile modes, plus zoom and composition controls.
- GPU brightness, saturation, contrast, hue, opacity, and multi-pass blur.
- Independent Hierarchy, Inspector, Project, and Console visibility and opacity.
- Independent Hierarchy, Inspector, Project, and Console images with automatic global-background inheritance.
- Safe Scene/Game View handling that leaves camera output untouched.
- HostView paint bridge and configurable dock/tab chrome tint.
- Simplified Chinese and English UI, eight presets, safe mode, and a copyable diagnostic report.
- Portable `.nekotheme` import/export with an optional embedded wallpaper.
- Experimental Windows Mica/Acrylic integration, disabled by default.

## Compatibility

- Unity `2022.3.x` LTS; primary target: `2022.3.42f1`
- Windows 10 and Windows 11
- Editor only; no runtime/player assembly is included
- Current version: `0.3.0`

The release pipeline has also been smoke-tested with Unity `2022.3.22f1c1`. Internal hooks are disabled by default outside Unity 2022.3.

## Installation

### GitHub Release (recommended)

Download `NekoThemesPlus-<version>-Windows.zip` from Releases, extract it, then select **Window > Package Manager > + > Add package from tarball...** and open the included `.tgz` file.

### Git URL

Replace `OWNER` with the repository owner:

```text
https://github.com/moekotori-yolo/NekoThemesPlus.git?path=/Packages/com.neko.themesplus#v0.3.0
```

After compilation, open **Window > Neko Themes Plus > Settings**.

## Build

On Windows with Unity 2022.3 installed:

```powershell
./Build-Release.ps1
```

Static validation and packaging without Unity:

```powershell
./Scripts/Validate-Repository.ps1
./Build-Release.ps1 -SkipUnityTest
```

See [CI setup](docs/CI.zh-CN.md) and the [GitHub publishing guide](docs/GITHUB_RELEASE.zh-CN.md).

## Credits and license

The HostView delegate-hooking and dock chrome-painting approach was informed by the MIT-licensed [System32X-code/UniPrism](https://github.com/System32X-code/UniPrism). See [THIRD_PARTY_NOTICES.md](Packages/com.neko.themesplus/THIRD_PARTY_NOTICES.md).

NekoThemesPlus is released under the [MIT License](LICENSE).
