using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.App.Views.Windows;
using TypeMaster.Core;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

public partial class TypingPage : UserControl
{
    private readonly TypingViewModel _vm;
    private readonly IKeyboardHookService _hook;
    private readonly ITextToSpeechService _speech;
    private readonly Random _rand = new();

    private static readonly SolidColorBrush RightBrush = new(Colors.Green);
    private static readonly SolidColorBrush WrongBrush = new(Colors.Red);
    private static readonly SolidColorBrush PendingBrush = new(Colors.Gray);
    private static readonly SolidColorBrush CaretBrush = new(Color.FromRgb(0xFF, 0xA7, 0x26));
    private static readonly SolidColorBrush CharBrush = new(Color.FromRgb(0x1B, 0x27, 0x33));

    private static readonly string[] FxBrushKeys =
        { "PrimaryHueMidBrush", "SecondaryHueMidBrush", "FestiveGreen", "FestiveAmber", "FestiveBlue", "FestiveTeal" };

    // ---------------- 中文拼音标注（每字一格：上拼音、下汉字）----------------
    //
    // 性能设计：普通渲染每次击键都重建整个段落（每字一个 Run）。
    // 拼音格比 Run 重得多（每个是 InlineUIContainer + 3 个元素），
    // 若每次击键全量重建会造成输入卡顿。因此拆成两步：
    //   · 换新文本时重建格子（_rubyTarget 变化才重建）
    //   · 击键时只对已有格子改颜色/高亮（纯属性赋值，无对象创建、无布局重建）
    private string? _rubyTarget;

    /// <summary>一行拼音格缓存，与 TargetText 逐字符对应。</summary>
    private List<RubyCell>? _rubyCells;

    /// <summary>拼音行的字体（Segoe UI 对声调字母 ā ǎ ǖ 的覆盖最完整）。</summary>
    private static readonly FontFamily PinyinFont = new("Segoe UI");

    /// <summary>韵母的着色（与声母的橙色区分，让音节结构一眼可见）。</summary>
    private static readonly SolidColorBrush FinalBrush = new(Color.FromRgb(0x1E, 0x88, 0xE5));

    /// <summary>一个拼音格：容器 + 声母 Run + 韵母 Run + 汉字行。</summary>
    private sealed record RubyCell(InlineUIContainer Container, Run PinyinInitial, Run PinyinFinal, TextBlock Char);

    public TypingPage()
    {
        InitializeComponent();
        _vm = App.ServiceProvider.GetRequiredService<TypingViewModel>();
        DataContext = _vm;
        _hook = App.ServiceProvider.GetRequiredService<IKeyboardHookService>();
        _speech = App.ServiceProvider.GetRequiredService<ITextToSpeechService>();

        _vm.PropertyChanged += Vm_PropertyChanged;
        _vm.EffectRequested += OnEffect;
        // 五笔学习提示的朗读：由 VM 在切换到新字时发起，页面负责调用语音服务
        _vm.WubiSpeakRequested += OnWubiSpeakRequested;
        // 中文练习打对汉字的拼读：同样由 VM 发起、页面调用语音服务
        _vm.ChineseCharCompleted += OnChineseCharCompleted;
        AppState.SettingsChanged += (_, _) => ApplySettings();
        InputBox.TextChanged += (_, _) => _vm.SetTyped(InputBox.Text);

        Loaded += OnLoaded;
        // 全局钩子为应用级单例：页面只负责 Start（幂等），不在此 Stop，
        // 避免“新页面 Loaded 先触发 Start、旧页面 Unloaded 后触发 Stop”导致钩子被关闭。
        Unloaded += (_, _) => { };
    }

    /// <summary>
    /// 朗读五笔学习提示（如「明，编码 J E，第1码按J键，日…」）。
    ///
    /// 面向中老年学习者：编码提示信息密度高，这里固定用比常规慢 2 档的语速
    /// （在设置语速基础上叠加负偏移），否则快语速下听不清编码。
    /// 系统缺少中文语音或朗读开关关闭时静默跳过，不影响练习。
    /// </summary>
    private void OnWubiSpeakRequested(object? sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        try
        {
            _speech.Speak(text, WubiHintSpeechRateOffset);
        }
        catch
        {
            // 朗读失败不应影响打字练习
        }
    }

