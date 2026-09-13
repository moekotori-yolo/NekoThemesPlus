using UnityEditor;
using NekoThemesPlus.Updates;

namespace NekoThemesPlus.Core
{
    [InitializeOnLoad]
    internal static class NekoThemesPlusBootstrap
    {
        static NekoThemesPlusBootstrap()
        {
            EditorApplication.delayCall += Initialize;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            EditorApplication.quitting += BeforeEditorQuit;
        }

        private static void Initialize()
        {
            NekoThemesPlusSafeMode.BeginInitialization();
            try
            {
                NekoThemesPlusManager.InitializeFromSettings();
                NekoThemesPlusUpdateService.Initialize();
                NekoThemesPlusSafeMode.MarkInitializationSucceeded();
            }
            catch (System.Exception exception)
            {
                NekoThemesPlusSafeMode.Enter();
                NekoThemesPlusManager.ShutdownForReload();
                NekoThemesPlusLogger.Error(NekoThemesPlusLocalization.Text(
                    "启动过程已安全停止：",
                    "Startup was stopped safely: ") + exception);
            }
        }

        private static void BeforeAssemblyReload()
        {
            NekoThemesPlusManager.ShutdownForReload();
            NekoThemesPlusUpdateService.Shutdown();
            NekoThemesPlusSafeMode.MarkInitializationSucceeded();
        }

        private static void BeforeEditorQuit()
        {
            NekoThemesPlusManager.ShutdownForReload();
            NekoThemesPlusUpdateService.Shutdown();
            NekoThemesPlusSafeMode.MarkInitializationSucceeded();
        }
    }
}
