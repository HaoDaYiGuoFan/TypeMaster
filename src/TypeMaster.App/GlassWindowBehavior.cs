using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using TypeMaster.Core;
using TypeMaster.Core.Enums;

namespace TypeMaster.App;

/// <summary>
/// 窗体玻璃质感附加行为：为任意 <see cref="Window"/> 启用 macOS 风格的无边框透明 +
/// 亚克力毛玻璃背景 + 最大化不遮挡任务栏。
/// 以附加属性方式提供（而非要求窗体继承基类），避免分部类基类声明不一致，
/// 同时保证 VS 设计器可正常加载。
/// </summary>
public static class GlassWindowBehavior
{
    #region 局部变量属性

    /// <summary>Win32 消息：查询窗口最大化 / 最小化的尺寸限制。</summary>
    private const int WmGetMinMaxInfo = 0x0024;

    /// <summary>Win32 常量：由窗口句柄获取最近的显示器。</summary>
    private const int MonitorDefaultToNearest = 0x00000002;

    #endregion 局部变量属性

    #region 依赖属性

    /// <summary>是否启用玻璃窗体的依赖属性。</summary>
    public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
        "Enable", typeof(bool), typeof(GlassWindowBehavior),
        new PropertyMetadata(false, OnEnableChanged));

    /// <summary>获取是否启用玻璃窗体。</summary>
    /// <param name="obj">目标窗体</param>
    /// <returns>已启用返回 true，否则返回 false</returns>
    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

    /// <summary>设置是否启用玻璃窗体。</summary>
    /// <param name="obj">目标窗体</param>
    /// <param name="value">是否启用</param>
    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

    #endregion 依赖属性

    #region 公开方法

    /// <summary>
    /// 标题栏空白处按下左键时拖动窗体，双击切换最大化 / 还原。
    /// 命中按钮时不接管事件，避免吞掉按钮点击。
    /// </summary>
    /// <param name="sender">标题栏元素</param>
    /// <param name="e">鼠标事件参数</param>
    public static void TryDragMove(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject element)
        {
            return;
        }
        if (Window.GetWindow(element) is not Window window)
        {
            return;
        }
        if (IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }
        if (e.ClickCount == 2 && window.ResizeMode == ResizeMode.CanResize)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }
        try
        {
            window.DragMove();
        }
        catch (InvalidOperationException)
        {
            // 鼠标已释放或窗口正在动画中，忽略本次拖拽
        }
    }

    #endregion 公开方法

    #region 私有方法

    /// <summary>
    /// 启用开关变化时初始化窗体外观：无边框、透明、缩放热区与毛玻璃背景。
    /// </summary>
    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window || e.NewValue is not true)
        {
            return;
        }

        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = true;
        window.Background = Brushes.Transparent;

        // 无边框后由 WindowChrome 提供缩放热区与命中测试
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(8),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false,
            NonClientFrameEdges = NonClientFrameEdges.None
        });

        if (IsSourceInitialized(window))
        {
            AttachHwndHook(window);
        }
        else
        {
            window.SourceInitialized += (_, _) => AttachHwndHook(window);
        }

        if (window.IsLoaded)
        {
            ApplyGlass(window);
        }
        else
        {
            window.Loaded += (_, _) => ApplyGlass(window);
        }
    }

    /// <summary>
    /// 应用毛玻璃背景，并在主题切换时同步刷新。
    /// </summary>
    /// <param name="window">目标窗体</param>
    private static void ApplyGlass(Window window)
    {
        GlassHelper.Enable(window, (ThemeType)AppState.Current.Theme);
        AppState.SettingsChanged += (_, _) =>
        {
            if (window.IsLoaded)
            {
                GlassHelper.Enable(window, (ThemeType)AppState.Current.Theme);
            }
        };
    }

    /// <summary>判断窗体句柄是否已创建。</summary>
    private static bool IsSourceInitialized(Window window)
        => new WindowInteropHelper(window).Handle != IntPtr.Zero;

    /// <summary>
    /// 挂接窗体消息钩子，用于限制最大化尺寸不超过工作区。
    /// </summary>
    /// <param name="window">目标窗体</param>
    private static void AttachHwndHook(Window window)
    {
        if (PresentationSource.FromVisual(window) is HwndSource source)
        {
            source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                WndProc(hwnd, msg, wParam, lParam, ref handled));
        }
    }

    /// <summary>
    /// 窗体消息钩子：仅处理 WM_GETMINMAXINFO，其余消息交回 WPF 默认流程。
    /// </summary>
    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfo)
        {
            ClampMaxSizeToWorkArea(hwnd, lParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// 把窗口的最大化位置与尺寸限制到所在显示器的工作区，避免盖住任务栏。
    /// </summary>
    /// <param name="hwnd">窗体句柄</param>
    /// <param name="lParam">指向 MINMAXINFO 结构的指针</param>
    private static void ClampMaxSizeToWorkArea(IntPtr hwnd, IntPtr lParam)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo();
        monitorInfo.CbSize = Marshal.SizeOf(monitorInfo);
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        mmi.PtMaxPosition.X = Math.Abs(monitorInfo.RcWork.Left - monitorInfo.RcMonitor.Left);
        mmi.PtMaxPosition.Y = Math.Abs(monitorInfo.RcWork.Top - monitorInfo.RcMonitor.Top);
        mmi.PtMaxSize.X = Math.Abs(monitorInfo.RcWork.Right - monitorInfo.RcWork.Left);
        mmi.PtMaxSize.Y = Math.Abs(monitorInfo.RcWork.Bottom - monitorInfo.RcWork.Top);
        Marshal.StructureToPtr(mmi, lParam, true);
    }

    /// <summary>
    /// 判断命中元素是否位于按钮内部，用于区分"拖动标题栏"与"点击按钮"。
    /// </summary>
    /// <param name="source">鼠标事件的原始命中元素</param>
    /// <returns>位于按钮内返回 true，否则返回 false</returns>
    private static bool IsInsideButton(DependencyObject? source)
    {
        DependencyObject? current = source;
        while (current != null)
        {
            if (current is Button)
            {
                return true;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    #endregion 私有方法

    #region Win32 声明

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint PtReserved;
        public NativePoint PtMaxSize;
        public NativePoint PtMaxPosition;
        public NativePoint PtMinTrackSize;
        public NativePoint PtMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int CbSize;
        public NativeRect RcMonitor;
        public NativeRect RcWork;
        public int DwFlags;
    }

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    #endregion Win32 声明
}
