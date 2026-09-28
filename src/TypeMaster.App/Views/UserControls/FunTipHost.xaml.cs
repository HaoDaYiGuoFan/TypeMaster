using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.Services;

namespace TypeMaster.App.Views.UserControls;

/// <summary>
/// 趣味提示气泡：订阅 <see cref="FunTipService.TipRequested"/>，按情绪显示彩色卡片 + 矢量图标，
/// 滑入后停留片刻自动淡出。仅作展示，不拦截任何鼠标事件。
/// </summary>
public partial class FunTipHost : UserControl
{
    private readonly FunTipService _fun;
    private readonly DispatcherTimer _hideTimer;
    private bool _showing;

    public FunTipHost()
    {
        InitializeComponent();
        _fun = App.ServiceProvider.GetRequiredService<FunTipService>();
        _fun.TipRequested += OnTip;
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2200) };
        _hideTimer.Tick += (_, _) => Hide();

        // 页面为瞬态，导航离开时本控件会脱离可视树：退订单例服务，避免重复弹窗与内存泄漏
        Unloaded += (_, _) =>
        {
            _fun.TipRequested -= OnTip;
            _hideTimer.Stop();
        };
    }

    private void OnTip(object? sender, FunTipEventArgs e)
        => Dispatcher.Invoke(() => ShowTip(e.Message, e.Mood));

    private void ShowTip(string message, TipMood mood)
    {
        (PackIconKind icon, string brushKey) = mood switch
        {
            TipMood.Oops => (PackIconKind.EmoticonSad, "FestiveAmber"),
            TipMood.Wow => (PackIconKind.Star, "PrimaryHueMidBrush"),
            _ => (PackIconKind.PartyPopper, "SecondaryHueMidBrush")
        };

        ToastIcon.Kind = icon;
        var brush = (Brush)FindResource(brushKey);
        ToastIcon.Foreground = brush;
        Toast.BorderBrush = brush;
        ToastGlow.Color = ((SolidColorBrush)brush).Color;
        ToastText.Text = message;

        Toast.Visibility = Visibility.Visible;
        _showing = true;
        var inSb = (Storyboard)Resources["ToastIn"]!;
        inSb.Begin();
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void Hide()
    {
        _hideTimer.Stop();
        if (!_showing) return;
        _showing = false;
        var outSb = (Storyboard)Resources["ToastOut"]!;
        outSb.Completed += (_, _) => Toast.Visibility = Visibility.Collapsed;
        outSb.Begin();
    }
}
