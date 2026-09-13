using System;
using System.Collections.Generic;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Rendering
{
    internal sealed class BackgroundProcessor : IDisposable
    {
        private static readonly int SourceSizeId = Shader.PropertyToID("_SourceSize");
        private static readonly int TargetSizeId = Shader.PropertyToID("_TargetSize");
        private static readonly int ImageModeId = Shader.PropertyToID("_ImageMode");
        private static readonly int ImageZoomId = Shader.PropertyToID("_ImageZoom");
        private static readonly int ImageAlignmentId = Shader.PropertyToID("_ImageAlignment");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int OffsetId = Shader.PropertyToID("_Offset");

        private Material backgroundMaterial;
        private Material colorMaterial;
        private Material blurMaterial;

        public RenderTexture ProcessedTexture { get; private set; }

        public bool Rebuild(Texture2D source, int width, int height, NekoThemesPlusSettings settings, out string error)
        {
            error = string.Empty;
            if (source == null)
            {
                ReleaseProcessedTexture();
                return true;
            }

            if (!EnsureMaterials(out error))
            {
                return false;
            }

            width = Mathf.Max(16, width);
            height = Mathf.Max(16, height);
            EnsureProcessedTexture(width, height);

            RenderTexture composed = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture adjusted = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            composed.filterMode = FilterMode.Bilinear;
            composed.wrapMode = TextureWrapMode.Clamp;
            adjusted.filterMode = FilterMode.Bilinear;
            adjusted.wrapMode = TextureWrapMode.Clamp;

            try
            {
                ConfigureBackgroundMaterial(source, width, height, settings.backgroundMode);
                backgroundMaterial.SetFloat(ImageZoomId, Mathf.Clamp(settings.backgroundZoom, 1f, 4f));
                backgroundMaterial.SetVector(ImageAlignmentId, new Vector4(
                    Mathf.Clamp01(settings.backgroundAlignment.x),
                    Mathf.Clamp01(settings.backgroundAlignment.y), 0f, 0f));
                Graphics.Blit(source, composed, backgroundMaterial, 0);
                ConfigureColorMaterial(settings);
                Graphics.Blit(composed, adjusted, colorMaterial, 0);

                int iterations = GetBlurIterations(settings.blurAmount);
                if (iterations == 0)
                {
                    Graphics.Blit(adjusted, ProcessedTexture);
                }
                else
                {
                    ApplyDualKawase(adjusted, ProcessedTexture, iterations, settings.blurAmount);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = NekoThemesPlusLocalization.Text("背景处理失败：", "Background processing failed: ") + exception.Message;
                return false;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(composed);
                RenderTexture.ReleaseTemporary(adjusted);
            }
        }

        private bool EnsureMaterials(out string error)
        {
            error = string.Empty;
            if (backgroundMaterial == null)
            {
                Shader shader = FindShader("Hidden/NekoThemesPlus/Background", "NekoBackground.shader");
                if (shader == null)
                {
                    error = NekoThemesPlusLocalization.Text("找不到背景 Shader：", "Background shader was not found: ") + "Hidden/NekoThemesPlus/Background";
                    return false;
                }

                backgroundMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (colorMaterial == null)
            {
                Shader shader = FindShader("Hidden/NekoThemesPlus/ColorAdjust", "NekoColorAdjust.shader");
                if (shader == null)
                {
                    error = NekoThemesPlusLocalization.Text("找不到调色 Shader：", "Color-adjust shader was not found: ") + "Hidden/NekoThemesPlus/ColorAdjust";
                    return false;
                }

                colorMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (blurMaterial == null)
            {
                Shader shader = FindShader("Hidden/NekoThemesPlus/Blur", "NekoBlur.shader");
                if (shader == null)
                {
                    error = NekoThemesPlusLocalization.Text("找不到模糊 Shader：", "Blur shader was not found: ") + "Hidden/NekoThemesPlus/Blur";
                    return false;
                }

                blurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            return true;
        }

        private static Shader FindShader(string shaderName, string fileName)
        {
            Shader shader = Shader.Find(shaderName);
            return shader != null
                ? shader
                : AssetDatabase.LoadAssetAtPath<Shader>(NekoThemesPlusConstants.PackageRoot + "/Editor/Shaders/" + fileName);
        }

        private void ConfigureBackgroundMaterial(Texture2D source, int width, int height, BackgroundImageMode mode)
        {
            backgroundMaterial.SetVector(SourceSizeId, new Vector4(source.width, source.height, 1f / source.width, 1f / source.height));
            backgroundMaterial.SetVector(TargetSizeId, new Vector4(width, height, 1f / width, 1f / height));
            backgroundMaterial.SetFloat(ImageModeId, (float)mode);
        }

        private void ConfigureColorMaterial(NekoThemesPlusSettings settings)
        {
            colorMaterial.SetFloat(BrightnessId, settings.brightness);
            colorMaterial.SetFloat(SaturationId, settings.saturation);
            colorMaterial.SetFloat(ContrastId, settings.contrast);
            colorMaterial.SetColor(TintId, settings.backgroundTint);
            colorMaterial.SetFloat(OpacityId, settings.backgroundOpacity);
        }

        private void ApplyDualKawase(RenderTexture source, RenderTexture destination, int iterations, float amount)
        {
            List<RenderTexture> down = new List<RenderTexture>(iterations);
            List<RenderTexture> temporaries = new List<RenderTexture>(iterations + 1);
            RenderTexture current = source;

            try
            {
                for (int index = 0; index < iterations; index++)
                {
                    int width = Mathf.Max(2, current.width / 2);
                    int height = Mathf.Max(2, current.height / 2);
                    RenderTexture target = GetTemporary(width, height);
                    temporaries.Add(target);
                    blurMaterial.SetFloat(OffsetId, 1f + amount / 25f + index * 0.35f);
                    Graphics.Blit(current, target, blurMaterial, 0);
                    down.Add(target);
                    current = target;
                }

                for (int index = down.Count - 2; index >= 0; index--)
                {
                    int width = down[index].width;
                    int height = down[index].height;
                    ReleaseTemporary(temporaries, down[index]);
                    down[index] = null;

                    RenderTexture target = GetTemporary(width, height);
                    temporaries.Add(target);
                    blurMaterial.SetFloat(OffsetId, 0.75f + amount / 30f + index * 0.25f);
                    Graphics.Blit(current, target, blurMaterial, 1);
                    ReleaseTemporary(temporaries, current);
                    current = target;
                }

                blurMaterial.SetFloat(OffsetId, 0.75f + amount / 30f);
                Graphics.Blit(current, destination, blurMaterial, 1);
            }
            finally
            {
                foreach (RenderTexture texture in temporaries)
                {
                    RenderTexture.ReleaseTemporary(texture);
                }
            }
        }

        private static RenderTexture GetTemporary(int width, int height)
        {
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static void ReleaseTemporary(List<RenderTexture> temporaries, RenderTexture texture)
        {
            if (texture != null && temporaries.Remove(texture))
            {
                RenderTexture.ReleaseTemporary(texture);
            }
        }

        private static int GetBlurIterations(float amount)
        {
            if (amount <= 0.01f)
            {
                return 0;
            }

            if (amount <= 10f)
            {
                return 2;
            }

            if (amount <= 18f)
            {
                return 3;
            }

            if (amount <= 25f)
            {
                return 4;
            }

            if (amount <= 38f)
            {
                return 5;
            }

            return 6;
        }

        private void EnsureProcessedTexture(int width, int height)
        {
            if (ProcessedTexture != null && ProcessedTexture.width == width && ProcessedTexture.height == height)
            {
                return;
            }

            ReleaseProcessedTexture();
            ProcessedTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            ProcessedTexture.name = "NekoThemesPlus Processed Background";
            ProcessedTexture.hideFlags = HideFlags.HideAndDontSave;
            ProcessedTexture.filterMode = FilterMode.Bilinear;
            ProcessedTexture.wrapMode = TextureWrapMode.Clamp;
            ProcessedTexture.Create();
        }

        private void ReleaseProcessedTexture()
        {
            if (ProcessedTexture != null)
            {
                ProcessedTexture.Release();
                UnityEngine.Object.DestroyImmediate(ProcessedTexture);
                ProcessedTexture = null;
            }
        }

        public void Dispose()
        {
            ReleaseProcessedTexture();
            if (backgroundMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(backgroundMaterial);
                backgroundMaterial = null;
            }

            if (colorMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(colorMaterial);
                colorMaterial = null;
            }

            if (blurMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(blurMaterial);
                blurMaterial = null;
            }
        }
    }
}
