using UnityEngine;
using UnityEngine.UIElements;

namespace NekoThemesPlus.Rendering
{
    internal sealed class NekoBackgroundElement : VisualElement
    {
        private Texture texture;
        private Rect uvRect = new Rect(0f, 0f, 1f, 1f);

        public NekoBackgroundElement()
        {
            name = "neko-themes-plus-background";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.right = 0f;
            style.top = 0f;
            style.bottom = 0f;
            generateVisualContent += GenerateBackground;
        }

        public void SetBackground(Texture value, Rect coordinates)
        {
            if (texture == value && uvRect == coordinates)
            {
                return;
            }

            texture = value;
            uvRect = coordinates;
            style.display = texture != null ? DisplayStyle.Flex : DisplayStyle.None;
            MarkDirtyRepaint();
        }

        private void GenerateBackground(MeshGenerationContext context)
        {
            if (texture == null || contentRect.width <= 0f || contentRect.height <= 0f)
            {
                return;
            }

            MeshWriteData mesh = context.Allocate(4, 6, texture);
            Rect atlas = mesh.uvRegion;
            Vector2 uvMin = new Vector2(
                atlas.x + uvRect.xMin * atlas.width,
                atlas.y + uvRect.yMin * atlas.height);
            Vector2 uvMax = new Vector2(
                atlas.x + uvRect.xMax * atlas.width,
                atlas.y + uvRect.yMax * atlas.height);
            Color32 white = new Color32(255, 255, 255, 255);
            float z = Vertex.nearZ;

            Vertex[] vertices =
            {
                new Vertex { position = new Vector3(contentRect.xMin, contentRect.yMin, z), tint = white, uv = new Vector2(uvMin.x, uvMax.y) },
                new Vertex { position = new Vector3(contentRect.xMax, contentRect.yMin, z), tint = white, uv = new Vector2(uvMax.x, uvMax.y) },
                new Vertex { position = new Vector3(contentRect.xMax, contentRect.yMax, z), tint = white, uv = new Vector2(uvMax.x, uvMin.y) },
                new Vertex { position = new Vector3(contentRect.xMin, contentRect.yMax, z), tint = white, uv = new Vector2(uvMin.x, uvMin.y) }
            };
            ushort[] indices = { 0, 1, 2, 2, 3, 0 };
            mesh.SetAllVertices(vertices);
            mesh.SetAllIndices(indices);
        }
    }
}
