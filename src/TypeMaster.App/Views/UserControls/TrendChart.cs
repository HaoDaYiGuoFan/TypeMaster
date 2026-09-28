using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.UserControls;

/// <summary>
/// 自绘趋势折线图：同时呈现「速度」与「正确率」两条曲线。
///
/// 之所以手绘而不是引入 LiveCharts2 之类的图表库：
/// 1) 需求只有两条折线 + 网格 + 坐标轴，第三方库的依赖体积与学习成本都不划算；
/// 2) 手绘可以精确控制与 MDIX 主题一致的配色，并直接复用评级颜色。
///
/// 左轴为速度（自适应上限），右轴固定为正确率 0~100%，
/// 这样两条量纲不同的曲线可以放在同一张图里对比。
/// </summary>
public class TrendChart : FrameworkElement
{
    #region 依赖属性

    /// <summary>趋势数据点（按时间升序）。</summary>
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IEnumerable<TrendPoint>), typeof(TrendChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<TrendPoint>? Points
    {
        get => (IEnumerable<TrendPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    #endregion 依赖属性

    #region 绘制常量

    private const double PadLeft = 46;
    private const double PadRight = 50;
    private const double PadTop = 26;
    private const double PadBottom = 26;

    private static readonly Typeface Face = new("Microsoft YaHei");
    private static readonly Brush AxisBrush = Frozen(Color.FromRgb(0x90, 0xA4, 0xB7));
    private static readonly Brush GridBrush = Frozen(Color.FromRgb(0xE0, 0xE6, 0xEC));
    private static readonly Brush TextBrush = Frozen(Color.FromRgb(0x42, 0x57, 0x6B));
    private static readonly Pen SpeedPen = FrozenPen(Color.FromRgb(0x19, 0x76, 0xD2), 2.0);
    private static readonly Pen AccuracyPen = FrozenPen(Color.FromRgb(0x2E, 0x7D, 0x32), 2.0, true);

    #endregion 绘制常量

    public TrendChart()
    {
        // 尺寸变化时需要重画，否则拉伸窗口后坐标会停留在旧值
        SizeChanged += (_, _) => InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth, h = ActualHeight;
        if (w <= PadLeft + PadRight + 10 || h <= PadTop + PadBottom + 10)
        {
            return;
        }

        var list = Points?.ToList() ?? new List<TrendPoint>();
        double plotW = w - PadLeft - PadRight;
        double plotH = h - PadTop - PadBottom;

        // 背景
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        if (list.Count == 0)
        {
            DrawText(dc, "暂无趋势数据，先完成几次练习吧", PadLeft, PadTop + plotH / 2 - 8, TextBrush, 12);
            return;
        }

        // 速度上限留 15% 余量，避免最高点贴顶
        double speedMax = Math.Max(10d, list.Max(p => p.Speed) * 1.15);

        #region 网格与左右轴刻度

        const int gridLines = 4;
        for (int i = 0; i <= gridLines; i++)
        {
            double y = PadTop + plotH * i / gridLines;
            dc.DrawLine(new Pen(GridBrush, 1), new Point(PadLeft, y), new Point(PadLeft + plotW, y));

            // 左轴：速度
            double speedVal = speedMax * (gridLines - i) / gridLines;
            DrawText(dc, speedVal.ToString("F0"), 6, y - 7, AxisBrush, 10);

            // 右轴：正确率
            double accVal = 100d * (gridLines - i) / gridLines;
            DrawText(dc, accVal.ToString("F0") + "%", PadLeft + plotW + 6, y - 7, AxisBrush, 10);
        }

        #endregion 网格与左右轴刻度

        // X 轴（数据为空时列表非空，安全）
        dc.DrawLine(new Pen(AxisBrush, 1), new Point(PadLeft, PadTop + plotH), new Point(PadLeft + plotW, PadTop + plotH));

        #region 曲线

        double StepX(int i) => list.Count == 1 ? PadLeft + plotW / 2 : PadLeft + plotW * i / (list.Count - 1);
        double YSpeed(double v) => PadTop + plotH - Math.Clamp(v / speedMax, 0, 1) * plotH;
        double YAcc(double v) => PadTop + plotH - Math.Clamp(v / 100d, 0, 1) * plotH;

        var speedGeo = new StreamGeometry();
        using (StreamGeometryContext ctx = speedGeo.Open())
        {
            ctx.BeginFigure(new Point(StepX(0), YSpeed(list[0].Speed)), false, false);
            for (int i = 1; i < list.Count; i++)
            {
                ctx.LineTo(new Point(StepX(i), YSpeed(list[i].Speed)), true, false);
            }
        }
        speedGeo.Freeze();
        dc.DrawGeometry(null, SpeedPen, speedGeo);

        var accGeo = new StreamGeometry();
        using (StreamGeometryContext ctx = accGeo.Open())
        {
            ctx.BeginFigure(new Point(StepX(0), YAcc(list[0].Accuracy)), false, false);
            for (int i = 1; i < list.Count; i++)
            {
                ctx.LineTo(new Point(StepX(i), YAcc(list[i].Accuracy)), true, false);
            }
        }
        accGeo.Freeze();
        dc.DrawGeometry(null, AccuracyPen, accGeo);

        #endregion 曲线

        #region 数据点（评级着色）

        // 点太多时只画关键点，避免糊成一团
        int dotStep = list.Count <= 40 ? 1 : (int)Math.Ceiling(list.Count / 40d);
        for (int i = 0; i < list.Count; i++)
        {
            if (i % dotStep != 0 && i != list.Count - 1) continue;
            var p = list[i];
            var center = new Point(StepX(i), YSpeed(p.Speed));
            dc.DrawEllipse(GradeBrush(p.Grade), new Pen(Brushes.White, 1), center, 3.5, 3.5);
        }

        #endregion 数据点（评级着色）

        #region 图例与端点标注

        double lx = PadLeft + 2;
        dc.DrawLine(SpeedPen, new Point(lx, PadTop - 14), new Point(lx + 18, PadTop - 14));
        DrawText(dc, "速度", lx + 22, PadTop - 21, TextBrush, 10);

        double lx2 = lx + 62;
        dc.DrawLine(AccuracyPen, new Point(lx2, PadTop - 14), new Point(lx2 + 18, PadTop - 14));
        DrawText(dc, "正确率", lx2 + 22, PadTop - 21, TextBrush, 10);

        // 首尾时间，帮助定位横轴
        DrawText(dc, list[0].Time.ToString("MM-dd"), PadLeft, PadTop + plotH + 4, AxisBrush, 10);
        string end = list[^1].Time.ToString("MM-dd");
        var ft = MakeText(end, AxisBrush, 10);
        DrawText(dc, end, PadLeft + plotW - ft.Width, PadTop + plotH + 4, AxisBrush, 10);

        #endregion 图例与端点标注
    }

    #region 绘制工具

    /// <summary>按评级取颜色：与成绩页其它位置的评级配色保持一致。</summary>
    private static Brush GradeBrush(int grade) => grade switch
    {
        4 => Frozen(Color.FromRgb(0x9C, 0x4D, 0xD8)),   // S
        3 => Frozen(Color.FromRgb(0x2E, 0x8B, 0x57)),   // A
        2 => Frozen(Color.FromRgb(0x46, 0x82, 0xB4)),   // B
        1 => Frozen(Color.FromRgb(0xB8, 0x86, 0x0B)),   // C
        _ => Frozen(Color.FromRgb(0xCD, 0x5C, 0x5C))    // D
    };

    private void DrawText(DrawingContext dc, string text, double x, double y, Brush brush, double size)
        => dc.DrawText(MakeText(text, brush, size), new Point(x, y));

    /// <summary>PixelsPerDip 按本控件的 DPI 取值，保证高 DPI 下文字清晰且不越界。</summary>
    private double PixelsPerDip => VisualTreeHelper.GetDpi(this).PixelsPerDip;

    private FormattedText MakeText(string text, Brush brush, double size)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Face, size, brush,
            PixelsPerDip);

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Pen FrozenPen(Color c, double thickness, bool dashed = false)
    {
        var p = new Pen(Frozen(c), thickness);
        if (dashed)
        {
            p.DashStyle = new DashStyle(new double[] { 4, 3 }, 0);
        }
        p.Freeze();
        return p;
    }

    #endregion 绘制工具
}