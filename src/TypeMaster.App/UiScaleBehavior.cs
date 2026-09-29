using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace TypeMaster.App;

/// <summary>
/// 界面整体缩放的附加属性。
///
/// 用法（在任意容器元素上）：
/// <code>
/// &lt;Grid local:UiScaleBehavior.Scale="{Binding Config.UiScale}"&gt;
/// </code>
///
/// 实现说明：用 <see cref="LayoutTransform"/> 而不是 RenderTransform——
/// 前者参与布局测量，缩放后父容器能正确分配尺寸（大屏放大不会被裁），
/// 后者只影响绘制、会导致布局仍按原尺寸计算而出现重叠或裁切。
///
/// 为什么用附加属性而不是自定义 Window/UserControl 基类：
/// 本项目既有约定——WPF 通用行为一律用附加属性实现，
/// 避免自定义基类继承带来的样式与模板连锁问题。
/// </summary>
public static class UiScaleBehavior
{
    /// <summary>界面缩放倍数（1.0 为原始大小）。</summary>
    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.RegisterAttached(
            "Scale",
            typeof(double),
            typeof(UiScaleBehavior),
            new PropertyMetadata(1.0, OnScaleChanged));

    /// <summary>读取缩放值。</summary>
    /// <param name="obj">目标元素</param>
    /// <returns>缩放倍数</returns>
    public static double GetScale(DependencyObject obj)
        => (double)obj.GetValue(ScaleProperty);

    /// <summary>设置缩放值。</summary>
    /// <param name="obj">目标元素</param>
    /// <param name="value">缩放倍数</param>
    public static void SetScale(DependencyObject obj, double value)
        => obj.SetValue(ScaleProperty, value);

    private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        double scale = (double)e.NewValue;

        // 收敛到合理区间，避免用户误设导致界面不可用
        if (double.IsNaN(scale) || scale <= 0)
        {
            scale = 1.0;
        }
        scale = Math.Clamp(scale, 0.7, 2.0);

        if (Math.Abs(scale - 1.0) < 0.001)
        {
            // 1.0 时移除变换，保持绘图路径最短
            element.LayoutTransform = null;
            return;
        }

        element.LayoutTransform = new ScaleTransform(scale, scale);
    }
}
