using System.Collections.Generic;
using NekoThemesPlus.Core;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Theme
{
    public static class SelectionStyleController
    {
        private static readonly HashSet<int> SelectedInstanceIds = new HashSet<int>();
        private static readonly HashSet<string> SelectedAssetGuids = new HashSet<string>();
        private static bool applied;

        public static void Apply()
        {
            if (applied || NekoThemesPlusSafeMode.IsActive)
            {
                return;
            }

            applied = true;
            Selection.selectionChanged += RefreshSelection;
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyAccent;
            EditorApplication.projectWindowItemOnGUI += DrawProjectAccent;
            RefreshSelection();
        }

        public static void Restore()
        {
            if (!applied)
            {
                return;
            }

            applied = false;
            Selection.selectionChanged -= RefreshSelection;
            EditorApplication.hierarchyWindowItemOnGUI -= DrawHierarchyAccent;
            EditorApplication.projectWindowItemOnGUI -= DrawProjectAccent;
            SelectedInstanceIds.Clear();
            SelectedAssetGuids.Clear();
        }

        private static void RefreshSelection()
        {
            SelectedInstanceIds.Clear();
            SelectedAssetGuids.Clear();
            foreach (Object selected in Selection.objects)
            {
                if (selected == null) continue;
                SelectedInstanceIds.Add(selected.GetInstanceID());
                string path = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedAssetGuids.Add(AssetDatabase.AssetPathToGUID(path));
                }
            }

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void DrawHierarchyAccent(int instanceId, Rect rect)
        {
            if (SelectedInstanceIds.Contains(instanceId))
            {
                DrawAccent(rect);
            }
        }

        private static void DrawProjectAccent(string guid, Rect rect)
        {
            if (SelectedAssetGuids.Contains(guid))
            {
                DrawAccent(rect);
            }
        }

        private static void DrawAccent(Rect rect)
        {
            Color color = NekoThemesPlusSettings.instance.selectionColor;
            color.a = Mathf.Clamp01(NekoThemesPlusSettings.instance.selectionOpacity);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), color);
        }
    }
}
