using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NekoThemesPlus.Reflection
{
    /// <summary>
    /// Keeps the small, version-sensitive HostView reflection surface in one fail-soft adapter.
    /// The interception technique was informed by the MIT-licensed UniPrism project; the bridge
    /// is integrated here with NekoThemesPlus' own settings, rendering and restore lifecycle.
    /// </summary>
    internal static class HostViewBridge
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static bool resolved;
        private static Type hostViewType;
        private static FieldInfo onGuiField;
        private static PropertyInfo actualViewProperty;
        private static PropertyInfo windowBackendProperty;
        private static PropertyInfo borderSizeProperty;

        public static string UnavailableReason { get; private set; }

        public static bool IsAvailable
        {
            get
            {
                Resolve();
                return string.IsNullOrEmpty(UnavailableReason);
            }
        }

        public static Type OnGuiDelegateType
        {
            get
            {
                Resolve();
                return onGuiField != null ? onGuiField.FieldType : null;
            }
        }

        public static IEnumerable<ScriptableObject> GetHostViews()
        {
            Resolve();
            if (hostViewType == null)
            {
                yield break;
            }

            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(hostViewType))
            {
                ScriptableObject host = candidate as ScriptableObject;
                if (host != null)
                {
                    yield return host;
                }
            }
        }

        public static EditorWindow GetActualView(ScriptableObject hostView)
        {
            Resolve();
            if (hostView == null || actualViewProperty == null)
            {
                return null;
            }

            try
            {
                return actualViewProperty.GetValue(hostView, null) as EditorWindow;
            }
            catch
            {
                return null;
            }
        }

        public static Delegate GetOnGui(ScriptableObject hostView)
        {
            Resolve();
            if (hostView == null || onGuiField == null)
            {
                return null;
            }

            try
            {
                return onGuiField.GetValue(hostView) as Delegate;
            }
            catch
            {
                return null;
            }
        }

        public static bool SetOnGui(ScriptableObject hostView, Delegate value)
        {
            Resolve();
            if (hostView == null || onGuiField == null)
            {
                return false;
            }

            try
            {
                onGuiField.SetValue(hostView, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static IMGUIContainer GetChromeContainer(ScriptableObject hostView)
        {
            Resolve();
            if (hostView == null || windowBackendProperty == null)
            {
                return null;
            }

            try
            {
                object backend = windowBackendProperty.GetValue(hostView, null);
                if (backend == null)
                {
                    return null;
                }

                PropertyInfo visualTree = backend.GetType().GetProperty("visualTree", Flags);
                VisualElement root = visualTree != null ? visualTree.GetValue(backend, null) as VisualElement : null;
                return FindChromeContainer(root);
            }
            catch
            {
                return null;
            }
        }

        public static RectOffset GetBorderSize(ScriptableObject hostView)
        {
            Resolve();
            if (hostView == null || borderSizeProperty == null)
            {
                return null;
            }

            try
            {
                return borderSizeProperty.GetValue(hostView, null) as RectOffset;
            }
            catch
            {
                return null;
            }
        }

        private static IMGUIContainer FindChromeContainer(VisualElement root)
        {
            if (root == null)
            {
                return null;
            }

            IMGUIContainer fallback = null;
            foreach (VisualElement element in root.Children())
            {
                IMGUIContainer container = element as IMGUIContainer;
                if (container != null)
                {
                    if (!string.IsNullOrEmpty(container.name) &&
                        container.name.StartsWith("Dockarea", StringComparison.OrdinalIgnoreCase))
                    {
                        return container;
                    }

                    if (fallback == null)
                    {
                        fallback = container;
                    }
                }
            }

            return fallback;
        }

        private static void Resolve()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            try
            {
                hostViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView", false);
                if (hostViewType == null)
                {
                    UnavailableReason = "UnityEditor.HostView not found";
                    return;
                }

                onGuiField = hostViewType.GetField("m_OnGUI", BindingFlags.Instance | BindingFlags.NonPublic);
                actualViewProperty = hostViewType.GetProperty("actualView", Flags);
                if (onGuiField == null || !typeof(Delegate).IsAssignableFrom(onGuiField.FieldType))
                {
                    UnavailableReason = "UnityEditor.HostView.m_OnGUI delegate not found";
                    return;
                }

                if (actualViewProperty == null)
                {
                    UnavailableReason = "UnityEditor.HostView.actualView not found";
                    return;
                }

                // Chrome support is optional. Content theming remains useful without it.
                windowBackendProperty = hostViewType.GetProperty("windowBackend", Flags);
                borderSizeProperty = hostViewType.GetProperty("borderSize", Flags);
                UnavailableReason = null;
            }
            catch (Exception exception)
            {
                UnavailableReason = exception.GetType().Name + ": " + exception.Message;
            }
        }
    }
}
