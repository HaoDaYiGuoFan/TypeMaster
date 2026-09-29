using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 系统设置页。
///
/// 本页承担三类联动逻辑：
/// 1) 长辈模式：勾选后一键应用一组"适合中老年"的参数（见 ApplyElderMode）
/// 2) 使用者管理：切换 / 新建 / 重命名 / 删除本地使用者
/// 3) 显示适配：按当前屏幕可用区域约束窗口尺寸滑块范围，并即时提示是否放得下
/// </summary>
public partial class SettingsPage : UserControl
{
    private readonly SettingsViewModel _vm;

    /// <summary>页面初始化期间为 true，避免加载配置时误触发联动。</summary>
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();
        _vm = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
        DataContext = _vm;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loading = false;
        ApplyScreenConstraints();
    }

    #region 长辈模式

    /// <summary>长辈模式勾选变化：调用 ViewModel 应用或撤销整套预设。</summary>
    private void ElderMode_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }
        if (sender is CheckBox box)
        {
            _vm.ApplyElderMode(box.IsChecked == true);
        }
    }

    #endregion 长辈模式

    #region 使用者管理

    /// <summary>切换到指定使用者：需要重启应用才生效（数据库连接已绑定旧目录）。</summary>
    private void SwitchUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string id)
        {
            return;
        }

        var target = _vm.UserRows.FirstOrDefault(r => r.Id == id);
        if (target is null || target.IsCurrent)
        {
            _vm.UserStatusMessage = "已经是在使用这位了";
            return;
        }

        var confirm = MessageBox.Show(
            $"切换到使用者「{target.Nickname}」后需要重启应用才能生效。\n\n" +
            "原因：数据库连接与各项服务在启动时就绑定到了当前使用者的数据，\n" +
            "重启可以确保不会串数据。\n\n现在切换并重启吗？",
            "切换使用者", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        if (_vm.SwitchUser(id))
        {
            RestartApplication();
        }
    }

    /// <summary>重命名使用者（只改昵称，不动数据目录，成绩不受影响）。</summary>
    private void RenameUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string id)
        {
            return;
        }
        var target = _vm.UserRows.FirstOrDefault(r => r.Id == id);
        if (target is null)
        {
            return;
        }

        var dlg = new Views.Windows.NicknameWindow
        {
            Owner = Window.GetWindow(this),
            InitialNickname = target.Nickname,
            IsEditMode = true
        };
        if (dlg.ShowDialog() == true)
        {
            _vm.RenameUser(id, dlg.Profile.Nickname);
        }
    }

    /// <summary>新建使用者。</summary>
    private void CreateUser_Click(object sender, RoutedEventArgs e) => _vm.CreateUser();

    /// <summary>
    /// 删除使用者：需要先选中列表中的一位（用"删除选中"按钮的行为，
    /// 因此这里取列表里第一位非当前使用者——列表按最近使用排序，
    /// 这样"删除选中"在没有显式选择时的语义是"删最近用过但不是我的那位"，
    /// 但仍然要用户二次确认，且明确告知会删除全部数据。
    /// </summary>
    private void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        var candidate = _vm.UserRows.FirstOrDefault(r => !r.IsCurrent);
        if (candidate is null)
        {
            _vm.UserStatusMessage = "没有可删除的使用者（至少要保留一位，且不能删除正在使用的这位）";
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要删除使用者「{candidate.Nickname}」吗？\n\n" +
            "该使用者的以下数据会被一并删除，且无法恢复：\n" +
            "  · 全部打字成绩\n" +
            "  · 课程闯关进度\n" +
            "  · 自定义文章\n" +
            "  · 个人设置与昵称\n\n" +
            "如需保留，请先取消并用「切换」进入该使用者导出成绩。",
            "删除使用者（不可恢复）", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        // 删除属于不可逆操作，再确认一次，避免误点
        var again = MessageBox.Show(
            $"再次确认：永久删除「{candidate.Nickname}」的全部数据？",
            "最后确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (again != MessageBoxResult.Yes)
        {
            return;
        }

        _vm.DeleteUser(candidate.Id);
    }

    #endregion 使用者管理

    #region 显示适配

    /// <summary>
    /// 按当前屏幕可用区域约束两个尺寸滑块的上限，并给出提示。
    ///
    /// 为什么要约束：窗口逻辑尺寸 × 系统缩放必须 ≤ 工作区物理尺寸，
    /// 否则窗口会超出屏幕（1080p 在 175% / 200% 缩放下就会发生）。
    /// 这里把上限压到"当前屏幕放得下"的范围，从源头避免用户设出无效值。
    /// </summary>
    private void ApplyScreenConstraints()
    {
        try
        {
            // WorkArea 已是设备无关单位（WPF 已按 DPI 换算），因此可直接比较
            var wa = SystemParameters.WorkArea;
            var primary = SystemParameters.PrimaryScreenWidth;

            // 换算当前系统缩放比，用于提示文案
            double dpiScale = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget is not null)
            {
                dpiScale = source.CompositionTarget.TransformToDevice.M11;
            }

            int screenW = (int)Math.Round(primary * dpiScale);
            int screenH = (int)Math.Round((SystemParameters.PrimaryScreenHeight) * dpiScale);

            ScreenInfoText.Text =
                $"当前屏幕：{screenW} × {screenH} 像素，系统缩放 {dpiScale * 100:F0}%；" +
                $"窗口可用区域约 {wa.Width:F0} × {wa.Height:F0}（逻辑单位）。";

            // 滑块上限：不超过工作区（留 20 逻辑单位余量），下限保持 900/600
            double maxW = Math.Max(900, Math.Floor(wa.Width) - 20);
            double maxH = Math.Max(600, Math.Floor(wa.Height) - 20);
            if (WindowWidthSlider is not null)
            {
                WindowWidthSlider.Maximum = Math.Min(1600, maxW);
            }
            if (WindowHeightSlider is not null)
            {
                WindowHeightSlider.Maximum = Math.Min(1000, maxH);
            }

            // 若配置值超出当前屏幕可容纳范围，提示用户（不强行改写其设置）
            int cfgW = _vm.Config.WindowWidth;
            int cfgH = _vm.Config.WindowHeight;
            bool tooBig = cfgW > maxW || cfgH > maxH;
            WindowSizeHintText.Text = tooBig
                ? $"提示：当前设置 {cfgW} × {cfgH} 超出本机可用区域，" +
                  $"启动时会自动缩小到 {Math.Min(cfgW, (int)maxW)} × {Math.Min(cfgH, (int)maxH)} 以内。" +
                  "可点下方按钮按屏幕重置。"
                : $"当前设置 {cfgW} × {cfgH} 在本机可以完整显示。";
        }
        catch
        {
            // 屏幕信息读取失败不影响其它设置项
            ScreenInfoText.Text = "未能读取屏幕信息；窗口尺寸会在启动时自动限制在可用区域内。";
        }
    }

    /// <summary>按当前屏幕把窗口尺寸重置为合适值。</summary>
    private void ResetWindowSize_Click(object sender, RoutedEventArgs e)
    {
        var wa = SystemParameters.WorkArea;
        // 取"工作区的 85%"，但不超过用户设定的偏好值
        int w = (int)Math.Round(Math.Min(_vm.Config.WindowWidth, wa.Width * 0.85));
        int h = (int)Math.Round(Math.Min(_vm.Config.WindowHeight, wa.Height * 0.85));
        _vm.Config.WindowWidth = Math.Max(900, w);
        _vm.Config.WindowHeight = Math.Max(600, h);
        ApplyScreenConstraints();
        _vm.StatusMessage = $"窗口尺寸已按当前屏幕重置为 {_vm.Config.WindowWidth} × {_vm.Config.WindowHeight}，保存后生效";
    }

    #endregion 显示适配

    #region 应用重启

    /// <summary>
    /// 重启当前应用（用于切换使用者后让新数据目录生效）。
    ///
    /// 实现方式：启动一个新的同路径进程，然后关闭当前进程。
    /// 不用 Process.Start(exe) 直接传入原命令行，是因为单文件发布时
    /// 命令行可能带有解包相关的参数，直接复用可能出错；这里只传 exe 路径。
    /// </summary>
    private static void RestartApplication()
    {
        try
        {
            string exe = Environment.ProcessPath ?? string.Empty;
            if (!string.IsNullOrEmpty(exe))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // 启动失败也要继续关闭自己，否则用户会卡在"提示要重启但没重启"的状态
        }
        Application.Current.Shutdown();
    }

    #endregion 应用重启
}
