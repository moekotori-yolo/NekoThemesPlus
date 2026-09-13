using System;
using System.Collections.Generic;
using System.IO;
using NekoThemesPlus.Background;
using NekoThemesPlus.Core;
using NekoThemesPlus.Reflection;
using NekoThemesPlus.Native;
using NekoThemesPlus.Windows;
using NekoThemesPlus.Theme;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NekoThemesPlus.UI
{
    public sealed class NekoThemesPlusWindow : EditorWindow
    {
        private static readonly string[] PageNames =
        {
            "Global",
            "Background",
            "Glass",
            "Windows",
            "Colors",
            "Windows Effects",
            "Presets",
            "Advanced",
            "About"
        };

        private readonly Dictionary<string, Button> navigationButtons = new Dictionary<string, Button>();
        private ScrollView pageContent;
        private Toggle enabledToggle;
        private Label presetLabel;
        private Label statusLabel;
        private Label supportLabel;
        private Image previewImage;
        private string currentPage = "Global";

        [MenuItem("Window/Neko Themes Plus/设置", priority = 2000)]
        [MenuItem("Tools/Neko Themes Plus/设置", priority = 2000)]
        public static void Open()
        {
            NekoThemesPlusWindow window = GetWindow<NekoThemesPlusWindow>();
            window.titleContent = new GUIContent(NekoThemesPlusConstants.DisplayName);
            window.minSize = new Vector2(780f, 520f);
            if (window.position.width < 900f || window.position.height < 600f)
            {
                Rect mainRect;
                if (MainWindowReflection.TryGetMainWindowRect(out mainRect))
                {
                    window.position = new Rect(
                        mainRect.center.x - 500f,
                        mainRect.center.y - 325f,
                        1000f,
                        650f);
                }
                else
                {
                    window.position = new Rect(window.position.x, window.position.y, 1000f, 650f);
                }
            }

            window.Show();
        }

        [MenuItem("Tools/Neko Themes Plus/启用", priority = 2001)]
        private static void EnableFromMenu()
        {
            NekoThemesPlusManager.Enable();
        }

        [MenuItem("Tools/Neko Themes Plus/停用", priority = 2002)]
        private static void DisableFromMenu()
        {
            NekoThemesPlusManager.Disable();
        }

        private void OnEnable()
        {
            BackgroundManager.BackgroundChanged -= OnBackgroundChanged;
            BackgroundManager.BackgroundChanged += OnBackgroundChanged;
            NekoThemesPlusManager.StateChanged -= OnStateChanged;
            NekoThemesPlusManager.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            BackgroundManager.BackgroundChanged -= OnBackgroundChanged;
            NekoThemesPlusManager.StateChanged -= OnStateChanged;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(NekoThemesPlusConstants.PackageRoot + "/Editor/UI/NekoThemesPlusWindow.uxml");
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(NekoThemesPlusConstants.PackageRoot + "/Editor/UI/NekoThemesPlusWindow.uss");

            if (tree != null)
            {
                tree.CloneTree(rootVisualElement);
            }
            else
            {
                BuildFallbackLayout();
            }

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            pageContent = rootVisualElement.Q<ScrollView>("page-content");
            enabledToggle = rootVisualElement.Q<Toggle>("enabled-toggle");
            presetLabel = rootVisualElement.Q<Label>("preset-label");
            statusLabel = rootVisualElement.Q<Label>("status-label");
            supportLabel = rootVisualElement.Q<Label>("support-label");
            Label versionLabel = rootVisualElement.Q<Label>("version-label");
            if (versionLabel != null) versionLabel.text = "v" + NekoThemesPlusConstants.Version;

            if (enabledToggle != null) enabledToggle.label = T("启用", "Enabled");
            Button localizedReset = rootVisualElement.Q<Button>("reset-button");
            if (localizedReset != null) localizedReset.text = T("重置", "Reset");
            Button localizedSave = rootVisualElement.Q<Button>("save-button");
            if (localizedSave != null) localizedSave.text = T("保存", "Save");

            VisualElement navigation = rootVisualElement.Q<VisualElement>("navigation");
            BuildNavigation(navigation);
            BindHeader();
            ShowPage(currentPage);
            UpdateChrome();
        }

        private void BuildFallbackLayout()
        {
            VisualElement root = new VisualElement();
            root.AddToClassList("neko-root");
            rootVisualElement.Add(root);

            VisualElement header = new VisualElement();
            header.AddToClassList("neko-header");
            header.Add(new Label(NekoThemesPlusConstants.DisplayName));
            enabledToggle = new Toggle(T("启用", "Enabled")) { name = "enabled-toggle" };
            header.Add(enabledToggle);
            root.Add(header);

            VisualElement body = new VisualElement();
            body.AddToClassList("neko-body");
            body.Add(new VisualElement { name = "navigation" });
            body.Add(new ScrollView { name = "page-content" });
            root.Add(body);

            VisualElement footer = new VisualElement();
            footer.AddToClassList("neko-footer");
            footer.Add(new Label { name = "status-label" });
            footer.Add(new Label { name = "support-label" });
            root.Add(footer);
        }

        private void BuildNavigation(VisualElement navigation)
        {
            if (navigation == null)
            {
                return;
            }

            navigation.Clear();
            navigation.AddToClassList("neko-navigation");
            navigationButtons.Clear();
            foreach (string pageName in PageNames)
            {
                string capturedName = pageName;
                Button button = new Button(delegate { ShowPage(capturedName); }) { text = NekoThemesPlusLocalization.PageName(pageName) };
                button.AddToClassList("neko-nav-button");
                navigation.Add(button);
                navigationButtons.Add(pageName, button);
            }
        }

        private void BindHeader()
        {
            if (enabledToggle != null)
            {
                enabledToggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        NekoThemesPlusManager.Enable();
                    }
                    else
                    {
                        NekoThemesPlusManager.Disable();
                    }

                    UpdateChrome();
                });
            }

            Button resetButton = rootVisualElement.Q<Button>("reset-button");
            if (resetButton != null)
            {
                resetButton.clicked += ResetSettings;
            }

            Button saveButton = rootVisualElement.Q<Button>("save-button");
            if (saveButton != null)
            {
                saveButton.clicked += delegate
                {
                    NekoThemesPlusSettings.instance.SaveSettings();
                    SetStatus(T("设置已保存到 ProjectSettings/NekoThemesPlusSettings.asset", "Settings saved to ProjectSettings/NekoThemesPlusSettings.asset"));
                };
            }
        }

        private void ShowPage(string pageName)
        {
            if (pageContent == null)
            {
                return;
            }

            currentPage = pageName;
            previewImage = null;
            pageContent.Clear();
            AddPageHeading(NekoThemesPlusLocalization.PageName(pageName), GetPageDescription(pageName));

            if (!NekoThemesPlusVersion.IsSupportedLts && !NekoThemesPlusSettings.instance.forceUnsupportedVersion)
            {
                AddMessage(T("当前 Unity 版本未经支持。除非强制开启实验支持，否则内部挂钩保持关闭。", "This Unity version is unsupported. Internal hooks remain disabled unless experimental support is forced."), "neko-warning");
            }

            if (NekoThemesPlusSafeMode.IsActive)
            {
                AddSafeModeMessage();
            }

            switch (pageName)
            {
                case "Global": BuildGlobalPage(); break;
                case "Background": BuildBackgroundPage(); break;
                case "Glass": BuildGlassPage(); break;
                case "Windows": BuildWindowsPage(); break;
                case "Colors": BuildColorsPage(); break;
                case "Windows Effects": BuildWindowsEffectsPage(); break;
                case "Presets": BuildPresetsPage(); break;
                case "Advanced": BuildAdvancedPage(); break;
                case "About": BuildAboutPage(); break;
            }

            foreach (KeyValuePair<string, Button> pair in navigationButtons)
            {
                pair.Value.EnableInClassList("neko-nav-button--active", pair.Key == pageName);
            }
        }

        private void BuildGlobalPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            VisualElement state = AddCard(T("主题状态", "Theme state"), T("控制 NekoThemesPlus 的整体启用状态。", "Control the overall NekoThemesPlus state."));
            AddToggle(state, T("启用 NekoThemesPlus", "Enable NekoThemesPlus"), settings.enabled && NekoThemesPlusManager.IsEnabled, value =>
            {
                if (value) NekoThemesPlusManager.Enable(); else NekoThemesPlusManager.Disable();
            }, false);
            state.Add(new Label(T("当前主题：", "Current theme: ") + PresetDisplayName(settings.currentPreset)));

            VisualElement presets = AddCard(T("快速预设", "Quick presets"), T("应用调校好的起点，然后在背景与玻璃效果页面继续调整。", "Apply a tuned starting point and continue adjusting it on the Background and Glass pages."));
            AddPresetButtons(presets);

            VisualElement preview = AddCard(T("背景预览", "Background preview"), T("修改背景参数时，缓存的 GPU 处理结果会实时更新。", "The cached GPU result updates as background parameters change."));
            AddPreview(preview);

            VisualElement controls = AddCard(T("全局控制", "Global controls"), T("主题共用的主要参数。", "High-level controls shared by the theme."));
            AddSlider(controls, T("全局透明度", "Global opacity"), settings.globalPanelOpacity, 0f, 1f, value => settings.globalPanelOpacity = value, false);
            AddSlider(controls, T("全局模糊", "Global blur"), settings.blurAmount, 0f, 50f, value => settings.blurAmount = value, true);
            AddColor(controls, T("强调色", "Accent color"), settings.accentColor, value => settings.accentColor = value, false);
        }

        private void BuildBackgroundPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            VisualElement imageCard = AddCard(T("背景图片", "Background image"), T("外部 PNG、JPG、JPEG 无需导入 Assets，重启后会从已保存路径恢复。", "External PNG, JPG, and JPEG files remain outside Assets and are restored from their saved path."));

            TextField pathField = new TextField(T("路径", "Path")) { value = settings.backgroundPath, isReadOnly = true };
            imageCard.Add(pathField);
            VisualElement buttons = AddRow(imageCard);
            Button choose = new Button(ChooseBackground) { text = T("选择图片", "Choose Image") };
            choose.AddToClassList("neko-primary-button");
            buttons.Add(choose);
            Button clear = new Button(delegate
            {
                BackgroundManager.ClearBackground();
                SetStatus(T("背景已清除", "Background cleared"));
                ShowPage(currentPage);
            }) { text = T("清除背景", "Clear Background") };
            clear.AddToClassList("neko-quiet-button");
            buttons.Add(clear);

            List<BackgroundImageMode> modeValues = new List<BackgroundImageMode>((BackgroundImageMode[])Enum.GetValues(typeof(BackgroundImageMode)));
            List<string> modeLabels = modeValues.ConvertAll(NekoThemesPlusLocalization.ImageModeName);
            PopupField<string> mode = new PopupField<string>(T("显示模式", "Mode"), modeLabels, modeValues.IndexOf(settings.backgroundMode));
            mode.RegisterValueChangedCallback(evt => ApplySettings(value => value.backgroundMode = modeValues[modeLabels.IndexOf(evt.newValue)], true));
            imageCard.Add(mode);
            AddSlider(imageCard, T("背景缩放", "Background zoom"), settings.backgroundZoom, 1f, 4f, value => settings.backgroundZoom = value, true);
            AddSlider(imageCard, T("水平构图（左 → 右）", "Horizontal framing (left → right)"), settings.backgroundAlignment.x, 0f, 1f, value => settings.backgroundAlignment = new Vector2(value, settings.backgroundAlignment.y), true);
            AddSlider(imageCard, T("垂直构图（上 → 下）", "Vertical framing (top → bottom)"), settings.backgroundAlignment.y, 0f, 1f, value => settings.backgroundAlignment = new Vector2(settings.backgroundAlignment.x, value), true);

            AddPreview(imageCard);
            AddBackgroundError(imageCard);

            VisualElement windowBackgrounds = AddCard(
                T("区域独立背景", "Per-window backgrounds"),
                T("为常用区域选择单独图片；未选择时自动继承上面的全局背景。每个窗口实例会按自己的尺寸处理图片。", "Choose a separate image for each common area. Empty entries inherit the global background, and every window instance is processed at its own size."));
            AddWindowBackgroundRow(windowBackgrounds, WindowKind.Hierarchy, T("层级", "Hierarchy"));
            AddWindowBackgroundRow(windowBackgrounds, WindowKind.Inspector, T("检视器", "Inspector"));
            AddWindowBackgroundRow(windowBackgrounds, WindowKind.Project, T("项目", "Project"));
            AddWindowBackgroundRow(windowBackgrounds, WindowKind.Console, T("控制台", "Console"));
            AddMessage(
                T("Scene View 和 Game View 继续使用安全工具栏模式，不允许独立图片覆盖相机区域。", "Scene View and Game View remain in safe toolbar mode; independent images never cover camera output."),
                "neko-warning",
                windowBackgrounds);

            VisualElement adjustments = AddCard(T("图像调整", "Image adjustments"), T("下列参数由缓存式 GPU 管线处理。", "These controls are applied by the cached GPU processing pipeline."));
            AddSlider(adjustments, T("亮度", "Brightness"), settings.brightness, 0f, 2f, value => settings.brightness = value, true);
            AddSlider(adjustments, T("饱和度", "Saturation"), settings.saturation, 0f, 2f, value => settings.saturation = value, true);
            AddSlider(adjustments, T("对比度", "Contrast"), settings.contrast, 0f, 2f, value => settings.contrast = value, true);
            AddColor(adjustments, T("背景色调", "Background tint"), settings.backgroundTint, value => settings.backgroundTint = value, true);
            AddSlider(adjustments, T("背景不透明度", "Background opacity"), settings.backgroundOpacity, 0f, 1f, value => settings.backgroundOpacity = value, true);
            AddSlider(adjustments, T("模糊强度", "Blur amount"), settings.blurAmount, 0f, 50f, value => settings.blurAmount = value, true);
        }

        private void BuildGlassPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            VisualElement card = AddCard(T("玻璃表面", "Glass surface"), T("面板使用独立的背景和着色层。", "Panels use independent background and tint layers."));
            AddColor(card, T("面板色调", "Panel tint"), settings.panelTint, value => settings.panelTint = value, false);
            AddSlider(card, T("全局面板不透明度", "Global panel opacity"), settings.globalPanelOpacity, 0f, 1f, value => settings.globalPanelOpacity = value, false);
            AddMessage(T("不会修改内容本身的透明度；文字、图标和控件始终保持完全清晰。", "Content opacity is never changed; text, icons, and controls remain fully opaque."), "neko-warning", card);
        }

        private void BuildWindowsPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            AddMessage(T("每个窗口都可以独立启用并调整玻璃层不透明度。Scene 和 Game 只处理编辑器边缘，不修改相机画面。", "Each window can be enabled and tuned independently. Scene and Game never alter camera output."), "neko-warning");
            AddWindowCard(T("层级", "Hierarchy"), settings.enableHierarchy, settings.hierarchyOpacity, value => settings.enableHierarchy = value, value => settings.hierarchyOpacity = value);
            AddWindowCard(T("检视器", "Inspector"), settings.enableInspector, settings.inspectorOpacity, value => settings.enableInspector = value, value => settings.inspectorOpacity = value);
            AddWindowCard(T("项目", "Project"), settings.enableProject, settings.projectOpacity, value => settings.enableProject = value, value => settings.projectOpacity = value);
            AddWindowCard(T("控制台", "Console"), settings.enableConsole, settings.consoleOpacity, value => settings.enableConsole = value, value => settings.consoleOpacity = value);
            AddWindowCard(T("场景视图", "Scene View"), settings.enableSceneView, settings.sceneOpacity, value => settings.enableSceneView = value, value => settings.sceneOpacity = value);
            AddWindowCard(T("游戏视图", "Game View"), settings.enableGameView, settings.gameOpacity, value => settings.enableGameView = value, value => settings.gameOpacity = value);
        }

        private void BuildColorsPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            VisualElement card = AddCard(T("编辑器颜色", "Editor colors"), T("可调整强调色和选择色；为了可读性，不修改文字颜色。", "Tune accent and selection colors while leaving editor text untouched."));
            AddColor(card, T("强调色", "Accent color"), settings.accentColor, value => settings.accentColor = value, false);
            AddColor(card, T("选择色", "Selection color"), settings.selectionColor, value => settings.selectionColor = value, false);
            AddSlider(card, T("选择色不透明度", "Selection opacity"), settings.selectionOpacity, 0f, 1f, value => settings.selectionOpacity = value, false);
            AddToggle(card, T("染色 Dock 标签栏与边框", "Tint Dock tabs and borders"), settings.enableDockChrome, value => settings.enableDockChrome = value, false);
            AddColor(card, T("边框颜色", "Border color"), settings.borderColor, value => settings.borderColor = value, false);
            AddSlider(card, T("边框染色强度", "Border tint strength"), settings.borderOpacity, 0f, 1f, value => settings.borderOpacity = value, false);
            AddMessage(T("Dock 染色会覆盖标签栏，因此标签文字也会轻微染色；建议强度保持在 0.15–0.35。", "Dock tint is drawn over the tab strip, so tab text is tinted slightly too; 0.15–0.35 is recommended."), "neko-warning", card);
        }

        private void BuildWindowsEffectsPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            AddMessage(T("实验功能——原生 Windows 效果默认关闭，部分选项可能需要重启编辑器。", "Experimental — native Windows effects default to off and some options may require an Editor restart."), "neko-warning");
            VisualElement card = AddCard(T("原生 Windows 效果", "Native Windows effects"), T("所有原生效果都会在不支持时安全失败。", "All native effects fail safely when unsupported."));
            AddToggle(card, T("启用原生玻璃", "Enable native glass"), settings.enableNativeGlass, value => settings.enableNativeGlass = value, false);
            AddToggle(card, T("深色标题栏", "Dark title bar"), settings.enableDarkTitlebar, value => settings.enableDarkTitlebar = value, false);
            AddToggle(card, T("圆角窗口", "Rounded corners"), settings.enableRoundedCorners, value => settings.enableRoundedCorners = value, false);
            AddToggle(card, "Mica", settings.enableMica, value => settings.enableMica = value, false);
            AddToggle(card, "Acrylic", settings.enableAcrylic, value => settings.enableAcrylic = value, false);
        }

        private void BuildPresetsPage()
        {
            VisualElement card = AddCard(T("内置预设", "Built-in presets"), T("选择一个预设作为起点；自定义参数会继续自动保存。", "Choose a preset as a starting point; custom values continue to save automatically."));
            AddPresetButtons(card);

            VisualElement sharing = AddCard(T("分享主题", "Share theme"), T("导出单个 .nekotheme 文件；壁纸会内嵌，机器相关的原生实验设置不会被导出。", "Export one .nekotheme file with its wallpaper embedded. Machine-specific native experiments are excluded."));
            VisualElement row = AddRow(sharing);
            row.Add(new Button(ExportTheme) { text = T("导出主题", "Export Theme") });
            row.Add(new Button(ImportTheme) { text = T("导入主题", "Import Theme") });
        }

        private void BuildAdvancedPage()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            Rect mainRect;
            bool found = MainWindowReflection.TryGetMainWindowRect(out mainRect);
            Vector2Int processed = BackgroundManager.ProcessedResolution;

            VisualElement diagnostics = AddCard(T("诊断信息", "Diagnostics"), T("当前编辑器与背景处理状态。", "Current editor and background processing state."));
            diagnostics.Add(new Label(T("Unity 版本：", "Unity version: ") + Application.unityVersion));
            diagnostics.Add(new Label(T("插件版本：", "Plugin version: ") + NekoThemesPlusConstants.Version));
            diagnostics.Add(new Label(T("主窗口区域：", "Main window rect: ") + (found ? mainRect.ToString() : T("本地模式", "Local mode"))));
            diagnostics.Add(new Label(T("处理后纹理：", "Processed texture: ") + (processed == Vector2Int.zero ? T("无", "None") : processed.x + " × " + processed.y)));
            diagnostics.Add(new Label(T("区域独立背景：", "Per-window backgrounds: ") + BackgroundManager.ActiveOverrideCount + " / 4"));
            diagnostics.Add(new Label("Safe Mode：" + (NekoThemesPlusSafeMode.IsActive ? T("开启", "On") : T("关闭", "Off"))));
            diagnostics.Add(new Label(T("检测到的系统：", "Detected system: ") + WindowsVersionHelper.DisplayName));
            diagnostics.Add(new Label(T("已发现窗口：", "Discovered windows: ") + EditorWindowRegistry.WindowCount));
            diagnostics.Add(new Label(T("已挂钩窗口：", "Hooked windows: ") + EditorWindowRegistry.HookedWindowCount));
            diagnostics.Add(new Label("HostView：" + (HostViewHookManager.IsAvailable ? T("可用", "Available") : T("不可用", "Unavailable"))));
            diagnostics.Add(new Label(T("HostView 内容挂钩：", "HostView content hooks: ") + HostViewHookManager.HookedCount));
            diagnostics.Add(new Label(T("Dock 边框挂钩：", "Dock chrome hooks: ") + HostViewHookManager.ChromeHookedCount));
            diagnostics.Add(new Label(T("背景状态：", "Background status: ") + (string.IsNullOrEmpty(BackgroundManager.LastError) ? T("就绪", "Ready") : BackgroundManager.LastError)));
            Button reportButton = new Button(delegate
            {
                NekoThemesPlusDiagnostics.LogAndCopyReport();
                SetStatus(T("诊断报告已复制到剪贴板并输出到 Console", "Diagnostics copied to the clipboard and written to Console"));
            }) { text = T("复制诊断报告", "Copy Diagnostics") };
            diagnostics.Add(reportButton);

            VisualElement limits = AddCard(T("兼容性与限制", "Compatibility and limits"), T("高开销或高风险的编辑器内部功能保持手动启用。", "Expensive or fragile editor internals remain opt-in."));
            VisualElement languageRow = AddRow(limits);
            languageRow.Add(new Label(T("界面语言", "Interface language")));
            languageRow.Add(new Button(delegate { SetLanguage(NekoThemesPlusLanguage.SimplifiedChinese); }) { text = "简体中文" });
            languageRow.Add(new Button(delegate { SetLanguage(NekoThemesPlusLanguage.English); }) { text = "English" });
            SliderInt resolution = new SliderInt(T("背景最大分辨率", "Max background resolution"), 512, 8192) { value = settings.maxBackgroundResolution, showInputField = true };
            resolution.RegisterValueChangedCallback(evt => ApplySettings(value => value.maxBackgroundResolution = evt.newValue, true));
            limits.Add(resolution);
            AddToggle(limits, T("旧式 GUIStyle 兼容（实验性）", "Legacy GUIStyle compatibility (experimental)"), settings.experimentalIMGUI, value => settings.experimentalIMGUI = value, false);
            AddToggle(limits, T("扩展内部样式（实验性）", "Extended internal styles (experimental)"), settings.experimentalInternalStyles, value => settings.experimentalInternalStyles = value, false);
            AddToggle(limits, T("强制支持当前版本", "Force unsupported version"), settings.forceUnsupportedVersion, value => settings.forceUnsupportedVersion = value, false);
            AddToggle(limits, T("调试日志", "Debug logging"), settings.debugLogging, value => settings.debugLogging = value, false);

            VisualElement actions = AddCard(T("维护", "Maintenance"), T("重建缓存资源，或让编辑器恢复原始外观。", "Rebuild cached resources or return the editor to the unmodified state."));
            VisualElement row = AddRow(actions);
            row.Add(new Button(delegate { NekoThemesPlusManager.RefreshWindows(); SetStatus(T("窗口已刷新", "Views refreshed")); }) { text = T("刷新窗口", "Refresh Windows") });
            row.Add(new Button(delegate { BackgroundManager.Rebuild(); SetStatus(T("背景已重建", "Background rebuilt")); }) { text = T("重建背景", "Rebuild Background") });
            row.Add(new Button(delegate { EditorStyleController.Restore(); SetStatus(T("Unity 样式已恢复", "Unity styles restored")); }) { text = T("恢复 Unity 样式", "Restore Unity Styles") });
            row.Add(new Button(delegate { NekoThemesPlusManager.RestoreUnity(); SetStatus(T("NekoThemesPlus 已停用", "NekoThemesPlus disabled")); }) { text = T("恢复 Unity", "Restore Unity") });
            row.Add(new Button(delegate { NekoThemesPlusSafeMode.Enter(); NekoThemesPlusManager.RefreshWindows(); ShowPage(currentPage); }) { text = T("进入安全模式", "Enter Safe Mode") });
        }

        private void BuildAboutPage()
        {
            VisualElement card = AddCard(NekoThemesPlusConstants.DisplayName, T("Unity 编辑器外观定制插件。", "Editor customization plugin."));
            card.Add(new Label(T("版本：", "Version: ") + NekoThemesPlusConstants.Version));
            card.Add(new Label(T("目标 Unity：", "Target Unity: ") + NekoThemesPlusConstants.TargetUnityVersion));
            card.Add(new Label(T("平台：", "Platform: ") + "Windows 10 / Windows 11"));
            card.Add(new Label(T("包名：", "Package: ") + NekoThemesPlusConstants.PackageId));
        }

        private void AddWindowCard(string title, bool enabled, float opacity, Action<bool> setEnabled, Action<float> setOpacity)
        {
            VisualElement card = AddCard(title, T("可独立控制启用状态和面板不透明度。", "Independent enable and panel opacity."));
            AddToggle(card, T("启用", "Enable"), enabled, setEnabled, false);
            AddSlider(card, T("不透明度", "Opacity"), opacity, 0f, 1f, setOpacity, false);
        }

        private void AddPresetButtons(VisualElement parent)
        {
            VisualElement row = AddRow(parent);
            row.Add(new Button(delegate { ApplyPreset("Default Dark"); }) { text = PresetDisplayName("Default Dark") });
            row.Add(new Button(delegate { ApplyPreset("Neko Glass"); }) { text = PresetDisplayName("Neko Glass") });
            row.Add(new Button(delegate { ApplyPreset("Aero Glass"); }) { text = PresetDisplayName("Aero Glass") });
            row.Add(new Button(delegate { ApplyPreset("Fluent Dark"); }) { text = PresetDisplayName("Fluent Dark") });
            VisualElement secondRow = AddRow(parent);
            secondRow.Add(new Button(delegate { ApplyPreset("Mica Dark"); }) { text = PresetDisplayName("Mica Dark") });
            secondRow.Add(new Button(delegate { ApplyPreset("VS Code Glass"); }) { text = PresetDisplayName("VS Code Glass") });
            secondRow.Add(new Button(delegate { ApplyPreset("Deep Black"); }) { text = PresetDisplayName("Deep Black") });
            secondRow.Add(new Button(delegate { ApplyPreset("Soft Frost"); }) { text = PresetDisplayName("Soft Frost") });
        }

        private void ApplyPreset(string preset)
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            settings.currentPreset = preset;
            settings.backgroundZoom = 1f;
            settings.backgroundAlignment = new Vector2(0.5f, 0.5f);

            switch (preset)
            {
                case "Default Dark":
                    settings.blurAmount = 0f;
                    settings.brightness = 0.60f;
                    settings.saturation = 0.70f;
                    settings.contrast = 1.05f;
                    settings.globalPanelOpacity = 0.86f;
                    settings.panelTint = new Color32(32, 34, 38, 255);
                    break;
                case "Aero Glass":
                    settings.blurAmount = 24f;
                    settings.brightness = 0.85f;
                    settings.saturation = 0.90f;
                    settings.contrast = 1.05f;
                    settings.panelTint = new Color32(36, 52, 69, 255);
                    settings.globalPanelOpacity = 0.62f;
                    break;
                case "Fluent Dark":
                    settings.blurAmount = 28f;
                    settings.brightness = 0.68f;
                    settings.saturation = 0.82f;
                    settings.contrast = 1.06f;
                    settings.panelTint = new Color32(32, 36, 43, 255);
                    settings.globalPanelOpacity = 0.78f;
                    break;
                case "Mica Dark":
                    settings.blurAmount = 22f;
                    settings.brightness = 0.66f;
                    settings.saturation = 0.72f;
                    settings.contrast = 1.08f;
                    settings.panelTint = new Color32(28, 31, 37, 255);
                    settings.globalPanelOpacity = 0.82f;
                    break;
                case "VS Code Glass":
                    settings.blurAmount = 18f;
                    settings.brightness = 0.68f;
                    settings.saturation = 0.78f;
                    settings.contrast = 1.08f;
                    settings.panelTint = new Color32(24, 30, 38, 255);
                    settings.globalPanelOpacity = 0.76f;
                    settings.accentColor = new Color32(0, 122, 204, 255);
                    break;
                case "Deep Black":
                    settings.blurAmount = 14f;
                    settings.brightness = 0.48f;
                    settings.saturation = 0.62f;
                    settings.contrast = 1.14f;
                    settings.panelTint = new Color32(8, 10, 13, 255);
                    settings.globalPanelOpacity = 0.88f;
                    break;
                case "Soft Frost":
                    settings.blurAmount = 34f;
                    settings.brightness = 0.92f;
                    settings.saturation = 0.66f;
                    settings.contrast = 0.94f;
                    settings.panelTint = new Color32(77, 88, 103, 255);
                    settings.globalPanelOpacity = 0.58f;
                    break;
                default:
                    settings.blurAmount = 20f;
                    settings.brightness = 0.70f;
                    settings.saturation = 0.82f;
                    settings.contrast = 1.06f;
                    settings.backgroundTint = new Color32(21, 26, 32, 90);
                    settings.panelTint = new Color32(21, 29, 38, 255);
                    settings.globalPanelOpacity = 0.70f;
                    settings.accentColor = new Color32(105, 168, 255, 255);
                    settings.selectionColor = new Color32(65, 106, 155, 255);
                    break;
            }

            settings.SaveSettings();
            NekoThemesPlusManager.Refresh();
            SetStatus(T("已应用预设：", "Preset applied: ") + PresetDisplayName(preset));
            ShowPage(currentPage);
        }

        private void ChooseBackground()
        {
            string currentPath = NekoThemesPlusSettings.instance.backgroundPath;
            string directory = !string.IsNullOrEmpty(currentPath) && File.Exists(currentPath)
                ? Path.GetDirectoryName(currentPath)
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = EditorUtility.OpenFilePanel(T("选择背景图片", "Select Background"), directory, "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            BackgroundManager.SetBackground(path);
            SetStatus(string.IsNullOrEmpty(BackgroundManager.LastError) ? T("背景已加载", "Background loaded") : BackgroundManager.LastError);
            ShowPage(currentPage);
        }

        private void AddWindowBackgroundRow(VisualElement parent, WindowKind kind, string label)
        {
            VisualElement group = new VisualElement();
            group.AddToClassList("neko-window-background");

            string path = BackgroundManager.GetBackgroundPath(kind);
            TextField pathField = new TextField(label)
            {
                value = path,
                isReadOnly = true
            };
            group.Add(pathField);

            VisualElement buttons = AddRow(group);
            Button choose = new Button(delegate { ChooseWindowBackground(kind); })
            {
                text = T("选择独立图片", "Choose Override")
            };
            choose.AddToClassList("neko-primary-button");
            buttons.Add(choose);

            Button inherit = new Button(delegate
            {
                BackgroundManager.ClearWindowBackground(kind);
                SetStatus(label + T("已恢复使用全局背景", " now inherits the global background"));
                ShowPage(currentPage);
            })
            {
                text = T("使用全局", "Use Global")
            };
            inherit.AddToClassList("neko-quiet-button");
            inherit.SetEnabled(!string.IsNullOrWhiteSpace(path));
            buttons.Add(inherit);

            string error = BackgroundManager.GetWindowError(kind);
            if (!string.IsNullOrEmpty(error))
            {
                AddMessage(error, "neko-error", group);
            }

            parent.Add(group);
        }

        private void ChooseWindowBackground(WindowKind kind)
        {
            string currentPath = BackgroundManager.GetBackgroundPath(kind);
            string directory = !string.IsNullOrEmpty(currentPath) && File.Exists(currentPath)
                ? Path.GetDirectoryName(currentPath)
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = EditorUtility.OpenFilePanel(
                T("选择区域独立背景", "Select Per-window Background"),
                directory,
                "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            bool loaded = BackgroundManager.SetWindowBackground(kind, path);
            string error = BackgroundManager.GetWindowError(kind);
            SetStatus(loaded
                ? T("区域独立背景已加载", "Per-window background loaded")
                : error);
            ShowPage(currentPage);
        }

        private void ExportTheme()
        {
            string path = EditorUtility.SaveFilePanel(
                T("导出 NekoThemesPlus 主题", "Export NekoThemesPlus Theme"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NekoThemesPlus-" + NekoThemesPlusSettings.instance.currentPreset,
                "nekotheme");
            if (string.IsNullOrEmpty(path)) return;

            string message;
            bool success = NekoThemeImportExport.Export(path, out message);
            SetStatus(message);
            if (!success)
            {
                EditorUtility.DisplayDialog(T("导出失败", "Export Failed"), message, T("确定", "OK"));
            }
        }

        private void ImportTheme()
        {
            string path = EditorUtility.OpenFilePanel(
                T("导入 NekoThemesPlus 主题", "Import NekoThemesPlus Theme"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "nekotheme");
            if (string.IsNullOrEmpty(path)) return;

            string message;
            bool success = NekoThemeImportExport.Import(path, out message);
            SetStatus(message);
            if (success)
            {
                ShowPage(currentPage);
            }
            else
            {
                EditorUtility.DisplayDialog(T("导入失败", "Import Failed"), message, T("确定", "OK"));
            }
        }

        private void ResetSettings()
        {
            if (!EditorUtility.DisplayDialog(T("重置 Neko Themes Plus", "Reset Neko Themes Plus"), T("确定将全部 NekoThemesPlus 设置恢复为默认值吗？", "Reset all NekoThemesPlus settings to their defaults?"), T("重置", "Reset"), T("取消", "Cancel")))
            {
                return;
            }

            NekoThemesPlusSettings.instance.ResetToDefaults();
            BackgroundManager.ReloadSource();
            NekoThemesPlusManager.Refresh();
            SetStatus(T("已恢复默认设置", "Default settings restored"));
            ShowPage(currentPage);
        }

        private void AddSafeModeMessage()
        {
            VisualElement box = new VisualElement();
            box.AddToClassList("neko-warning");
            box.Add(new Label(T("NekoThemesPlus 已进入安全模式。反射、原生玻璃和内部样式修改均已停用。", "NekoThemesPlus started in Safe Mode. Reflection, native glass, and internal style changes are disabled.")));
            Button exit = new Button(delegate
            {
                NekoThemesPlusSafeMode.Exit();
                NekoThemesPlusManager.Refresh();
                ShowPage(currentPage);
            }) { text = T("正常启用", "Enable Normally") };
            box.Add(exit);
            pageContent.Add(box);
        }

        private void AddPreview(VisualElement parent)
        {
            RenderTexture texture = BackgroundManager.ProcessedTexture;
            if (texture == null)
            {
                Label empty = new Label(T("请选择背景图片以开始实时预览。", "Choose a background image to start the live preview."));
                empty.AddToClassList("neko-empty-preview");
                parent.Add(empty);
                return;
            }

            previewImage = new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit
            };
            previewImage.AddToClassList("neko-preview");
            parent.Add(previewImage);
        }

        private void AddBackgroundError(VisualElement parent)
        {
            if (!string.IsNullOrEmpty(BackgroundManager.LastError))
            {
                AddMessage(BackgroundManager.LastError, "neko-error", parent);
            }
        }

        private VisualElement AddCard(string title, string description)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("neko-card");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("neko-card-title");
            card.Add(titleLabel);
            if (!string.IsNullOrEmpty(description))
            {
                Label descriptionLabel = new Label(description);
                descriptionLabel.AddToClassList("neko-card-description");
                card.Add(descriptionLabel);
            }

            pageContent.Add(card);
            return card;
        }

        private static VisualElement AddRow(VisualElement parent)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("neko-row");
            parent.Add(row);
            return row;
        }

        private void AddToggle(VisualElement parent, string label, bool current, Action<bool> setter, bool affectsBackground)
        {
            Toggle field = new Toggle(label) { value = current };
            field.RegisterValueChangedCallback(evt =>
            {
                setter(evt.newValue);
                NekoThemesPlusSettings.instance.SaveSettings();
                if (affectsBackground) NekoThemesPlusManager.RefreshBackground(); else NekoThemesPlusManager.RefreshWindows();
                UpdateChrome();
            });
            parent.Add(field);
        }

        private void AddSlider(VisualElement parent, string label, float current, float minimum, float maximum, Action<float> setter, bool affectsBackground)
        {
            Slider field = new Slider(label, minimum, maximum) { value = current, showInputField = true };
            field.RegisterValueChangedCallback(evt =>
            {
                setter(evt.newValue);
                NekoThemesPlusSettings.instance.SaveSettings();
                if (affectsBackground) NekoThemesPlusManager.RefreshBackground(); else NekoThemesPlusManager.RefreshWindows();
                UpdateChrome();
            });
            parent.Add(field);
        }

        private void AddColor(VisualElement parent, string label, Color current, Action<Color> setter, bool affectsBackground)
        {
            ColorField field = new ColorField(label) { value = current, showAlpha = true };
            field.RegisterValueChangedCallback(evt =>
            {
                setter(evt.newValue);
                NekoThemesPlusSettings.instance.SaveSettings();
                if (affectsBackground) NekoThemesPlusManager.RefreshBackground(); else NekoThemesPlusManager.RefreshWindows();
                UpdateChrome();
            });
            parent.Add(field);
        }

        private void ApplySettings(Action<NekoThemesPlusSettings> change, bool affectsBackground)
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            change(settings);
            settings.SaveSettings();
            if (affectsBackground)
            {
                NekoThemesPlusManager.RefreshBackground();
            }
            else
            {
                NekoThemesPlusManager.RefreshWindows();
            }

            UpdateChrome();
        }

        private void AddPageHeading(string title, string subtitle)
        {
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("neko-page-title");
            pageContent.Add(titleLabel);
            Label subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("neko-page-subtitle");
            pageContent.Add(subtitleLabel);
        }

        private void AddMessage(string text, string className, VisualElement parent = null)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            (parent ?? pageContent).Add(label);
        }

        private void OnBackgroundChanged()
        {
            if (previewImage != null)
            {
                previewImage.image = BackgroundManager.ProcessedTexture;
                previewImage.MarkDirtyRepaint();
            }

            UpdateChrome();
        }

        private void OnStateChanged()
        {
            UpdateChrome();
        }

        private void UpdateChrome()
        {
            NekoThemesPlusSettings settings = NekoThemesPlusSettings.instance;
            if (enabledToggle != null)
            {
                enabledToggle.SetValueWithoutNotify(settings.enabled && NekoThemesPlusManager.IsEnabled);
            }

            if (presetLabel != null)
            {
                presetLabel.text = PresetDisplayName(settings.currentPreset);
            }

            if (supportLabel != null)
            {
                supportLabel.text = SupportDisplayLabel() + " · Unity " + Application.unityVersion;
            }

            if (statusLabel != null && string.IsNullOrEmpty(statusLabel.text))
            {
                statusLabel.text = string.IsNullOrEmpty(BackgroundManager.LastError) ? T("就绪", "Ready") : BackgroundManager.LastError;
            }
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null)
            {
                statusLabel.text = text;
            }
        }

        private static string GetPageDescription(string pageName)
        {
            switch (pageName)
            {
                case "Global": return T("主题概览、快速预设和实时 GPU 背景预览。", "Theme overview, quick presets, and the live processed-background preview.");
                case "Background": return T("选择并调整全局背景模拟所使用的图片。", "Choose and tune the image used by the global background simulation.");
                case "Glass": return T("配置位于编辑器内容下方的独立着色层。", "Configure the independent tint layers behind editor content.");
                case "Windows": return T("控制各编辑器窗口及其玻璃不透明度。", "Control each editor window and its glass opacity.");
                case "Colors": return T("调整强调色和选择色，同时保持文字原样。", "Tune accent and selection colors without changing editor text.");
                case "Windows Effects": return T("可选的 Windows 10/11 原生效果（实验性）。", "Optional native Windows 10/11 effects (experimental).");
                case "Presets": return T("经过调校的视觉起点。", "Curated visual starting points.");
                case "Advanced": return T("兼容性、诊断、资源限制和维护。", "Compatibility, diagnostics, resource limits, and maintenance.");
                default: return T("插件和兼容性信息。", "Package and compatibility information.");
            }
        }

        private void SetLanguage(NekoThemesPlusLanguage language)
        {
            NekoThemesPlusSettings.instance.language = language;
            NekoThemesPlusSettings.instance.SaveSettings();
            CreateGUI();
        }

        private static string PresetDisplayName(string preset)
        {
            switch (preset)
            {
                case "Default Dark": return T("默认深色", "Default Dark");
                case "Neko Glass": return T("Neko 玻璃", "Neko Glass");
                case "Aero Glass": return T("Aero 玻璃", "Aero Glass");
                case "Fluent Dark": return T("Fluent 深色", "Fluent Dark");
                case "Mica Dark": return T("Mica 深色", "Mica Dark");
                case "VS Code Glass": return T("VS Code 玻璃", "VS Code Glass");
                case "Deep Black": return T("深邃黑", "Deep Black");
                case "Soft Frost": return T("柔和磨砂", "Soft Frost");
                case "Custom": return T("自定义", "Custom");
                default: return preset;
            }
        }

        private static string SupportDisplayLabel()
        {
            if (NekoThemesPlusVersion.IsExactTarget)
            {
                return T("正式支持版本", "Supported target");
            }

            return NekoThemesPlusVersion.IsSupportedLts
                ? T("支持的 2022.3 LTS", "Supported 2022.3 LTS")
                : T("实验性 / 不支持", "Experimental / unsupported");
        }

        private static string T(string simplifiedChinese, string english)
        {
            return NekoThemesPlusLocalization.Text(simplifiedChinese, english);
        }
    }
}
