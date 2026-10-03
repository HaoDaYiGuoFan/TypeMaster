using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.Core;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 打字小游戏对战页：Canvas 渲染活动目标，DispatcherTimer 驱动引擎每帧推进，
/// 文本框捕获输入并回显当前锁定目标的前缀。界面层额外负责播放炫光特效。
/// </summary>
public partial class GamePlayPage : UserControl
{
    private readonly GameViewModel _vm;
    private readonly IKeyboardHookService _hook;
    private readonly IMusicService _music;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _sw = new();
    private readonly Random _rand = new();
    private int _lastLen;
    private int _tickCount;
    private bool _wasRunning;
    private bool _shakeBusy;

    private static readonly string[] FxBrushKeys =
        { "PrimaryHueMidBrush", "SecondaryHueMidBrush", "FestiveGreen", "FestiveAmber", "FestiveBlue", "FestiveTeal" };

    public GamePlayPage()
    {
        InitializeComponent();
        _vm = App.ServiceProvider.GetRequiredService<GameViewModel>();
        DataContext = _vm;
        _hook = App.ServiceProvider.GetRequiredService<IKeyboardHookService>();
        _music = App.ServiceProvider.GetRequiredService<IMusicService>();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
        _timer.Tick += OnTimerTick;
        _vm.EffectRequested += OnEffect;
        ComboText.RenderTransformOrigin = new Point(0.5, 0.5);

        Loaded += OnLoaded;
        Unloaded += (_, _) =>
        {
            _timer.Stop();
            // 离开对战页时停止 BGM，不在其它页面残留声音
            _music.Stop();
        };
    }

    /// <summary>由导航服务在解析本页后调用，注入游戏模式</summary>
    public void Configure(GameMode mode)
    {
        _vm.Initialize(mode);
        // 每款游戏一首独立 BGM，风格与玩法匹配
        _music.Play(MusicLibrary.TrackFor(mode));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.SetBounds(PlayArea.ActualWidth, PlayArea.ActualHeight);
        PlayArea.SizeChanged += (_, _) => _vm.SetBounds(PlayArea.ActualWidth, PlayArea.ActualHeight);
        _hook.Start();
        _timer.Start();
        _sw.Restart();
        InputBox.Focus();
    }

    /// <summary>每帧用真实经过时间推进引擎（帧率无关，避免卡顿后运动突变）</summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_sw.IsRunning) { _sw.Restart(); return; }
        double dt = _sw.Elapsed.TotalSeconds;
        _sw.Restart();
        if (dt > 0.05) dt = 0.05; // 切回前台后首帧可能很大，钳制防止跳变
        _vm.Tick(dt);
        SyncRacingRunners();

