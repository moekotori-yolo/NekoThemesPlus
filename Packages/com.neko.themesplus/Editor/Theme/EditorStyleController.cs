using System;
using System.Collections.Generic;
using System.Reflection;
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
        private static bool appliedTextColors;
        private static Color appliedPrimaryTextColor;
        private static Color appliedSecondaryTextColor;

        public static int ThemedStyleCount { get { return appliedTextColors ? Backup.Count : 0; } }

        public static void Apply()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            bool useConservative = settings.experimentalIMGUI || settings.experimentalInternalStyles;
            bool useExtended = settings.experimentalInternalStyles;
            bool useTextColors = settings.enableTextColors;
            if ((!useConservative && !useExtended && !useTextColors) || NekoThemesPlusSafeMode.IsActive)
            {
                Restore();
                return;
            }

            if (applied &&
                appliedConservative == useConservative &&
                appliedExtended == useExtended &&
                appliedTextColors == useTextColors &&
                (!useTextColors ||
                 (Approximately(appliedPrimaryTextColor, settings.primaryTextColor) &&
                  Approximately(appliedSecondaryTextColor, settings.secondaryTextColor))))
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

                if (useTextColors)
                {
                    ApplyTextColors(skin, settings.primaryTextColor, settings.secondaryTextColor);
                }

                applied = true;
                appliedConservative = useConservative;
                appliedExtended = useExtended;
                appliedTextColors = useTextColors;
                appliedPrimaryTextColor = settings.primaryTextColor;
                appliedSecondaryTextColor = settings.secondaryTextColor;
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
            appliedTextColors = false;
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

        private static void ApplyTextColors(GUISkin skin, Color primary, Color secondary)
        {
            HashSet<GUIStyle> styles = new HashSet<GUIStyle>();
            AddSkinStyles(styles, skin);
            AddEditorStyles(styles);

            foreach (GUIStyle style in styles)
            {
                if (style == null) continue;
                Backup.Capture(style);
                SetTextColor(style, IsSecondaryStyle(style) ? secondary : primary);
            }
        }

        private static void AddSkinStyles(HashSet<GUIStyle> styles, GUISkin skin)
        {
            styles.Add(skin.box);
            styles.Add(skin.button);
            styles.Add(skin.toggle);
            styles.Add(skin.label);
            styles.Add(skin.textField);
            styles.Add(skin.textArea);
            styles.Add(skin.window);
            styles.Add(skin.horizontalSlider);
            styles.Add(skin.horizontalSliderThumb);
            styles.Add(skin.verticalSlider);
            styles.Add(skin.verticalSliderThumb);
            styles.Add(skin.horizontalScrollbar);
            styles.Add(skin.horizontalScrollbarThumb);
            styles.Add(skin.horizontalScrollbarLeftButton);
            styles.Add(skin.horizontalScrollbarRightButton);
            styles.Add(skin.verticalScrollbar);
            styles.Add(skin.verticalScrollbarThumb);
            styles.Add(skin.verticalScrollbarUpButton);
            styles.Add(skin.verticalScrollbarDownButton);

            if (skin.customStyles == null) return;
            foreach (GUIStyle style in skin.customStyles)
            {
                styles.Add(style);
            }
        }

        private static void AddEditorStyles(HashSet<GUIStyle> styles)
        {
            PropertyInfo[] properties = typeof(EditorStyles).GetProperties(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType != typeof(GUIStyle) || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                try
                {
                    styles.Add(property.GetValue(null, null) as GUIStyle);
                }
                catch
                {
                    // A few Unity-internal style accessors are unavailable in some layouts.
                }
            }
        }

        private static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private static bool IsSecondaryStyle(GUIStyle style)
        {
            string name = (style.name ?? string.Empty).ToLowerInvariant();
            return name.Contains("mini") || name.Contains("grey") || name.Contains("gray") ||
                   name.Contains("disabled") || name.Contains("placeholder") || name.Contains("help");
        }

        private static bool Approximately(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) < 0.001f &&
                   Mathf.Abs(left.g - right.g) < 0.001f &&
                   Mathf.Abs(left.b - right.b) < 0.001f &&
                   Mathf.Abs(left.a - right.a) < 0.001f;
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
