using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Windows;

/// <summary>
/// 主窗口：macOS 风格无边框玻璃窗体，包含交通灯窗口按钮、玻璃工具栏与页面容器。
/// 无边框 / 透明 / 毛玻璃 / 最大化不遮任务栏等公共能力由 <see cref="GlassWindowBehavior"/> 提供。
/// </summary>
public partial class MainWindow : Window
{
    #region 局部变量属性

    /// <summary>窗体还原状态下的圆角半径。</summary>
    private const double NormalCornerRadius = 14;

    #endregion 局部变量属性

    #region 构造函数

    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MainViewModel>();
        EditNicknameCommand = new RelayCommand(EditNickname);

        var nav = (NavigationService)App.ServiceProvider.GetRequiredService<INavigationService>();
        nav.ViewChanged += (_, e) => ContentArea.Content = e.View;

        // 初次导航到首页
        ((MainViewModel)DataContext).GoHomeCommand.Execute(null);

        // 尺寸与 DPI 适配：按用户设置与当前屏幕可用区决定窗口大小/位置。
        // 注意顺序——必须先裁剪到工作区，再落地尺寸，否则会出现
        // "设置值放不下但仍然照设"导致的窗口超出屏幕。
        ApplyWindowSizing();

        // 系统缩放变化（把窗口拖到另一块不同缩放比的显示器、或用户改了系统缩放）时，
        // 重新计算，保证始终不超出工作区。
        DpiChanged += (_, _) => ApplyWindowSizing();

        // 显示器拓扑变化（插拔外接显示器）同样要重算：
        // 否则窗口可能停留在已经不存在的那块屏幕的坐标上。
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Closed += (_, _) => Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    /// <summary>
    /// 显示器设置变化（分辨率/缩放/插拔外接屏）时的处理。
    /// SystemEvents 在非 UI 线程触发，因此切回 UI 线程再动窗口。
    /// </summary>
    private void OnDisplaySettingsChanged(object? sender, System.EventArgs e)
        => Dispatcher.BeginInvoke(new System.Action(ApplyWindowSizing));

    #endregion 构造函数

    #region 公开属性

    /// <summary>修改昵称命令（供顶栏铅笔按钮绑定）。</summary>
    public ICommand EditNicknameCommand { get; }

    #endregion 公开属性

    #region 标题栏交互

    /// <summary>
    /// 顶栏空白处按下左键拖动窗体，双击切换最大化 / 还原。
    /// </summary>
    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => GlassWindowBehavior.TryDragMove(sender, e);

    /// <summary>关闭窗体。</summary>
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();

    /// <summary>最小化窗体。</summary>
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    /// <summary>在最大化与还原之间切换。</summary>
    private void MaximizeRestoreWindow(object sender, RoutedEventArgs e) => ToggleMaximize();

    /// <summary>切换最大化状态。</summary>
    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    /// <summary>
    /// 最大化时去掉圆角（贴近 macOS 全屏观感），还原时恢复圆角；
    /// 同时把最大化按钮符号切换为"还原"图标。
    /// </summary>
    private void MainWindow_StateChanged(object? sender, System.EventArgs e)
    {
        bool maximized = WindowState == WindowState.Maximized;
        double radius = maximized ? 0 : NormalCornerRadius;
        RootBorder.CornerRadius = new CornerRadius(radius);
        // Segoe MDL2 Assets：E922=最大化，E923=还原
        MaximizeButton.Content = maximized ? "\uE923" : "\uE922";
    }

    #endregion 标题栏交互

    #region 昵称维护

    /// <summary>
    /// 打开修改昵称对话框，保存后同步主窗口显示与趣味提示称呼。
    /// </summary>
    private void EditNickname()
    {
        var profile = App.ServiceProvider.GetRequiredService<IUserProfile>();
        var funTip = App.ServiceProvider.GetRequiredService<FunTipService>();
        var vm = (MainViewModel)DataContext;

        var dlg = new NicknameWindow
        {
            Owner = this,
            InitialNickname = vm.Nickname,
            IsEditMode = true
        };
        if (dlg.ShowDialog() == true)
        {
            var saved = profile.Load();
            saved.Nickname = dlg.Profile.Nickname;
            saved.Initialized = true;
            profile.Save(saved);
            funTip.Nickname = saved.Nickname;
            vm.Nickname = saved.Nickname;
        }
    }

    #endregion 昵称维护

    #region 尺寸自适应

