using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace TypeMaster.App.Views.Windows;

/// <summary>
/// 首次启动新手引导：卡片式步骤向导，介绍核心功能。
/// 关闭（完成或跳过）后由调用方标记 HasSeenGuide，避免重复打扰。
/// </summary>
public partial class OnboardingWindow : Window
{
    private readonly List<(PackIconKind Icon, string Title, string Desc)> _steps;
    private int _current;

    public OnboardingWindow(string nickname)
    {
        InitializeComponent();
        _steps = new List<(PackIconKind, string, string)>
        {
            (PackIconKind.AccountCircle,
                $"嗨，{nickname}！欢迎来到打字练习机",
                "这里有打字练习，也有超好玩的小游戏。先带你快速逛一圈，几秒钟就看完啦～"),
            (PackIconKind.Typewriter,
                "打字练习",
                "选一篇喜欢的文章，挑一个难度，跟着提示把字打出来。打错了还会有搞笑提示陪你一起练哦。"),
            (PackIconKind.ChartLine,
                "成绩统计",
                "每次练习的速度、正确率和得分都会记在这里，随时回来看看自己进步了多少。"),
            (PackIconKind.Gamepad,
                "游戏乐园",
                "飞船大战、打地鼠、抓小偷、青蛙捉虫——4 款小游戏，边玩边练手速，越玩越快！"),
            (PackIconKind.CogOutline,
                "自定义 & 设置",
                "想打自己的作文？在设置里导入文章。左下角随时改昵称，所有提示都会叫你的名字。"),
            (PackIconKind.PartyPopper,
                "准备好啦！",
                "点下面的按钮，开始你的打字之旅吧～加油，你一定可以的！")
        };
        BuildDots();
        ShowStep(0);
    }

    private void BuildDots()
    {
        DotsPanel.Children.Clear();
        for (int i = 0; i < _steps.Count; i++)
        {
            DotsPanel.Children.Add(new Ellipse
            {
                Width = 9,
                Height = 9,
                Margin = new Thickness(4, 0, 4, 0),
                Fill = (Brush)FindResource("MaterialDesignBodyLight")
            });
        }
    }

    private void ShowStep(int index)
    {
        _current = index;
        var step = _steps[index];
        StepIcon.Kind = step.Icon;
        StepTitle.Text = step.Title;
        StepDesc.Text = step.Desc;

        for (int i = 0; i < DotsPanel.Children.Count; i++)
        {
            var dot = (Ellipse)DotsPanel.Children[i];
            bool active = i == index;
            dot.Fill = (Brush)FindResource(active ? "PrimaryHueMidBrush" : "MaterialDesignBodyLight");
            dot.Width = dot.Height = active ? 11 : 9;
        }

        PrevBtn.IsEnabled = index > 0;
        NextBtn.Content = index == _steps.Count - 1 ? "开始体验" : "下一步";
    }

    private void Prev_Click(object sender, RoutedEventArgs e) =>
        ShowStep(System.Math.Max(0, _current - 1));

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_current == _steps.Count - 1)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            ShowStep(_current + 1);
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
