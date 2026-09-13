using System.Collections.Generic;
using UnityEngine;

namespace NekoThemesPlus.Theme
{
    internal sealed class EditorStyleBackup
    {
        private readonly Dictionary<GUIStyle, StyleStateBackup> backups = new Dictionary<GUIStyle, StyleStateBackup>();

        private struct StyleStateBackup
        {
            public Texture2D normal;
            public Texture2D hover;
            public Texture2D active;
            public Texture2D focused;
            public Texture2D onNormal;
            public Texture2D onHover;
            public Texture2D onActive;
            public Texture2D onFocused;
            public Color normalText;
            public Color hoverText;
            public Color activeText;
            public Color focusedText;
            public Color onNormalText;
            public Color onHoverText;
            public Color onActiveText;
            public Color onFocusedText;
        }

        public int Count { get { return backups.Count; } }

        public void Capture(GUIStyle style)
        {
            if (style == null || backups.ContainsKey(style))
            {
                return;
            }

            backups.Add(style, new StyleStateBackup
            {
                normal = style.normal.background,
                hover = style.hover.background,
                active = style.active.background,
                focused = style.focused.background,
                onNormal = style.onNormal.background,
                onHover = style.onHover.background,
                onActive = style.onActive.background,
                onFocused = style.onFocused.background,
                normalText = style.normal.textColor,
                hoverText = style.hover.textColor,
                activeText = style.active.textColor,
                focusedText = style.focused.textColor,
                onNormalText = style.onNormal.textColor,
                onHoverText = style.onHover.textColor,
                onActiveText = style.onActive.textColor,
                onFocusedText = style.onFocused.textColor
            });
        }

        public void Restore()
        {
            foreach (KeyValuePair<GUIStyle, StyleStateBackup> pair in backups)
            {
                GUIStyle style = pair.Key;
                StyleStateBackup backup = pair.Value;
                if (style == null) continue;
                style.normal.background = backup.normal;
                style.hover.background = backup.hover;
                style.active.background = backup.active;
                style.focused.background = backup.focused;
                style.onNormal.background = backup.onNormal;
                style.onHover.background = backup.onHover;
                style.onActive.background = backup.onActive;
                style.onFocused.background = backup.onFocused;
                style.normal.textColor = backup.normalText;
                style.hover.textColor = backup.hoverText;
                style.active.textColor = backup.activeText;
                style.focused.textColor = backup.focusedText;
                style.onNormal.textColor = backup.onNormalText;
                style.onHover.textColor = backup.onHoverText;
                style.onActive.textColor = backup.onActiveText;
                style.onFocused.textColor = backup.onFocusedText;
            }

            backups.Clear();
        }
    }
}
