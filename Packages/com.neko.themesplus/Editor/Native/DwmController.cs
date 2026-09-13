using System;
using System.Runtime.InteropServices;
using NekoThemesPlus.Core;
using UnityEngine;

namespace NekoThemesPlus.Native
{
    public static class DwmController
    {
        private static IntPtr appliedWindow;
#if UNITY_EDITOR_WIN
        private static bool backupCaptured;
        private static bool hasDarkModeBackup;
        private static bool hasCornerBackup;
        private static bool hasBackdropBackup;
        private static int darkModeBackup;
        private static int darkModeAttribute = WindowsNative.DwmUseImmersiveDarkMode;
        private static int cornerBackup;
        private static int backdropBackup;
#endif

        public static bool Apply()
        {
#if UNITY_EDITOR_WIN
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            if (NekoThemesPlusSafeMode.IsActive || !settings.enableNativeGlass)
            {
                Restore();
                return false;
            }

            IntPtr window;
            if (!WindowHandleProvider.TryGetMainWindowHandle(out window))
            {
                NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                    "找不到用于原生效果的 Unity 主窗口句柄。",
                    "Could not find the Unity main HWND for native effects."));
                return false;
            }

            try
            {
                CaptureOriginalValues(window);
                appliedWindow = window;
                ApplyDwmInt(window, WindowsNative.DwmUseImmersiveDarkMode, settings.enableDarkTitlebar ? 1 : 0,
                    WindowsNative.DwmUseImmersiveDarkModeBefore20H1);
                ApplyDwmInt(window, WindowsNative.DwmWindowCornerPreference,
                    settings.enableRoundedCorners ? WindowsNative.CornerRound : WindowsNative.CornerDefault);
                ApplyDwmInt(window, WindowsNative.DwmSystemBackdropType,
                    settings.enableMica && WindowsVersionHelper.IsWindows11OrNewer
                        ? WindowsNative.BackdropMainWindow
                        : WindowsNative.BackdropAuto);
                ApplyAccent(window, settings.enableAcrylic, settings.panelTint, settings.globalPanelOpacity);
                return true;
            }
            catch (Exception exception)
            {
                NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                    "无法应用 Windows 原生效果：",
                    "Native Windows effects were not applied: ") + exception.Message);
                return false;
            }
#else
            return false;
#endif
        }

        public static void Restore()
        {
#if UNITY_EDITOR_WIN
            if (appliedWindow == IntPtr.Zero)
            {
                return;
            }

            try
            {
                if (hasDarkModeBackup)
                {
                    ApplyDwmInt(appliedWindow, darkModeAttribute, darkModeBackup);
                }

                if (hasCornerBackup)
                {
                    ApplyDwmInt(appliedWindow, WindowsNative.DwmWindowCornerPreference, cornerBackup);
                }

                if (hasBackdropBackup)
                {
                    ApplyDwmInt(appliedWindow, WindowsNative.DwmSystemBackdropType, backdropBackup);
                }

                ApplyAccent(appliedWindow, false, Color.clear, 0f);
            }
            catch (Exception exception)
            {
                NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                    "Windows 原生效果未能完全恢复：",
                    "Native Windows effects could not be fully restored: ") + exception.Message);
            }
            finally
            {
                appliedWindow = IntPtr.Zero;
                backupCaptured = false;
                hasDarkModeBackup = false;
                hasCornerBackup = false;
                hasBackdropBackup = false;
            }
#endif
        }

#if UNITY_EDITOR_WIN
        private static void CaptureOriginalValues(IntPtr window)
        {
            if (backupCaptured && appliedWindow == window)
            {
                return;
            }

            backupCaptured = true;
            darkModeAttribute = WindowsNative.DwmUseImmersiveDarkMode;
            hasDarkModeBackup = WindowsNative.DwmGetWindowAttribute(
                window, darkModeAttribute, out darkModeBackup, sizeof(int)) == 0;
            if (!hasDarkModeBackup)
            {
                darkModeAttribute = WindowsNative.DwmUseImmersiveDarkModeBefore20H1;
                hasDarkModeBackup = WindowsNative.DwmGetWindowAttribute(
                    window, darkModeAttribute, out darkModeBackup, sizeof(int)) == 0;
            }

            hasCornerBackup = WindowsNative.DwmGetWindowAttribute(
                window, WindowsNative.DwmWindowCornerPreference, out cornerBackup, sizeof(int)) == 0;
            hasBackdropBackup = WindowsNative.DwmGetWindowAttribute(
                window, WindowsNative.DwmSystemBackdropType, out backdropBackup, sizeof(int)) == 0;
        }

        private static void ApplyDwmInt(IntPtr window, int attribute, int value, int fallbackAttribute = -1)
        {
            int result = WindowsNative.DwmSetWindowAttribute(window, attribute, ref value, sizeof(int));
            if (result != 0 && fallbackAttribute >= 0)
            {
                WindowsNative.DwmSetWindowAttribute(window, fallbackAttribute, ref value, sizeof(int));
            }
        }

        private static void ApplyAccent(IntPtr window, bool enabled, Color tint, float opacity)
        {
            Color32 color = tint;
            int alpha = Mathf.RoundToInt(Mathf.Clamp01(opacity) * 180f);
            int gradient = (alpha << 24) | (color.b << 16) | (color.g << 8) | color.r;
            WindowsNative.AccentPolicy policy = new WindowsNative.AccentPolicy
            {
                accentState = enabled ? WindowsNative.AccentState.EnableAcrylicBlurBehind : WindowsNative.AccentState.Disabled,
                accentFlags = enabled ? 2 : 0,
                gradientColor = enabled ? gradient : 0,
                animationId = 0
            };

            int size = Marshal.SizeOf(typeof(WindowsNative.AccentPolicy));
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, memory, false);
                WindowsNative.WindowCompositionAttributeData data = new WindowsNative.WindowCompositionAttributeData
                {
                    attribute = WindowsNative.WindowCompositionAttribute.AccentPolicy,
                    data = memory,
                    sizeOfData = size
                };
                WindowsNative.SetWindowCompositionAttribute(window, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }
#endif
    }
}
