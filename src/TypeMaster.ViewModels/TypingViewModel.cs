using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.ViewModels;

/// <summary>打字练习页需要播放的视觉特效类型。</summary>
public enum TypingEffectKind
{
    WrongKey,  // 敲错：输入框抖动
    Complete   // 完成：满屏彩屑
}

/// <summary>打字练习页特效请求事件参数。</summary>
public class TypingEffectEventArgs : EventArgs
{
    public TypingEffectKind Kind { get; init; }
}

/// <summary>练习文章选择项：内置小学题库（随机 / 指定篇目）or 用户自定义导入。</summary>
public class ArticleItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsBuiltIn { get; init; }
    public bool IsRandomBuiltIn { get; init; }
    public string? Content { get; init; }
    public override string ToString() => Name;
}

/// <summary>限时测速的时长选项。</summary>
public class DurationOption
{
    public int Minutes { get; init; }
    public string Name { get; init; } = string.Empty;
    public override string ToString() => Name;
}

public partial class TypingViewModel : ObservableObject
{
    #region 局部变量属性

    private readonly ITypingService _typing;
    private readonly ISoundService _sound;
    private readonly IUnitOfWork _uow;
    private readonly FunTipService _fun;
    private readonly IArticleLibrary _library;
    private readonly CourseSession _course;
    private readonly DispatcherTimer _timer;
    private DateTime _startTime;
    private int _prevLen;
    private bool _committed;

    /// <summary>本次练习已自动续接的段数（用于界面提示，不参与成绩计算）。</summary>
    private int _autoExtendCount;
    private DateTime _lastErrTip = DateTime.MinValue;

    /// <summary>最近一次评测结果，提交时用于取逐键统计。</summary>
    private TypingResult? _lastResult;

    /// <summary>限时测速：本轮的对照文本，超时交卷时用于计算成绩。</summary>
    private string _speedTarget = string.Empty;

    #endregion 局部变量属性

    #region 绑定属性

    [ObservableProperty]
    private PracticeType _selectedType = PracticeType.Chinese;

    /// <summary>是否处于五笔练习模式：界面据此切换对照区渲染与输入提示。</summary>
    [ObservableProperty]
    private bool _isWubiMode;

    /// <summary>五笔练习：期望的编码串（"编码 空格 编码…"，与 WubiUnits 一一对应）。</summary>
    [ObservableProperty]
    private string _wubiExpectedCodes = string.Empty;

    /// <summary>五笔练习：当前练习单元（汉字 + 编码）列表，供对照区渲染。</summary>
    [ObservableProperty]
    private List<WubiUnit> _wubiUnits = new();

    [ObservableProperty]
    private Difficulty _selectedDifficulty = Difficulty.Easy;

    /// <summary>难度细分等级（1~10），与 SelectedDifficulty 三档双向联动。</summary>
    [ObservableProperty]
    private int _difficultyLevel = DifficultyScale.Min;

    /// <summary>难度细分等级的中文描述，滑动条旁展示。</summary>
    [ObservableProperty]
    private string _difficultyLevelText = DifficultyScale.ToText(DifficultyScale.Min);

    /// <summary>界面层订阅以播放抖动 / 彩屑等特效。</summary>
    public event EventHandler<TypingEffectEventArgs>? EffectRequested;

    [ObservableProperty]
    private string _targetText = string.Empty;

    [ObservableProperty]
    private string _typedText = string.Empty;

    [ObservableProperty]
    private int _rightCount;

    [ObservableProperty]
    private int _wrongCount;

    [ObservableProperty]
    private string _accuracyText = "100%";

    [ObservableProperty]
    private string _speedText = "0";

    [ObservableProperty]
    private string _timeText = "00:00";

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private string _statusMessage = "选择类型与难度后开始练习";

    [ObservableProperty]
    private ObservableCollection<ArticleItem> _articles = new();

    [ObservableProperty]
    private ArticleItem? _selectedArticle;

    [ObservableProperty]
    private bool _canDeleteSelected;

    #region 限时测速

    /// <summary>限时测速是否处于生效状态（界面据此显示倒计时）。</summary>
    public bool IsSpeedTestMode => SelectedType == PracticeType.SpeedTest;

    /// <summary>可选的测速时长。</summary>
    public IReadOnlyList<DurationOption> DurationOptions { get; } = new List<DurationOption>
    {
        new() { Minutes = 1, Name = "1 分钟" },
        new() { Minutes = 3, Name = "3 分钟" },
        new() { Minutes = 5, Name = "5 分钟" }
    };