    /// <summary>五笔编码提示的相对语速偏移（负数更慢）。</summary>
    private const int WubiHintSpeechRateOffset = -2;

    /// <summary>中文打对时的朗读语速偏移（比常规慢 1 档，字音更清晰）。</summary>
    private const int ChineseCharSpeechRateOffset = -1;

    /// <summary>
    /// 朗读打对的汉字（如「泪」→ lèi 的标准读音）。
    ///
    /// 为什么读汉字而不是拼音串：TTS 读拉丁拼音（如 "lěi"）会被当成
    /// 英文音节念错；读汉字本身得到的就是该字的标准读音，即拼读结果。
    /// </summary>
    private void OnChineseCharCompleted(object? sender, char ch)
    {
        try
        {
            _speech.Speak(ch.ToString(), ChineseCharSpeechRateOffset);
        }
        catch
        {
            // 朗读失败不应影响打字
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        _hook.Start();
        InputBox.Focus();
        RenderDisplay();
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TypingViewModel.TargetText) or nameof(TypingViewModel.TypedText))
            RenderDisplay();
        else if (e.PropertyName == nameof(TypingViewModel.SelectedArticle))
            _vm.ApplySelectedArticle();
        else if (e.PropertyName == nameof(TypingViewModel.IsWubiMode))
            UpdateInputHint();
    }

    /// <summary>按练习模式更新输入框提示与输入法开关（五笔模式直接键入字母编码，须关闭中文输入法）。</summary>
    private void UpdateInputHint()
    {
        InputBox.SetValue(MaterialDesignThemes.Wpf.HintAssist.HintProperty,
            _vm.IsWubiMode ? "依次键入每个汉字的五笔编码（编码之间自动用空格分隔）" : "在此输入上方对照文本（支持中文输入法）");
        InputBox.SetValue(System.Windows.Input.InputMethod.IsInputMethodEnabledProperty, !_vm.IsWubiMode);
    }

    private void RefocusInput(object sender, RoutedEventArgs e) => InputBox.Focus();

    private void OpenImport(object sender, RoutedEventArgs e)
    {
        var dlg = new ImportArticleWindow { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            _vm.ImportArticle(dlg.ArticleTitle, dlg.ArticleContent);
            InputBox.Focus();
        }
    }

    private void DeleteArticle(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedArticle is { IsBuiltIn: false })
            _vm.DeleteSelectedArticle();
        InputBox.Focus();
    }

    // ---- 视觉特效 ----

    private void OnEffect(object? sender, TypingEffectEventArgs e)
    {
        if (e.Kind == TypingEffectKind.WrongKey) ShakeInput();
        else if (e.Kind == TypingEffectKind.Complete) Confetti();
    }

    private Brush RandomFxBrush() => (Brush)FindResource(FxBrushKeys[_rand.Next(FxBrushKeys.Length)]);

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

    /// <summary>完成：满屏彩屑飘落</summary>
    private void Confetti()
    {
        var canvas = FxCanvas;
        double w = canvas.ActualWidth, h = canvas.ActualHeight;
        for (int i = 0; i < 60; i++)
        {
            var shape = new PackIcon
            {
                Kind = _rand.Next(2) == 0 ? PackIconKind.Star : PackIconKind.Diamond,
                Width = 9 + _rand.Next(9),
                Height = 9 + _rand.Next(9),
                Foreground = RandomFxBrush(),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform()
            };
            double startX = _rand.NextDouble() * Math.Max(1, w);
            Canvas.SetLeft(shape, startX); Canvas.SetTop(shape, -20);
            canvas.Children.Add(shape);

            var sb = new Storyboard();
            AddDouble(sb, shape, "(Canvas.Top)", -20, h + 40, 1200 + _rand.Next(900), new QuadraticEase { EasingMode = EasingMode.EaseOut });
            AddDouble(sb, shape, "(UIElement.RenderTransform).(RotateTransform.Angle)", 0, 360 * (_rand.Next(2) == 0 ? 1 : -1), 1600);
            AddDouble(sb, shape, "Opacity", 1, 0, 1500 + _rand.Next(700));
            sb.Completed += (_, _) => canvas.Children.Remove(shape);
            sb.Begin();
        }
    }

    private static void AddDouble(Storyboard sb, DependencyObject target, string prop, double from, double to, double ms, IEasingFunction? ease = null)
    {
        var a = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };
        Storyboard.SetTarget(a, target);
        Storyboard.SetTargetProperty(a, new PropertyPath(prop));
        sb.Children.Add(a);
    }

    /// <summary>
    /// 根据 AppState 实时应用字体、字号与虚拟键盘可见性
    /// </summary>
    private void ApplySettings()
    {
        var cfg = AppState.Current;
        var font = new FontFamily(cfg.FontFamily);
        DisplayBox.FontFamily = font;
        DisplayBox.FontSize = cfg.FontSize;
        InputBox.FontFamily = font;
        InputBox.FontSize = cfg.FontSize;
        VkHost.Visibility = cfg.ShowVirtualKeyboard ? Visibility.Visible : Visibility.Collapsed;

        // 五笔学习提示的可见性：长辈模式下默认开启，也可在设置里单独控制
        _vm.ShowWubiKeyHint = cfg.ShowWubiKeyHint;
        _vm.ShowWubiBigChar = cfg.ShowWubiBigChar;

        // 长辈模式：练习区字号进一步放大，方便看清
        if (cfg.ElderMode)
        {
            DisplayBox.FontSize = Math.Max(cfg.FontSize, 24);
            InputBox.FontSize = Math.Max(cfg.FontSize, 22);
        }

        // 拼音格的列宽/字号都依赖这些设置：丢弃缓存，下一次渲染按新设置重建
        _rubyTarget = null;
        RenderDisplay();
    }

    /// <summary>
    /// 渲染彩色对照文本：已输入且正确=绿，错误=红，未输入=灰，当前字符=橙色高亮。
    /// 五笔模式下渲染为"汉字 + 编码"对照：汉字加粗展示，编码逐字符着色。
    /// </summary>
    private void RenderDisplay()
    {
        if (_vm.IsWubiMode)
        {
            DisplayBox.Document.Blocks.Clear();
            _rubyCells = null;
            RenderWubiDisplay();
            return;
        }

        var target = _vm.TargetText ?? string.Empty;
        var typed = _vm.TypedText ?? string.Empty;

        // 中文模式且开启拼音标注：每字一格（上拼音、下汉字）
        if (UseChineseRuby())
        {
            RenderChineseRuby(target, typed);
            return;
        }

        _rubyCells = null;
        DisplayBox.Document.Blocks.Clear();

        var para = new Paragraph { Margin = new Thickness(0), LineHeight = 1.7 };
        for (int i = 0; i < target.Length; i++)
        {
            var run = new Run(target[i].ToString());
            if (i < typed.Length)
                run.Foreground = target[i] == typed[i] ? RightBrush : WrongBrush;
            else
            {
                run.Foreground = PendingBrush;
                if (i == typed.Length) run.Background = CaretBrush;
            }
            para.Inlines.Add(run);
        }

        DisplayBox.Document.Blocks.Add(para);
        ScrollCaretIntoView(typed.Length);
    }

    /// <summary>当前模式是否启用拼音标注（仅中文 / 中文词组，且设置开启）。</summary>
    private bool UseChineseRuby()
        => AppState.Current.ShowPinyinAbove
           && !_vm.IsWubiMode
           && _vm.SelectedType is PracticeType.Chinese or PracticeType.ChineseWord;

    /// <summary>
    /// 中文练习的拼音标注渲染：每个汉字上方标注带声调拼音（如 lěi）。
    ///
    /// 结构：每字一个固定宽度的竖排格（拼音行 + 汉字行），横向排布、
    /// 自动换行——拼音列宽按自身长度自适应，汉字（等宽方块字）居中，
    /// 视觉上拼音正好落在对应汉字上方。
    ///
    /// 性能：只在文本变化时重建；击键仅做颜色更新（见 UpdateRubyColors）。
    /// </summary>
    /// <param name="target">对照文本</param>
    /// <param name="typed">已输入文本</param>
    private void RenderChineseRuby(string target, string typed)
    {
        bool rebuild = _rubyTarget != target || _rubyCells is null;

        if (rebuild)
        {
            DisplayBox.Document.Blocks.Clear();
            _rubyCells = new List<RubyCell>(target.Length);
            var para = new Paragraph { Margin = new Thickness(0) };

            // 汉字字号以 DisplayBox 实际生效值为准（长辈模式会被调大）
            double fs = DisplayBox.FontSize;
            double pyFs = Math.Clamp(fs * 0.55, 11, 16);
            double pinyinH = pyFs * 1.45;
            double cellH = pinyinH + fs * 1.3;

            for (int i = 0; i < target.Length; i++)
            {
                char ch = target[i];
                string py = PinyinCharMap.TryGetPinyin(ch, out string found) ? found : string.Empty;

                // 列宽：取「汉字宽」与「拼音宽」的较大者，避免相邻拼音互相重叠
                bool wide = ch >= 0x2E80;                       // CJK 与全角标点
                double charW = wide ? fs : fs * 0.6;
                double pyW = py.Length * pyFs * 0.66 + 4;
                double cellW = Math.Max(charW, py.Length > 0 ? pyW : 0);

                // 拼音行拆成「声母 + 韵母」两个 Run：当前字的声母/韵母分别着色，
                // 帮助学习者看清音节结构（如 l + ěi）
                var pinyinTb = new TextBlock
                {
                    FontSize = pyFs,
                    FontFamily = PinyinFont,
                    TextAlignment = TextAlignment.Center,
                    Height = pinyinH
                };
                var initialRun = new Run();
                var finalRun = new Run();
                pinyinTb.Inlines.Add(initialRun);
                pinyinTb.Inlines.Add(finalRun);
                if (py.Length > 0)
                {
                    var (pyInitial, pyFinal) = PinyinCharMap.SplitSyllable(py);
                    initialRun.Text = pyInitial;
                    finalRun.Text = pyFinal;
                }
                var charTb = new TextBlock
                {
                    Text = ch.ToString(),
                    FontSize = fs,
                    FontFamily = DisplayBox.FontFamily,
                    Foreground = PendingBrush,
                    TextAlignment = TextAlignment.Center
                };
                var cell = new StackPanel { Width = cellW, Height = cellH, Children = { pinyinTb, charTb } };

                var container = new InlineUIContainer { Child = cell };
                para.Inlines.Add(container);
                _rubyCells.Add(new RubyCell(container, initialRun, finalRun, charTb));
            }

            DisplayBox.Document.Blocks.Add(para);
            _rubyTarget = target;
        }

        UpdateRubyColors(target, typed);
        ScrollCaretIntoView(typed.Length);
    }

    /// <summary>
    /// 击键时的颜色更新：逐格设置前景/背景（含当前格的橙色光标底）。
    /// 纯属性赋值、无对象创建，长文本下也不卡顿。
    /// </summary>
    /// <param name="target">对照文本</param>
    /// <param name="typed">已输入文本</param>
    private void UpdateRubyColors(string target, string typed)
    {
        if (_rubyCells is null)
        {
            return;
        }

        int n = Math.Min(_rubyCells.Count, target.Length);
        for (int i = 0; i < n; i++)
        {
            var cell = _rubyCells[i];
            if (i < typed.Length)
            {
                // 已输入：整体绿 / 红（不拆声韵母，避免满屏花）
                bool ok = target[i] == typed[i];
                cell.Char.Foreground = ok ? RightBrush : WrongBrush;
                cell.Char.Background = Brushes.Transparent;
                cell.PinyinInitial.Foreground = ok ? RightBrush : WrongBrush;
                cell.PinyinFinal.Foreground = ok ? RightBrush : WrongBrush;
            }
            else
            {
                cell.Char.Foreground = PendingBrush;
                // 当前待输入格：橙色底作为光标指示
                cell.Char.Background = i == typed.Length ? CaretBrush : Brushes.Transparent;

                if (i == typed.Length)
                {
                    // 当前字：声母橙 / 韵母蓝——把音节结构摆到眼前
                    cell.PinyinInitial.Foreground = CaretBrush;
                    cell.PinyinFinal.Foreground = FinalBrush;
                }
                else
                {
                    cell.PinyinInitial.Foreground = PendingBrush;
                    cell.PinyinFinal.Foreground = PendingBrush;
                }
            }
        }
    }

    /// <summary>
    /// 让当前输入位置（橙色高亮字符）始终可见。
    ///
    /// 实现方式：找到该字符所在的 Run，取其相对 FlowDocument 的位置，
    /// 与 RichTextBox 的可视区间比较，必要时滚动到"当前行略靠上"的位置，
    /// 这样用户既能看到已打的内容，也能看到接下来几个字符。
    /// </summary>
    /// <param name="caretIndex">当前待输入字符的下标（等于已输入长度）</param>
    private void ScrollCaretIntoView(int caretIndex)
    {
        try
        {
            var doc = DisplayBox.Document;
            if (doc.Blocks.FirstBlock is not Paragraph para || para.Inlines.Count == 0)
            {
                return;
            }

            // 越界保护：续接文本后下标可能暂时超出。
            // 兼容两种 inline：普通 Run 与拼音标注的 InlineUIContainer。
            int idx = Math.Clamp(caretIndex, 0, para.Inlines.Count - 1);
            var caretInline = para.Inlines.ElementAtOrDefault(idx);
            if (caretInline is null)
            {
                return;
            }

            Rect rect = caretInline.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            if (rect.IsEmpty)
            {
                return;
            }

            // RichTextBox 没有 Viewport 属性（那是 ScrollViewer 的），
            // 因此用「文档内坐标 - 当前滚动偏移」换算成可视区间来判断。
            double offset = DisplayBox.VerticalOffset;
            double viewH = DisplayBox.ActualHeight;
            if (viewH <= 0)
            {
                return;
            }

            // 当前字符相对视口的位置
            double topInView = rect.Top - offset;
            double bottomInView = rect.Bottom - offset;

            // 跑到视口上方了：把当前行滚到视口上部 1/4 处，保留上文
            if (topInView < viewH * 0.25)
            {
                DisplayBox.ScrollToVerticalOffset(Math.Max(0, rect.Top - viewH * 0.25));
            }
            // 跑到视口底部之外：把当前行滚到视口下部 3/4 处
            else if (bottomInView > viewH * 0.9)
            {
                DisplayBox.ScrollToVerticalOffset(rect.Bottom - viewH * 0.75);
            }
        }
        catch
        {
            // 滚动只是体验优化，任何异常都不应影响输入
        }
    }

    /// <summary>
    /// 五笔对照渲染：每个单元显示"汉字 + 编码"，编码部分按全局输入进度逐字符着色。
    /// </summary>
    private void RenderWubiDisplay()
    {
        var units = _vm.WubiUnits ?? new List<WubiUnit>();
        var typed = _vm.TypedText ?? string.Empty;
        var expected = _vm.WubiExpectedCodes ?? string.Empty;

        var para = new Paragraph { Margin = new Thickness(0), LineHeight = 2.0 };
        int codeIndex = 0; // 当前字符在期望编码串中的全局下标
        foreach (var unit in units)
        {
            // 汉字本体：加粗深色，不参与输入比对
            var charRun = new Run(unit.Char + " ") { FontWeight = FontWeights.Bold, Foreground = CharBrush };
            para.Inlines.Add(charRun);

            // 编码 + 分隔空格：与期望编码串逐字符比对着色
            string piece = unit.Code + " ";
            foreach (char c in piece)
            {
                if (codeIndex >= expected.Length) break;
                var run = new Run(c.ToString());
                if (codeIndex < typed.Length)
                    run.Foreground = expected[codeIndex] == typed[codeIndex] ? RightBrush : WrongBrush;
                else
                {
                    run.Foreground = PendingBrush;
                    if (codeIndex == typed.Length) run.Background = CaretBrush;
                }
                para.Inlines.Add(run);
                codeIndex++;
            }
        }

        DisplayBox.Document.Blocks.Add(para);
        ScrollCaretIntoView(typed.Length);
    }
}
