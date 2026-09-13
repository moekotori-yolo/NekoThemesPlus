using System;
using NekoThemesPlus.Core;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace NekoThemesPlus.Updates
{
    internal static class NekoThemesPlusUpdateService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/moekotori-yolo/NekoThemesPlus/releases/latest";
        private const string ReleasesPage = "https://github.com/moekotori-yolo/NekoThemesPlus/releases";
        private const string GitPackageUrl = "https://github.com/moekotori-yolo/NekoThemesPlus.git?path=/Packages/com.neko.themesplus#v";
        private const string LastCheckKey = "NekoThemesPlus.Update.LastCheckUtc";
        private const string CachedReleaseKey = "NekoThemesPlus.Update.CachedRelease";
        private const string EtagKey = "NekoThemesPlus.Update.ETag";
        private const string LastPromptedVersionKey = "NekoThemesPlus.Update.LastPromptedVersion";
        private const string PendingVersionKey = "NekoThemesPlus.Update.PendingVersion";
        private const double AutomaticCheckDelaySeconds = 5d;
        private const double CheckIntervalHours = 24d;

        [Serializable]
        private sealed class GitHubRelease
        {
            public string tag_name;
            public string name;
        }

        private static UnityWebRequest webRequest;
        private static UnityWebRequestAsyncOperation webOperation;
        private static AddRequest addRequest;
        private static double nextAutomaticCheck;
        private static bool initialized;
        private static bool manualCheck;
        private static string latestVersion = string.Empty;
        private static string status = string.Empty;

        public static event Action StatusChanged;

        public static bool IsChecking { get { return webRequest != null; } }
        public static bool IsInstalling { get { return addRequest != null; } }
        public static string LatestVersion { get { return latestVersion; } }
        public static string InstallationSource
        {
            get
            {
                UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(NekoThemesPlusUpdateService).Assembly);
                return package != null ? package.source.ToString() : T("未知", "Unknown");
            }
        }
        public static string Status
        {
            get
            {
                return string.IsNullOrEmpty(status)
                    ? T("尚未检查更新", "Updates have not been checked yet")
                    : status;
            }
        }

        public static bool HasUpdate
        {
            get { return IsNewerVersion(latestVersion, NekoThemesPlusConstants.Version); }
        }

        public static void Initialize()
        {
            if (initialized || Application.isBatchMode)
            {
                return;
            }

            initialized = true;
            nextAutomaticCheck = EditorApplication.timeSinceStartup + AutomaticCheckDelaySeconds;
            EditorApplication.update += Update;
            ReportCompletedUpdateAfterReload();
        }

        public static void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            EditorApplication.update -= Update;
            initialized = false;
            DisposeWebRequest();
            addRequest = null;
        }

        [MenuItem("Tools/Neko Themes Plus/检查更新", priority = 2004)]
        public static void CheckNowFromMenu()
        {
            CheckNow(true);
        }

        public static void CheckNow(bool interactive)
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (IsChecking || IsInstalling)
            {
                SetStatus(T("更新操作正在进行中…", "An update operation is already in progress…"));
                return;
            }

            manualCheck = interactive;
            SetStatus(T("正在检查 GitHub Release…", "Checking GitHub Releases…"));
            webRequest = UnityWebRequest.Get(LatestReleaseApi);
            webRequest.timeout = 15;
            webRequest.SetRequestHeader("Accept", "application/vnd.github+json");
            webRequest.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
            webRequest.SetRequestHeader("User-Agent", "NekoThemesPlus/" + NekoThemesPlusConstants.Version);
            string etag = EditorPrefs.GetString(EtagKey, string.Empty);
            if (!string.IsNullOrEmpty(etag))
            {
                webRequest.SetRequestHeader("If-None-Match", etag);
            }

            webOperation = webRequest.SendWebRequest();
        }

        public static void InstallLatest()
        {
            if (!HasUpdate || IsInstalling)
            {
                return;
            }

            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(NekoThemesPlusUpdateService).Assembly);
            if (package == null)
            {
                ShowManualUpdate(T("无法识别当前包的安装来源。", "The current package source could not be identified."));
                return;
            }

            if (package.source == PackageSource.Embedded || package.source == PackageSource.Local)
            {
                ShowManualUpdate(T(
                    "当前是源码嵌入 / 本地开发模式。为保护本地修改，插件不会自动覆盖源码。",
                    "This is an embedded/local development package. Automatic replacement is disabled to protect local changes."));
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SetStatus(T("已取消更新：场景尚未保存。", "Update cancelled because scene saving was cancelled."));
                return;
            }

            AssetDatabase.SaveAssets();
            string target = latestVersion;
            SessionState.SetString(PendingVersionKey, target);
            SetStatus(T("正在通过 Unity Package Manager 更新到 v", "Updating through Unity Package Manager to v") + target + "…");

            try
            {
                addRequest = Client.Add(GitPackageUrl + target);
            }
            catch (Exception exception)
            {
                SessionState.EraseString(PendingVersionKey);
                addRequest = null;
                HandleInstallFailure(exception.Message);
            }
        }

        public static void OpenReleasesPage()
        {
            Application.OpenURL(ReleasesPage);
        }

        internal static bool IsNewerVersion(string candidate, string current)
        {
            Version candidateVersion;
            Version currentVersion;
            return TryParseStableVersion(candidate, out candidateVersion) &&
                   TryParseStableVersion(current, out currentVersion) &&
                   candidateVersion > currentVersion;
        }

        private static void Update()
        {
            if (webRequest != null)
            {
                PollReleaseRequest();
            }

            if (addRequest != null)
            {
                PollAddRequest();
            }

            if (!NekoThemesPlusSettings.instance.automaticallyCheckForUpdates ||
                IsChecking || IsInstalling || EditorApplication.timeSinceStartup < nextAutomaticCheck)
            {
                return;
            }

            // If the previous check is still fresh, re-evaluate in one hour so an
            // Editor session that remains open for days still checks every 24 hours.
            nextAutomaticCheck = EditorApplication.timeSinceStartup + TimeSpan.FromHours(1d).TotalSeconds;
            if (ShouldRunAutomaticCheck())
            {
                CheckNow(false);
            }
        }

        private static void PollReleaseRequest()
        {
            if (webOperation == null || !webOperation.isDone)
            {
                return;
            }

            bool wasManual = manualCheck;
            string json = string.Empty;
            long responseCode = webRequest.responseCode;
            if (responseCode == 304)
            {
                json = EditorPrefs.GetString(CachedReleaseKey, string.Empty);
            }
            else if (webRequest.result == UnityWebRequest.Result.Success)
            {
                json = webRequest.downloadHandler.text;
                string etag = webRequest.GetResponseHeader("ETag");
                if (!string.IsNullOrEmpty(etag)) EditorPrefs.SetString(EtagKey, etag);
            }
            else
            {
                string error = string.IsNullOrEmpty(webRequest.error) ? "HTTP " + responseCode : webRequest.error;
                DisposeWebRequest();
                SetStatus(T("检查更新失败：", "Update check failed: ") + error);
                return;
            }

            DisposeWebRequest();
            GitHubRelease release;
            try
            {
                release = JsonUtility.FromJson<GitHubRelease>(json);
            }
            catch (Exception exception)
            {
                SetStatus(T("Release 数据无法解析：", "Release data could not be parsed: ") + exception.Message);
                return;
            }

            string version = NormalizeTag(release != null ? release.tag_name : string.Empty);
            Version parsed;
            if (!TryParseStableVersion(version, out parsed))
            {
                SetStatus(T("GitHub Release 版本号无效。", "The GitHub Release version is invalid."));
                return;
            }

            latestVersion = version;
            EditorPrefs.SetString(LastCheckKey, DateTime.UtcNow.Ticks.ToString());
            EditorPrefs.SetString(CachedReleaseKey, JsonUtility.ToJson(release));
            nextAutomaticCheck = EditorApplication.timeSinceStartup + TimeSpan.FromHours(CheckIntervalHours).TotalSeconds;
            if (HasUpdate)
            {
                SetStatus(T("发现新版本 v", "New version available: v") + latestVersion);
                OfferUpdate(wasManual, release != null ? release.name : string.Empty);
            }
            else
            {
                SetStatus(T("已是最新版 v", "You are up to date: v") + NekoThemesPlusConstants.Version);
            }
        }

        private static void PollAddRequest()
        {
            if (!addRequest.IsCompleted)
            {
                return;
            }

            if (addRequest.Status == StatusCode.Success)
            {
                string installed = addRequest.Result != null ? addRequest.Result.version : latestVersion;
                addRequest = null;
                SessionState.EraseString(PendingVersionKey);
                SetStatus(T("已更新到 v", "Updated to v") + installed + T("，Unity 正在重新编译。", ". Unity is recompiling."));
            }
            else
            {
                string error = addRequest.Error != null ? addRequest.Error.message : T("未知错误", "Unknown error");
                addRequest = null;
                SessionState.EraseString(PendingVersionKey);
                HandleInstallFailure(error);
            }
        }

        private static void OfferUpdate(bool forcePrompt, string releaseName)
        {
            if (!forcePrompt && EditorPrefs.GetString(LastPromptedVersionKey, string.Empty) == latestVersion)
            {
                return;
            }

            EditorPrefs.SetString(LastPromptedVersionKey, latestVersion);
            string title = T("NekoThemesPlus 更新", "NekoThemesPlus Update");
            string displayName = string.IsNullOrWhiteSpace(releaseName) ? "v" + latestVersion : releaseName;
            string message = T(
                "发现新版本：" + displayName + "\n当前版本：v" + NekoThemesPlusConstants.Version +
                "\n\n更新前请保存工程。确认后会先提示保存未保存场景，再由 Unity Package Manager 安装 GitHub 标签版本并触发安全重编译。",
                "A new version is available: " + displayName + "\nCurrent version: v" + NekoThemesPlusConstants.Version +
                "\n\nSave the project before updating. Unity will prompt for unsaved scenes, install the tagged GitHub package through Package Manager, and recompile safely.");
            int choice = EditorUtility.DisplayDialogComplex(
                title,
                message,
                T("保存并更新", "Save and Update"),
                T("稍后", "Later"),
                T("查看 Release", "View Release"));
            if (choice == 0)
            {
                InstallLatest();
            }
            else if (choice == 2)
            {
                OpenReleasesPage();
            }
        }

        private static void ShowManualUpdate(string reason)
        {
            SetStatus(reason);
            bool open = EditorUtility.DisplayDialog(
                T("需要手动更新", "Manual Update Required"),
                reason + T(
                    "\n\n请先提交或备份本地修改，再从 Release 页面更新。",
                    "\n\nCommit or back up local changes, then update from the Releases page."),
                T("打开 Release", "Open Releases"),
                T("取消", "Cancel"));
            if (open) OpenReleasesPage();
        }

        private static void HandleInstallFailure(string error)
        {
            SetStatus(T("自动更新失败：", "Automatic update failed: ") + error);
            bool open = EditorUtility.DisplayDialog(
                T("NekoThemesPlus 更新失败", "NekoThemesPlus Update Failed"),
                Status + T("\n\n可以打开 Release 页面手动安装。", "\n\nYou can install manually from the Releases page."),
                T("打开 Release", "Open Releases"),
                T("关闭", "Close"));
            if (open) OpenReleasesPage();
        }

        private static void ReportCompletedUpdateAfterReload()
        {
            string pending = SessionState.GetString(PendingVersionKey, string.Empty);
            if (string.IsNullOrEmpty(pending))
            {
                return;
            }

            if (!IsNewerVersion(pending, NekoThemesPlusConstants.Version))
            {
                SessionState.EraseString(PendingVersionKey);
                latestVersion = NekoThemesPlusConstants.Version;
                SetStatus(T("更新完成，当前版本 v", "Update complete. Current version: v") + NekoThemesPlusConstants.Version);
            }
        }

        private static bool ShouldRunAutomaticCheck()
        {
            string ticksText = EditorPrefs.GetString(LastCheckKey, string.Empty);
            long ticks;
            if (!long.TryParse(ticksText, out ticks))
            {
                return true;
            }

            try
            {
                return DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) >= TimeSpan.FromHours(CheckIntervalHours);
            }
            catch
            {
                return true;
            }
        }

        private static bool TryParseStableVersion(string value, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf('-') >= 0 || value.IndexOf('+') >= 0)
            {
                return false;
            }

            string normalized = NormalizeTag(value);
            string[] parts = normalized.Split('.');
            int major;
            int minor;
            int patch;
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out major) ||
                !int.TryParse(parts[1], out minor) ||
                !int.TryParse(parts[2], out patch) ||
                major < 0 || minor < 0 || patch < 0)
            {
                return false;
            }

            version = new Version(major, minor, patch);
            return true;
        }

        private static string NormalizeTag(string tag)
        {
            string value = (tag ?? string.Empty).Trim();
            return value.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? value.Substring(1) : value;
        }

        private static void DisposeWebRequest()
        {
            if (webRequest != null)
            {
                webRequest.Dispose();
            }

            webRequest = null;
            webOperation = null;
            manualCheck = false;
        }

        private static void SetStatus(string value)
        {
            status = value;
            if (StatusChanged != null)
            {
                StatusChanged.Invoke();
            }
        }

        private static string T(string simplifiedChinese, string english)
        {
            return NekoThemesPlusLocalization.Text(simplifiedChinese, english);
        }
    }
}