    /// <summary>当前选择的测速时长（分钟）。</summary>
    [ObservableProperty]
    private int _selectedDurationMinutes = 1;

    /// <summary>剩余时间（mm:ss 文本），仅在限时测速下刷新。</summary>
    [ObservableProperty]
    private string _remainingText = "01:00";

    /// <summary>剩余秒数（供进度条使用）。</summary>
    [ObservableProperty]
    private int _remainingSeconds = 60;

    #endregion 限时测速

    #region 评级

    /// <summary>本次评级的字母（S/A/B/C/D）。</summary>
    [ObservableProperty]
    private string _gradeText = "-";

    /// <summary>本次评级的中文说明。</summary>
    [ObservableProperty]
    private string _gradeChineseText = "尚未评定";

    /// <summary>本次评级的星级（1~5）。</summary>
    [ObservableProperty]
    private int _gradeStars;

    /// <summary>当前类型与等级的基准速度（B 级门槛），用于告诉用户「打到多少才算好」。</summary>
    [ObservableProperty]
    private string _baselineSpeedText = string.Empty;

    #endregion 评级

    #region 课程关卡

    /// <summary>是否处于课程关卡模式。</summary>
    public bool IsLessonMode => _course.CurrentLesson != null;

    /// <summary>当前关卡的标题（自由练习时为空）。</summary>
    [ObservableProperty]
    private string _lessonTitle = string.Empty;

    /// <summary>当前关卡的通关标准说明。</summary>
    [ObservableProperty]
    private string _lessonRuleText = string.Empty;

    /// <summary>关卡结果提示（通过 / 未通过）。</summary>
    [ObservableProperty]
    private string _lessonResultText = string.Empty;

    #endregion 课程关卡

    #endregion 绑定属性

    #region 构造函数

    public TypingViewModel(ITypingService typing, ISoundService sound, IUnitOfWork uow,
        FunTipService fun, IArticleLibrary library, CourseSession course)
    {
        _typing = typing;
        _sound = sound;
        _uow = uow;
        _fun = fun;
        _library = library;
        _course = course;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => OnTimerTick();
        RefreshArticles();
        SelectedArticle = Articles.First(a => a.IsBuiltIn);
        ApplySelectedArticle();
    }

    #endregion 构造函数

    #region 难度调节

    /// <summary>
    /// 难度细分等级变化时：同步三档难度、刷新等级文案与基准速度，并按新等级重取对照文本。
    /// </summary>
    /// <param name="value">新的细分等级</param>
    partial void OnDifficultyLevelChanged(int value)
    {
        // 越界值归一后再使用，避免影响题库索引
        int level = DifficultyScale.Clamp(value);
        DifficultyLevelText = DifficultyScale.ToText(level);
        SelectedDifficulty = DifficultyScale.ToDifficulty(level);
        RefreshBaselineSpeed();
        Debug.WriteLine($"[TypingViewModel] 难度调整为 {DifficultyLevelText}");
        if (!IsLessonMode)
        {
            RefreshPracticeText();
        }
    }

    /// <summary>刷新「基准速度」提示文案。</summary>
    private void RefreshBaselineSpeed()
    {
        double baseline = GradeScale.GetBaselineSpeed(SelectedType, DifficultyLevel);
        string unit = SelectedType is PracticeType.Chinese or PracticeType.ChineseWord
            ? "字/分"
            : SelectedType == PracticeType.Wubi ? "字/分" : "WPM";
        BaselineSpeedText = $"B 级基准 {baseline:F0} {unit}（{GradeScale.ToText(Grade.B)} = 达标线）";
    }

    [RelayCommand]
    private void SelectType(PracticeType type)
    {
        SelectedType = type;
        IsWubiMode = type == PracticeType.Wubi;
        RefreshBaselineSpeed();
        OnPropertyChanged(nameof(IsSpeedTestMode));

        // 限时测速：抽取足够长的文本并按所选时长启动倒计时
        if (type == PracticeType.SpeedTest)
        {
            ResetCore();
            LoadSpeedTestText();
            StatusMessage = $"限时测速已就绪：限时 {SelectedDurationMinutes} 分钟，点进输入框即开始计时";
            return;
        }

        if (IsLessonMode)
        {
            LoadLessonText();
            return;
        }

        SelectedArticle = Articles.First(a => a.IsBuiltIn);
        ApplySelectedArticle();
    }

