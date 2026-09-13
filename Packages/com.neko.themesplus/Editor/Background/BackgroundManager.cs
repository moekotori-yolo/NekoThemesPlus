using System;
using System.Collections.Generic;
using NekoThemesPlus.Core;
using NekoThemesPlus.Reflection;
using NekoThemesPlus.Rendering;
using NekoThemesPlus.Windows;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Background
{
    public static class BackgroundManager
    {
        private static readonly WindowKind[] OverrideKinds =
        {
            WindowKind.Hierarchy,
            WindowKind.Inspector,
            WindowKind.Project,
            WindowKind.Console
        };

        private sealed class WindowSource : IDisposable
        {
            public Texture2D texture;
            public string error = string.Empty;

            public void Dispose()
            {
                BackgroundLoader.Destroy(texture);
                texture = null;
                error = string.Empty;
            }
        }

        private sealed class WindowRender : IDisposable
        {
            public WindowKind kind;
            public readonly BackgroundProcessor processor = new BackgroundProcessor();
            public Vector2Int size;

            public void Dispose()
            {
                processor.Dispose();
            }
        }

        private static readonly Dictionary<WindowKind, WindowSource> WindowSources =
            new Dictionary<WindowKind, WindowSource>();
        private static readonly Dictionary<int, WindowRender> WindowRenders =
            new Dictionary<int, WindowRender>();

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

        public static int ActiveOverrideCount
        {
            get
            {
                int count = 0;
                foreach (WindowKind kind in OverrideKinds)
                {
                    WindowSource source;
                    if (WindowSources.TryGetValue(kind, out source) && source.texture != null) count++;
                }

                return count;
            }
        }

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

        public static bool SetWindowBackground(WindowKind kind, string path)
        {
            if (!SupportsOverride(kind))
            {
                LastError = NekoThemesPlusLocalization.Text(
                    "该窗口不支持独立背景。",
                    "This window does not support an independent background.");
                return false;
            }

            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            Texture2D candidate;
            string error;
            if (!BackgroundLoader.TryLoadExternal(path, settings.maxBackgroundResolution, out candidate, out error))
            {
                GetOrCreateSource(kind).error = error;
                NekoThemesPlusLogger.WarnOnce(error);
                RaiseChanged();
                return false;
            }

            WindowSource state = GetOrCreateSource(kind);
            BackgroundLoader.Destroy(state.texture);
            state.texture = candidate;
            state.error = string.Empty;
            SetBackgroundPath(settings, kind, path);
            settings.SaveSettings();
            ReleaseWindowCaches(kind);
            RaiseChanged();
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

        public static void ClearWindowBackground(WindowKind kind)
        {
            if (!SupportsOverride(kind))
            {
                return;
            }

            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            SetBackgroundPath(settings, kind, string.Empty);
            settings.SaveSettings();

            WindowSource state;
            if (WindowSources.TryGetValue(kind, out state))
            {
                state.Dispose();
                WindowSources.Remove(kind);
            }

            ReleaseWindowCaches(kind);
            RaiseChanged();
        }

        public static bool ReloadSource()
        {
            ReleaseSource();
            ReleaseAllWindowSources();
            ReleaseAllWindowCaches();

            bool globalLoaded = LoadGlobalSource();
            foreach (WindowKind kind in OverrideKinds)
            {
                LoadWindowSource(kind);
            }

            return globalLoaded;
        }

        public static Texture GetProcessedTexture(WindowKind kind, int windowInstanceId, Rect screenRect, out Rect uv)
        {
            WindowSource source;
            if (SupportsOverride(kind) &&
                WindowSources.TryGetValue(kind, out source) &&
                source.texture != null)
            {
                Texture overrideTexture = GetOrBuildWindowTexture(kind, windowInstanceId, screenRect, source);
                if (overrideTexture != null)
                {
                    uv = new Rect(0f, 0f, 1f, 1f);
                    return overrideTexture;
                }
            }

            uv = BackgroundCoordinateSystem.GetUvRect(screenRect);
            return ProcessedTexture;
        }

        public static string GetBackgroundPath(WindowKind kind)
        {
            return GetBackgroundPath(NekoThemesPlusSettings.instance, kind);
        }

        public static string GetWindowError(WindowKind kind)
        {
            WindowSource source;
            return WindowSources.TryGetValue(kind, out source) ? source.error : string.Empty;
        }

        public static bool HasWindowOverride(WindowKind kind)
        {
            return SupportsOverride(kind) && !string.IsNullOrWhiteSpace(GetBackgroundPath(kind));
        }

        public static void ReleaseWindowCache(int windowInstanceId)
        {
            WindowRender render;
            if (WindowRenders.TryGetValue(windowInstanceId, out render))
            {
                render.Dispose();
                WindowRenders.Remove(windowInstanceId);
            }
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
                LoadGlobalSource();
            }

            EnsureConfiguredWindowSources();

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

            ReleaseAllWindowCaches();
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
            ReleaseAllWindowSources();
            ReleaseAllWindowCaches();
            if (processor != null)
            {
                processor.Dispose();
                processor = null;
            }

            RaiseChanged();
        }

        private static bool LoadGlobalSource()
        {
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

        private static void LoadWindowSource(WindowKind kind)
        {
            string path = GetBackgroundPath(kind);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            WindowSource state = GetOrCreateSource(kind);
            string error;
            if (!BackgroundLoader.TryLoadExternal(path, NekoThemesPlusSettings.instance.maxBackgroundResolution, out state.texture, out error))
            {
                state.error = error;
                NekoThemesPlusLogger.WarnOnce(error);
            }
            else
            {
                state.error = string.Empty;
            }
        }

        private static void EnsureConfiguredWindowSources()
        {
            foreach (WindowKind kind in OverrideKinds)
            {
                if (!HasWindowOverride(kind))
                {
                    continue;
                }

                WindowSource state;
                if (!WindowSources.TryGetValue(kind, out state) || (state.texture == null && string.IsNullOrEmpty(state.error)))
                {
                    LoadWindowSource(kind);
                }
            }
        }

        private static Texture GetOrBuildWindowTexture(
            WindowKind kind,
            int windowInstanceId,
            Rect screenRect,
            WindowSource source)
        {
            Vector2Int physical = DpiUtility.LogicalToPhysicalSize(screenRect.width, screenRect.height);
            Vector2Int requested = LimitOutputSize(
                physical.x,
                physical.y,
                NekoThemesPlusSettings.instance.maxBackgroundResolution);

            WindowRender render;
            if (WindowRenders.TryGetValue(windowInstanceId, out render) && render.kind != kind)
            {
                ReleaseWindowCache(windowInstanceId);
                render = null;
            }

            if (render == null)
            {
                render = new WindowRender { kind = kind };
                WindowRenders[windowInstanceId] = render;
            }

            RenderTexture texture = render.processor.ProcessedTexture;
            bool needsRebuild = texture == null ||
                                Mathf.Abs(render.size.x - requested.x) > 8 ||
                                Mathf.Abs(render.size.y - requested.y) > 8;
            if (needsRebuild)
            {
                string error;
                if (!render.processor.Rebuild(
                    source.texture,
                    requested.x,
                    requested.y,
                    NekoThemesPlusSettings.instance,
                    out error))
                {
                    source.error = error;
                    NekoThemesPlusLogger.WarnOnce(error);
                    ReleaseWindowCache(windowInstanceId);
                    return null;
                }

                render.size = requested;
                source.error = string.Empty;
            }

            return render.processor.ProcessedTexture;
        }

        private static WindowSource GetOrCreateSource(WindowKind kind)
        {
            WindowSource source;
            if (!WindowSources.TryGetValue(kind, out source))
            {
                source = new WindowSource();
                WindowSources.Add(kind, source);
            }

            return source;
        }

        private static bool SupportsOverride(WindowKind kind)
        {
            return kind == WindowKind.Hierarchy ||
                   kind == WindowKind.Inspector ||
                   kind == WindowKind.Project ||
                   kind == WindowKind.Console;
        }

        private static string GetBackgroundPath(NekoThemesPlusSettings settings, WindowKind kind)
        {
            switch (kind)
            {
                case WindowKind.Hierarchy: return settings.hierarchyBackgroundPath;
                case WindowKind.Inspector: return settings.inspectorBackgroundPath;
                case WindowKind.Project: return settings.projectBackgroundPath;
                case WindowKind.Console: return settings.consoleBackgroundPath;
                default: return string.Empty;
            }
        }

        private static void SetBackgroundPath(NekoThemesPlusSettings settings, WindowKind kind, string path)
        {
            switch (kind)
            {
                case WindowKind.Hierarchy: settings.hierarchyBackgroundPath = path; break;
                case WindowKind.Inspector: settings.inspectorBackgroundPath = path; break;
                case WindowKind.Project: settings.projectBackgroundPath = path; break;
                case WindowKind.Console: settings.consoleBackgroundPath = path; break;
            }
        }

        private static void ReleaseWindowCaches(WindowKind kind)
        {
            List<int> remove = null;
            foreach (KeyValuePair<int, WindowRender> pair in WindowRenders)
            {
                if (pair.Value.kind != kind)
                {
                    continue;
                }

                pair.Value.Dispose();
                if (remove == null) remove = new List<int>();
                remove.Add(pair.Key);
            }

            if (remove != null)
            {
                foreach (int instanceId in remove)
                {
                    WindowRenders.Remove(instanceId);
                }
            }
        }

        private static void ReleaseAllWindowCaches()
        {
            foreach (WindowRender render in WindowRenders.Values)
            {
                render.Dispose();
            }

            WindowRenders.Clear();
        }

        private static void ReleaseAllWindowSources()
        {
            foreach (WindowSource source in WindowSources.Values)
            {
                source.Dispose();
            }

            WindowSources.Clear();
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
            return new Vector2Int(
                Mathf.Max(16, Mathf.RoundToInt(width * scale)),
                Mathf.Max(16, Mathf.RoundToInt(height * scale)));
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
