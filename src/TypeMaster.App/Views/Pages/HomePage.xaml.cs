using System.Windows;
using System.Windows.Input;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// Jinshan 风格首页：蓝色渐变横幅 + 4 张功能卡片 + 底部快捷栏。
/// 卡片点击通过 DataContext（MainViewModel）的导航命令跳转到对应页面。
/// </summary>
public partial class HomePage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void CardClick_Typing(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.GoTypingCommand.Execute(null);
    }

    private void CardClick_History(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.GoHistoryCommand.Execute(null);
    }

    private void CardClick_GameCenter(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.GoGameCenterCommand.Execute(null);
    }

    private void CardClick_Learn(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.GoLearnCommand.Execute(null);
    }

    private void CardClick_Course(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.GoCourseCommand.Execute(null);
    }

    private void CardClick_Settings(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.GoSettingsCommand.Execute(null);
    }

    /// <summary>打开新手入门操作指引（可随时重复查看，不影响"首次启动已看过"标记）。</summary>
    private void ShowGuide(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is Views.Windows.MainWindow main)
        {
            main.ShowOnboarding();
        }
    }
}
