using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App.Views.Windows;
using TypeMaster.Core;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 课程中心页：展示「指法入门 → 单键 → 单词 → 句子 → 文章」五阶段关卡路线，
/// 支持选中查看详情、开始挑战与重置进度。
/// 页面本身不处理业务逻辑，关卡状态与解锁判定全部由 <see cref="CourseViewModel"/> 计算。
/// </summary>
public partial class CoursePage : UserControl
{
    private readonly CourseViewModel _vm;

    public CoursePage()
    {
        InitializeComponent();
        _vm = App.ServiceProvider.GetRequiredService<CourseViewModel>();
        DataContext = _vm;

        // 点击「开始挑战」后：进入打字练习页
        _vm.StartRequested += (_, lesson) =>
        {
            if (Window.GetWindow(this)?.DataContext is MainViewModel main)
            {
                main.GoTypingCommand.Execute(null);
            }
        };

        // 每次进入页面都重新读一次进度，保证从练习页返回后解锁状态是最新的
        Loaded += (_, _) => _vm.Reload();
    }

    /// <summary>点击关卡条目：选中它，右侧显示详情。</summary>
    private void Lesson_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LessonItem item })
        {
            _vm.SelectedLesson = item;
        }
    }
}