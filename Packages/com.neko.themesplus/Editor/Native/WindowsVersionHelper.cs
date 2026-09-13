using System;

namespace NekoThemesPlus.Native
{
    public static class WindowsVersionHelper
    {
        public static bool IsWindows
        {
            get { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
        }

        public static bool IsWindows11OrNewer
        {
            get { return IsWindows && Environment.OSVersion.Version.Build >= 22000; }
        }

        public static string DisplayName
        {
            get
            {
                if (!IsWindows) return NekoThemesPlus.Core.NekoThemesPlusLocalization.Text("不支持的系统", "Unsupported OS");
                return IsWindows11OrNewer
                    ? "Windows 11"
                    : NekoThemesPlus.Core.NekoThemesPlusLocalization.Text("Windows 10 或更早版本", "Windows 10 / earlier");
            }
        }
    }
}
