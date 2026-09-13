using UnityEngine;
using UnityEngine.UIElements;

namespace NekoThemesPlus.Rendering
{
    internal sealed class NekoGlassElement : VisualElement
    {
        public NekoGlassElement()
        {
            name = "neko-themes-plus-glass";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.right = 0f;
            style.top = 0f;
            style.bottom = 0f;
        }

        public void SetTint(Color tint, float opacity)
        {
            tint.a = Mathf.Clamp01(opacity);
            style.backgroundColor = tint;
        }
    }
}
