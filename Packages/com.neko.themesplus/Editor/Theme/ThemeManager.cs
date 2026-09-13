using NekoThemesPlus.Core;
using NekoThemesPlus.Windows;
using NekoThemesPlus.Native;

namespace NekoThemesPlus.Theme
{
    public static class ThemeManager
    {
        public static void Apply()
        {
            if (NekoThemesPlusSafeMode.IsActive)
            {
                Restore();
                return;
            }

            if (!NekoThemesPlusVersion.IsSupportedLts && !NekoThemesPlusSettings.instance.forceUnsupportedVersion)
            {
                Restore();
                return;
            }

            WindowHookManager.Initialize();
            HostViewHookManager.Initialize();
            WindowHookManager.RefreshAll();
            HostViewHookManager.Refresh();
            SelectionStyleController.Apply();
            EditorStyleController.Apply();
            DwmController.Apply();
        }

        public static void Refresh()
        {
            Apply();
        }

        public static void Restore()
        {
            HostViewHookManager.Shutdown();
            WindowHookManager.Shutdown();
            SelectionStyleController.Restore();
            EditorStyleController.Restore();
            DwmController.Restore();
        }
    }
}
