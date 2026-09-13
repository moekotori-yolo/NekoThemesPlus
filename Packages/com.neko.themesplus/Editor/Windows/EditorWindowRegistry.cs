using System.Collections.Generic;
using NekoThemesPlus.Background;
using NekoThemesPlus.Reflection;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Windows
{
    internal sealed class TrackedEditorWindow
    {
        public EditorWindow window;
        public System.Type type;
        public int instanceId;
        public Rect lastRect;
        public WindowKind kind;
        public bool hooked;
        public bool uiToolkitSupported;
    }

    public static class EditorWindowRegistry
    {
        private static readonly Dictionary<int, TrackedEditorWindow> Tracked = new Dictionary<int, TrackedEditorWindow>();
        private static double nextScan;
        private static bool initialized;

        public static int WindowCount { get { return Tracked.Count; } }

        public static int HookedWindowCount
        {
            get
            {
                int count = 0;
                foreach (TrackedEditorWindow tracked in Tracked.Values)
                {
                    if (tracked.hooked) count++;
                }

                return count;
            }
        }

        public static void Initialize()
        {
            if (initialized || Application.isBatchMode)
            {
                return;
            }

            initialized = true;
            nextScan = 0d;
            EditorApplication.update += Update;
            EditorApplication.delayCall += Scan;
        }

        public static void Shutdown()
        {
            if (initialized)
            {
                EditorApplication.update -= Update;
                EditorApplication.delayCall -= Scan;
            }

            initialized = false;
            Tracked.Clear();
        }

        public static void Scan()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            HashSet<int> seen = new HashSet<int>();
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (EditorWindow window in windows)
            {
                if (window == null)
                {
                    continue;
                }

                int instanceId = window.GetInstanceID();
                seen.Add(instanceId);
                TrackedEditorWindow tracked;
                if (!Tracked.TryGetValue(instanceId, out tracked))
                {
                    tracked = new TrackedEditorWindow
                    {
                        window = window,
                        type = window.GetType(),
                        instanceId = instanceId,
                        lastRect = window.position,
                        kind = UnityInternalTypes.Classify(window.GetType()),
                        uiToolkitSupported = window.rootVisualElement != null
                    };
                    Tracked.Add(instanceId, tracked);
                    tracked.hooked = WindowHookManager.Attach(window, tracked.kind);
                }
                else
                {
                    Rect current = window.position;
                    if (!Approximately(current, tracked.lastRect) || !WindowHookManager.IsAttached(instanceId))
                    {
                        tracked.lastRect = current;
                        tracked.hooked = WindowHookManager.Refresh(window, tracked.kind);
                    }

                    WindowHookManager.RefreshDynamicStyles(instanceId);
                }
            }

            List<int> removed = null;
            foreach (int instanceId in Tracked.Keys)
            {
                if (!seen.Contains(instanceId))
                {
                    if (removed == null) removed = new List<int>();
                    removed.Add(instanceId);
                }
            }

            if (removed != null)
            {
                foreach (int instanceId in removed)
                {
                    WindowHookManager.Detach(instanceId);
                    BackgroundManager.ReleaseWindowCache(instanceId);
                    Tracked.Remove(instanceId);
                }
            }
        }

        public static void MarkHooked(int instanceId, bool hooked)
        {
            TrackedEditorWindow tracked;
            if (Tracked.TryGetValue(instanceId, out tracked))
            {
                tracked.hooked = hooked;
            }
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < nextScan)
            {
                return;
            }

            nextScan = now + 0.75d;
            Scan();
        }

        private static bool Approximately(Rect left, Rect right)
        {
            return Mathf.Abs(left.x - right.x) < 0.25f &&
                   Mathf.Abs(left.y - right.y) < 0.25f &&
                   Mathf.Abs(left.width - right.width) < 0.25f &&
                   Mathf.Abs(left.height - right.height) < 0.25f;
        }
    }
}