    /// <summary>
    /// 按用户设置与当前屏幕可用区域，决定窗口的尺寸与位置。
    ///
    /// 这是本窗口唯一的尺寸决策入口，会在三种时机被调用：
    ///   · 启动时
    ///   · 系统 DPI 变化时（拖到另一块不同缩放的屏幕 / 改系统缩放）
    ///   · 显示器拓扑变化时（插拔外接屏）
    ///
    /// 处理要点：
    ///   1) 尊重用户在设置里的窗口尺寸与"自动适配"开关
    ///   2) 无论设置多大，都保证窗口完整落在工作区内（含任务栏避让）
    ///   3) 工作区比窗口最小尺寸还小时，临时放宽最小限制——
    ///      否则会出现"窗口比屏幕还大"的病态状态（1080p @200% 就是这种情形）
    /// </summary>
    private void ApplyWindowSizing()
    {
        try
        {
            var cfg = TypeMaster.Core.AppState.Current;

            // ---- 取当前窗口所在显示器的工作区（多屏时取实际所在的那块）----
            Rect workArea = GetCurrentWorkArea();

            // 工作区减去安全边距，避免窗口贴边或压住任务栏
            const double margin = 8;
            double availW = System.Math.Max(320, workArea.Width - margin * 2);
            double availH = System.Math.Max(240, workArea.Height - margin * 2);

            // ---- 期望尺寸 ----
            double desiredW, desiredH;
            if (cfg.AutoFitWindow)
            {
                // 自动适配：以"工作区的 92%"为目标，但不超过用户设定的偏好值，
                // 也不小于一个可用的下限，避免大屏上窗口小得可怜。
                double targetW = System.Math.Min(availW, System.Math.Max(1100, availW * 0.92));
                double targetH = System.Math.Min(availH, System.Math.Max(720, availH * 0.92));
                desiredW = System.Math.Min(targetW, System.Math.Max(1100, cfg.WindowWidth));
                desiredH = System.Math.Min(targetH, System.Math.Max(720, cfg.WindowHeight));

                // 用户把设置调得比屏幕还小时，尊重用户的选择（不强行拉大）
                desiredW = System.Math.Min(desiredW, cfg.WindowWidth <= 0 ? desiredW : System.Math.Max(cfg.WindowWidth, 900));
                desiredH = System.Math.Min(desiredH, cfg.WindowHeight <= 0 ? desiredH : System.Math.Max(cfg.WindowHeight, 600));
            }
            else
            {
                desiredW = cfg.WindowWidth;
                desiredH = cfg.WindowHeight;
            }

            // ---- 关键：绝不超出工作区 ----
            // 屏幕极小时（如 1080p @200%，工作区仅约 960x500 逻辑单位），
            // 连最小尺寸都放不下，此时按可用区收窄，并同步放宽 MinWidth/MinHeight，
            // 否则 WPF 会坚持按最小尺寸布局，窗口依旧溢出屏幕。
            bool cramped = availW < MinWidth || availH < MinHeight;
            if (cramped)
            {
                MinWidth = System.Math.Min(MinWidth, availW);
                MinHeight = System.Math.Min(MinHeight, availH);
            }
            else
            {
                // 恢复常规最小尺寸（在足够大的屏幕上）
                MinWidth = System.Math.Min(760, availW);
                MinHeight = System.Math.Min(560, availH);
            }

            double finalW = System.Math.Clamp(desiredW, System.Math.Min(MinWidth, availW), availW);
            double finalH = System.Math.Clamp(desiredH, System.Math.Min(MinHeight, availH), availH);

            // 最大化状态下改尺寸无效，只有在普通状态才落地
            if (WindowState == WindowState.Normal)
            {
                Width = finalW;
                Height = finalH;

                // 居中（在当前显示器工作区内）
                Left = workArea.Left + (workArea.Width - finalW) / 2;
                Top = workArea.Top + (workArea.Height - finalH) / 2;

                // 防御：极端情况下不要让标题栏跑到屏幕外（否则拖不回来）
                if (Top < workArea.Top)
                {
                    Top = workArea.Top + margin;
                }
                if (Left < workArea.Left)
                {
                    Left = workArea.Left + margin;
                }
            }

            Debug.WriteLine($"[MainWindow] 尺寸适配：工作区 {workArea.Width:F0}x{workArea.Height:F0} " +
                            $"可用 {availW:F0}x{availH:F0} -> 窗口 {finalW:F0}x{finalH:F0} " +
                            $"{(cramped ? "(屏幕偏小，已放宽最小限制)" : "")}");
        }
        catch (System.Exception ex)
        {
            // 适配失败不能让窗口起不来：退回一个保守尺寸
            Debug.WriteLine("[MainWindow] 尺寸适配异常：" + ex.Message);
            if (WindowState == WindowState.Normal && (Width > 2000 || Height > 2000))
            {
                Width = 1100;
                Height = 720;
            }
        }
    }

    /// <summary>
    /// 取当前窗口所在显示器的工作区（逻辑单位）。
    /// 优先用 Win32 <c>MonitorFromWindow</c> + <c>GetMonitorInfo</c>，
    /// 这样多显示器且各屏缩放不同时也能取到"窗口实际所在那块"的数值；
    /// 调用失败则退回 <see cref="SystemParameters.WorkArea"/>。
    /// </summary>
    private Rect GetCurrentWorkArea()
    {
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                IntPtr mon = MonitorFromWindow(helper.Handle, MONITOR_DEFAULTTONEAREST);
                var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(mon, ref info))
                {
                    // 取回的是物理像素，需换算成 WPF 逻辑单位
                    double scale = 1.0;
                    var src = System.Windows.PresentationSource.FromVisual(this);
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
            // 落到下面的兜底分支
        }

        // 兜底：WPF 提供的全屏工作区（主屏）
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

    #endregion 尺寸自适应
}
