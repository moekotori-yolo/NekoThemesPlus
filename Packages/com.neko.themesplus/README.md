# Neko Themes Plus

Neko Themes Plus 是仅作用于 Unity Editor 的全局壁纸、彩色磨砂玻璃与文字主题插件，不会进入 Player 构建，也不会修改 Scene/Game 相机输出。

## 环境

- 正式目标：Unity `2022.3.42f1` LTS
- 兼容范围：Unity `2022.3.x` LTS
- 系统：Windows 10 / Windows 11
- 当前版本：`0.4.0`

## 安装

在 Unity 中打开 **Window > Package Manager**，点击左上角 **+**：

1. 成品压缩包：选择 **Add package from tarball...**，打开 `com.neko.themesplus-0.4.0.tgz`。
2. 源码目录：选择 **Add package from disk...**，打开本包的 `package.json`。

本开发项目已经以内嵌包方式安装，无需重复安装。编译完成后，通过 **Window > Neko Themes Plus > 设置** 打开设置中心。

## 快速开始

1. 打开“背景”页面，点击“选择图片”，选择外部 PNG、JPG 或 JPEG。
2. 调整亮度、饱和度、对比度、色调、不透明度和模糊；结果会实时保存和预览。
3. 在“窗口”页面分别启用层级、检视器、项目和控制台，并调整面板不透明度。
4. 在“玻璃”页选择磨砂玻璃颜色与 Alpha，在“颜色”页按需启用主文字色和次要文字色。
5. 2022.3 会自动应用保守的 IMGUI 背景兼容；如果仍有区域遮住壁纸，可在“高级”中启用“扩展内部样式（实验性）”并点击“刷新窗口”。
6. 需要完全撤销效果时，点击“高级 > 恢复 Unity”，或使用 **Tools > Neko Themes Plus > 停用**。

## 已包含功能

- 全局连续壁纸 UV 映射；浮动到主窗口外时自动采用本地背景。
- HostView 绘制桥在 Unity 重置 GUI 状态后注入背景和面板 alpha，提升旧式 IMGUI 窗口的真实透底效果。
- Fill、Fit、Stretch、Center、Tile 五种显示模式。
- 背景 1–4 倍缩放和水平/垂直构图对齐。
- GPU 调色和多级 Dual Kawase Blur，仅在参数或主窗口尺寸变化时重建。
- Hierarchy、Inspector、Project、Console 独立开关与透明度。
- Hierarchy、Inspector、Project、Console 可各自选择独立背景；留空时继承全局壁纸。
- Scene View / Game View 可选安全模式，只处理 24 px 编辑器工具条，不触碰渲染区域。
- 可调颜色和 Alpha 的磨砂玻璃层，以及 UI Toolkit / IMGUI 双路径的可恢复文字主题。
- 9 个内置预设（含“樱花磨砂”）、自定义选择强调条、简体中文/English 界面。
- Dock 标签栏/边框染色，以及可复制到剪贴板的完整诊断报告。
- `.nekotheme` 单文件主题导入/导出，可内嵌全局壁纸和四张区域壁纸；单张上限 64 MB、合计上限 96 MB。
- 设置保存在 `ProjectSettings/NekoThemesPlusSettings.asset`，重启自动恢复。
- Disable、域重载和退出时移除注入元素、恢复 GUIStyle、释放纹理和原生效果。
- Windows 深色标题栏、圆角、Mica、Acrylic 实验选项，默认关闭。
- 启动异常检测和 Safe Mode。

## 安全说明

- 插件不修改 Unity 安装目录和内置 Skin 资源。
- 不对窗口根节点设置整体透明度，因此文字、图标和控件不会随面板变透明。
- 原生 Windows 效果只修改 Unity 主窗口；关闭时会恢复应用前可读取到的 DWM 值。
- Unity `2022.3.x` 之外的版本默认停止内部主题挂钩，可在“高级”中手动强制测试。

更完整的安装、验收和故障恢复步骤见 [中文使用手册](Documentation~/README.zh-CN.md)。

## 开源致谢

HostView 委托挂钩和 Dock chrome 绘制思路参考了 MIT 许可的 [System32X-code/UniPrism](https://github.com/System32X-code/UniPrism)。NekoThemesPlus 保留自己的 GPU 处理、设置模型、中文界面、安全模式和恢复生命周期；许可原文见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

---

English UI is available from **高级 > 界面语言 > English**. This package is editor-only and targets Unity 2022.3 LTS on Windows.