    /// <summary>
    /// 选择三档难度：把滑动条同步到该档的代表等级，后续逻辑由细分等级统一驱动。
    /// </summary>
    /// <param name="difficulty">目标难度档位</param>
    [RelayCommand]
    private void SelectDifficulty(Difficulty difficulty)
    {
        DifficultyLevel = DifficultyScale.FromDifficulty(difficulty);
        if (IsLessonMode)
        {
            LoadLessonText();
            return;
        }
        SelectedArticle = Articles.First(a => a.IsBuiltIn);
        ApplySelectedArticle();
    }

    /// <summary>
    /// 仅在"随机练习"模式下按当前类型 / 难度 / 细分等级重取对照文本，
    /// 指定篇目时不改变文本，避免打断正在进行的练习。
    /// </summary>
    private void RefreshPracticeText()
    {
        if (SelectedArticle is { IsBuiltIn: true, IsRandomBuiltIn: true })
        {
            if (SelectedType == PracticeType.Wubi)
            {
                // 五笔随机练习：按细分等级从字库取字并生成编码对照
                ApplyWubiSession(GetRandomPracticeText(SelectedType, SelectedDifficulty, DifficultyLevel));
            }
            else
            {
                TargetText = GetRandomPracticeText(SelectedType, SelectedDifficulty, DifficultyLevel);
            }
            StatusMessage = $"难度 {DifficultyLevelText}，对照文本已就绪，开始输入吧";
            ResetCore();
        }
    }

    #endregion 难度调节

    #region 限时测速

    /// <summary>
    /// 抽取限时测速用的对照文本。
    /// 文本按「时长 × 较高手速」估算长度，确保时间到之前不会把文本打完；
    /// 若真的提前打完，则自动追加更多文本继续（见 <see cref="EnsureSpeedTextLongEnough"/>）。
    /// </summary>
    private void LoadSpeedTestText()
    {
        int target = SelectedDurationMinutes * 420;   // 约 420 字符/分钟的高手速余量
        var sb = new StringBuilder();
        int guard = 0;
        while (sb.Length < target && guard++ < 40)
        {
            sb.Append(TextLibrary.GetRandomText(PracticeType.SpeedTest, SelectedDifficulty, DifficultyScale.Max));
            sb.Append(' ');
        }
        _speedTarget = sb.ToString().TrimEnd();
        TargetText = _speedTarget;
        RemainingSeconds = SelectedDurationMinutes * 60;
        RemainingText = FormatClock(RemainingSeconds);
    }

    /// <summary>测速文本快打完时补充文本，避免用户被文本长度而非时间限制。</summary>
    private void EnsureSpeedTextLongEnough()
    {
        if (SelectedType != PracticeType.SpeedTest) return;
        if (TypedText.Length < TargetText.Length - 200) return;

        _speedTarget = TargetText + " " + TextLibrary.GetRandomText(PracticeType.SpeedTest, SelectedDifficulty, DifficultyScale.Max);
        TargetText = _speedTarget;
    }

    /// <summary>
    /// 通用自动续接：任意练习类型打到接近末尾时，自动追加一段**新的**随机内容。
    ///
    /// 为什么需要：只要对照文本长度有限，用户打到末尾就会停住——
    /// 这正是"打完第一行不继续"的根本原因。仅靠加长文本只是把这个时刻往后推，
    /// 并没有消除它。所以这里补上续接机制，让练习可以一直进行下去，
    /// 用户想停时按「提交成绩」即可。
    ///
    /// 与测速模式的区别：测速受时间限制、必须一直有文本；本方法适用于全部类型，
    /// 且追加的是**新内容**（不是重复旧内容），并记录续接次数供界面提示。
    /// </summary>
    private void AutoExtendIfNeeded()
    {
        // 测速模式有自己的续接逻辑（与时长绑定），不重复处理
        if (SelectedType == PracticeType.SpeedTest) return;
        // 未开始 / 已交卷时不续接
        if (_committed) return;
        if (TargetText.Length == 0) return;
        // 关卡模式下每关文本由课程规定，不自动续接，保证考核公平
        if (IsLessonMode) return;
        // 指定了具体文章（非随机练习）时也不续接，尊重用户选择的那一篇
        if (SelectedArticle is { IsBuiltIn: true, IsRandomBuiltIn: false }) return;
        // 五笔模式文本来自字库生成，同样不续接
        if (IsWubiMode) return;

        // 剩余不足阈值才续接，避免频繁触发
        const int threshold = 160;
        if (TypedText.Length < TargetText.Length - threshold) return;

        string addition = GetRandomPracticeText(SelectedType, SelectedDifficulty, DifficultyLevel);
        if (string.IsNullOrWhiteSpace(addition)) return;

        // 中文文章之间用换行分隔，英文/单词之间用空格，读起来更自然
        bool isChinese = SelectedType is PracticeType.Chinese;
        string sep = isChinese ? "\n" : " ";

        TargetText = TargetText.TrimEnd() + sep + addition.TrimStart();
        _autoExtendCount++;
        StatusMessage = $"已自动续接第 {_autoExtendCount} 段新内容，可继续练习，随时可提交成绩";
    }

