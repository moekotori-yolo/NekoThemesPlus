using System;
using NekoThemesPlus.Core;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Theme
{
    public static class EditorStyleController
    {
        private static readonly string[] ConservativeStyleNames =
        {
            "CN Box",
            "CN EntryBackEven",
            "CN EntryBackOdd",
            "PR Background",
            "ProjectBrowserPreviewBg",
            "ProjectBrowserTopBarBg",
            "ProjectBrowserBottomBarBg",
            "OL Box"
        };

        private static readonly string[] ExtendedStyleNames =
        {
            "IN BigTitle",
            "IN Footer",
            "RL Background",
            "TV Background",
            "DD Background"
        };

        private static readonly EditorStyleBackup Backup = new EditorStyleBackup();
        private static Texture2D transparentTexture;
        private static bool applied;
        private static bool appliedConservative;
        private static bool appliedExtended;

        public static void Apply()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            bool useConservative = settings.experimentalIMGUI || settings.experimentalInternalStyles;
            bool useExtended = settings.experimentalInternalStyles;
            if ((!useConservative && !useExtended) || NekoThemesPlusSafeMode.IsActive)
            {
                Restore();
                return;
            }

            if (applied && appliedConservative == useConservative && appliedExtended == useExtended)
            {
                return;
            }

            if (applied)
            {
                Restore();
            }

            try
            {
                EnsureTransparentTexture();
                GUISkin skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                if (skin == null)
                {
                    return;
                }

                if (useConservative)
                {
                    ApplyStyles(skin, ConservativeStyleNames);
                }

                if (useExtended)
                {
                    ApplyStyles(skin, ExtendedStyleNames);
                }

                applied = true;
                appliedConservative = useConservative;
                appliedExtended = useExtended;
            }
            catch (Exception exception)
            {
                NekoThemesPlusLogger.WarnOnce(NekoThemesPlusLocalization.Text(
                    "未能修改编辑器样式：",
                    "Editor styles were not changed: ") + exception.Message);
                Restore();
            }
        }

        public static void Restore()
        {
            Backup.Restore();
            applied = false;
            appliedConservative = false;
            appliedExtended = false;
            if (transparentTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(transparentTexture);
                transparentTexture = null;
            }
        }

        private static void MakeTransparent(GUIStyle style)
        {
            style.normal.background = transparentTexture;
            style.hover.background = transparentTexture;
            style.active.background = transparentTexture;
            style.focused.background = transparentTexture;
            style.onNormal.background = transparentTexture;
            style.onHover.background = transparentTexture;
            style.onActive.background = transparentTexture;
            style.onFocused.background = transparentTexture;
        }

        private static void ApplyStyles(GUISkin skin, string[] names)
        {
            foreach (string styleName in names)
            {
                GUIStyle style = skin.FindStyle(styleName);
                if (style == null) continue;
                Backup.Capture(style);
                MakeTransparent(style);
            }
        }

        private static void EnsureTransparentTexture()
        {
            if (transparentTexture != null)
            {
                return;
            }

            transparentTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            transparentTexture.name = "NekoThemesPlus Transparent Style";
            transparentTexture.hideFlags = HideFlags.HideAndDontSave;
            transparentTexture.SetPixel(0, 0, Color.clear);
            transparentTexture.Apply(false, true);
        }
    }
}
