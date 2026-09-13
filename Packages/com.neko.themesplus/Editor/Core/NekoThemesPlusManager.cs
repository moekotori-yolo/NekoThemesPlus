using System;
using NekoThemesPlus.Background;
using NekoThemesPlus.Theme;
using NekoThemesPlus.Windows;
using UnityEditor;

namespace NekoThemesPlus.Core
{
    public static class NekoThemesPlusManager
    {
        private static bool windowRefreshQueued;
        public static bool IsEnabled { get; private set; }
        public static event Action StateChanged;

        internal static void InitializeFromSettings()
        {
            if (NekoThemesPlusSettings.instance.enabled)
            {
                EnableInternal(false);
            }
            else
            {
                IsEnabled = false;
                RaiseStateChanged();
            }
        }

        public static void Enable()
        {
            EnableInternal(true);
        }

        public static void Disable()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            settings.enabled = false;
            settings.SaveSettings();

            ThemeManager.Restore();
            BackgroundManager.Dispose();
            IsEnabled = false;
            RepaintAllViews();
            RaiseStateChanged();
        }

        public static void Refresh()
        {
            if (!NekoThemesPlusSettings.instance.enabled)
            {
                if (IsEnabled)
                {
                    Disable();
                }

                return;
            }

            if (!IsEnabled)
            {
                EnableInternal(false);
                return;
            }

            BackgroundManager.QueueRebuild();
            ThemeManager.Refresh();
            RaiseStateChanged();
        }

        public static void RefreshBackground()
        {
            // The settings preview remains interactive even while editor theming is disabled.
            BackgroundManager.QueueRebuild();
        }

        public static void RefreshWindows()
        {
            if (windowRefreshQueued)
            {
                return;
            }

            windowRefreshQueued = true;
            EditorApplication.delayCall += PerformWindowRefresh;
        }

        public static void RestoreUnity()
        {
            Disable();
        }

        internal static void ShutdownForReload()
        {
            if (windowRefreshQueued)
            {
                EditorApplication.delayCall -= PerformWindowRefresh;
                windowRefreshQueued = false;
            }

            ThemeManager.Restore();
            BackgroundManager.Dispose();
            IsEnabled = false;
        }

        private static void EnableInternal(bool saveSetting)
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            settings.enabled = true;
            if (saveSetting)
            {
                settings.SaveSettings();
            }

            try
            {
                BackgroundManager.Initialize();
                IsEnabled = true;
                ThemeManager.Apply();
                RepaintAllViews();
                RaiseStateChanged();
            }
            catch (Exception exception)
            {
                IsEnabled = false;
                ThemeManager.Restore();
                NekoThemesPlusSafeMode.Enter();
                NekoThemesPlusLogger.Error(NekoThemesPlusLocalization.Text(
                    "初始化失败，已自动进入安全模式：",
                    "Initialization failed; Safe Mode was enabled: ") + exception);
                RaiseStateChanged();
            }
        }

        private static void RepaintAllViews()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void PerformWindowRefresh()
        {
            windowRefreshQueued = false;
            if (NekoThemesPlusSettings.instance.enabled)
            {
                ThemeManager.Refresh();
                RepaintAllViews();
            }

            RaiseStateChanged();
        }

        private static void RaiseStateChanged()
        {
            if (StateChanged != null)
            {
                StateChanged.Invoke();
            }
        }
    }
}