        // 持续特效：拖尾 + 连击火焰（仅在对局进行中每帧推进）
        if (_vm.IsRunning)
        {
            _tickCount++;
            SpawnTrails();
            UpdateFlames();
        }
        else if (_wasRunning)
        {
            ClearContinuousFx();
        }
        _wasRunning = _vm.IsRunning;
    }

    private void InputBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var txt = InputBox.Text;
        if (txt.Length > _lastLen)
            _vm.Feed(txt[^1]);
        else if (txt.Length < _lastLen)
            _vm.ResetLock();
        _lastLen = txt.Length;

        // 让输入框内容始终等于当前锁定目标已匹配的前缀
        var buf = _vm.CurrentBuffer;
        if (InputBox.Text != buf)
        {
            InputBox.Text = buf;
            _lastLen = buf.Length;
        }
        InputBox.CaretIndex = buf.Length;
    }

    private void InputBox_GotFocus(object? sender, RoutedEventArgs e) => InputBox.CaretIndex = InputBox.Text.Length;

    private void RefocusInput(object sender, RoutedEventArgs e) => InputBox.Focus();

    /// <summary>
    /// 生死时速：把引擎中两名选手的横向位置同步到跑道装饰层（其它模式不处理）。
    /// </summary>
    private void SyncRacingRunners()
    {
        if (_vm.Mode != GameMode.LifeDeathSpeed)
        {
            return;
        }
        if (PlayerRunner.RenderTransform is TranslateTransform pt)
        {
            pt.X = _vm.PlayerX;
        }
        if (RivalRunner.RenderTransform is TranslateTransform rt)
        {
            rt.X = _vm.RivalX;
        }
    }

    // ---- 视觉特效 ----

    private void OnEffect(object? sender, GameEffectEventArgs e)
    {
        switch (e.Kind)
        {
            case GameEffectKind.WordBurst:
                Burst(e.X, e.Y, e.Value);
                break;
            case GameEffectKind.ComboPulse:
                PulseCombo();
                Flash(0xFB, 0x8C, 0x00, 360, 0.18);
                break;
            case GameEffectKind.LifeLost:
                FlashDanger();
                break;
            case GameEffectKind.WrongKey:
                ShakeInput();
                break;
            case GameEffectKind.ScreenShake:
                ShakeScreen(e.Value);
                break;
            case GameEffectKind.GameOver:
                Confetti();
                break;
        }
    }

    private Brush RandomFxBrush() => (Brush)FindResource(FxBrushKeys[_rand.Next(FxBrushKeys.Length)]);

    /// <summary>
    /// 该游戏模式下移动目标对应的卡通精灵（用于消散动画）。
    /// 与 <c>GameGlyphToSpriteConverter</c> 的映射保持一致。
    /// </summary>
    private string SpriteForCurrentMode() => _vm.Mode switch
    {
        GameMode.SpaceWar => "ship.png",
        GameMode.WhackMole => "mole.png",
        GameMode.CatchThief => "thief.png",
        GameMode.FrogBug => "bug.png",
        GameMode.BalloonPop => "balloon.png",
        _ => "ship.png"
    };

    /// <summary>
    /// 角色消散动画：在原地复制一份卡通精灵，先轻微放大（打击感），
    /// 再边旋转边缩小并淡出，同时向上飘。
    ///
    /// 为什么要单独画一份：目标被消灭后会立即从 Targets 集合移除，
    /// 数据模板随之销毁，无法在模板内部把动画播完。故在特效层复制一份，
    /// 让"打中了"有明确的视觉交代，而不是目标凭空消失。
    /// </summary>
    private void SpriteDisintegrate(double x, double y)
    {
        const double size = 72;

        var scale = new ScaleTransform(1, 1);
        var rotate = new RotateTransform(0);
        var lift = new TranslateTransform(0, 0);
        var group = new TransformGroup();
        group.Children.Add(scale);
        group.Children.Add(rotate);
        group.Children.Add(lift);

        var img = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/TypeMaster;component/Assets/Sprites/" + SpriteForCurrentMode())),
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = group
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

        Canvas.SetLeft(img, x - size / 2);
        Canvas.SetTop(img, y - size / 2);
        FxCanvas.Children.Add(img);

        var sb = new Storyboard();
        // 阶段一：快速放大一点点，做出"被击中"的顿挫感
        AddTransformDouble(sb, scale, "ScaleX", 1.0, 1.24, 110, 0, EaseOut);
        AddTransformDouble(sb, scale, "ScaleY", 1.0, 1.24, 110, 0, EaseOut);
        // 阶段二：旋转着缩小并上飘
        AddTransformDouble(sb, scale, "ScaleX", 1.24, 0.05, 360, 110, EaseIn);
        AddTransformDouble(sb, scale, "ScaleY", 1.24, 0.05, 360, 110, EaseIn);
        AddTransformDouble(sb, rotate, "Angle", 0, 160, 470, 0, EaseOut);
        AddTransformDouble(sb, lift, "Y", 0, -58, 470, 0, EaseOut);
        AddDouble(sb, img, "Opacity", 1.0, 0.0, 350, EaseIn, 120);

        sb.Completed += (_, _) => FxCanvas.Children.Remove(img);
        sb.Begin();
    }

    /// <summary>消灭目标：角色消散 + 扩散光环 + 星形粒子四散 + 飘字加分</summary>
    private void Burst(double x, double y, int gained)
    {
        var canvas = FxCanvas;

        // 角色消散：让"打中了"有明确的视觉交代
        SpriteDisintegrate(x, y);

        // 扩散光环
        var ring = new Ellipse
        {
            Width = 12, Height = 12,
            Stroke = _vm.AccentBrush, StrokeThickness = 3,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(ring, x - 6); Canvas.SetTop(ring, y - 6);
        canvas.Children.Add(ring);
        var ringSb = new Storyboard();
        AddDouble(ringSb, ring, "Width", 12, 130, 600);
        AddDouble(ringSb, ring, "Height", 12, 130, 600);
        AddDouble(ringSb, ring, "Opacity", 0.9, 0, 600);
        ringSb.Completed += (_, _) => canvas.Children.Remove(ring);
        ringSb.Begin();

        // 星形粒子四散
        const int count = 12;
        for (int i = 0; i < count; i++)
        {
            double ang = i * (Math.PI * 2 / count) + _rand.NextDouble() * 0.4;
            double dist = 55 + _rand.Next(45);
            var star = new PackIcon
            {
                Kind = PackIconKind.Star,
                Width = 13, Height = 13,
                Foreground = RandomFxBrush(),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform()
            };
            Canvas.SetLeft(star, x - 6); Canvas.SetTop(star, y - 6);
            canvas.Children.Add(star);

            var sb = new Storyboard();
            AddDouble(sb, star, "(Canvas.Left)", x - 6, x - 6 + Math.Cos(ang) * dist, 650, EaseOut);
            AddDouble(sb, star, "(Canvas.Top)", y - 6, y - 6 + Math.Sin(ang) * dist, 650, EaseOut);
            AddDouble(sb, star, "Opacity", 1, 0, 650);
            AddDouble(sb, star, "(UIElement.RenderTransform).(RotateTransform.Angle)", 0, 360, 650);
            sb.Completed += (_, _) => canvas.Children.Remove(star);
            sb.Begin();
        }

        // 飘字 +分
        var label = new TextBlock
        {
            Text = $"+{gained}",
            Foreground = _vm.AccentBrush,
            FontWeight = FontWeights.Bold,
            FontSize = 22
        };
        Canvas.SetLeft(label, x - 10); Canvas.SetTop(label, y - 14);
        canvas.Children.Add(label);
        var lblSb = new Storyboard();
        AddDouble(lblSb, label, "(Canvas.Top)", y - 14, y - 70, 800, EaseOut);
        AddDouble(lblSb, label, "Opacity", 1, 0, 800);
        lblSb.Completed += (_, _) => canvas.Children.Remove(label);
        lblSb.Begin();
    }

    /// <summary>连击数字脉冲放大</summary>
    private void PulseCombo()
    {
        var st = (ScaleTransform)ComboText.RenderTransform;
        var sb = new Storyboard();
        var kx = new DoubleAnimationUsingKeyFrames();
        kx.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        kx.KeyFrames.Add(new EasingDoubleKeyFrame(1.6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170)), EaseOut));
        kx.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420)), EaseOut));
        var ky = kx.Clone();
        Storyboard.SetTarget(kx, st); Storyboard.SetTargetProperty(kx, new PropertyPath("ScaleX"));
        Storyboard.SetTarget(ky, st); Storyboard.SetTargetProperty(ky, new PropertyPath("ScaleY"));
        sb.Children.Add(kx); sb.Children.Add(ky);
        sb.Begin();
    }

    /// <summary>漏掉目标：游戏区红色描边闪一下</summary>
    private void FlashDanger()
    {
        var flash = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50)),
            BorderThickness = new Thickness(4),
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(8),
            Width = FxCanvas.ActualWidth,
            Height = FxCanvas.ActualHeight
        };
        Canvas.SetLeft(flash, 0); Canvas.SetTop(flash, 0);
        FxCanvas.Children.Add(flash);
        var sb = new Storyboard();
        AddDouble(sb, flash, "Opacity", 0.6, 0, 380, EaseIn);
        sb.Completed += (_, _) => FxCanvas.Children.Remove(flash);
        sb.Begin();
    }

    /// <summary>敲错：输入框左右抖一下</summary>
    private void ShakeInput()
    {
        var tt = new TranslateTransform();
        InputBox.RenderTransform = tt;
        var sb = new Storyboard();
        var kf = new DoubleAnimationUsingKeyFrames();
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(-7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45))));
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
        Storyboard.SetTarget(kf, tt); Storyboard.SetTargetProperty(kf, new PropertyPath("X"));
        sb.Children.Add(kf);
        sb.Begin();
    }

    /// <summary>游戏结束：满屏彩屑飘落（星形 / 菱形 / 丝带混合）</summary>
    private void Confetti()
    {
        var canvas = FxCanvas;
        double w = canvas.ActualWidth, h = canvas.ActualHeight;
        for (int i = 0; i < 90; i++)
        {
            UIElement shape;
            int kind = _rand.Next(3);
            if (kind == 0)
                shape = new PackIcon { Kind = PackIconKind.Star, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform() };
            else if (kind == 1)
                shape = new PackIcon { Kind = PackIconKind.Diamond, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform() };
            else
                shape = new Rectangle { Width = 5 + _rand.Next(7), Height = 12 + _rand.Next(14), RadiusX = 2, RadiusY = 2, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform() };

            double size = 9 + _rand.Next(9);
            if (shape is FrameworkElement fe) { fe.Width = size; fe.Height = size; }
            if (shape is Shape sh) sh.Fill = RandomFxBrush();
            else if (shape is PackIcon pi) pi.Foreground = RandomFxBrush();

            double startX = _rand.NextDouble() * Math.Max(1, w);
            Canvas.SetLeft(shape, startX); Canvas.SetTop(shape, -20);
            canvas.Children.Add(shape);

            var sb = new Storyboard();
            double fall = h + 40;
            AddDouble(sb, shape, "(Canvas.Top)", -20, fall, 1200 + _rand.Next(900), EaseOut);
            if (shape is PackIcon || shape is Rectangle)
                AddDouble(sb, shape, "(UIElement.RenderTransform).(RotateTransform.Angle)", 0, 360 * (_rand.Next(2) == 0 ? 1 : -1), 1600);
            AddDouble(sb, shape, "Opacity", 1, 0, 1500 + _rand.Next(700));
            sb.Completed += (_, _) => canvas.Children.Remove(shape);
            sb.Begin();
        }
    }

    // ---- 拖尾：移动目标身后的渐隐光点 ----
    private void SpawnTrails()
    {
        if (_tickCount % 2 != 0) return;
        foreach (var t in _vm.Targets)
        {
            double cx = t.X + 115, cy = t.Y + 35; // 目标格子约 230 宽、中心略偏下
            if (t.Vx != 0 || t.Vy != 0)
                AddTrailDot(cx, cy, 7, _vm.AccentBrush, 0.85, 360);
            if (t.IsLocked) // 锁定的目标拖尾更亮更大
                AddTrailDot(cx, cy, 12, _vm.AccentBrush, 0.9, 480);
        }
    }

    private void AddTrailDot(double cx, double cy, double size, Brush brush, double fromOp, int ms)
    {
        var dot = new Ellipse { Width = size, Height = size, Fill = brush, Opacity = fromOp };
        Canvas.SetLeft(dot, cx - size / 2);
        Canvas.SetTop(dot, cy - size / 2);
        TrailCanvas.Children.Add(dot);
        var sb = new Storyboard();
        AddDouble(sb, dot, "Opacity", fromOp, 0, ms, EaseOut);
        AddDouble(sb, dot, "Width", size, size * 0.3, ms, EaseOut);
        AddDouble(sb, dot, "Height", size, size * 0.3, ms, EaseOut);
        sb.Completed += (_, _) => TrailCanvas.Children.Remove(dot);
        sb.Begin();
    }

    // ---- 连击火焰：从底部升腾的火苗，连击越高越旺 ----
    private void UpdateFlames()
    {
        if (_vm.Combo < 5) return;
        if (_tickCount % 3 != 0) return; // 约每 75ms 喷一次，避免过量
        int perEmit = Math.Min(4, 1 + _vm.Combo / 12);
        for (int i = 0; i < perEmit; i++) SpawnFlame();
    }

    private void SpawnFlame()
    {
        double w = FlameCanvas.ActualWidth, h = FlameCanvas.ActualHeight;
        double x = _rand.NextDouble() * Math.Max(1, w);
        double baseY = h - 6;
        double size = 16 + Math.Min(26, _vm.Combo / 2.0) + _rand.Next(10);
        var fire = new PackIcon
        {
            Kind = PackIconKind.Fire,
            Width = size, Height = size,
            Foreground = _rand.Next(2) == 0 ? (Brush)FindResource("FestiveAmber") : (Brush)FindResource("FestiveGreen"),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform()
        };
        Canvas.SetLeft(fire, x - size / 2);
        Canvas.SetTop(fire, baseY - size);
        FlameCanvas.Children.Add(fire);
        var sb = new Storyboard();
        AddDouble(sb, fire, "(Canvas.Top)", baseY - size, baseY - size - (40 + _rand.Next(60)), 520 + _rand.Next(320), EaseOut);
        AddDouble(sb, fire, "Opacity", 0.95, 0, 640 + _rand.Next(220));
        var s = (ScaleTransform)fire.RenderTransform;
        var kx = new DoubleAnimationUsingKeyFrames();
        kx.KeyFrames.Add(new EasingDoubleKeyFrame(0.8, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        kx.KeyFrames.Add(new EasingDoubleKeyFrame(1.25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
        kx.KeyFrames.Add(new EasingDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(430))));
        Storyboard.SetTarget(kx, s); Storyboard.SetTargetProperty(kx, new PropertyPath("ScaleX"));
        var ky = kx.Clone(); Storyboard.SetTarget(ky, s); Storyboard.SetTargetProperty(ky, new PropertyPath("ScaleY"));
        sb.Children.Add(kx); sb.Children.Add(ky);
        sb.Completed += (_, _) => FlameCanvas.Children.Remove(fire);
        sb.Begin();
    }

    // ---- 屏幕震动：漏怪 / 游戏结束时整体抖一下 ----
    private void ShakeScreen(int intensity)
    {
        if (_shakeBusy) return; // 防重叠
        _shakeBusy = true;
        double amp = intensity >= 2 ? 12 : 5;
        var tt = new TranslateTransform();
        PlayArea.RenderTransform = tt;
        var sb = new Storyboard();
        var kf = new DoubleAnimationUsingKeyFrames();
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        for (int i = 1; i <= 6; i++)
        {
            double s = (i % 2 == 0 ? 1 : -1) * amp * (1 - i / 7.0);
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(s, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 45))));
        }
        kf.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(7 * 45))));
        Storyboard.SetTarget(kf, tt); Storyboard.SetTargetProperty(kf, new PropertyPath("X"));
        sb.Children.Add(kf);
        sb.Completed += (_, _) => { tt.X = 0; _shakeBusy = false; };
        sb.Begin();
    }

    // ---- 庆祝闪光：连击里程碑瞬间点亮整个竞技场 ----
    private void Flash(byte r, byte g, byte b, int peakMs, double peakOp)
    {
        FlashOverlay.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
        FlashOverlay.Opacity = 0;
        var a = new DoubleAnimationUsingKeyFrames();
        a.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        a.KeyFrames.Add(new EasingDoubleKeyFrame(peakOp, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(peakMs / 2)), EaseOut));
        a.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(peakMs)), EaseOut));
        Storyboard.SetTarget(a, FlashOverlay); Storyboard.SetTargetProperty(a, new PropertyPath("Opacity"));
        var sb = new Storyboard(); sb.Children.Add(a); sb.Begin();
    }

    private void ClearContinuousFx()
    {
        TrailCanvas.Children.Clear();
        FlameCanvas.Children.Clear();
    }

    private static readonly QuadraticEase EaseOut = new() { EasingMode = EasingMode.EaseOut };
    private static readonly QuadraticEase EaseIn = new() { EasingMode = EasingMode.EaseIn };

    private static void AddDouble(Storyboard sb, DependencyObject target, string prop,
                                  double from, double to, double ms,
                                  IEasingFunction? ease = null, double delayMs = 0)
    {
        var a = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms))
        {
            EasingFunction = ease,
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        Storyboard.SetTarget(a, target);
        Storyboard.SetTargetProperty(a, new PropertyPath(prop));
        sb.Children.Add(a);
    }

    /// <summary>
    /// 对「变换对象」（ScaleTransform / RotateTransform / TranslateTransform）做动画。
    ///
    /// 为什么单独写一个：在 PropertyPath 里用 <c>TransformGroup.Children[0]</c>
    /// 这类带索引的路径在 WPF 中解析不稳定；直接把变换对象本身作为动画目标
    /// （它们是 Freezable，可被独立驱动）既简单又可靠，也能单独指定延迟。
    /// </summary>
    private static void AddTransformDouble(Storyboard sb, Animatable transform, string prop,
                                           double from, double to, double ms,
                                           double delayMs = 0, IEasingFunction? ease = null)
    {
        var a = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms))
        {
            EasingFunction = ease,
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        Storyboard.SetTarget(a, transform);
        Storyboard.SetTargetProperty(a, new PropertyPath(prop));
        sb.Children.Add(a);
    }
}
