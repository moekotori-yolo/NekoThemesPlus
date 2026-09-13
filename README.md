# NekoThemesPlus

[![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-000000?logo=unity)](ProjectSettings/ProjectVersion.txt)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![CI](https://github.com/moekotori-yolo/NekoThemesPlus/actions/workflows/ci.yml/badge.svg)](https://github.com/moekotori-yolo/NekoThemesPlus/actions/workflows/ci.yml)

NekoThemesPlus 是一个仅作用于 Unity Editor 的全局壁纸与玻璃主题扩展。它提供连续背景、独立窗口透明度、GPU 调色与模糊、Dock 栏染色、中文界面，以及可分享的 `.nekotheme` 主题文件。

> 发布前运行 `./Scripts/Set-GitHubOwner.ps1 -Owner 你的用户名`，自动替换仓库链接中的 `OWNER` 占位符。功能使用不受这个占位符影响。

[English](README.en.md) · [中文手册](Packages/com.neko.themesplus/Documentation~/README.zh-CN.md) · [更新日志](Packages/com.neko.themesplus/CHANGELOG.md) · [参与贡献](CONTRIBUTING.md)

## 功能

- Unity 主窗口范围内的连续壁纸映射，浮动窗口自动使用本地背景。
- Fill、Fit、Stretch、Center、Tile 五种布局，以及缩放和构图位置控制。
- GPU 亮度、饱和度、对比度、色调、不透明度和多级模糊。
- Hierarchy、Inspector、Project、Console 独立开关与透明度。
- Scene View / Game View 安全模式，不修改相机渲染内容。
- HostView 绘制桥、Dock 标签栏与边框染色。
- 简体中文 / English 设置界面、8 个预设和诊断报告。
- `.nekotheme` 单文件导入导出，可内嵌壁纸和视觉参数。
- Safe Mode、完整恢复流程，以及默认关闭的 Windows Mica/Acrylic 实验功能。

## 兼容性

| 项目 | 支持范围 |
| --- | --- |
| Unity | `2022.3.x` LTS；正式目标 `2022.3.42f1` |
| 操作系统 | Windows 10 / Windows 11 |
| Player 构建 | 不参与；插件程序集仅限 Editor |
| 当前版本 | `0.2.0` |

本项目也在 Unity `2022.3.22f1c1` 上完成过隔离安装与编译自检。其他 Unity 大版本默认不会启用内部窗口挂钩。

## 安装

### 从 Releases 安装（推荐）

1. 打开仓库右侧 **Releases**，下载 `NekoThemesPlus-版本-Windows.zip`。
2. 解压 ZIP。
3. 在 Unity 中打开 **Window > Package Manager**。
4. 点击 **+ > Add package from tarball...**。
5. 选择 `com.neko.themesplus-版本.tgz`。
6. 编译完成后打开 **Window > Neko Themes Plus > 设置**。

### 从 Git URL 安装

在 Package Manager 中选择 **Add package from git URL...**，输入：

```text
https://github.com/moekotori-yolo/NekoThemesPlus.git?path=/Packages/com.neko.themesplus#v0.2.0
```

### 开发者安装

克隆仓库后直接用 Unity 2022.3 LTS 打开根目录。包以内嵌形式位于 `Packages/com.neko.themesplus`。

## 快速开始

1. 打开设置窗口并进入“背景”。
2. 选择 PNG、JPG 或 JPEG 壁纸。
3. 应用“Neko 玻璃”预设，再微调模糊、亮度和面板透明度。
4. 在“窗口”页决定需要处理的编辑器面板。
5. 如遇问题，到“高级”页复制诊断报告或点击“恢复 Unity”。

## 构建与测试

Windows 上双击 `Build-Release.cmd`，或运行：

```powershell
./Build-Release.ps1
```

它会检查 UPM 包结构、生成 tarball、在临时 Unity 工程中安装并编译、自检 HostView/Shader/主题导入导出，然后输出分享 ZIP 和 SHA-256。

无 Unity 环境时仍可做静态校验和打包：

```powershell
./Scripts/Validate-Repository.ps1
./Build-Release.ps1 -SkipUnityTest
```

GitHub Actions 的配置和许可证设置见 [CI 与自动发布指南](docs/CI.zh-CN.md)。首次建仓、推送和发版步骤见 [GitHub 发布指南](docs/GITHUB_RELEASE.zh-CN.md)。

## 项目结构

```text
Packages/com.neko.themesplus/  UPM 插件源码与包内文档
Assets/Tests/Editor/           Unity EditMode 自动测试
Scripts/                       仓库校验和 Release Notes 工具
.github/workflows/             CI、Unity 编译测试和自动发布
Build-Release.ps1              Windows 本地一键成品包
```

## 安全与隐私

插件只修改当前 Unity Editor 的显示状态，不修改 Unity 安装目录，不进入游戏构建，也不上传壁纸或设置。背景图片路径保存在当前项目设置中。Windows 原生视觉效果均为实验功能且默认关闭。

发现安全问题请不要提交公开 Issue，处理方式见 [SECURITY.md](SECURITY.md)。

## 致谢与许可证

HostView 委托挂钩和 Dock chrome 绘制思路参考了 MIT 许可的 [System32X-code/UniPrism](https://github.com/System32X-code/UniPrism)。完整第三方声明见 [THIRD_PARTY_NOTICES.md](Packages/com.neko.themesplus/THIRD_PARTY_NOTICES.md)。

NekoThemesPlus 使用 [MIT License](LICENSE) 发布。
