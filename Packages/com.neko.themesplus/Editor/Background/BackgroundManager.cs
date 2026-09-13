using System;
using NekoThemesPlus.Core;
using NekoThemesPlus.Reflection;
using NekoThemesPlus.Rendering;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Background
{
    public static class BackgroundManager
    {
        private static Texture2D sourceTexture;
        private static BackgroundProcessor processor;
        private static bool rebuildQueued;
        private static bool monitoringWindow;
        private static double nextWindowCheck;
        private static Rect lastMainWindowRect;

        public static event Action BackgroundChanged;

        public static Texture2D SourceTexture { get { return sourceTexture; } }
        public static RenderTexture ProcessedTexture { get { return processor != null ? processor.ProcessedTexture : null; } }
        public static string LastError { get; private set; }

        public static Vector2Int ProcessedResolution
        {
            get
            {
                RenderTexture texture = ProcessedTexture;
                return texture != null ? new Vector2Int(texture.width, texture.height) : Vector2Int.zero;
            }
        }

        public static void Initialize()
        {
            if (processor == null)
            {
                processor = new BackgroundProcessor();
            }

            if (!Application.isBatchMode)
            {
                StartWindowMonitoring();
            }
            ReloadSource();
            Rebuild();
        }

        public static bool SetBackground(string path)
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            Texture2D candidate;
            string error;
            if (!BackgroundLoader.TryLoadExternal(path, settings.maxBackgroundResolution, out candidate, out error))
            {
                LastError = error;
                NekoThemesPlusLogger.WarnOnce(error);
                RaiseChanged();
                return false;
            }

            ReleaseSource();
            sourceTexture = candidate;
            settings.backgroundPath = path;
            settings.SaveSettings();
            LastError = string.Empty;
            Rebuild();
            return true;
        }

        public static void ClearBackground()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            settings.backgroundPath = string.Empty;
            settings.SaveSettings();
            ReleaseSource();
            LastError = string.Empty;
            if (processor != null)
            {
                processor.Rebuild(null, 16, 16, settings, out _);
            }

            RaiseChanged();
        }

        public static bool ReloadSource()
        {
            ReleaseSource();
            string path = NekoThemesPlusSettings.instance.backgroundPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                LastError = string.Empty;
                return false;
            }

            string error;
            if (!BackgroundLoader.TryLoadExternal(path, NekoThemesPlusSettings.instance.maxBackgroundResolution, out sourceTexture, out error))
            {
                LastError = error;
                NekoThemesPlusLogger.WarnOnce(error);
                return false;
            }

            LastError = string.Empty;
            return true;
        }

        public static void QueueRebuild()
        {
            if (rebuildQueued)
            {
                return;
            }

            rebuildQueued = true;
            EditorApplication.delayCall += RebuildQueued;
        }

        public static void Rebuild()
        {
            if (rebuildQueued)
            {
                EditorApplication.delayCall -= RebuildQueued;
            }

            rebuildQueued = false;
            if (processor == null)
            {
                processor = new BackgroundProcessor();
            }

            if (sourceTexture == null && !string.IsNullOrWhiteSpace(NekoThemesPlusSettings.instance.backgroundPath))
            {
                ReloadSource();
            }

            Rect mainRect;
            if (!MainWindowReflection.TryGetMainWindowRect(out mainRect))
            {
                mainRect = new Rect(0f, 0f, 1920f, 1080f);
            }

            lastMainWindowRect = mainRect;

            Vector2Int physicalSize = DpiUtility.LogicalToPhysicalSize(mainRect.width, mainRect.height);
            Vector2Int size = LimitOutputSize(physicalSize.x, physicalSize.y, NekoThemesPlusSettings.instance.maxBackgroundResolution);
            string error;
            if (!processor.Rebuild(sourceTexture, size.x, size.y, NekoThemesPlusSettings.instance, out error))
            {
                LastError = error;
                NekoThemesPlusLogger.WarnOnce(error);
            }
            else if (sourceTexture != null)
            {
                LastError = string.Empty;
            }

            RaiseChanged();
        }

        public static void Dispose()
        {
            if (rebuildQueued)
            {
                EditorApplication.delayCall -= RebuildQueued;
                rebuildQueued = false;
            }

            StopWindowMonitoring();

            ReleaseSource();
            if (processor != null)
            {
                processor.Dispose();
                processor = null;
            }

            RaiseChanged();
        }

        private static void RebuildQueued()
        {
            rebuildQueued = false;
            Rebuild();
        }

        private static void StartWindowMonitoring()
        {
            if (monitoringWindow)
            {
                return;
            }

            monitoringWindow = true;
            nextWindowCheck = 0d;
            EditorApplication.update += MonitorMainWindowSize;
        }

        private static void StopWindowMonitoring()
        {
            if (!monitoringWindow)
            {
                return;
            }

            monitoringWindow = false;
            EditorApplication.update -= MonitorMainWindowSize;
        }

        private static void MonitorMainWindowSize()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < nextWindowCheck)
            {
                return;
            }

            nextWindowCheck = now + 0.75d;
            Rect current;
            if (!MainWindowReflection.TryGetMainWindowRect(out current))
            {
                return;
            }

            if (Mathf.Abs(current.width - lastMainWindowRect.width) > 0.5f ||
                Mathf.Abs(current.height - lastMainWindowRect.height) > 0.5f)
            {
                lastMainWindowRect = current;
                QueueRebuild();
            }
        }

        private static Vector2Int LimitOutputSize(int width, int height, int maximum)
        {
            width = Mathf.Max(16, width);
            height = Mathf.Max(16, height);
            maximum = Mathf.Clamp(maximum, 512, 8192);
            int largest = Mathf.Max(width, height);
            if (largest <= maximum)
            {
                return new Vector2Int(width, height);
            }

            float scale = maximum / (float)largest;
            return new Vector2Int(Mathf.Max(16, Mathf.RoundToInt(width * scale)), Mathf.Max(16, Mathf.RoundToInt(height * scale)));
        }

        private static void ReleaseSource()
        {
            BackgroundLoader.Destroy(sourceTexture);
            sourceTexture = null;
        }

        private static void RaiseChanged()
        {
            if (BackgroundChanged != null)
            {
                BackgroundChanged.Invoke();
            }
        }
    }
}