    /// <summary>时长变化（切换到其他分钟数）时重新准备测速文本。</summary>
    partial void OnSelectedDurationMinutesChanged(int value)
    {
        if (SelectedType != PracticeType.SpeedTest) return;
        ResetCore();
        LoadSpeedTestText();
        StatusMessage = $"限时测速已就绪：限时 {value} 分钟";
    }

    /// <summary>mm:ss 格式化。</summary>
    private static string FormatClock(int seconds)
    {
        if (seconds < 0) seconds = 0;
        return $"{seconds / 60:D2}:{seconds % 60:D2}";
    }

    #endregion 限时测速

    #region 特效

    private void RaiseEffect(TypingEffectKind kind)
        => EffectRequested?.Invoke(this, new TypingEffectEventArgs { Kind = kind });

    #endregion 特效

    #region 练习文本

    /// <summary>应用当前选中的文章：内置小学题库（随机/指定篇目）或用户自定义导入。</summary>
    public void ApplySelectedArticle()
    {
        var sel = SelectedArticle;
        if (sel == null) return;

        #region 五笔模式：随机 = 字库取字；指定篇目 / 自定义文章 = 逐字转换为编码单元

        if (SelectedType == PracticeType.Wubi)
        {
            bool isRandom = sel.IsBuiltIn && sel.IsRandomBuiltIn;
            string text = isRandom
                ? GetRandomPracticeText(PracticeType.Wubi, SelectedDifficulty, DifficultyLevel)
                : sel.Content ?? string.Empty;
            ApplyWubiSession(text);
            StatusMessage = isRandom
                ? $"难度 {DifficultyLevelText}，五笔对照已就绪，按编码键入吧"
                : $"已载入五笔练习：{sel.Name}（字库外字符已自动跳过）";
            CanDeleteSelected = sel is { IsBuiltIn: false };
            ResetCore();
            return;
        }

        #endregion 五笔模式：随机 = 字库取字；指定篇目 / 自定义文章 = 逐字转换为编码单元

        if (sel.IsBuiltIn)
        {
            if (sel.IsRandomBuiltIn)
            {
                // 随机练习：按当前练习类型取文（中文=小学题库 / 英文 / 限时测速）
                TargetText = GetRandomPracticeText(SelectedType, SelectedDifficulty, DifficultyLevel);
                StatusMessage = $"难度 {DifficultyLevelText}，对照文本已就绪，开始输入吧";
            }
            else
            {
                // 指定某篇小学文章：按正文语言判定评测类型（中文/英文）
                bool isCn = (sel.Content ?? string.Empty).Any(IsCjk);
                SelectedType = isCn ? PracticeType.Chinese : PracticeType.English;
                IsWubiMode = false;
                TargetText = sel.Content ?? string.Empty;
                StatusMessage = $"已载入文章：{sel.Name}";
            }
        }
        else
        {
            bool isCn = (sel.Content ?? string.Empty).Any(IsCjk);
            SelectedType = isCn ? PracticeType.Chinese : PracticeType.English;
            IsWubiMode = false;
            TargetText = sel.Content ?? string.Empty;
            StatusMessage = $"已载入自定义文章：{sel.Name}";
        }

        CanDeleteSelected = sel is { IsBuiltIn: false };
        RefreshBaselineSpeed();
        // 指定文章会按正文语言推断类型，测速模式的界面（倒计时 / 时长选择）需要同步切换
        OnPropertyChanged(nameof(IsSpeedTestMode));
        ResetCore();
    }

