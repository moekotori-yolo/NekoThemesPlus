using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using NekoThemesPlus.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace NekoThemesPlus.Windows
{
    internal sealed class WindowThemeController
    {
        private readonly EditorWindow window;
        private readonly WindowKind kind;
        private NekoBackgroundElement backgroundElement;
        private NekoGlassElement glassElement;
        private readonly List<InlineBackgroundBackup> inlineBackgrounds = new List<InlineBackgroundBackup>();
        private readonly Dictionary<VisualElement, StyleColor> inlineTextColors = new Dictionary<VisualElement, StyleColor>();

        private struct InlineBackgroundBackup
        {
            public VisualElement element;
            public StyleColor backgroundColor;
            public StyleBackground backgroundImage;
        }

        public WindowThemeController(EditorWindow window, WindowKind kind)
        {
            this.window = window;
            this.kind = kind;
        }

        public bool IsAttached
        {
            get { return backgroundElement != null && backgroundElement.parent != null; }
        }

        public EditorWindow Window { get { return window; } }
        public WindowKind Kind { get { return kind; } }
        public int ThemedTextCount { get { return inlineTextColors.Count; } }

        public bool Attach()
        {
            if (window == null || window.rootVisualElement == null)
            {
                return false;
            }

            VisualElement root = window.rootVisualElement;
            VisualElement existingBackground = root.Q<VisualElement>("neko-themes-plus-background");
            VisualElement existingGlass = root.Q<VisualElement>("neko-themes-plus-glass");
            if (existingBackground != null) existingBackground.RemoveFromHierarchy();
            if (existingGlass != null) existingGlass.RemoveFromHierarchy();

            backgroundElement = new NekoBackgroundElement();
            glassElement = new NekoGlassElement();
            ConfigureSafeBounds();
            root.Insert(0, backgroundElement);
            root.Insert(1, glassElement);
            MakeLegacyContainersTransparent(root);
            Refresh();
            window.Repaint();
            return true;
        }

        public void Refresh()
        {
            if (window == null)
            {
                return;
            }

            if (!IsAttached)
            {
                Attach();
                return;
            }

            Rect themedRect = window.position;
            if (kind == WindowKind.SceneView || kind == WindowKind.GameView)
            {
                themedRect.height = Mathf.Min(24f, themedRect.height);
            }

            Rect uv;
            Texture texture = BackgroundManager.GetProcessedTexture(
                kind,
                window.GetInstanceID(),
                themedRect,
                out uv);
            backgroundElement.SetBackground(texture, uv);

            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            float opacity = Mathf.Clamp01(settings.globalPanelOpacity * GetWindowOpacity(settings));
            glassElement.SetTint(settings.panelTint, opacity);
            RefreshTextColors();
            window.Repaint();
        }

        public void RefreshDynamicStyles()
        {
            if (window == null || !IsAttached)
            {
                return;
            }

            RefreshTextColors();
        }

        public void Detach()
        {
            if (backgroundElement != null)
            {
                backgroundElement.RemoveFromHierarchy();
                backgroundElement = null;
            }

            if (glassElement != null)
            {
                glassElement.RemoveFromHierarchy();
                glassElement = null;
            }

            RestoreLegacyContainers();
            RestoreTextColors();

            if (window != null)
            {
                window.Repaint();
            }
        }

        private float GetWindowOpacity(NekoThemesPlusSettings settings)
        {
            switch (kind)
            {
                case WindowKind.Hierarchy: return settings.hierarchyOpacity;
                case WindowKind.Inspector: return settings.inspectorOpacity;
                case WindowKind.Project: return settings.projectOpacity;
                case WindowKind.Console: return settings.consoleOpacity;
                case WindowKind.SceneView: return settings.sceneOpacity;
                case WindowKind.GameView: return settings.gameOpacity;
                default: return 1f;
            }
        }

        private void ConfigureSafeBounds()
        {
            if (kind != WindowKind.SceneView && kind != WindowKind.GameView)
            {
                return;
            }

            // Only the toolbar strip is themed. The camera render area begins below it and
            // remains completely untouched by NekoThemesPlus.
            backgroundElement.style.bottom = StyleKeyword.Auto;
            backgroundElement.style.height = 24f;
            glassElement.style.bottom = StyleKeyword.Auto;
            glassElement.style.height = 24f;
        }

        private void MakeLegacyContainersTransparent(VisualElement root)
        {
            RestoreLegacyContainers();
            CollectLegacyContainers(root);
            foreach (InlineBackgroundBackup backup in inlineBackgrounds)
            {
                if (backup.element != null)
                {
                    backup.element.style.backgroundColor = Color.clear;
                    backup.element.style.backgroundImage = StyleKeyword.None;
                }
            }
        }

        private void CollectLegacyContainers(VisualElement element)
        {
            for (int index = 0; index < element.childCount; index++)
            {
                VisualElement child = element[index];
                if (child is IMGUIContainer)
                {
                    inlineBackgrounds.Add(new InlineBackgroundBackup
                    {
                        element = child,
                        backgroundColor = child.style.backgroundColor,
                        backgroundImage = child.style.backgroundImage
                    });
                }

                CollectLegacyContainers(child);
            }
        }

        private void RestoreLegacyContainers()
        {
            foreach (InlineBackgroundBackup backup in inlineBackgrounds)
            {
                if (backup.element != null)
                {
                    backup.element.style.backgroundColor = backup.backgroundColor;
                    backup.element.style.backgroundImage = backup.backgroundImage;
                }
            }

            inlineBackgrounds.Clear();
        }

        private void RefreshTextColors()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            if (!settings.enableTextColors || NekoThemesPlusSafeMode.IsActive)
            {
                RestoreTextColors();
                return;
            }

            VisualElement root = window.rootVisualElement;
            if (root == null)
            {
                RestoreTextColors();
                return;
            }

            RestoreDetachedTextElements();
            ApplyTextColors(root, settings.primaryTextColor, settings.secondaryTextColor);
        }

        private void ApplyTextColors(VisualElement element, Color primary, Color secondary)
        {
            if (element is TextElement)
            {
                if (!inlineTextColors.ContainsKey(element))
                {
                    inlineTextColors.Add(element, element.style.color);
                }

                element.style.color = IsSecondaryText(element) ? secondary : primary;
            }

            for (int index = 0; index < element.childCount; index++)
            {
                ApplyTextColors(element[index], primary, secondary);
            }
        }

        private static bool IsSecondaryText(VisualElement element)
        {
            if (!element.enabledInHierarchy)
            {
                return true;
            }

            string name = (element.name ?? string.Empty).ToLowerInvariant();
            if (name.Contains("secondary") || name.Contains("description") ||
                name.Contains("placeholder") || name.Contains("help") || name.Contains("hint"))
            {
                return true;
            }

            float fontSize = element.resolvedStyle.fontSize;
            return fontSize > 0f && fontSize <= 10f;
        }

        private void RestoreDetachedTextElements()
        {
            List<VisualElement> detached = null;
            foreach (KeyValuePair<VisualElement, StyleColor> pair in inlineTextColors)
            {
                if (pair.Key == null || pair.Key.panel != null)
                {
                    continue;
                }

                if (pair.Key != null)
                {
                    pair.Key.style.color = pair.Value;
                }

                if (detached == null) detached = new List<VisualElement>();
                detached.Add(pair.Key);
            }

            if (detached == null) return;
            foreach (VisualElement element in detached)
            {
                inlineTextColors.Remove(element);
            }
        }

        private void RestoreTextColors()
        {
            foreach (KeyValuePair<VisualElement, StyleColor> pair in inlineTextColors)
            {
                if (pair.Key != null)
                {
                    pair.Key.style.color = pair.Value;
                }
            }

            inlineTextColors.Clear();
        }
    }
}
