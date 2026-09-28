using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TypeMaster.Core.Enums;

namespace TypeMaster.App;

/// <summary>
/// 主题管理：亮色 / 护眼绿 / 暗黑 三套主题切换，并同步刷新玻璃质感配色与窗体毛玻璃背景。
/// </summary>
public static class ThemeManager
{
    #region 主题切换

    /// <summary>
    /// 应用指定主题：切换 MDIX 基础主题、主色、窗口底色，并同步玻璃质感相关画刷。
    /// </summary>
    /// <param name="type">目标主题</param>
    public static void ApplyTheme(ThemeType type)
    {
        var palette = new PaletteHelper();
        ITheme theme = palette.GetTheme();

        theme.SetBaseTheme(type == ThemeType.Dark
            ? new MaterialDesignDarkTheme()
            : new MaterialDesignLightTheme());

        (Color primary, Color background) = type switch
        {
            ThemeType.Dark => (Color.FromRgb(0x42, 0xA5, 0xF5), Color.FromRgb(0x21, 0x21, 0x21)),
            ThemeType.EyeCare => (Color.FromRgb(0x68, 0x9F, 0x38), Color.FromRgb(0xE8, 0xF5, 0xE9)),
            _ => (Color.FromRgb(0x21, 0x96, 0xF3), Colors.White)
        };

        theme.SetPrimaryColor(primary);
        palette.SetTheme(theme);

        Application.Current.Resources["WindowBackground"] = new SolidColorBrush(background);
        ApplyGlassPalette(type);
    }

    #endregion 主题切换

    #region 玻璃配色

    /// <summary>
    /// 按主题刷新玻璃质感相关的动态资源，保证深色/护眼主题下通透而不失可读性。
    /// </summary>
    /// <param name="type">目标主题</param>
    private static void ApplyGlassPalette(ThemeType type)
    {
        (Color top, Color bottom) glass = type switch
        {
            ThemeType.Dark => (Color.FromArgb(0xD9, 0x2B, 0x2F, 0x36), Color.FromArgb(0xD9, 0x1A, 0x1E, 0x24)),
            ThemeType.EyeCare => (Color.FromArgb(0xD9, 0xF1, 0xF8, 0xE9), Color.FromArgb(0xD9, 0xE3, 0xF0, 0xDC)),
            _ => (Color.FromArgb(0xD9, 0xF4, 0xF8, 0xFF), Color.FromArgb(0xD9, 0xEC, 0xF4, 0xFF))
        };

        (Color top, Color bottom) bar = type switch
        {
            ThemeType.Dark => (Color.FromArgb(0xF2, 0x3A, 0x3F, 0x47), Color.FromArgb(0xF2, 0x2A, 0x2E, 0x35)),
            ThemeType.EyeCare => (Color.FromArgb(0xF2, 0xFB, 0xFD, 0xF5), Color.FromArgb(0xF2, 0xEB, 0xF5, 0xE4)),
            _ => (Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xF2, 0xEA, 0xF3, 0xFC))
        };

        Color surface = type switch
        {
            ThemeType.Dark => Color.FromArgb(0xB8, 0x33, 0x37, 0x3F),
            ThemeType.EyeCare => Color.FromArgb(0xB8, 0xFF, 0xFF, 0xFF),
            _ => Color.FromArgb(0xB8, 0xFF, 0xFF, 0xFF)
        };

        Color surfaceStrong = type switch
        {
            ThemeType.Dark => Color.FromArgb(0xD9, 0x3D, 0x42, 0x4B),
            ThemeType.EyeCare => Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF),
            _ => Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF)
        };

        Color text = type switch
        {
            ThemeType.Dark => Color.FromArgb(0xF2, 0xF2, 0xF4, 0xF7),
            _ => Color.FromArgb(0xF2, 0x14, 0x20, 0x29)
        };

        // 次色在浅色主题下要足够深，保证小字号在半透明玻璃上仍清晰
        Color textSecondary = type switch
        {
            ThemeType.Dark => Color.FromArgb(0xEE, 0xC9, 0xD4, 0xDD),
            _ => Color.FromArgb(0xEE, 0x42, 0x57, 0x6B)
        };

        var resources = Application.Current.Resources;
        resources["GlassWindowBrush"] = new LinearGradientBrush(glass.top, glass.bottom, new Point(0, 0), new Point(1, 1));
        resources["GlassTopBarBrush"] = new LinearGradientBrush(bar.top, bar.bottom, new Point(0, 0), new Point(0, 1));
        resources["GlassSurfaceBrush"] = new SolidColorBrush(surface);
        resources["GlassSurfaceStrongBrush"] = new SolidColorBrush(surfaceStrong);
        resources["GlassTextBrush"] = new SolidColorBrush(text);
        resources["GlassTextSecondaryBrush"] = new SolidColorBrush(textSecondary);
    }

    #endregion 玻璃配色
}
