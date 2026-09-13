using System.Text;
using NekoThemesPlus.Background;
using NekoThemesPlus.Reflection;
using NekoThemesPlus.Theme;
using NekoThemesPlus.Windows;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Core
{
    internal static class NekoThemesPlusDiagnostics
    {
        public static string CreateReport()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            StringBuilder report = new StringBuilder();
            report.AppendLine("Neko Themes Plus 诊断报告");
            report.AppendLine("================================");
            report.AppendLine("插件版本：" + NekoThemesPlusConstants.Version);
            report.AppendLine("Unity 版本：" + Application.unityVersion);
            report.AppendLine("操作系统：" + SystemInfo.operatingSystem);
            report.AppendLine("像素倍率：" + EditorGUIUtility.pixelsPerPoint);
            report.AppendLine("已启用：" + NekoThemesPlusManager.IsEnabled);
            report.AppendLine("安全模式：" + NekoThemesPlusSafeMode.IsActive);
            report.AppendLine();

            Rect mainRect;
            report.AppendLine(MainWindowReflection.TryGetMainWindowRect(out mainRect)
                ? "主窗口：" + mainRect
                : "主窗口：未检测到（本地背景模式）");
            Vector2Int resolution = BackgroundManager.ProcessedResolution;
            report.AppendLine("处理后背景：" + (resolution == Vector2Int.zero ? "无" : resolution.x + " × " + resolution.y));
            report.AppendLine("背景路径：" + (string.IsNullOrEmpty(settings.backgroundPath) ? "未选择" : settings.backgroundPath));
            report.AppendLine("背景状态：" + (string.IsNullOrEmpty(BackgroundManager.LastError) ? "就绪" : BackgroundManager.LastError));
            report.AppendLine("区域独立背景数：" + BackgroundManager.ActiveOverrideCount);
            AppendWindowBackground(report, "Hierarchy", WindowKind.Hierarchy);
            AppendWindowBackground(report, "Inspector", WindowKind.Inspector);
            AppendWindowBackground(report, "Project", WindowKind.Project);
            AppendWindowBackground(report, "Console", WindowKind.Console);
            report.AppendLine();

            report.AppendLine("窗口注册数：" + EditorWindowRegistry.WindowCount);
            report.AppendLine("VisualElement 挂钩数：" + EditorWindowRegistry.HookedWindowCount);
            report.AppendLine("HostView 桥：" + (HostViewHookManager.IsAvailable
                ? "可用"
                : "不可用 - " + HostViewHookManager.UnavailableReason));
            report.AppendLine("HostView 内容挂钩数：" + HostViewHookManager.HookedCount);
            report.AppendLine("Dock 边框挂钩数：" + HostViewHookManager.ChromeHookedCount);
            report.AppendLine("文字主题：" + settings.enableTextColors);
            report.AppendLine("UI Toolkit 文字元素：" + WindowHookManager.ThemedTextElementCount);
            report.AppendLine("IMGUI 文字样式：" + EditorStyleController.ThemedStyleCount);
            foreach (string hook in HostViewHookManager.DescribeHooks())
            {
                report.AppendLine("  - " + hook);
            }

            report.AppendLine();
            report.AppendLine("窗口开关：");
            report.AppendLine("  Hierarchy=" + settings.enableHierarchy + ", opacity=" + settings.hierarchyOpacity);
            report.AppendLine("  Inspector=" + settings.enableInspector + ", opacity=" + settings.inspectorOpacity);
            report.AppendLine("  Project=" + settings.enableProject + ", opacity=" + settings.projectOpacity);
            report.AppendLine("  Console=" + settings.enableConsole + ", opacity=" + settings.consoleOpacity);
            report.AppendLine("  Scene=" + settings.enableSceneView + ", Game=" + settings.enableGameView);
            report.AppendLine("  DockChrome=" + settings.enableDockChrome + ", strength=" + settings.borderOpacity);
            report.AppendLine("  NativeGlass=" + settings.enableNativeGlass);
            return report.ToString();
        }

        private static void AppendWindowBackground(StringBuilder report, string label, WindowKind kind)
        {
            string path = BackgroundManager.GetBackgroundPath(kind);
            string error = BackgroundManager.GetWindowError(kind);
            report.AppendLine("  " + label + "背景=" +
                              (string.IsNullOrEmpty(path) ? "继承全局" : path) +
                              (string.IsNullOrEmpty(error) ? string.Empty : "，错误=" + error));
        }

        public static void LogAndCopyReport()
        {
            string report = CreateReport();
            EditorGUIUtility.systemCopyBuffer = report;
            Debug.Log(report);
        }
    }
}
