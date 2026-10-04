using System;
using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using TypeMaster.Core.Enums;
using TwoDim = System.Windows;

namespace TypeMaster.App.Converters;

public class PracticeTypeToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            (int)0 => "英文",
            (int)1 => "中文",
            (int)2 => "测速",
            (int)3 => "五笔",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public class DifficultyToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            // 与 Difficulty 枚举取值一致：0简单 / 1普通 / 2困难 / 3入门 / 4地狱
            //（入门、地狱是后追加的档，历史数据中不会出现旧值冲突）
            (int)0 => "简单",
            (int)1 => "普通",
            (int)2 => "困难",
            (int)3 => "入门",
            (int)4 => "地狱",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 将游戏图形枚举映射到 MDIX PackIcon 类型（统一矢量图标，避免 emoji）。
/// </summary>
public class GameGlyphToPackIconKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            GameGlyph.Ship => PackIconKind.Rocket,
            GameGlyph.Mole => PackIconKind.Rodent,
            GameGlyph.Thief => PackIconKind.Run,
            GameGlyph.Bug => PackIconKind.Bug,
            GameGlyph.Balloon => PackIconKind.Balloon,
            GameGlyph.Runner => PackIconKind.RunFast,
            _ => PackIconKind.Target
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 将游戏图形枚举映射到卡通角色精灵的 pack 路径。
///
/// 说明：早期版本用 MDIX PackIcon 单色矢量图标表示角色，
/// 现改为内嵌的卡通精灵图，形象更丰富、更有辨识度。
/// 精灵以 WPF Resource 打包进程序集，故用 pack://application 路径引用，
/// 单文件发布也不会丢资源。
/// </summary>
public class GameGlyphToSpriteConverter : IValueConverter
{
    /// <summary>图形枚举 -> 精灵文件名（位于 Assets/Sprites）。</summary>
    private static string FileFor(object? value) => value switch
    {
        GameGlyph.Ship => "ship.png",       // 太空大战：我方飞船
        GameGlyph.Mole => "mole.png",       // 打地鼠
        GameGlyph.Thief => "thief.png",     // 抓小偷
        GameGlyph.Bug => "bug.png",         // 青蛙吃虫：虫子是移动目标
        GameGlyph.Balloon => "balloon.png", // 打气球
        GameGlyph.Runner => "rocket.png",   // 兜底（生死时速的选手由页面单独渲染）
        _ => "ship.png"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => "pack://application:,,,/TypeMaster;component/Assets/Sprites/" + FileFor(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 将游戏模式枚举与参数比较，相等返回 Visible（用于按模式显示对应的背景装饰层）。
/// </summary>
public class GameModeEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return System.Windows.Visibility.Collapsed;
        bool eq = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return eq ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}

/// <summary>
/// 多选值转换器：传入 [按钮代表的枚举值(Tag), 当前选中的枚举值]，相等返回 true。
/// 用于练习类型 / 难度 分组按钮高亮当前选中项（不依赖 emoji、不破坏 P0 规范）。
/// </summary>
public class EnumEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
            return false;
        return values[0].Equals(values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => new object[] { System.Windows.Data.Binding.DoNothing, System.Windows.Data.Binding.DoNothing };
}

/// <summary>
/// 布尔取反转换器：把 true 变 false、false 变 true。
/// 用于「已解锁显示序号 / 未解锁显示锁图标」这类互斥显示。
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

/// <summary>
/// 布尔 -> Visibility 的取反版本：true 折叠、false 显示。
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}

/// <summary>
/// 热力等级（0~4）-> 画刷：错误越多颜色越深。
/// 0 表示无数据，用中性灰，避免与「错误率很低」混淆。
/// </summary>
public class HeatLevelToBrushConverter : IValueConverter
{
    private static readonly System.Windows.Media.SolidColorBrush NoData =
        Frozen(System.Windows.Media.Color.FromRgb(0xEE, 0xF1, 0xF4));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int level = value is int i ? i : 0;
        return level switch
        {
            1 => Frozen(System.Windows.Media.Color.FromRgb(0xFF, 0xE7, 0xBA)),   // 浅黄
            2 => Frozen(System.Windows.Media.Color.FromRgb(0xFF, 0xCC, 0x80)),   // 橙黄
            3 => Frozen(System.Windows.Media.Color.FromRgb(0xFF, 0xA7, 0x4D)),   // 橙
            4 => Frozen(System.Windows.Media.Color.FromRgb(0xEF, 0x63, 0x4B)),   // 红橙
            _ => NoData
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;

    private static System.Windows.Media.SolidColorBrush Frozen(System.Windows.Media.Color c)
    {
        var b = new System.Windows.Media.SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}

/// <summary>
/// 评级字母 -> 画刷：S/A/B/C/D 用不同颜色，让表格与列表一眼可辨。
/// </summary>
public class GradeToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string g = value?.ToString() ?? string.Empty;
        if (g.StartsWith("S")) return System.Windows.Media.Brushes.MediumPurple;
        if (g.StartsWith("A")) return System.Windows.Media.Brushes.SeaGreen;
        if (g.StartsWith("B")) return System.Windows.Media.Brushes.SteelBlue;
        if (g.StartsWith("C")) return System.Windows.Media.Brushes.DarkGoldenrod;
        return System.Windows.Media.Brushes.IndianRed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}

/// <summary>
/// 评级整数（0~4）-> 评级字母：成绩表里评级以整数存档，展示时需要还原成 S/A/B/C/D。
/// </summary>
public class GradeIntToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int v = value is int i ? i : (int.TryParse(value?.ToString(), out int p) ? p : 0);
        return GradeScale.ToText(GradeScale.FromInt(v));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}

/// <summary>
/// 评级整数（0~4）-> 画刷：与 <see cref="GradeToBrushConverter"/> 同一套配色，
/// 区别只是入参为整数，方便直接绑定成绩记录的 Grade 字段。
/// </summary>
public class GradeIntToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int v = value is int i ? i : (int.TryParse(value?.ToString(), out int p) ? p : 0);
        return v switch
        {
            4 => System.Windows.Media.Brushes.MediumPurple,
            3 => System.Windows.Media.Brushes.SeaGreen,
            2 => System.Windows.Media.Brushes.SteelBlue,
            1 => System.Windows.Media.Brushes.DarkGoldenrod,
            _ => System.Windows.Media.Brushes.IndianRed
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}