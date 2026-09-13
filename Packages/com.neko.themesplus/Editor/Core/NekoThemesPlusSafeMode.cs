using UnityEditor;

namespace NekoThemesPlus.Core
{
    internal static class NekoThemesPlusSafeMode
    {
        public static bool IsActive
        {
            get { return EditorPrefs.GetBool(NekoThemesPlusConstants.SafeModeKey, false); }
        }

        public static bool PreviousSessionEndedCleanly
        {
            get { return EditorPrefs.GetBool(NekoThemesPlusConstants.LastSessionOkKey, true); }
        }

        public static void BeginInitialization()
        {
            if (!PreviousSessionEndedCleanly)
            {
                EditorPrefs.SetBool(NekoThemesPlusConstants.SafeModeKey, true);
            }

            EditorPrefs.SetBool(NekoThemesPlusConstants.LastSessionOkKey, false);
        }

        public static void MarkInitializationSucceeded()
        {
            EditorPrefs.SetBool(NekoThemesPlusConstants.LastSessionOkKey, true);
        }

        public static void Enter()
        {
            EditorPrefs.SetBool(NekoThemesPlusConstants.SafeModeKey, true);
        }

        public static void Exit()
        {
            EditorPrefs.SetBool(NekoThemesPlusConstants.SafeModeKey, false);
            EditorPrefs.SetBool(NekoThemesPlusConstants.LastSessionOkKey, true);
        }
    }
}
