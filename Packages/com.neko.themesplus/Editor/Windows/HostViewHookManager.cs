using System;
using System.Collections.Generic;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using NekoThemesPlus.Reflection;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Windows
{
    /// <summary>
    /// Wraps the host's window draw delegate so IMGUI backdrop alpha is applied after Unity's
    /// ResetGUIState and before the window paints. VisualElement injection remains as a fallback
    /// for UI Toolkit views; this hook is the reliable path for legacy editor windows.
    /// </summary>
    internal static class HostViewHookManager
    {
        private const double ScanInterval = 0.5d;
        private const double LayoutBroadcastInterval = 1d / 60d;
        private static readonly Dictionary<int, ContentHook> ContentHooks = new Dictionary<int, ContentHook>();
        private static readonly Dictionary<int, ChromeHook> ChromeHooks = new Dictionary<int, ChromeHook>();
        private static readonly List<int> StaleIds = new List<int>();
        private static bool initialized;
        private static double nextScan;
        private static double lastLayoutBroadcast;

        public static int HookedCount { get { return ContentHooks.Count; } }
        public static int ChromeHookedCount { get { return ChromeHooks.Count; } }
        public static bool IsAvailable { get { return HostViewBridge.IsAvailable; } }
        public static string UnavailableReason { get { return HostViewBridge.UnavailableReason; } }

        public static void Initialize()
        {
            if (initialized || Application.isBatchMode)
            {
                return;
            }

            initialized = true;
            nextScan = 0d;
            EditorApplication.update += Update;
            BackgroundManager.BackgroundChanged += RepaintHookedWindows;
            EditorApplication.delayCall += Refresh;
        }

        public static void Refresh()
        {
            if (!initialized || Application.isBatchMode || !HostViewBridge.IsAvailable)
            {
                return;
            }

            PruneDeadHooks(ContentHooks);
            PruneDeadHooks(ChromeHooks);
            foreach (ScriptableObject hostView in HostViewBridge.GetHostViews())
            {
                AttachContent(hostView);
                AttachChrome(hostView);
            }
        }

        public static IEnumerable<string> DescribeHooks()
        {
            foreach (ContentHook hook in ContentHooks.Values)
            {
                yield return hook.Describe();
            }
        }

        public static void Shutdown()
        {
            if (initialized)
            {
                EditorApplication.update -= Update;
                EditorApplication.delayCall -= Refresh;
                BackgroundManager.BackgroundChanged -= RepaintHookedWindows;
            }

            initialized = false;
            foreach (ChromeHook hook in ChromeHooks.Values)
            {
                hook.Detach();
            }

            foreach (ContentHook hook in ContentHooks.Values)
            {
                hook.Detach();
            }

            ChromeHooks.Clear();
            ContentHooks.Clear();
            StaleIds.Clear();
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < nextScan)
            {
                return;
            }

            nextScan = now + ScanInterval;
            Refresh();
        }

        private static void AttachContent(ScriptableObject hostView)
        {
            if (hostView == null)
            {
                return;
            }

            EditorWindow window = HostViewBridge.GetActualView(hostView);
            WindowKind kind = UnityInternalTypes.Classify(window != null ? window.GetType() : null);
            if (!IsContentKind(kind))
            {
                return;
            }

            Delegate current = HostViewBridge.GetOnGui(hostView);
            if (current == null)
            {
                return;
            }

            int id = hostView.GetInstanceID();
            ContentHook existing;
            if (ContentHooks.TryGetValue(id, out existing))
            {
                if (existing.IsInstalled(current))
                {
                    return;
                }

                ContentHooks.Remove(id);
            }

            ContentHook hook = ContentHook.Create(hostView, current);
            if (hook != null)
            {
                ContentHooks[id] = hook;
            }
        }

        private static void AttachChrome(ScriptableObject hostView)
        {
            if (hostView == null)
            {
                return;
            }

            EditorWindow window = HostViewBridge.GetActualView(hostView);
            WindowKind kind = UnityInternalTypes.Classify(window != null ? window.GetType() : null);
            if (!IsChromeKind(kind))
            {
                return;
            }

            UnityEngine.UIElements.IMGUIContainer container = HostViewBridge.GetChromeContainer(hostView);
            if (container == null || container.onGUIHandler == null)
            {
                return;
            }

            int id = hostView.GetInstanceID();
            ChromeHook existing;
            if (ChromeHooks.TryGetValue(id, out existing))
            {
                if (existing.IsInstalledOn(container))
                {
                    return;
                }

                ChromeHooks.Remove(id);
            }

            ChromeHook hook = ChromeHook.Create(hostView, container);
            if (hook != null)
            {
                ChromeHooks[id] = hook;
            }
        }

        private static void PruneDeadHooks<T>(Dictionary<int, T> hooks) where T : class, IHostHook
        {
            StaleIds.Clear();
            foreach (KeyValuePair<int, T> pair in hooks)
            {
                if (!pair.Value.IsAlive)
                {
                    StaleIds.Add(pair.Key);
                }
            }

            foreach (int id in StaleIds)
            {
                hooks.Remove(id);
            }
        }

        private static void RepaintHookedWindows()
        {
            foreach (ContentHook hook in ContentHooks.Values)
            {
                hook.Repaint();
            }
        }

        private static void BroadcastLayoutChange(ScriptableObject origin)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - lastLayoutBroadcast < LayoutBroadcastInterval)
            {
                return;
            }

            lastLayoutBroadcast = now;
            foreach (ContentHook hook in ContentHooks.Values)
            {
                if (!hook.IsHost(origin))
                {
                    hook.Repaint();
                }
            }
        }

        private static float GetOpacity(NekoThemesPlusSettings settings, WindowKind kind)
        {
            switch (kind)
            {
                case WindowKind.Hierarchy: return settings.hierarchyOpacity;
                case WindowKind.Inspector: return settings.inspectorOpacity;
                case WindowKind.Project: return settings.projectOpacity;
                case WindowKind.Console: return settings.consoleOpacity;
                default: return 1f;
            }
        }

        private static bool IsContentKind(WindowKind kind)
        {
            return kind == WindowKind.Hierarchy || kind == WindowKind.Inspector ||
                   kind == WindowKind.Project || kind == WindowKind.Console;
        }

        private static bool IsChromeKind(WindowKind kind)
        {
            return IsContentKind(kind) || kind == WindowKind.SceneView || kind == WindowKind.GameView;
        }

        private interface IHostHook
        {
            bool IsAlive { get; }
        }

        private sealed class ContentHook : IHostHook
        {
            private readonly ScriptableObject hostView;
            private readonly Delegate original;
            private readonly Action invokeOriginal;
            private Delegate installed;
            private int drawCount;
            private Rect lastScreenRect;

            private ContentHook(ScriptableObject hostView, Delegate original, Action invokeOriginal)
            {
                this.hostView = hostView;
                this.original = original;
                this.invokeOriginal = invokeOriginal;
            }

            public bool IsAlive { get { return hostView != null; } }

            public static ContentHook Create(ScriptableObject hostView, Delegate original)
            {
                Action action = AsAction(original);
                Type delegateType = HostViewBridge.OnGuiDelegateType;
                if (action == null || delegateType == null)
                {
                    return null;
                }

                ContentHook hook = new ContentHook(hostView, original, action);
                try
                {
                    hook.installed = Delegate.CreateDelegate(delegateType, hook, "Invoke");
                    return HostViewBridge.SetOnGui(hostView, hook.installed) ? hook : null;
                }
                catch
                {
                    return null;
                }
            }

            public bool IsInstalled(Delegate current)
            {
                return ReferenceEquals(current, installed);
            }

            public void Detach()
            {
                if (hostView != null && ReferenceEquals(HostViewBridge.GetOnGui(hostView), installed))
                {
                    HostViewBridge.SetOnGui(hostView, original);
                }
            }

            public void Repaint()
            {
                EditorWindow window = HostViewBridge.GetActualView(hostView);
                if (window != null)
                {
                    window.Repaint();
                }
            }

            public bool IsHost(ScriptableObject candidate)
            {
                return ReferenceEquals(hostView, candidate);
            }

            public string Describe()
            {
                EditorWindow window = HostViewBridge.GetActualView(hostView);
                string title = window != null && window.titleContent != null ? window.titleContent.text : "<none>";
                return title + " - draws: " + drawCount;
            }

            public void Invoke()
            {
                drawCount++;
                EditorWindow window = HostViewBridge.GetActualView(hostView);
                WindowKind kind = UnityInternalTypes.Classify(window != null ? window.GetType() : null);
                bool themed = window != null && IsContentKind(kind) && WindowHookManager.ShouldAttach(kind);
                Color previousBackground = GUI.backgroundColor;
                bool changedGuiColor = false;

                if (themed && Event.current != null && Event.current.type == EventType.Repaint)
                {
                    try
                    {
                        DrawBackground(window);
                        NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
                        // GUI.backgroundColor is multiplicative. Feeding a dark panel colour
                        // directly would crush the UI to near-black, so convert it into a gentle
                        // colour multiplier while keeping opacity as the true glass strength.
                        Color requested = settings.panelTint;
                        Color tint = Color.Lerp(Color.white, new Color(
                            Mathf.Clamp01(requested.r * 2f),
                            Mathf.Clamp01(requested.g * 2f),
                            Mathf.Clamp01(requested.b * 2f),
                            1f), 0.35f);
                        tint.a = Mathf.Clamp01(settings.globalPanelOpacity * GetOpacity(settings, kind));
                        GUI.backgroundColor = previousBackground * tint;
                        changedGuiColor = true;
                    }
                    catch (Exception exception)
                    {
                        NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                            "HostView 背景绘制失败：",
                            "HostView background painting failed: ") + exception.Message);
                    }
                }

                try
                {
                    invokeOriginal.Invoke();
                }
                finally
                {
                    if (changedGuiColor)
                    {
                        GUI.backgroundColor = previousBackground;
                    }
                }
            }

            private void DrawBackground(EditorWindow window)
            {
                Texture texture = BackgroundManager.ProcessedTexture;
                if (texture == null)
                {
                    return;
                }

                Rect localRect = new Rect(0f, 0f, window.position.width, window.position.height);
                Vector2 screenPoint = GUIUtility.GUIToScreenPoint(Vector2.zero);
                Rect screenRect = new Rect(screenPoint.x, screenPoint.y, localRect.width, localRect.height);
                if (!Approximately(screenRect, lastScreenRect))
                {
                    lastScreenRect = screenRect;
                    BroadcastLayoutChange(hostView);
                }
                Rect uv = BackgroundCoordinateSystem.GetUvRect(screenRect);
                Color previous = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(localRect, texture, uv, true);
                }
                finally
                {
                    GUI.color = previous;
                }
            }

            private static bool Approximately(Rect left, Rect right)
            {
                return Mathf.Abs(left.x - right.x) < 0.5f &&
                       Mathf.Abs(left.y - right.y) < 0.5f &&
                       Mathf.Abs(left.width - right.width) < 0.5f &&
                       Mathf.Abs(left.height - right.height) < 0.5f;
            }

            private static Action AsAction(Delegate source)
            {
                try
                {
                    return (Action)Delegate.CreateDelegate(typeof(Action), source.Target, source.Method);
                }
                catch
                {
                    return delegate { source.DynamicInvoke(); };
                }
            }
        }

        private sealed class ChromeHook : IHostHook
        {
            private readonly ScriptableObject hostView;
            private readonly UnityEngine.UIElements.IMGUIContainer container;
            private readonly Action original;
            private readonly Action installed;

            private ChromeHook(ScriptableObject hostView, UnityEngine.UIElements.IMGUIContainer container, Action original)
            {
                this.hostView = hostView;
                this.container = container;
                this.original = original;
                installed = Invoke;
            }

            public bool IsAlive
            {
                get { return hostView != null && container != null && container.onGUIHandler == installed; }
            }

            public static ChromeHook Create(ScriptableObject hostView, UnityEngine.UIElements.IMGUIContainer container)
            {
                Action original = container.onGUIHandler;
                if (original == null)
                {
                    return null;
                }

                ChromeHook hook = new ChromeHook(hostView, container, original);
                container.onGUIHandler = hook.installed;
                return hook;
            }

            public bool IsInstalledOn(UnityEngine.UIElements.IMGUIContainer candidate)
            {
                return ReferenceEquals(candidate, container) && IsAlive;
            }

            public void Detach()
            {
                if (container != null && container.onGUIHandler == installed)
                {
                    container.onGUIHandler = original;
                }
            }

            private void Invoke()
            {
                original.Invoke();
                if (Event.current == null || Event.current.type != EventType.Repaint)
                {
                    return;
                }

                NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
                EditorWindow window = HostViewBridge.GetActualView(hostView);
                WindowKind kind = UnityInternalTypes.Classify(window != null ? window.GetType() : null);
                if (window == null || !settings.enableDockChrome || !WindowHookManager.ShouldAttach(kind))
                {
                    return;
                }

                Color color = settings.borderColor;
                color.a = Mathf.Clamp01(settings.borderOpacity);
                foreach (Rect band in GetChromeBands(window))
                {
                    if (band.width > 0f && band.height > 0f)
                    {
                        EditorGUI.DrawRect(band, color);
                    }
                }
            }

            private IEnumerable<Rect> GetChromeBands(EditorWindow window)
            {
                Rect outer = container.contentRect;
                if (outer.width <= 0f || outer.height <= 0f)
                {
                    yield break;
                }

                Rect content = GetContentRect(outer, window);
                yield return new Rect(0f, 0f, outer.width, content.y);
                yield return new Rect(0f, content.yMax, outer.width, outer.height - content.yMax);
                yield return new Rect(0f, content.y, content.x, content.height);
                yield return new Rect(content.xMax, content.y, outer.width - content.xMax, content.height);
            }

            private Rect GetContentRect(Rect outer, EditorWindow window)
            {
                RectOffset border = HostViewBridge.GetBorderSize(hostView);
                if (border != null)
                {
                    return new Rect(
                        border.left,
                        border.top,
                        Mathf.Max(0f, outer.width - border.horizontal),
                        Mathf.Max(0f, outer.height - border.vertical));
                }

                float top = Mathf.Clamp(outer.height - window.position.height, 0f, outer.height);
                return new Rect(0f, top, outer.width, outer.height - top);
            }
        }
    }
}