    /// <summary>
    /// 构建五笔练习会话：把中文文本逐字转换为"汉字 + 编码"单元，
    /// 生成期望编码串（空格分隔），并把汉字串同步到 TargetText。
    /// </summary>
    /// <param name="text">练习用中文文本</param>
    private void ApplyWubiSession(string text)
    {
        var units = WubiLibrary.BuildUnits(text);
        WubiUnits = units;
        WubiExpectedCodes = string.Join(" ", units.Select(u => u.Code));
        // TargetText 同步保存汉字串，便于文章重打与展示兜底
        TargetText = string.Concat(units.Select(u => u.Char));
        Debug.WriteLine($"[TypingViewModel] 五笔会话构建完成：{units.Count} 个字，编码串长度 {WubiExpectedCodes.Length}");
    }

    /// <summary>载入当前关卡的对照文本，并强制使用关卡规定的类型与等级。</summary>
    private void LoadLessonText()
    {
        CourseLesson? lesson = _course.CurrentLesson;
        if (lesson == null) return;

        LessonTitle = lesson.Title;
        LessonRuleText = $"通关标准：{CourseLibrary.PassRuleText}";

        // 关卡规定的类型 / 难度 / 等级优先，避免用户误改设置后失去考核意义
        SelectedType = lesson.PracticeType;
        IsWubiMode = lesson.PracticeType == PracticeType.Wubi;
        SelectedDifficulty = lesson.Difficulty;
        DifficultyLevel = lesson.Level;
        OnPropertyChanged(nameof(IsSpeedTestMode));

        string text = CourseLibrary.GetText(lesson);
        if (IsWubiMode)
        {
            ApplyWubiSession(text);
        }
        else
        {
            WubiUnits = new List<WubiUnit>();
            WubiExpectedCodes = string.Empty;
            TargetText = text;
        }

        RefreshBaselineSpeed();
        ResetCore();
        LessonResultText = string.Empty;
        StatusMessage = $"第 {lesson.StageOrder + 1} 关 · {lesson.Title}：{lesson.Description}";
    }

    [RelayCommand]
    private void LoadText()
    {
        // 关卡模式下「换一篇」= 重新生成本关文本
        if (IsLessonMode)
        {
            LoadLessonText();
            return;
        }

        // 限时测速模式下「换一篇」= 重新抽测速文本
        if (SelectedType == PracticeType.SpeedTest)
        {
            ResetCore();
            LoadSpeedTestText();
            StatusMessage = $"限时测速已就绪：限时 {SelectedDurationMinutes} 分钟";
            return;
        }

        var sel = SelectedArticle;
        if (sel != null && !sel.IsBuiltIn)
        {
            ResetCore();
            StatusMessage = $"已重置自定义文章：{sel.Name}";
            return;
        }

        if (sel != null && sel.IsRandomBuiltIn)
        {
            if (SelectedType == PracticeType.Wubi)
            {
                ApplyWubiSession(GetRandomPracticeText(PracticeType.Wubi, SelectedDifficulty, DifficultyLevel));
                StatusMessage = $"难度 {DifficultyLevelText}，五笔对照已就绪，按编码键入吧";
            }
            else
            {
                TargetText = GetRandomPracticeText(SelectedType, SelectedDifficulty, DifficultyLevel);
                StatusMessage = $"难度 {DifficultyLevelText}，对照文本已就绪，开始输入吧";
            }
            ResetCore();
            return;
        }

        // 选了具体的某篇小学文章：重置重打同一篇
        ResetCore();
        StatusMessage = $"已重置：{sel?.Name}";
    }

    /// <summary>
    /// 随机练习按类型取文：中文走小学题库，英文/限时测速走对应词库，
    /// 单词 / 词组练习走 WordLibrary；细分等级决定文本长度。
    /// </summary>
    /// <param name="type">练习类型</param>
    /// <param name="difficulty">三档难度</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>按细分等级调整过长度的对照文本</returns>
    private static string GetRandomPracticeText(PracticeType type, Difficulty difficulty, int level)
    {
        return type switch
        {
            PracticeType.Chinese => TextLibrary.GetRandomBuiltIn(difficulty, level),
            PracticeType.Wubi => WubiLibrary.GetRandomPractice(difficulty, level),
            PracticeType.EnglishWord => WordLibrary.GetEnglishWordText(difficulty, level),
            PracticeType.ChineseWord => WordLibrary.GetChineseWordText(difficulty, level),
            PracticeType.SpeedTest => TextLibrary.GetRandomText(PracticeType.SpeedTest, difficulty, level),
            _ => TextLibrary.GetRandomText(PracticeType.English, difficulty, level)
        };
    }

    #endregion 练习文本

    #region 练习流程

