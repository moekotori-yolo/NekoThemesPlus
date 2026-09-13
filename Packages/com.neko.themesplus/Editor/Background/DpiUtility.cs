using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Background
{
    public static class DpiUtility
    {
        public static float PixelsPerPoint
        {
            get { return Mathf.Max(1f, EditorGUIUtility.pixelsPerPoint); }
        }

        public static Vector2Int LogicalToPhysicalSize(float width, float height)
        {
            float scale = PixelsPerPoint;
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }
    }
}
