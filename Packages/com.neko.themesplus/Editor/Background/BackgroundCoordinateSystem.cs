using NekoThemesPlus.Reflection;
using UnityEngine;

namespace NekoThemesPlus.Background
{
    public static class BackgroundCoordinateSystem
    {
        public static bool IsLocalMode { get; private set; }

        public static Rect GetUvRect(Rect windowRect)
        {
            Rect mainWindowRect;
            if (!MainWindowReflection.TryGetMainWindowRect(out mainWindowRect))
            {
                IsLocalMode = true;
                return new Rect(0f, 0f, 1f, 1f);
            }

            IsLocalMode = false;
            return GetUvRect(windowRect, mainWindowRect, true);
        }

        public static Rect GetUvRect(Rect windowRect, Rect mainWindowRect, bool flipY)
        {
            if (mainWindowRect.width <= 0f || mainWindowRect.height <= 0f)
            {
                IsLocalMode = true;
                return new Rect(0f, 0f, 1f, 1f);
            }

            // A detached window can live on another monitor and no longer belongs to the
            // main-window canvas. Sampling outside that canvas would clamp to an edge pixel,
            // so give a non-overlapping floating window its own complete background instead.
            float overlapWidth = Mathf.Min(windowRect.xMax, mainWindowRect.xMax) - Mathf.Max(windowRect.xMin, mainWindowRect.xMin);
            float overlapHeight = Mathf.Min(windowRect.yMax, mainWindowRect.yMax) - Mathf.Max(windowRect.yMin, mainWindowRect.yMin);
            if (overlapWidth <= 0f || overlapHeight <= 0f)
            {
                IsLocalMode = true;
                return new Rect(0f, 0f, 1f, 1f);
            }

            IsLocalMode = false;
            float x = (windowRect.x - mainWindowRect.x) / mainWindowRect.width;
            float top = (windowRect.y - mainWindowRect.y) / mainWindowRect.height;
            float width = windowRect.width / mainWindowRect.width;
            float height = windowRect.height / mainWindowRect.height;
            float y = flipY ? 1f - top - height : top;
            return new Rect(x, y, width, height);
        }
    }
}
