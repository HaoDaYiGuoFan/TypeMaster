using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace TypeMaster.App.Views.Windows;

/// <summary>
/// 自定义文章导入对话框：支持手动粘贴或从 .txt 文件读取，
/// 返回文章标题与正文供打字练习使用。
/// </summary>
public partial class ImportArticleWindow : Window
{
    public string ArticleTitle { get; private set; } = string.Empty;
    public string ArticleContent { get; private set; } = string.Empty;

    public ImportArticleWindow()
    {
        InitializeComponent();
    }

    private void PickFile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择文章文本文件"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                ContentBox.Text = File.ReadAllText(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败：{ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void Ok(object sender, RoutedEventArgs e)
    {
        var content = (ContentBox.Text ?? string.Empty).Trim();
        if (content.Length == 0)
        {
            MessageBox.Show("请先输入或导入文章正文", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var title = (TitleBox.Text ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            var firstLine = content.Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0) ?? "我的文章";
            title = firstLine.Length > 24 ? firstLine.Substring(0, 24) : firstLine;
        }

        ArticleTitle = title;
        ArticleContent = content;
        DialogResult = true;
    }

    private void Cancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
