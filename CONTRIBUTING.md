# 参与贡献

感谢你愿意改进 NekoThemesPlus。提交代码前请先阅读以下约定。

## 开发环境

- Windows 10 或 Windows 11
- Unity 2022.3 LTS，推荐 `2022.3.42f1`
- PowerShell 7 或 Windows PowerShell 5.1
- Node.js/npm（用于生成标准 UPM tarball；没有 npm 时脚本会尝试使用 `tar`）

克隆仓库后，用 Unity 打开仓库根目录。主要代码位于 `Packages/com.neko.themesplus/Editor`。

## 开发原则

- 插件必须保持 Editor-only，禁止新增 Player 运行时依赖。
- Scene View 和 Game View 的相机渲染区域不得被主题层覆盖。
- 所有反射和 Windows 原生调用必须 fail-soft，失败时应回退而不是阻止 Unity 启动。
- 新增挂钩必须可在停用、域重载和编辑器退出时完整恢复。
- 面向用户的文本应同时提供简体中文和 English。
- 不要提交 `Library`、`Temp`、`Logs`、`UserSettings`、`Dist` 或个人壁纸。

## 提交前检查

```powershell
./Scripts/Validate-Repository.ps1
./Build-Release.ps1
```

至少手动确认：

1. Unity Console 没有新增编译或 Shader 错误。
2. Hierarchy、Inspector、Project、Console 正常绘制和交互。
3. Scene/Game 相机内容不受影响。
4. “恢复 Unity”和禁用插件可以完整移除效果。
5. `.nekotheme` 导出后可重新导入。

## Pull Request

- 一个 PR 聚焦一个主题。
- 说明问题、实现方案、验证过的 Unity/Windows 版本和风险区域。
- UI 变化请附截图；反射或原生代码变化请写清失败回退路径。
- 用户可见变化必须更新 `Packages/com.neko.themesplus/CHANGELOG.md`。
- 不要在功能 PR 中顺带大范围格式化无关文件。

## 提交信息建议

推荐使用简洁的 Conventional Commits 风格，例如：

```text
feat: add per-window blur strength
fix: restore HostView delegate after domain reload
docs: clarify tarball installation
```

## 第三方代码

引入或改写第三方代码前必须确认许可证兼容，并把项目名称、来源、版权和许可证添加到 `THIRD_PARTY_NOTICES.md`。不要复制许可证不明确的代码或资源。
