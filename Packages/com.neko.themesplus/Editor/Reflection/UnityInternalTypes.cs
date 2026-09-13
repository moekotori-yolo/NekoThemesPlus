using System;
using System.Collections.Generic;
using NekoThemesPlus.Windows;
using UnityEditor;

namespace NekoThemesPlus.Reflection
{
    internal static class UnityInternalTypes
    {
        private static readonly Dictionary<string, WindowKind> Kinds = new Dictionary<string, WindowKind>
        {
            { "UnityEditor.SceneHierarchyWindow", WindowKind.Hierarchy },
            { "UnityEditor.InspectorWindow", WindowKind.Inspector },
            { "UnityEditor.ProjectBrowser", WindowKind.Project },
            { "UnityEditor.ConsoleWindow", WindowKind.Console },
            { "UnityEditor.SceneView", WindowKind.SceneView },
            { "UnityEditor.GameView", WindowKind.GameView },
            { "UnityEditor.AnimationWindow", WindowKind.Animation },
            { "UnityEditor.Graphs.AnimatorControllerTool", WindowKind.Animator },
            { "UnityEditor.ProfilerWindow", WindowKind.Profiler },
            { "UnityEditor.PackageManager.UI.PackageManagerWindow", WindowKind.PackageManager },
            { "UnityEditor.SettingsWindow", WindowKind.Preferences }
        };

        public static Type Find(string fullName)
        {
            return typeof(EditorWindow).Assembly.GetType(fullName);
        }

        public static WindowKind Classify(Type type)
        {
            if (type == null)
            {
                return WindowKind.Unknown;
            }

            WindowKind kind;
            return Kinds.TryGetValue(type.FullName, out kind) ? kind : WindowKind.Other;
        }
    }
}
