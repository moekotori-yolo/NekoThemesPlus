# 更新日志

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

## [0.1.0] - 2026-09-13

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