    private void ResetCore()
    {
        TypedText = string.Empty;
        _prevLen = 0;
        _committed = false;
        _autoExtendCount = 0;
        _lastResult = null;
        IsRunning = false;
        IsCompleted = false;
        RightCount = 0;
        WrongCount = 0;
        AccuracyText = "100%";
        SpeedText = "0";
        TimeText = "00:00";
        Progress = 0;
        GradeText = "-";
        GradeChineseText = "尚未评定";
        GradeStars = 0;
        _timer.Stop();

        if (SelectedType == PracticeType.SpeedTest)
        {
            RemainingSeconds = SelectedDurationMinutes * 60;
            RemainingText = FormatClock(RemainingSeconds);
        }
    }

    [RelayCommand]
    private void Reset()
    {
        ResetCore();
        StatusMessage = "已重置，重新开始吧";
    }

    #endregion 练习流程

    #region 文章管理

    /// <summary>刷新可选文章列表：随机练习（小学题库） + 每一篇小学文章（分类 · 标题） + 用户自定义导入。</summary>
    public void RefreshArticles()
    {
        var list = new ObservableCollection<ArticleItem>
        {
            new() { Id = -1, Name = "随机练习（按所选类型）", IsBuiltIn = true, IsRandomBuiltIn = true, Content = null }
        };
        int idx = 0;
        foreach (var a in TextLibrary.GetBuiltInArticles())
        {
            list.Add(new ArticleItem
            {
                Id = 1000 + idx,
                Name = $"{a.Category} · {a.Title}",
                IsBuiltIn = true,
                IsRandomBuiltIn = false,
                Content = a.Body
            });
            idx++;
        }
        foreach (var a in _library.GetAll())
            list.Add(new ArticleItem { Id = a.Id, Name = a.Title, IsBuiltIn = false, IsRandomBuiltIn = false, Content = a.Content });
        Articles = list;
    }

    /// <summary>导入自定义文章并立即选中它。</summary>
    public void ImportArticle(string title, string content)
    {
        var added = _library.Add(title, content);
        RefreshArticles();
        var item = Articles.FirstOrDefault(a => a.Id == added.Id);
        if (item != null) SelectedArticle = item;
        ApplySelectedArticle();
        StatusMessage = $"已导入文章：{added.Title}";
    }

    /// <summary>删除当前选中的自定义文章，回退到内置随机。</summary>
    public void DeleteSelectedArticle()
    {
        var sel = SelectedArticle;
        if (sel == null || sel.IsBuiltIn) return;
        _library.Delete(sel.Id);
        RefreshArticles();
        SelectedArticle = Articles.First(a => a.IsBuiltIn);
        ApplySelectedArticle();
        StatusMessage = "已删除该自定义文章";
    }

    #endregion 文章管理

    #region 记录管理

    /// <summary>查询历史成绩（供课程页等外部页面读取），按时间倒序。</summary>
    /// <returns>全部成绩</returns>
    public async System.Threading.Tasks.Task<IReadOnlyList<TypingRecord>> LoadRecordsAsync()
        => await _uow.TypingRecords.GetAllAsync();

    #endregion 记录管理

    #region 打字评测

    /// <summary>当前练习的对照目标串（五笔为编码串，其余为文本）。</summary>
    private string CurrentTarget => SelectedType == PracticeType.Wubi ? WubiExpectedCodes : TargetText;

    /// <summary>
    /// 由页面在输入框文本变化时调用（文本框为输入源，VM 只计算与广播）。
    /// 五笔模式：对照文本为编码串，按编码单元（字）统计成绩。
    /// 限时测速：时间到会自动结束并交卷，之后不再接受输入。
    /// </summary>
    /// <param name="typed">输入框当前内容</param>
    public void SetTyped(string typed)
    {
        // 已完成（打完 / 时间到）后不再响应输入，避免改动已交卷的成绩
        if (_committed) return;

        if (string.IsNullOrEmpty(typed) && !IsRunning) return;

        if (!IsRunning && typed.Length > 0)
        {
            _startTime = DateTime.Now;
            IsRunning = true;
            _timer.Start();
        }

        int elapsed = (int)(DateTime.Now - _startTime).TotalSeconds;

        // 限时测速：文本快用完时自动补充，保证是「时间限制」而不是「文本长度限制」
        if (SelectedType == PracticeType.SpeedTest)
        {
            EnsureSpeedTextLongEnough();
        }
        else
        {
            // 其它类型：同样在接近末尾时续接新内容，避免打完就停住
            AutoExtendIfNeeded();
        }

        string target = CurrentTarget;
        TypingResult result = SelectedType == PracticeType.Wubi
            ? _typing.EvaluateWubi(WubiExpectedCodes, typed, elapsed)
            : _typing.Evaluate(TargetText, typed, elapsed, SelectedType);
        _lastResult = result;

        RightCount = result.RightCount;
        WrongCount = result.WrongCount;
        AccuracyText = result.Accuracy.ToString("F1") + "%";
        SpeedText = result.Speed.ToString("F1");
        Progress = target.Length == 0
            ? 0
            : Math.Min(100, typed.Length * 100 / target.Length);

        UpdateGradePreview(result);

        PlayFeedback(target, typed);

        if (result.IsCompleted && !_committed)
        {
            FinishSession(result, "已完成！可点击提交保存成绩");
        }
    }

