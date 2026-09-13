using System.Collections.Generic;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using UnityEditor;

namespace NekoThemesPlus.Windows
{
    public static class WindowHookManager
    {
        private static readonly Dictionary<int, WindowThemeController> Controllers = new Dictionary<int, WindowThemeController>();
        private static bool initialized;

        public static int HookedCount { get { return Controllers.Count; } }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            BackgroundManager.BackgroundChanged += RefreshAll;
            EditorWindowRegistry.Initialize();
            EditorWindowRegistry.Scan();
        }

        public static void Scan()
        {
            EditorWindowRegistry.Scan();
        }

        public static bool Attach(EditorWindow window)
        {
            return Attach(window, Reflection.UnityInternalTypes.Classify(window != null ? window.GetType() : null));
        }

        internal static bool Attach(EditorWindow window, WindowKind kind)
        {
            if (window == null || !ShouldAttach(kind))
            {
                return false;
            }

            int instanceId = window.GetInstanceID();
            WindowThemeController controller;
            if (!Controllers.TryGetValue(instanceId, out controller))
            {
                controller = new WindowThemeController(window, kind);
                Controllers.Add(instanceId, controller);
            }

            bool attached = controller.IsAttached || controller.Attach();
            EditorWindowRegistry.MarkHooked(instanceId, attached);
            return attached;
        }

        internal static bool Refresh(EditorWindow window, WindowKind kind)
        {
            if (window == null)
            {
                return false;
            }

            int instanceId = window.GetInstanceID();
            if (!ShouldAttach(kind))
            {
                Detach(instanceId);
                return false;
            }

            WindowThemeController controller;
            if (!Controllers.TryGetValue(instanceId, out controller))
            {
                return Attach(window, kind);
            }

            controller.Refresh();
            EditorWindowRegistry.MarkHooked(instanceId, true);
            return true;
        }

        public static void Detach(EditorWindow window)
        {
            if (window != null) Detach(window.GetInstanceID());
        }

        internal static void Detach(int instanceId)
        {
            WindowThemeController controller;
            if (Controllers.TryGetValue(instanceId, out controller))
            {
                controller.Detach();
                Controllers.Remove(instanceId);
            }

            EditorWindowRegistry.MarkHooked(instanceId, false);
        }

        public static void DetachAll()
        {
            foreach (WindowThemeController controller in Controllers.Values)
            {
                controller.Detach();
            }

            Controllers.Clear();
        }

        public static void RefreshAll()
        {
            EditorWindowRegistry.Scan();
            List<int> detach = null;
            foreach (KeyValuePair<int, WindowThemeController> pair in Controllers)
            {
                if (!ShouldAttach(pair.Value.Kind) || pair.Value.Window == null)
                {
                    if (detach == null) detach = new List<int>();
                    detach.Add(pair.Key);
                }
                else
                {
                    pair.Value.Refresh();
                }
            }

            if (detach != null)
            {
                foreach (int instanceId in detach)
                {
                    Detach(instanceId);
                }
            }
        }

        public static bool IsAttached(int instanceId)
        {
            WindowThemeController controller;
            return Controllers.TryGetValue(instanceId, out controller) && controller.IsAttached;
        }

        public static void Shutdown()
        {
            if (initialized)
            {
                BackgroundManager.BackgroundChanged -= RefreshAll;
            }

            initialized = false;
            DetachAll();
            EditorWindowRegistry.Shutdown();
        }

        internal static bool ShouldAttach(WindowKind kind)
        {
            if (!NekoThemesPlusSettings.instance.enabled || NekoThemesPlusSafeMode.IsActive)
            {
                return false;
            }

            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            switch (kind)
            {
                case WindowKind.Hierarchy: return settings.enableHierarchy;
                case WindowKind.Inspector: return settings.enableInspector;
                case WindowKind.Project: return settings.enableProject;
                case WindowKind.Console: return settings.enableConsole;
                case WindowKind.SceneView: return settings.enableSceneView;
                case WindowKind.GameView: return settings.enableGameView;
                default: return false;
            }
        }
    }
}
