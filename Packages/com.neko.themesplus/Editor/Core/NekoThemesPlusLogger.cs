using System.Collections.Generic;
using UnityEngine;

namespace NekoThemesPlus.Core
{
    internal static class NekoThemesPlusLogger
    {
        private static readonly HashSet<string> WarnedMessages = new HashSet<string>();

        public static void Info(string message)
        {
            if (NekoThemesPlusSettings.instance.debugLogging)
            {
                Debug.Log("[NekoThemesPlus] " + message);
            }
        }

        public static void WarnOnce(string message)
        {
            if (WarnedMessages.Add(message))
            {
                Debug.LogWarning("[NekoThemesPlus] " + message);
            }
        }

        public static void Error(string message)
        {
            Debug.LogError("[NekoThemesPlus] " + message);
        }
    }
}
