using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 系统设置页。
///
/// 长辈模式的联动说明：勾选框绑定的是 Config.ElderMode，
/// 仅改变这个值不会自动应用其它参数，因此这里在勾选变化时
/// 通知 ViewModel 执行"一键应用 / 撤销"的逻辑。
/// </summary>
public partial class SettingsPage : UserControl
{
    private readonly SettingsViewModel _vm;

    /// <summary>页面初始化期间为 true，避免加载配置时误触发长辈模式联动。</summary>
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();
        _vm = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
        DataContext = _vm;

        // 配置是异步加载的，加载完成后才允许联动，避免把默认值当成用户操作
        Loaded += (_, _) => _loading = false;
    }

    /// <summary>
    /// 长辈模式勾选变化：调用 ViewModel 应用或撤销整套预设。
    /// </summary>
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
}
