# 更新日志

## [0.5.0] - 2026-09-14

### 新增

- 每 24 小时自动检查一次 GitHub 最新稳定 Release，也可从设置页或 Tools 菜单立即检查。
- 使用 ETag 缓存减少 GitHub API 请求，并严格比较三段式稳定版本号。
- 发现新版时明确提示先保存工程；确认后由 Unity Package Manager 安装对应 Git 标签并正常触发脚本重编译。
- 设置首页显示当前版本、GitHub 最新版、检查状态和“保存并更新”按钮，诊断报告同时记录安装来源与更新状态。

### 安全

- Embedded / Local 源码开发模式禁止自动覆盖，只提供 Release 页面入口。
- 更新失败可安全回退到手动 Release；不会下载并热替换正在运行的 DLL。

## [0.4.0] - 2026-09-13

### 新增

- 磨砂玻璃层支持任意颜色与 Alpha；最终透明度由颜色 Alpha、全局强度和区域强度共同计算。
- 新增可开关的全局文字主题，分别设置主文字色和次要 / 提示文字色。
- 同时覆盖 UI Toolkit 文本元素与 Unity 内置 IMGUI GUIStyle，并持续发现动态生成的列表、标签和输入控件。
- 新增参考淡粉外观调校的“樱花磨砂”预设。
- `.nekotheme` 升级到 Schema 3，保存玻璃色与文字主题，并继续兼容 Schema 1/2。

### 调整

- 停用文字主题、安全模式、域重载或“恢复 Unity”时，完整还原捕获到的内联颜色和 GUIStyle 状态。
- Scene View / Game View 仍只允许安全工具栏主题，不会给相机画面叠加颜色。

## [0.3.0] - 2026-09-13

### 新增

- Hierarchy、Inspector、Project、Console 可分别选择独立 PNG/JPG/JPEG 背景。
- 独立背景为空或加载失败时自动继承全局连续背景。
- 每个实际窗口实例使用按自身尺寸生成的 GPU 缓存，兼容多个不同尺寸的 Inspector。
- `.nekotheme` Schema 2 可内嵌全局图片和四张区域图片，并向后兼容 Schema 1。

### 调整

- 窗口关闭、换图、全局图像参数变化和插件停用时会释放对应的独立背景缓存。
- Scene View 与 Game View 继续严格限制在安全工具栏区域，不支持图片覆盖相机内容。

## [0.2.0] - 2026-09-13

### 新增

- HostView `m_OnGUI` 安全包装，在旧式 IMGUI 内容绘制前注入连续背景和面板透明度。
- Dock 标签栏与边框染色，可独立设置颜色和强度。
- 布局拖动期间最多 60 Hz 的跨窗口重绘广播，连续壁纸更及时地对齐。
- 背景 1–4 倍缩放和水平/垂直构图控制。
- `.nekotheme` 单文件主题导入/导出，内嵌背景图片并排除机器相关的原生实验设置。
- 可复制到剪贴板的中文诊断报告，包含 HostView、窗口挂钩和背景处理状态。
- UniPrism MIT 来源和许可声明。

### 调整

- GUIStyle 背景修改不再默认启用，仅作为 HostView 不可用时的手动实验兼容层。
- Scene/Game 内容仍不进入 HostView tint 路径，只允许安全的 Dock chrome 绘制。
- 补齐 GitHub 双语首页、开源许可证、贡献与安全政策、Issue/PR 模板、EditMode 测试、自动打包和标签 Release 工作流。

## [0.1.0] - 2024-12-01

### 新增

- 完整 UPM 包和仅编辑器程序集。
- 简体中文默认界面，并可切换 English。
- `ProjectSettings/NekoThemesPlusSettings.asset` 持久化设置。
- 包含全局、背景、玻璃、窗口、颜色、Windows 效果、预设、高级和关于九个页面的 UI Toolkit 设置中心。
- 外部 PNG/JPG/JPEG 加载、最大分辨率限制和安全释放。
- Fill、Fit、Stretch、Center、Tile 背景布局。
- GPU 亮度、饱和度、对比度、色调、不透明度处理和多级 Dual Kawase Blur。
- 主编辑器窗口检测、DPI 输出尺寸与跨窗口连续 UV 映射。
- Hierarchy、Inspector、Project、Console 的背景/玻璃层注入与独立透明度。
- 多 Inspector、新窗口、浮动窗口和布局变化的定时发现与刷新。
- Scene View / Game View 的工具条限定主题，保持相机渲染区不变。
- 可恢复的 GUIStyle 背景兼容层与 Hierarchy/Project 选择强调条。
- 8 个内置视觉预设。
- 默认关闭的 Windows 深色标题栏、圆角、Mica 和 Acrylic 实验支持。
- 延迟启动、Safe Mode、启用/停用、域重载恢复和一键恢复 Unity 生命周期。
