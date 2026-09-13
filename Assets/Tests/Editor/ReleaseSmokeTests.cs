using System;
using System.IO;
using System.Reflection;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using NekoThemesPlus.Theme;
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
            bool oldTextEnabled = NekoThemesPlusSettings.instance.enableTextColors;
            Color oldPrimaryText = NekoThemesPlusSettings.instance.primaryTextColor;
            Color oldSecondaryText = NekoThemesPlusSettings.instance.secondaryTextColor;
            string importedHierarchyPath = string.Empty;
            try
            {
                File.WriteAllBytes(imagePath, Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
                BackgroundManager.ClearBackground();
                NekoThemesPlusSettings.instance.enableTextColors = true;
                NekoThemesPlusSettings.instance.primaryTextColor = new Color32(70, 47, 61, 255);
                NekoThemesPlusSettings.instance.secondaryTextColor = new Color32(122, 86, 105, 255);
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
                StringAssert.Contains("\"schemaVersion\": 3", json);
                StringAssert.Contains("\"hierarchyBackgroundBase64\"", json);
                StringAssert.Contains("\"enableTextColors\"", json);

                BackgroundManager.ClearWindowBackground(WindowKind.Hierarchy);
                NekoThemesPlusSettings.instance.enableTextColors = false;
                object[] importArgs = { path, null };
                Assert.That((bool)import.Invoke(null, importArgs), Is.True, importArgs[1] as string);
                importedHierarchyPath = BackgroundManager.GetBackgroundPath(WindowKind.Hierarchy);
                Assert.That(importedHierarchyPath, Is.Not.Empty);
                Assert.That(File.Exists(importedHierarchyPath), Is.True);
                Assert.That(NekoThemesPlusSettings.instance.enableTextColors, Is.True);
                Assert.That(NekoThemesPlusSettings.instance.primaryTextColor, Is.EqualTo((Color)new Color32(70, 47, 61, 255)));
                Assert.That(NekoThemesPlusSettings.instance.secondaryTextColor, Is.EqualTo((Color)new Color32(122, 86, 105, 255)));
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

                NekoThemesPlusSettings.instance.enableTextColors = oldTextEnabled;
                NekoThemesPlusSettings.instance.primaryTextColor = oldPrimaryText;
                NekoThemesPlusSettings.instance.secondaryTextColor = oldSecondaryText;
                NekoThemesPlusSettings.instance.SaveSettings();
            }
        }

        [Test]
        public void EditorStyleBackup_RestoresAllTextStates()
        {
            Type backupType = typeof(EditorStyleController).Assembly.GetType(
                "NekoThemesPlus.Theme.EditorStyleBackup",
                false);
            Assert.That(backupType, Is.Not.Null);

            object backup = Activator.CreateInstance(backupType, true);
            MethodInfo capture = backupType.GetMethod("Capture", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo restore = backupType.GetMethod("Restore", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(capture, Is.Not.Null);
            Assert.That(restore, Is.Not.Null);

            GUIStyle style = new GUIStyle();
            Color original = new Color32(17, 33, 49, 211);
            style.normal.textColor = original;
            style.hover.textColor = original;
            style.active.textColor = original;
            style.focused.textColor = original;
            style.onNormal.textColor = original;
            style.onHover.textColor = original;
            style.onActive.textColor = original;
            style.onFocused.textColor = original;

            capture.Invoke(backup, new object[] { style });
            Color changed = Color.magenta;
            style.normal.textColor = changed;
            style.hover.textColor = changed;
            style.active.textColor = changed;
            style.focused.textColor = changed;
            style.onNormal.textColor = changed;
            style.onHover.textColor = changed;
            style.onActive.textColor = changed;
            style.onFocused.textColor = changed;
            restore.Invoke(backup, null);

            Assert.That(style.normal.textColor, Is.EqualTo(original));
            Assert.That(style.hover.textColor, Is.EqualTo(original));
            Assert.That(style.active.textColor, Is.EqualTo(original));
            Assert.That(style.focused.textColor, Is.EqualTo(original));
            Assert.That(style.onNormal.textColor, Is.EqualTo(original));
            Assert.That(style.onHover.textColor, Is.EqualTo(original));
            Assert.That(style.onActive.textColor, Is.EqualTo(original));
            Assert.That(style.onFocused.textColor, Is.EqualTo(original));
        }
    }
}