    /// <summary>实时刷新评级预览，让用户边打边知道当前是什么水平。</summary>
    private void UpdateGradePreview(TypingResult result)
    {
        Grade grade = GradeScale.Evaluate(SelectedType, DifficultyLevel, result.Speed, result.Accuracy);
        GradeText = GradeScale.ToText(grade);
        GradeChineseText = GradeScale.ToChinese(grade);
        GradeStars = GradeScale.ToStars(grade);
    }

    /// <summary>结束本轮练习（打完或超时），统一处理音效、特效与提示。</summary>
    private void FinishSession(TypingResult result, string message)
    {
        _committed = true;
        IsCompleted = true;
        IsRunning = false;
        _timer.Stop();

        if (AppState.Current.EnableSound) _sound.PlayComplete();
        if (result.WrongCount == 0) _fun.ShowPerfect();
        else _fun.ShowPracticeComplete();

        StatusMessage = message;
        RaiseEffect(TypingEffectKind.Complete);

        // 关卡模式：立即结算并把结果回流给课程页
        if (IsLessonMode)
        {
            ReportLessonOutcome(result);
        }
    }

    /// <summary>把关卡结果写入进度并给出通过 / 未通过提示。</summary>
    private void ReportLessonOutcome(TypingResult result)
    {
        Grade grade = GradeScale.Evaluate(SelectedType, DifficultyLevel, result.Speed, result.Accuracy);
        LessonOutcome? outcome = _course.Complete(result.Accuracy, grade);
        if (outcome == null) return;

        LessonResultText = outcome.Passed
            ? $"通关！{GradeScale.ToText(outcome.Grade)} 级 · {outcome.Stars} 星，正确率 {outcome.Accuracy:F1}%"
            : $"未达通关标准：{GradeScale.ToText(outcome.Grade)} 级，正确率 {outcome.Accuracy:F1}%，需要 {CourseLibrary.PassRuleText}";
        StatusMessage = outcome.Passed ? "本关已通关，可返回课程中心挑战下一关" : "再练一次，达标即可解锁下一关";
    }

    /// <summary>
    /// 按最近一次新增的字符播放按键音 / 错误提示与抖动特效。
    /// </summary>
    /// <param name="expected">对照目标串（普通练习为文本，五笔为编码串）</param>
    /// <param name="typed">已输入内容</param>
    private void PlayFeedback(string expected, string typed)
    {
        if (typed.Length > _prevLen && typed.Length <= expected.Length)
        {
            int idx = typed.Length - 1;
            bool ok = expected[idx] == typed[idx];
            if (AppState.Current.EnableSound)
            {
                if (ok) _sound.PlayKey();
                else _sound.PlayError();
            }
            if (!ok)
            {
                if ((DateTime.Now - _lastErrTip).TotalSeconds >= 2.5)
                {
                    _lastErrTip = DateTime.Now;
                    _fun.ShowTypingError();
                }
                RaiseEffect(TypingEffectKind.WrongKey);
            }
        }
        _prevLen = typed.Length;
    }

    /// <summary>每 250ms 刷新耗时；限时测速下同时刷新倒计时并在时间到时自动交卷。</summary>
    private void OnTimerTick()
    {
        int elapsed = (int)(DateTime.Now - _startTime).TotalSeconds;
        TimeText = $"{elapsed / 60:D2}:{elapsed % 60:D2}";

        if (SelectedType != PracticeType.SpeedTest || _committed) return;

        int remain = SelectedDurationMinutes * 60 - elapsed;
        RemainingSeconds = Math.Max(0, remain);
        RemainingText = FormatClock(RemainingSeconds);

        if (remain <= 0)
        {
            TimeUp();
        }
    }

