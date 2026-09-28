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

        // DPI 自适应：确保窗口在任何缩放比例下不超出屏幕工作区
        FitToWorkingArea();
    }

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
    /// 将窗口尺寸限制在屏幕工作区内，兼容 100%/125%/150%/200% 等缩放。
    /// 窗口居中显示，顶部不被任务栏/屏幕边缘截断。
    /// </summary>
    private void FitToWorkingArea()
    {
        var workArea = SystemParameters.WorkArea; // 已自动处理 DPI 缩放，返回设备无关单位
        var desiredWidth = Width;
        var desiredHeight = Height;

        // 如果窗口超出工作区，等比缩小至合适尺寸（保留最小边距）
        if (desiredWidth > workArea.Width || desiredHeight > workArea.Height)
        {
            double scaleX = (workArea.Width - 20) / desiredWidth;
            double scaleY = (workArea.Height - 20) / desiredHeight;
            double scale = System.Math.Min(scaleX, scaleY);
            scale = System.Math.Max(scale, 0.7); // 不要缩得太小

            Width = desiredWidth * scale;
            Height = desiredHeight * scale;
        }

        // 居中显示（确保在可视区域内）
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;

        // 防止顶部被截断（多显示器或特殊任务栏位置时）
        if (Top < workArea.Top)
        {
            Top = workArea.Left + 10;
        }
    }

    #endregion 尺寸自适应
}
