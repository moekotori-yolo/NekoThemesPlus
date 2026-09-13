# 第一次上传 GitHub 与后续发版

## 一、上传前只需改一个占位符

运行下面的脚本，把 `OWNER` 替换为你的 GitHub 用户名或组织名。脚本会同步 README、Issue 安全入口和本文命令：

```powershell
./Scripts/Set-GitHubOwner.ps1 -Owner 你的GitHub用户名
```

如果仓库名称不是 `NekoThemesPlus`，同时传入 `-Repository 仓库名`。

建议仓库名保持 `NekoThemesPlus`。

## 二、在 GitHub 新建空仓库

1. 登录 GitHub，点击 **New repository**。
2. Repository name 填 `NekoThemesPlus`。
3. 选择 Public。
4. 不要勾选自动生成 README、LICENSE 或 `.gitignore`，本地已经具备。
5. 创建仓库并复制远程地址。

## 三、首次提交和推送

先在仓库根目录运行：

```powershell
./Scripts/Validate-Repository.ps1
./Build-Release.ps1
git status
```

检查无误后执行；把远程地址替换成你自己的：

```powershell
git add .
git commit -m "feat: publish NekoThemesPlus 0.2.0"
git branch -M main
git remote add origin https://github.com/moekotori-yolo/NekoThemesPlus.git
git push -u origin main
```

如果已经存在 `origin`，用 `git remote set-url origin ...` 更新，不要再次 `remote add`。

## 四、发布 V0.2.0

确认 `main` 的 Actions 通过后：

```powershell
git tag -a v0.2.0 -m "NekoThemesPlus 0.2.0"
git push origin v0.2.0
```

标签推送后，GitHub Actions 会自动创建 Release，并附带：

- `com.neko.themesplus-0.2.0.tgz`
- `NekoThemesPlus-0.2.0-Windows.zip`
- `SHA256SUMS.txt`

不要把本地 `Dist` 提交进 Git；它已经写入 `.gitignore`，正式二进制产物由 Releases 和 Actions Artifacts 保存。

## 五、以后发布新版本

以 `0.3.0` 为例：

1. 修改 package.json 版本号为 `0.3.0`。
2. 更新代码 fallback 版本和设置窗口显示；静态检查会指出未同步位置。
3. 在 CHANGELOG 顶部增加 `## [0.3.0] - YYYY-MM-DD`。
4. 运行完整本地构建并提交。
5. 创建和推送 `v0.3.0` 标签。

不要移动已经公开的版本标签。如果旧 Release 有问题，发布补丁版本，例如 `0.2.1`。

## 六、GitHub 仓库设置建议

- **About**：`Unity 2022.3 editor wallpaper and glass theme plugin`。
- **Topics**：`unity`、`unity-editor`、`theme`、`wallpaper`、`upm-package`、`windows`。
- 开启 Issues 和 Discussions。
- 在 Security 中开启 Private vulnerability reporting。
- 按 [CI 指南](CI.zh-CN.md) 配置 Actions 和分支保护。
