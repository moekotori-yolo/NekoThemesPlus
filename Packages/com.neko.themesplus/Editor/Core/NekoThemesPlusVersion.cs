using UnityEngine;

namespace NekoThemesPlus.Core
{
    internal static class NekoThemesPlusVersion
    {
        public static bool IsSupportedLts
        {
            get { return Application.unityVersion.StartsWith("2022.3."); }
        }

        public static bool IsExactTarget
        {
            get { return Application.unityVersion == NekoThemesPlusConstants.TargetUnityVersion; }
        }

        public static string SupportLabel
        {
            get
            {
                if (IsExactTarget)
                {
                    return "Supported target";
                }

                return IsSupportedLts ? "Supported 2022.3 LTS" : "Experimental / unsupported";
            }
        }
    }
}
