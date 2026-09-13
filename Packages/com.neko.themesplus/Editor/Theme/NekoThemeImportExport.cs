using System;
using System.IO;
using System.Text;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using NekoThemesPlus.Windows;
using UnityEngine;

namespace NekoThemesPlus.Theme
{
    internal static class NekoThemeImportExport
    {
        private const string Format = "NekoThemesPlus.Theme";
        private const int SchemaVersion = 2;
        private const int MinimumSchemaVersion = 1;
        private const long MaximumEmbeddedImageBytes = 64L * 1024L * 1024L;
        private const long MaximumTotalEmbeddedImageBytes = 96L * 1024L * 1024L;
        private const long MaximumThemeFileBytes = 132L * 1024L * 1024L;
        private static string ImportCacheDirectory
        {
            get
            {
                return Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Library",
                    "NekoThemesPlus",
                    "ImportedBackgrounds"));
            }
        }

        [Serializable]
        private sealed class ThemeData
        {
            public string format = Format;
            public int schemaVersion = SchemaVersion;
            public string sourceVersion = NekoThemesPlusConstants.Version;
            public string preset;
            public int backgroundMode;
            public float backgroundZoom;
            public Vector2 backgroundAlignment;
            public float backgroundOpacity;
            public float blurAmount;
            public float brightness;
            public float saturation;
            public float contrast;
            public Color backgroundTint;
            public Color panelTint;
            public float globalPanelOpacity;
            public float hierarchyOpacity;
            public float inspectorOpacity;
            public float projectOpacity;
            public float consoleOpacity;
            public float sceneOpacity;
            public float gameOpacity;
            public Color accentColor;
            public Color selectionColor;
            public float selectionOpacity;
            public bool enableHierarchy;
            public bool enableInspector;
            public bool enableProject;
            public bool enableConsole;
            public bool enableSceneView;
            public bool enableGameView;
            public bool enableDockChrome;
            public Color borderColor;
            public float borderOpacity;
            public string backgroundExtension;
            public string backgroundBase64;
            public string hierarchyBackgroundExtension;
            public string hierarchyBackgroundBase64;
            public string inspectorBackgroundExtension;
            public string inspectorBackgroundBase64;
            public string projectBackgroundExtension;
            public string projectBackgroundBase64;
            public string consoleBackgroundExtension;
            public string consoleBackgroundBase64;
        }

        public static bool Export(string path, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
                if (!ValidateTotalImageSize(settings, out message))
                {
                    return false;
                }

                ThemeData data = Capture(settings);
                if (!TryEmbedBackground(settings.backgroundPath, data, out message))
                {
                    return false;
                }

                if (!TryEmbedImage(settings.hierarchyBackgroundPath, out data.hierarchyBackgroundExtension, out data.hierarchyBackgroundBase64, out message) ||
                    !TryEmbedImage(settings.inspectorBackgroundPath, out data.inspectorBackgroundExtension, out data.inspectorBackgroundBase64, out message) ||
                    !TryEmbedImage(settings.projectBackgroundPath, out data.projectBackgroundExtension, out data.projectBackgroundBase64, out message) ||
                    !TryEmbedImage(settings.consoleBackgroundPath, out data.consoleBackgroundExtension, out data.consoleBackgroundBase64, out message))
                {
                    return false;
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json, new UTF8Encoding(false));
                message = NekoThemesPlusLocalization.Text("主题已导出：", "Theme exported: ") + path;
                return true;
            }
            catch (Exception exception)
            {
                message = NekoThemesPlusLocalization.Text("主题导出失败：", "Theme export failed: ") + exception.Message;
                return false;
            }
        }

        public static bool Import(string path, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                message = NekoThemesPlusLocalization.Text("找不到主题文件。", "Theme file was not found.");
                return false;
            }

            try
            {
                FileInfo file = new FileInfo(path);
                if (file.Length > MaximumThemeFileBytes)
                {
                    message = NekoThemesPlusLocalization.Text("主题文件过大，已拒绝导入。", "The theme file is too large to import safely.");
                    return false;
                }

                ThemeData data = JsonUtility.FromJson<ThemeData>(File.ReadAllText(path, Encoding.UTF8));
                if (data == null || data.format != Format ||
                    data.schemaVersion < MinimumSchemaVersion || data.schemaVersion > SchemaVersion)
                {
                    message = NekoThemesPlusLocalization.Text("不是受支持的 NekoThemesPlus 主题文件。", "This is not a supported NekoThemesPlus theme file.");
                    return false;
                }

                string importedBackground;
                if (!TryRestoreBackground(data, out importedBackground, out message))
                {
                    return false;
                }


                string importedHierarchy = string.Empty;
                string importedInspector = string.Empty;
                string importedProject = string.Empty;
                string importedConsole = string.Empty;
                if (data.schemaVersion >= 2 &&
                    (!TryRestoreImage(data.hierarchyBackgroundExtension, data.hierarchyBackgroundBase64, out importedHierarchy, out message) ||
                     !TryRestoreImage(data.inspectorBackgroundExtension, data.inspectorBackgroundBase64, out importedInspector, out message) ||
                     !TryRestoreImage(data.projectBackgroundExtension, data.projectBackgroundBase64, out importedProject, out message) ||
                     !TryRestoreImage(data.consoleBackgroundExtension, data.consoleBackgroundBase64, out importedConsole, out message)))
                {
                    return false;
                }

                NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
                string oldBackground = settings.backgroundPath;
                string oldHierarchy = settings.hierarchyBackgroundPath;
                string oldInspector = settings.inspectorBackgroundPath;
                string oldProject = settings.projectBackgroundPath;
                string oldConsole = settings.consoleBackgroundPath;
                Apply(data, settings);
                settings.currentPreset = "Custom";
                settings.SaveSettings();

                if (!string.IsNullOrEmpty(importedBackground))
                {
                    if (!BackgroundManager.SetBackground(importedBackground))
                    {
                        message = BackgroundManager.LastError;
                        return false;
                    }
                }
                else
                {
                    BackgroundManager.ClearBackground();
                }

                if (!ApplyWindowBackground(WindowKind.Hierarchy, importedHierarchy, out message) ||
                    !ApplyWindowBackground(WindowKind.Inspector, importedInspector, out message) ||
                    !ApplyWindowBackground(WindowKind.Project, importedProject, out message) ||
                    !ApplyWindowBackground(WindowKind.Console, importedConsole, out message))
                {
                    return false;
                }

                DeleteOldImportedBackground(oldBackground, importedBackground);
                DeleteOldImportedBackground(oldHierarchy, importedHierarchy);
                DeleteOldImportedBackground(oldInspector, importedInspector);
                DeleteOldImportedBackground(oldProject, importedProject);
                DeleteOldImportedBackground(oldConsole, importedConsole);
                NekoThemesPlusManager.Refresh();
                message = NekoThemesPlusLocalization.Text("主题已导入并应用。", "Theme imported and applied.");
                return true;
            }
            catch (Exception exception)
            {
                message = NekoThemesPlusLocalization.Text("主题导入失败：", "Theme import failed: ") + exception.Message;
                return false;
            }
        }

        private static ThemeData Capture(NekoThemesPlusSettings settings)
        {
            return new ThemeData
            {
                preset = settings.currentPreset,
                backgroundMode = (int)settings.backgroundMode,
                backgroundZoom = settings.backgroundZoom,
                backgroundAlignment = settings.backgroundAlignment,
                backgroundOpacity = settings.backgroundOpacity,
                blurAmount = settings.blurAmount,
                brightness = settings.brightness,
                saturation = settings.saturation,
                contrast = settings.contrast,
                backgroundTint = settings.backgroundTint,
                panelTint = settings.panelTint,
                globalPanelOpacity = settings.globalPanelOpacity,
                hierarchyOpacity = settings.hierarchyOpacity,
                inspectorOpacity = settings.inspectorOpacity,
                projectOpacity = settings.projectOpacity,
                consoleOpacity = settings.consoleOpacity,
                sceneOpacity = settings.sceneOpacity,
                gameOpacity = settings.gameOpacity,
                accentColor = settings.accentColor,
                selectionColor = settings.selectionColor,
                selectionOpacity = settings.selectionOpacity,
                enableHierarchy = settings.enableHierarchy,
                enableInspector = settings.enableInspector,
                enableProject = settings.enableProject,
                enableConsole = settings.enableConsole,
                enableSceneView = settings.enableSceneView,
                enableGameView = settings.enableGameView,
                enableDockChrome = settings.enableDockChrome,
                borderColor = settings.borderColor,
                borderOpacity = settings.borderOpacity
            };
        }

        private static void Apply(ThemeData data, NekoThemesPlusSettings settings)
        {
            settings.backgroundMode = Enum.IsDefined(typeof(BackgroundImageMode), data.backgroundMode)
                ? (BackgroundImageMode)data.backgroundMode
                : BackgroundImageMode.Fill;
            settings.backgroundZoom = Mathf.Clamp(data.backgroundZoom <= 0f ? 1f : data.backgroundZoom, 1f, 4f);
            settings.backgroundAlignment = new Vector2(
                Mathf.Clamp01(data.backgroundAlignment.x),
                Mathf.Clamp01(data.backgroundAlignment.y));
            settings.backgroundOpacity = Mathf.Clamp01(data.backgroundOpacity);
            settings.blurAmount = Mathf.Clamp(data.blurAmount, 0f, 50f);
            settings.brightness = Mathf.Clamp(data.brightness, 0f, 2f);
            settings.saturation = Mathf.Clamp(data.saturation, 0f, 2f);
            settings.contrast = Mathf.Clamp(data.contrast, 0f, 2f);
            settings.backgroundTint = data.backgroundTint;
            settings.panelTint = data.panelTint;
            settings.globalPanelOpacity = Mathf.Clamp01(data.globalPanelOpacity);
            settings.hierarchyOpacity = Mathf.Clamp01(data.hierarchyOpacity);
            settings.inspectorOpacity = Mathf.Clamp01(data.inspectorOpacity);
            settings.projectOpacity = Mathf.Clamp01(data.projectOpacity);
            settings.consoleOpacity = Mathf.Clamp01(data.consoleOpacity);
            settings.sceneOpacity = Mathf.Clamp01(data.sceneOpacity);
            settings.gameOpacity = Mathf.Clamp01(data.gameOpacity);
            settings.accentColor = data.accentColor;
            settings.selectionColor = data.selectionColor;
            settings.selectionOpacity = Mathf.Clamp01(data.selectionOpacity);
            settings.enableHierarchy = data.enableHierarchy;
            settings.enableInspector = data.enableInspector;
            settings.enableProject = data.enableProject;
            settings.enableConsole = data.enableConsole;
            settings.enableSceneView = data.enableSceneView;
            settings.enableGameView = data.enableGameView;
            settings.enableDockChrome = data.enableDockChrome;
            settings.borderColor = data.borderColor;
            settings.borderOpacity = Mathf.Clamp01(data.borderOpacity);
        }

        private static bool TryEmbedBackground(string path, ThemeData data, out string message)
        {
            return TryEmbedImage(path, out data.backgroundExtension, out data.backgroundBase64, out message);
        }

        private static bool ValidateTotalImageSize(NekoThemesPlusSettings settings, out string message)
        {
            message = string.Empty;
            long total = 0L;
            string[] paths =
            {
                settings.backgroundPath,
                settings.hierarchyBackgroundPath,
                settings.inspectorBackgroundPath,
                settings.projectBackgroundPath,
                settings.consoleBackgroundPath
            };

            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    total += new FileInfo(path).Length;
                }
            }

            if (total <= MaximumTotalEmbeddedImageBytes)
            {
                return true;
            }

            message = NekoThemesPlusLocalization.Text(
                "全部背景图片合计超过 96 MB，无法导出为单个主题。",
                "The combined background images exceed the 96 MB single-theme limit.");
            return false;
        }

        private static bool TryEmbedImage(string path, out string extension, out string base64, out string message)
        {
            extension = string.Empty;
            base64 = string.Empty;
            message = string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }

            if (!File.Exists(path))
            {
                message = NekoThemesPlusLocalization.Text("背景图片不存在，无法导出完整主题。", "A background image is missing, so the complete theme cannot be exported.");
                return false;
            }

            extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            {
                message = NekoThemesPlusLocalization.Text("主题只能内嵌 PNG/JPG/JPEG 背景。", "A theme can only embed PNG/JPG/JPEG backgrounds.");
                return false;
            }

            FileInfo file = new FileInfo(path);
            if (file.Length > MaximumEmbeddedImageBytes)
            {
                message = NekoThemesPlusLocalization.Text("单张背景图片超过 64 MB，无法内嵌。", "A background image exceeds the 64 MB embedding limit.");
                return false;
            }

            base64 = Convert.ToBase64String(File.ReadAllBytes(path));
            return true;
        }

        private static bool TryRestoreBackground(ThemeData data, out string path, out string message)
        {
            return TryRestoreImage(data.backgroundExtension, data.backgroundBase64, out path, out message);
        }

        private static bool TryRestoreImage(string embeddedExtension, string embeddedBase64, out string path, out string message)
        {
            path = string.Empty;
            message = string.Empty;
            if (string.IsNullOrEmpty(embeddedBase64))
            {
                return true;
            }

            string extension = (embeddedExtension ?? string.Empty).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            {
                message = NekoThemesPlusLocalization.Text("主题内的背景格式无效。", "The embedded background format is invalid.");
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(embeddedBase64);
            }
            catch (FormatException)
            {
                message = NekoThemesPlusLocalization.Text("主题内的背景数据已损坏。", "The embedded background data is corrupt.");
                return false;
            }

            if (bytes.Length == 0 || bytes.LongLength > MaximumEmbeddedImageBytes)
            {
                message = NekoThemesPlusLocalization.Text("主题内的背景大小无效。", "The embedded background size is invalid.");
                return false;
            }

            Directory.CreateDirectory(ImportCacheDirectory);
            path = Path.GetFullPath(Path.Combine(ImportCacheDirectory, Guid.NewGuid().ToString("N") + extension));
            File.WriteAllBytes(path, bytes);

            Texture2D probe;
            string loadError;
            if (!BackgroundLoader.TryLoadExternal(path, NekoThemesPlusSettings.instance.maxBackgroundResolution, out probe, out loadError))
            {
                File.Delete(path);
                path = string.Empty;
                message = loadError;
                return false;
            }

            BackgroundLoader.Destroy(probe);
            return true;
        }

        private static bool ApplyWindowBackground(WindowKind kind, string path, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                BackgroundManager.ClearWindowBackground(kind);
                return true;
            }

            if (BackgroundManager.SetWindowBackground(kind, path))
            {
                return true;
            }

            message = BackgroundManager.GetWindowError(kind);
            return false;
        }

        private static void DeleteOldImportedBackground(string oldPath, string currentPath)
        {
            if (string.IsNullOrEmpty(oldPath) || string.Equals(oldPath, currentPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                string cache = Path.GetFullPath(ImportCacheDirectory) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(oldPath);
                if (candidate.StartsWith(cache, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                {
                    File.Delete(candidate);
                }
            }
            catch
            {
                // Cache cleanup is best effort and must never make a successful import fail.
            }
        }
    }
}
