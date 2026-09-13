using System;
using System.Reflection;
using NekoThemesPlus.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NekoThemesPlus.Reflection
{
    public static class MainWindowReflection
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly Type ContainerWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ContainerWindow");
        private static readonly PropertyInfo PositionProperty = ContainerWindowType != null ? ContainerWindowType.GetProperty("position", Flags) : null;
        private static readonly PropertyInfo ShowModeProperty = ContainerWindowType != null ? ContainerWindowType.GetProperty("showMode", Flags) : null;
        private static readonly FieldInfo ShowModeField = ContainerWindowType != null ? ContainerWindowType.GetField("m_ShowMode", Flags) : null;

        public static bool TryGetMainWindowRect(out Rect rect)
        {
            rect = default(Rect);

            try
            {
                if (TryFindContainerWindow(out rect))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                    "主窗口反射检测失败，已改用公开接口：",
                    "Main-window reflection failed; using the public fallback: ") + exception.Message);
            }

            try
            {
                rect = EditorGUIUtility.GetMainWindowPosition();
                return rect.width > 0f && rect.height > 0f;
            }
            catch (Exception exception)
            {
                NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                    "无法检测 Unity 主窗口，背景坐标已切换为本地模式：",
                    "The Unity main window could not be detected. Background coordinates will use local mode: ") + exception.Message);
                rect = default(Rect);
                return false;
            }
        }

        private static bool TryFindContainerWindow(out Rect rect)
        {
            rect = default(Rect);
            if (ContainerWindowType == null || PositionProperty == null)
            {
                return false;
            }

            Object[] windows = Resources.FindObjectsOfTypeAll(ContainerWindowType);
            Rect largest = default(Rect);
            float largestArea = 0f;

            foreach (Object window in windows)
            {
                Rect candidate = (Rect)PositionProperty.GetValue(window, null);
                int showMode = ReadShowMode(window);
                if (showMode == 4 && candidate.width > 0f && candidate.height > 0f)
                {
                    rect = candidate;
                    return true;
                }

                float area = candidate.width * candidate.height;
                if (area > largestArea)
                {
                    largest = candidate;
                    largestArea = area;
                }
            }

            if (largestArea > 0f)
            {
                rect = largest;
                return true;
            }

            return false;
        }

        private static int ReadShowMode(Object window)
        {
            object value = null;
            if (ShowModeProperty != null)
            {
                value = ShowModeProperty.GetValue(window, null);
            }
            else if (ShowModeField != null)
            {
                value = ShowModeField.GetValue(window);
            }

            return value != null ? Convert.ToInt32(value) : -1;
        }
    }
}
