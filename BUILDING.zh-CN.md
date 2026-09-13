# Neko Themes Plus 一键发布

双击根目录的 `Build-Release.cmd` 即可。

脚本会自动：

1. 读取 `Packages/com.neko.themesplus/package.json` 中的版本号。
2. 检查必要源码、Shader、asmdef 和 Unity `.meta` 文件。
3. 生成标准 UPM tarball。
4. 如果检测到 Unity 2022.3，用全新的临时项目安装 tarball 并编译。
5. 扫描 C#、Shader 和批处理失败信息。
6. 生成 SHA-256 校验码。
7. 输出可直接发送给朋友的 `Dist/NekoThemesPlus-版本-Windows.zip`。
8. 同时生成包含 TGZ 和 ZIP 校验值的 `Dist/SHA256SUMS.txt`。

命令行用法：

```powershell
# 正常发布，自动寻找 Unity 2022.3
.\Build-Release.ps1

# 指定 Unity
.\Build-Release.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.42f1\Editor\Unity.exe"

# 只打包，不运行 Unity 测试
.\Build-Release.ps1 -SkipUnityTest
```

发给朋友的是 `NekoThemesPlus-版本-Windows.zip`，不是整个 Unity 工程。

## GitHub 自动构建

仓库已经包含：

- `.github/workflows/ci.yml`：提交和 PR 自动校验、打包；配置许可证后运行 Unity EditMode 测试。
- `.github/workflows/release.yml`：推送 `v0.2.0` 这类标签后自动创建 GitHub Release。
- `Scripts/Validate-Repository.ps1`：版本、文档、JSON、`.meta`、GUID 和第三方声明检查。
- `Scripts/Get-ReleaseNotes.ps1`：从 CHANGELOG 提取当前版本发布说明。

完整设置见 `docs/CI.zh-CN.md`。
