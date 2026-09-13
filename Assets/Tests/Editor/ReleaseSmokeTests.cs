using System;
using System.IO;
using System.Reflection;
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
        public void ThemeFile_CanRoundTripWithoutWallpaper()
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
            try
            {
                object[] exportArgs = { path, null };
                Assert.That((bool)export.Invoke(null, exportArgs), Is.True, exportArgs[1] as string);
                Assert.That(File.Exists(path), Is.True);

                object[] importArgs = { path, null };
                Assert.That((bool)import.Invoke(null, importArgs), Is.True, importArgs[1] as string);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
