namespace NekoThemesPlus.Core
{
    public enum NekoThemesPlusLanguage
    {
        SimplifiedChinese,
        English
    }

    internal static class NekoThemesPlusLocalization
    {
        public static bool IsChinese
        {
            get { return NekoThemesPlusSettings.instance.language == NekoThemesPlusLanguage.SimplifiedChinese; }
        }

        public static string Text(string simplifiedChinese, string english)
        {
            return IsChinese ? simplifiedChinese : english;
        }

        public static string PageName(string key)
        {
            switch (key)
            {
                case "Global": return Text("全局", "Global");
                case "Background": return Text("背景", "Background");
                case "Glass": return Text("玻璃效果", "Glass");
                case "Windows": return Text("窗口", "Windows");
                case "Colors": return Text("颜色", "Colors");
                case "Windows Effects": return Text("Windows 效果", "Windows Effects");
                case "Presets": return Text("预设", "Presets");
                case "Advanced": return Text("高级", "Advanced");
                case "About": return Text("关于", "About");
                default: return key;
            }
        }

        public static string ImageModeName(Background.BackgroundImageMode mode)
        {
            switch (mode)
            {
                case Background.BackgroundImageMode.Fill: return Text("填充", "Fill");
                case Background.BackgroundImageMode.Fit: return Text("适应", "Fit");
                case Background.BackgroundImageMode.Stretch: return Text("拉伸", "Stretch");
                case Background.BackgroundImageMode.Center: return Text("居中", "Center");
                case Background.BackgroundImageMode.Tile: return Text("平铺", "Tile");
                default: return mode.ToString();
            }
        }
    }
}
