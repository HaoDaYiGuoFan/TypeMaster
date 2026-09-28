using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 成绩统计页：趋势曲线、分类汇总、弱点分析与热力图、CSV 导出。
/// 统计逻辑全在 <see cref="HistoryViewModel"/> 中，页面只负责进入时加载与导出结果提示。
/// </summary>
public partial class HistoryPage : UserControl
{
    private readonly HistoryViewModel _vm;

    public HistoryPage()
    {
        InitializeComponent();
        _vm = App.ServiceProvider.GetRequiredService<HistoryViewModel>();
        DataContext = _vm;

        // 进入页面即自动加载，省去用户先点一次「加载成绩」
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);

        _vm.ExportRequested += OnExportRequested;
    }

    /// <summary>导出完成后提示文件位置，并提供「打开所在文件夹」的便利入口。</summary>
    private void OnExportRequested(object? sender, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show(_vm.StatusMessage, "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"已导出到：\n{path}\n\n是否打开所在文件夹？",
            "导出成功", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            catch
            {
                // 打不开资源管理器（如策略限制）时静默忽略，路径已在对话框里给出
            }
        }
    }
}