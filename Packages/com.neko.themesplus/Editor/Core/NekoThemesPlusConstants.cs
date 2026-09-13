namespace NekoThemesPlus.Core
{
    internal static class NekoThemesPlusConstants
    {
        public const string DisplayName = "Neko Themes Plus";
        public const string PackageId = "com.neko.themesplus";
        public const string PackageRoot = "Packages/com.neko.themesplus";
        public const string FallbackVersion = "0.3.0";
        public const string TargetUnityVersion = "2022.3.42f1";
        public const string SafeModeKey = "NekoThemesPlus.SafeMode";
        public const string LastSessionOkKey = "NekoThemesPlus.LastSessionOk";

        public static string Version
        {
            get
            {
                try
                {
                    UnityEditor.PackageManager.PackageInfo package =
                        UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageRoot + "/package.json");
                    if (package != null && !string.IsNullOrEmpty(package.version))
                    {
                        return package.version;
                    }
                }
                catch
                {
                    // Package Manager can be unavailable during early import; keep a safe label.
                }

                return FallbackVersion;
            }
        }
    }
}
