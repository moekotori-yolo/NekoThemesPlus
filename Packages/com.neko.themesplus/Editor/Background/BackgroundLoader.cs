using System;
using System.IO;
using NekoThemesPlus.Core;
using UnityEngine;

namespace NekoThemesPlus.Background
{
    internal static class BackgroundLoader
    {
        public static bool TryLoadExternal(string path, int maxResolution, out Texture2D texture, out string error)
        {
            texture = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = NekoThemesPlusLocalization.Text("尚未选择背景图片。", "No background image selected.");
                return false;
            }

            if (!File.Exists(path))
            {
                error = NekoThemesPlusLocalization.Text("找不到背景图片：", "Background image was not found: ") + path;
                return false;
            }

            string extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                error = NekoThemesPlusLocalization.Text("仅支持 PNG、JPG 和 JPEG 图片。", "Only PNG, JPG, and JPEG images are supported.");
                return false;
            }

            Texture2D loaded = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                loaded.name = "NekoThemesPlus Background";
                loaded.hideFlags = HideFlags.HideAndDontSave;

                if (!loaded.LoadImage(bytes, true))
                {
                    Destroy(loaded);
                    error = NekoThemesPlusLocalization.Text("Unity 无法解码所选图片。", "Unity could not decode the selected image.");
                    return false;
                }

                loaded.wrapMode = TextureWrapMode.Clamp;
                loaded.filterMode = FilterMode.Bilinear;
                texture = LimitResolution(loaded, Mathf.Clamp(maxResolution, 512, 8192));
                return true;
            }
            catch (Exception exception)
            {
                Destroy(loaded);
                error = NekoThemesPlusLocalization.Text("无法加载背景图片：", "Could not load background image: ") + exception.Message;
                return false;
            }
        }

        private static Texture2D LimitResolution(Texture2D source, int maximum)
        {
            int largestSide = Mathf.Max(source.width, source.height);
            if (largestSide <= maximum)
            {
                return source;
            }

            float scale = maximum / (float)largestSide;
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
                resized.name = source.name + " (Limited)";
                resized.hideFlags = HideFlags.HideAndDontSave;
                resized.wrapMode = TextureWrapMode.Clamp;
                resized.filterMode = FilterMode.Bilinear;
                resized.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                resized.Apply(false, true);
                Destroy(source);
                return resized;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        public static void Destroy(Texture texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
