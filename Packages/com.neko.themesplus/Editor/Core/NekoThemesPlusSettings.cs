using NekoThemesPlus.Background;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Core
{
    [FilePath("ProjectSettings/NekoThemesPlusSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NekoThemesPlusSettings : ScriptableSingleton<NekoThemesPlusSettings>
    {
        public NekoThemesPlusLanguage language = NekoThemesPlusLanguage.SimplifiedChinese;
        public bool enabled = true;
        public string backgroundPath = string.Empty;
        public BackgroundImageMode backgroundMode = BackgroundImageMode.Fill;
        [Range(1f, 4f)] public float backgroundZoom = 1f;
        public Vector2 backgroundAlignment = new Vector2(0.5f, 0.5f);
        [Range(0f, 1f)] public float backgroundOpacity = 1f;
        [Range(0f, 50f)] public float blurAmount = 18f;
        [Range(0f, 2f)] public float brightness = 0.75f;
        [Range(0f, 2f)] public float saturation = 0.85f;
        [Range(0f, 2f)] public float contrast = 1.05f;
        public Color backgroundTint = new Color32(21, 27, 35, 90);

        public Color panelTint = new Color32(21, 29, 38, 255);
        [Range(0f, 1f)] public float globalPanelOpacity = 0.70f;
        [Range(0f, 1f)] public float hierarchyOpacity = 0.68f;
        [Range(0f, 1f)] public float inspectorOpacity = 0.72f;
        [Range(0f, 1f)] public float projectOpacity = 0.72f;
        [Range(0f, 1f)] public float consoleOpacity = 0.78f;
        [Range(0f, 1f)] public float sceneOpacity = 1f;
        [Range(0f, 1f)] public float gameOpacity = 1f;

        public Color accentColor = new Color32(105, 168, 255, 255);
        public Color selectionColor = new Color32(65, 106, 155, 255);
        [Range(0f, 1f)] public float selectionOpacity = 0.85f;
        public bool enableDockChrome = true;
        public Color borderColor = new Color32(72, 126, 193, 255);
        [Range(0f, 1f)] public float borderOpacity = 0.22f;

        public bool enableHierarchy = true;
        public bool enableInspector = true;
        public bool enableProject = true;
        public bool enableConsole = true;
        public bool enableSceneView;
        public bool enableGameView;

        public bool enableNativeGlass;
        public bool enableDarkTitlebar;
        public bool enableRoundedCorners;
        public bool enableMica;
        public bool enableAcrylic;
        public bool experimentalIMGUI;
        public bool experimentalInternalStyles;
        public bool forceUnsupportedVersion;
        public bool debugLogging;
        public int maxBackgroundResolution = 4096;
        public string currentPreset = "Neko Glass";

        public void SaveSettings()
        {
            Save(true);
        }

        public void ResetToDefaults()
        {
            language = NekoThemesPlusLanguage.SimplifiedChinese;
            enabled = true;
            backgroundPath = string.Empty;
            backgroundMode = BackgroundImageMode.Fill;
            backgroundZoom = 1f;
            backgroundAlignment = new Vector2(0.5f, 0.5f);
            backgroundOpacity = 1f;
            blurAmount = 18f;
            brightness = 0.75f;
            saturation = 0.85f;
            contrast = 1.05f;
            backgroundTint = new Color32(21, 27, 35, 90);
            panelTint = new Color32(21, 29, 38, 255);
            globalPanelOpacity = 0.70f;
            hierarchyOpacity = 0.68f;
            inspectorOpacity = 0.72f;
            projectOpacity = 0.72f;
            consoleOpacity = 0.78f;
            sceneOpacity = 1f;
            gameOpacity = 1f;
            accentColor = new Color32(105, 168, 255, 255);
            selectionColor = new Color32(65, 106, 155, 255);
            selectionOpacity = 0.85f;
            enableDockChrome = true;
            borderColor = new Color32(72, 126, 193, 255);
            borderOpacity = 0.22f;
            enableHierarchy = true;
            enableInspector = true;
            enableProject = true;
            enableConsole = true;
            enableSceneView = false;
            enableGameView = false;
            enableNativeGlass = false;
            enableDarkTitlebar = false;
            enableRoundedCorners = false;
            enableMica = false;
            enableAcrylic = false;
            experimentalIMGUI = false;
            experimentalInternalStyles = false;
            forceUnsupportedVersion = false;
            debugLogging = false;
            maxBackgroundResolution = 4096;
            currentPreset = "Neko Glass";
            SaveSettings();
        }
    }
}
