#if UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace NekoThemesPlus.Native
{
    internal static class WindowsNative
    {
        internal const int DwmUseImmersiveDarkMode = 20;
        internal const int DwmUseImmersiveDarkModeBefore20H1 = 19;
        internal const int DwmWindowCornerPreference = 33;
        internal const int DwmSystemBackdropType = 38;

        internal const int CornerDefault = 0;
        internal const int CornerRound = 2;
        internal const int BackdropAuto = 0;
        internal const int BackdropMainWindow = 2;

        internal enum AccentState
        {
            Disabled = 0,
            EnableBlurBehind = 3,
            EnableAcrylicBlurBehind = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState accentState;
            public int accentFlags;
            public int gradientColor;
            public int animationId;
        }

        internal enum WindowCompositionAttribute
        {
            AccentPolicy = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute attribute;
            public IntPtr data;
            public int sizeOfData;
        }

        internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int valueSize);

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr window, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr window);
    }
}
#endif