    /// <summary>
    /// 限时测速时间到：按已输入内容结算成绩，自动交卷入库存档。
    /// </summary>
    private async void TimeUp()
    {
        if (_committed) return;

        int elapsed = SelectedDurationMinutes * 60;
        TypingResult result = _typing.Evaluate(TargetText, TypedText, elapsed, SelectedType);
        _lastResult = result;

        RightCount = result.RightCount;
        WrongCount = result.WrongCount;
        AccuracyText = result.Accuracy.ToString("F1") + "%";
        SpeedText = result.Speed.ToString("F1");
        UpdateGradePreview(result);

        FinishSession(result, $"时间到！速度 {result.Speed:F1}，正确率 {result.Accuracy:F1}%，成绩已自动保存");
        await SaveRecordAsync(result);
    }

    #endregion 打字评测

    #region 成绩提交

    [RelayCommand]
    private async System.Threading.Tasks.Task SubmitAsync()
    {
        // 未打完且未超时：不允许提交，避免把半截成绩当成完整成绩存档
        if (!_committed && TypedText.Length < TargetText.Length) return;

        int elapsed = (int)(DateTime.Now - _startTime).TotalSeconds;
        TypingResult result = _lastResult ?? (SelectedType == PracticeType.Wubi
            ? _typing.EvaluateWubi(WubiExpectedCodes, TypedText, elapsed)
            : _typing.Evaluate(TargetText, TypedText, elapsed, SelectedType));

        await SaveRecordAsync(result);

        if (!IsLessonMode)
        {
            LoadText();
        }
    }

    /// <summary>把一次评测结果落库（含细分等级、评级与逐键统计）。</summary>
    private async System.Threading.Tasks.Task SaveRecordAsync(TypingResult result)
    {
        bool isWubi = SelectedType == PracticeType.Wubi;
        Grade grade = GradeScale.Evaluate(SelectedType, DifficultyLevel, result.Speed, result.Accuracy);

        var record = new TypingRecord
        {
            PracticeType = (int)SelectedType,
            Difficulty = (int)SelectedDifficulty,
            // 五笔按"字"统计总数，其余模式按字符数统计
            TotalCharCount = isWubi ? WubiUnits.Count : TargetText.Length,
            RightCharCount = result.RightCount,
            WrongCharCount = result.WrongCount,
            Speed = result.Speed,
            Accuracy = result.Accuracy,
            UseSecond = SelectedType == PracticeType.SpeedTest
                ? SelectedDurationMinutes * 60
                : Math.Max(1, (int)(DateTime.Now - _startTime).TotalSeconds),
            CreateTime = DateTime.Now,
            Level = DifficultyScale.Clamp(DifficultyLevel),
            Grade = (int)grade,
            KeyStatsJson = KeyStatsCodec.Serialize(result.KeyDeltas)
        };

        await _uow.TypingRecords.AddAsync(record);
        StatusMessage = $"成绩已保存：{GradeScale.ToText(grade)} 级 · 速度 {result.Speed:F1} / 正确率 {result.Accuracy:F1}%";
        Debug.WriteLine($"[TypingViewModel] 成绩已保存：等级 {DifficultyLevelText}，评级 {GradeScale.ToText(grade)}，速度 {result.Speed:F1}，正确率 {result.Accuracy:F1}%");
    }

    /// <summary>退出关卡模式，回到自由练习。</summary>
    [RelayCommand]
    private void ExitLesson()
    {
        _course.End();
        LessonTitle = string.Empty;
        LessonRuleText = string.Empty;
        LessonResultText = string.Empty;
        OnPropertyChanged(nameof(IsLessonMode));
        SelectedArticle = Articles.First(a => a.IsBuiltIn);
        ApplySelectedArticle();
        StatusMessage = "已退出关卡，回到自由练习";
    }

    /// <summary>进入关卡模式并载入该关文本（由课程页调用）。</summary>
    /// <param name="lesson">关卡</param>
    public void EnterLesson(CourseLesson lesson)
    {
        _course.Begin(lesson);
        OnPropertyChanged(nameof(IsLessonMode));
        LoadLessonText();
    }

    #endregion 成绩提交

    #region 私有工具

    private static bool IsCjk(char c) => c is >= (char)0x4E00 and <= (char)0x9FFF;

    #endregion 私有工具
}