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

            Rect uv = BackgroundCoordinateSystem.GetUvRect(themedRect);
            backgroundElement.SetBackground(BackgroundManager.ProcessedTexture, uv);

            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            float opacity = Mathf.Clamp01(settings.globalPanelOpacity * GetWindowOpacity(settings));
            glassElement.SetTint(settings.panelTint, opacity);
            window.Repaint();
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
    }
}
