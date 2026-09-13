using System;
using System.IO;
using System.Reflection;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using NekoThemesPlus.Windows;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NekoThemesPlus.Tests
{
    public sealed class ReleaseSmokeTests
    {
        [Test]
        public void HostViewContract_IsAvailable()
        {
            Type hostView = typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView", false);
            Assert.That(hostView, Is.Not.Null, "UnityEditor.HostView was not found.");

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Assert.That(hostView.GetField("m_OnGUI", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(hostView.GetProperty("actualView", flags), Is.Not.Null);
        }

        [TestCase("Hidden/NekoThemesPlus/Background")]
        [TestCase("Hidden/NekoThemesPlus/ColorAdjust")]
        [TestCase("Hidden/NekoThemesPlus/Blur")]
        public void RequiredShader_IsDiscoverable(string shaderName)
        {
            Assert.That(Shader.Find(shaderName), Is.Not.Null, shaderName + " was not found.");
        }

        [Test]
        public void ThemeFile_CanRoundTripWithPerWindowImage()
        {
            Type themeType = Type.GetType(
                "NekoThemesPlus.Theme.NekoThemeImportExport, NekoThemesPlus.Editor",
                false);
            Assert.That(themeType, Is.Not.Null, "Theme import/export type was not found.");

            MethodInfo export = themeType.GetMethod("Export", BindingFlags.Static | BindingFlags.Public);
            MethodInfo import = themeType.GetMethod("Import", BindingFlags.Static | BindingFlags.Public);
            Assert.That(export, Is.Not.Null);
            Assert.That(import, Is.Not.Null);

            string path = Path.Combine(Path.GetTempPath(), "NekoThemesPlus.EditModeTests.nekotheme");
            string imagePath = Path.Combine(Path.GetTempPath(), "NekoThemesPlus.EditModeTests.png");
            string oldGlobalPath = NekoThemesPlusSettings.instance.backgroundPath;
            string oldHierarchyPath = BackgroundManager.GetBackgroundPath(WindowKind.Hierarchy);
            string importedHierarchyPath = string.Empty;
            try
            {
                File.WriteAllBytes(imagePath, Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
                BackgroundManager.ClearBackground();
                Assert.That(BackgroundManager.SetWindowBackground(WindowKind.Hierarchy, imagePath), Is.True);
                Rect uv;
                Texture windowTexture = BackgroundManager.GetProcessedTexture(
                    WindowKind.Hierarchy,
                    -9137,
                    new Rect(0f, 0f, 320f, 240f),
                    out uv);
                Assert.That(windowTexture, Is.Not.Null);
                Assert.That(windowTexture.width, Is.EqualTo(320));
                Assert.That(windowTexture.height, Is.EqualTo(240));
                Assert.That(uv, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
                BackgroundManager.ReleaseWindowCache(-9137);

                object[] exportArgs = { path, null };
                Assert.That((bool)export.Invoke(null, exportArgs), Is.True, exportArgs[1] as string);
                Assert.That(File.Exists(path), Is.True);
                string json = File.ReadAllText(path);
                StringAssert.Contains("\"schemaVersion\": 2", json);
                StringAssert.Contains("\"hierarchyBackgroundBase64\"", json);

                BackgroundManager.ClearWindowBackground(WindowKind.Hierarchy);
                object[] importArgs = { path, null };
                Assert.That((bool)import.Invoke(null, importArgs), Is.True, importArgs[1] as string);
                importedHierarchyPath = BackgroundManager.GetBackgroundPath(WindowKind.Hierarchy);
                Assert.That(importedHierarchyPath, Is.Not.Empty);
                Assert.That(File.Exists(importedHierarchyPath), Is.True);
            }
            finally
            {
                BackgroundManager.ClearWindowBackground(WindowKind.Hierarchy);
                BackgroundManager.ClearBackground();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }

                if (!string.IsNullOrEmpty(importedHierarchyPath) && File.Exists(importedHierarchyPath))
                {
                    File.Delete(importedHierarchyPath);
                }

                if (!string.IsNullOrEmpty(oldGlobalPath) && File.Exists(oldGlobalPath))
                {
                    BackgroundManager.SetBackground(oldGlobalPath);
                }

                if (!string.IsNullOrEmpty(oldHierarchyPath) && File.Exists(oldHierarchyPath))
                {
                    BackgroundManager.SetWindowBackground(WindowKind.Hierarchy, oldHierarchyPath);
                }
            }
        }
    }
}
