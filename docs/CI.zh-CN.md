# GitHub Actions：自动校验、Unity 编译和发布

仓库包含两条工作流：

- `.github/workflows/ci.yml`：每次 push、Pull Request 和手动运行时执行。
- `.github/workflows/release.yml`：推送 `v*.*.*` 标签时创建或更新 GitHub Release。

## 一、默认启用的无许可证任务

无需任何 Secret 即可运行：

- 校验 package.json、版本号、asmdef、文档和 `.meta` 完整性。
- 检查重复 Unity GUID。
- 用 `npm pack` 生成标准 UPM tarball。
- 生成 Windows 分享 ZIP 和 SHA-256。
- 把产物上传到 Actions Artifacts。

这部分不会启动 Unity，因此不能替代真实编译测试。

## 二、启用真实 Unity 编译测试

Unity 的云端 Editor 运行需要有效许可证。仓库使用 GameCI `unity-test-runner`，目标版本为 `2022.3.42f1`。

在 GitHub 仓库中打开 **Settings > Secrets and variables > Actions**：

1. 在 **Variables** 新建 `UNITY_CI_ENABLED`，值为 `true`。
2. 在 **Secrets** 添加 GameCI 当前要求的 Unity 凭据：
   - `UNITY_LICENSE`
   - `UNITY_EMAIL`
   - `UNITY_PASSWORD`
3. 打开 **Actions > Validate and package > Run workflow** 做一次手动验证。

许可证内容和激活方式可能随 Unity/GameCI 政策变化，请以 GameCI 官方 Licensing 文档为准。不要把 `.ulf`、账号或密码提交到仓库；`.gitignore` 已排除常见许可证文件。

如果暂时不配置许可证，保持变量不存在或值不是 `true`，Unity job 会显示为 skipped；静态校验、打包和 Release 仍可运行。

来自 fork 的 Pull Request 不会获得仓库 Secrets，因此工作流会跳过其 Unity job，避免不可信代码读取凭据。

## 三、自动 Release

发布前必须同步三个位置：

1. `Packages/com.neko.themesplus/package.json` 的 `version`。
2. `Packages/com.neko.themesplus/CHANGELOG.md` 的对应版本标题。
3. 代码中的 fallback 版本；`Validate-Repository.ps1` 会自动检查。

然后创建与版本完全一致的标签：

```powershell
git tag -a v0.4.0 -m "NekoThemesPlus 0.4.0"
git push origin v0.4.0
```

Release 工作流会：

1. 验证标签与 package.json 版本一致。
2. 在已启用 Unity CI 时先运行 EditMode 测试。
3. 构建 `.tgz`、Windows ZIP 和 `SHA256SUMS.txt`。
4. 从 CHANGELOG 提取当前版本说明。
5. 使用仓库自带的 `GITHUB_TOKEN` 创建 GitHub Release 并上传产物。

工作流权限采用最小授权：普通 CI 只有 `contents: read`；Release 只有发布 job 使用 `contents: write`。

## 四、分支保护建议

在 **Settings > Branches > Add branch protection rule** 中保护 `main`：

- Require a pull request before merging。
- Require status checks to pass before merging。
- 选择 `Static validation and package`。
- 已启用 Unity CI 后，再要求 `Unity 2022.3 EditMode tests`。
- 禁止直接 force push 和删除 `main`。

## 五、故障排查

- **Unity job skipped**：确认仓库 Variable `UNITY_CI_ENABLED` 精确为 `true`。
- **许可证失败**：重新生成 Secret，并检查 GameCI Licensing 文档。
- **tag/version mismatch**：标签必须是 `v` 加 package.json 版本，例如 `v0.4.0`。
- **找不到 Shader**：确认 Shader 和 `.meta` 已提交，并检查 Actions 上传的 test artifacts。
- **Release 已存在**：工作流会更新说明并用新构建覆盖同名附件。
