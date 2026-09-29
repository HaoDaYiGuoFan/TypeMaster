using System;
using System.Windows;
using System.Windows.Media;

namespace TypeMaster.App;

/// <summary>
/// 对话框尺寸自适应附加属性。
///
/// 背景：项目里有若干固定尺寸的对话框（昵称、新手引导、导入文章、选择使用者等）。
/// 在"低分屏 + 高缩放"的组合下，固定尺寸会超出屏幕可用区域——
/// 例如 1080p @200% 时工作区只有约 960 × 500 逻辑单位，
/// 而新手引导窗口高 580，会有一截落到屏幕外、按钮点不到。
///
/// 用法（在 Window 根元素上）：
/// <code>
/// &lt;Window local:DialogSizingBehavior.ConstrainToWorkArea="True" ...&gt;
/// </code>
///
/// 行为：在窗口加载时把尺寸收进当前屏幕工作区（保留边距），
/// 并按可用区同步放宽 MinWidth / MinHeight，避免 WPF 坚持按最小值布局而溢出。
///
/// 为什么用附加属性而不是让每个对话框继承自定义基类：
/// 本项目既有约定——WPF 通用行为一律用附加属性实现，
/// 避免自定义基类继承带来的样式与模板连锁问题。
/// </summary>
public static class DialogSizingBehavior
{
    /// <summary>是否把窗口尺寸约束到屏幕工作区内。</summary>
    public static readonly DependencyProperty ConstrainToWorkAreaProperty =
        DependencyProperty.RegisterAttached(
            "ConstrainToWorkArea",
            typeof(bool),
            typeof(DialogSizingBehavior),
            new PropertyMetadata(false, OnConstrainChanged));

    /// <summary>读取是否启用约束。</summary>
    /// <param name="obj">目标对象</param>
    /// <returns>是否启用</returns>
    public static bool GetConstrainToWorkArea(DependencyObject obj)
        => (bool)obj.GetValue(ConstrainToWorkAreaProperty);

    /// <summary>设置是否启用约束。</summary>
    /// <param name="obj">目标对象</param>
    /// <param name="value">是否启用</param>
    public static void SetConstrainToWorkArea(DependencyObject obj, bool value)
        => obj.SetValue(ConstrainToWorkAreaProperty, value);

    private static void OnConstrainChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window || e.NewValue is not true)
        {
            return;
        }

        // 在 Loaded 时处理，此时窗口已知道自己在哪块显示器上
        window.Loaded += (_, _) => Fit(window);
        // 缩放变化时重新收一次（拖到另一块不同缩放的屏幕）
        window.DpiChanged += (_, _) => Fit(window);
    }

    /// <summary>
    /// 把窗口尺寸收进其所在显示器的工作区。
    /// </summary>
    /// <param name="window">目标窗口</param>
    private static void Fit(Window window)
    {
        try
        {
            Rect work = GetWorkArea(window);

            const double margin = 16;
            double availW = Math.Max(320, work.Width - margin * 2);
            double availH = Math.Max(240, work.Height - margin * 2);

            // 先放宽最小限制：否则 WPF 会坚持按 MinWidth/MinHeight 布局而溢出屏幕
            if (window.MinWidth > availW)
            {
                window.MinWidth = availW;
            }
            if (window.MinHeight > availH)
            {
                window.MinHeight = availH;
            }

            double w = Math.Min(window.Width, availW);
            double h = Math.Min(window.Height, availH);

            // 允许放大到工作区（小屏上保持原尺寸不动即可，不强行拉大）
            if (w < window.Width)
            {
                window.Width = w;
            }
            if (h < window.Height)
            {
                window.Height = h;
            }

            // 居中到工作区（CenterScreen / CenterOwner 在尺寸变更后可能偏出）
            if (window.WindowStartupLocation == WindowStartupLocation.CenterScreen)
            {
                window.Left = work.Left + (work.Width - window.Width) / 2;
                window.Top = work.Top + (work.Height - window.Height) / 2;
            }
        }
        catch
        {
            // 适配失败不应阻止对话框显示
        }
    }

    /// <summary>
    /// 取窗口所在显示器的工作区（逻辑单位）。
    /// 优先用 Win32 接口（多屏不同缩放时取实际的屏幕），失败则退回 WPF 的主屏工作区。
    /// </summary>
    /// <param name="window">目标窗口</param>
    /// <returns>工作区矩形（逻辑单位）</returns>
    private static Rect GetWorkArea(Window window)
    {
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            if (helper.Handle != IntPtr.Zero)
            {
                IntPtr mon = MonitorFromWindow(helper.Handle, MONITOR_DEFAULTTONEAREST);
                var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(mon, ref info))
                {
                    double scale = 1.0;
                    var src = PresentationSource.FromVisual(window);
                    if (src?.CompositionTarget is not null)
                    {
                        scale = src.CompositionTarget.TransformToDevice.M11;
                    }
                    if (scale <= 0)
                    {
                        scale = 1.0;
                    }

                    var r = info.rcWork;
                    return new Rect(
                        r.Left / scale,
                        r.Top / scale,
                        (r.Right - r.Left) / scale,
                        (r.Bottom - r.Top) / scale);
                }
            }
        }
        catch
        {
            // 落到兜底
        }

        return SystemParameters.WorkArea;
    }

    #region Win32 互操作

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    #endregion Win32 互操作
}
