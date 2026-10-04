using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.ViewModels;

/// <summary>
/// 课程页上的一个关卡条目：把关卡定义 + 进度状态合成界面可直接绑定的形状。
/// </summary>
public class LessonItem
{
    /// <summary>关卡定义</summary>
    public CourseLesson Lesson { get; init; } = null!;

    /// <summary>显示序号（从 1 开始）</summary>
    public int Index { get; init; }

    /// <summary>关卡标题</summary>
    public string Title => Lesson.Title;

    /// <summary>关卡说明</summary>
    public string Description => Lesson.Description;

    /// <summary>练习类型中文名</summary>
    public string TypeText { get; init; } = string.Empty;

    /// <summary>难度与等级描述</summary>
    public string LevelText { get; init; } = string.Empty;

    /// <summary>是否已解锁</summary>
    public bool IsUnlocked { get; init; }

    /// <summary>是否已通关</summary>
    public bool IsCleared { get; init; }

    /// <summary>最佳星级（0~5）</summary>
    public int Stars { get; init; }

    /// <summary>最佳正确率</summary>
    public double BestAccuracy { get; init; }

    /// <summary>状态文案：已通关 / 可挑战 / 未解锁</summary>
    public string StateText => IsCleared ? "已通关" : IsUnlocked ? "可挑战" : "未解锁";

    /// <summary>锁定说明（未解锁时提示需要先通过哪一关）</summary>
    public string LockHint { get; init; } = string.Empty;

    /// <summary>星级展示（实心星 + 空心星）</summary>
    public string StarsText => Stars <= 0
        ? "未评定"
        : new string('★', Stars) + new string('☆', 5 - Stars);

    /// <summary>是否正确率已达标但评级不足（用于给出更具体的提示）。</summary>
    public bool IsAccuracyEnough { get; init; }
}

/// <summary>
/// 课程中心页 ViewModel：展示五阶段关卡路线、锁定状态与闯关进度。
/// </summary>
public partial class CourseViewModel : ObservableObject
{
    #region 局部变量属性

    private readonly ICourseProgressStore _store;
    private readonly CourseSession _session;

    #endregion 局部变量属性

    #region 绑定属性

    /// <summary>全部关卡条目（按学习路线顺序）。</summary>
    [ObservableProperty]
    private ObservableCollection<LessonItem> _lessons = new();

    /// <summary>按阶段分组后的关卡（供分组列表渲染）。</summary>
    [ObservableProperty]
    private ObservableCollection<LessonGroup> _groups = new();

    /// <summary>状态提示。</summary>
    [ObservableProperty]
    private string _statusMessage = "从第一关开始，按顺序解锁";

    /// <summary>总体进度百分比。</summary>
    [ObservableProperty]
    private int _overallProgress;

    /// <summary>进度描述（已通关 / 总关卡）。</summary>
    [ObservableProperty]
    private string _progressText = string.Empty;

    /// <summary>通关标准说明。</summary>
    [ObservableProperty]
    private string _passRuleText = CourseLibrary.PassRuleText;

    /// <summary>总星数。</summary>
    [ObservableProperty]
    private int _totalStars;

    /// <summary>总星数上限。</summary>
    [ObservableProperty]
    private int _maxStars;

    /// <summary>段位文本。</summary>
    [ObservableProperty]
    private string _rankText = GradeScale.ToText(TypingRank.Novice);

    /// <summary>当前选中的关卡（用于查看详情与开始挑战）。</summary>
    [ObservableProperty]
    private LessonItem? _selectedLesson;

    /// <summary>选中关卡的详情文本。</summary>
    [ObservableProperty]
    private string _selectedDetail = "选择左侧任意关卡查看详情";

    /// <summary>是否可以开始挑战当前选中关卡。</summary>
    [ObservableProperty]
    private bool _canStart;

    #endregion 绑定属性

    #region 构造函数

    public CourseViewModel(ICourseProgressStore store, CourseSession session)
    {
        _store = store;
        _session = session;
        _session.LessonEvaluated += OnLessonEvaluated;
        Reload();
    }

    #endregion 构造函数

    #region 进度加载

    /// <summary>
    /// 重新读取进度并重建关卡列表。关卡解锁、星级、进度均在这里统一计算，
    /// 保证界面展示与存档一致。
    /// </summary>
    public void Reload()
    {
        CourseProgress progress = _store.Load();
        var all = CourseLibrary.GetAll();

        var items = new List<LessonItem>();
        for (int i = 0; i < all.Count; i++)
        {
            CourseLesson lesson = all[i];
            bool unlocked = CourseLibrary.IsUnlocked(lesson, progress);
            CourseLesson? prev = CourseLibrary.GetPrevious(lesson);

            items.Add(new LessonItem
            {
                Lesson = lesson,
                Index = i + 1,
                TypeText = PracticeTypeText(lesson.PracticeType),
                LevelText = $"{DifficultyText(lesson.Difficulty)} · {lesson.Level} 级",
                IsUnlocked = unlocked,
                IsCleared = progress.IsCleared(lesson.Id),
                Stars = progress.GetStars(lesson.Id),
                BestAccuracy = progress.BestAccuracy.TryGetValue(lesson.Id, out double a) ? a : 0d,
                LockHint = unlocked || prev == null ? string.Empty : $"需先通关「{prev.Title}」"
            });
        }

        Lessons = new ObservableCollection<LessonItem>(items);
        Groups = new ObservableCollection<LessonGroup>(
            items.GroupBy(i => i.Lesson.Stage).OrderBy(g => (int)g.Key).Select(g => new LessonGroup
            {
                Stage = g.Key,
                StageName = CourseLibrary.StageText(g.Key),
                StageDescription = CourseLibrary.StageDescription(g.Key),
                Items = new ObservableCollection<LessonItem>(g.ToList())
            }));

        int cleared = items.Count(i => i.IsCleared);
        OverallProgress = items.Count == 0 ? 0 : cleared * 100 / items.Count;
        ProgressText = $"已通关 {cleared} / {items.Count} 关";
        TotalStars = items.Sum(i => i.Stars);
        MaxStars = items.Count * 5;

        // 段位取历史成绩的评级分布推导，和成绩页保持一致口径
        RankText = GradeScale.ToText(TypingRank.Novice);

        StatusMessage = cleared == 0
            ? "从「基准键」开始，先把手指放到正确的位置"
            : cleared == items.Count
                ? "全部关卡已通关，去成绩统计看看自己的水平吧"
                : $"进度 {OverallProgress}%，继续保持";

        // 默认选中第一个未通关且已解锁的关卡，方便直接开始
        LessonItem? next = items.FirstOrDefault(i => i.IsUnlocked && !i.IsCleared)
                           ?? items.FirstOrDefault(i => i.IsUnlocked);
        SelectedLesson = next;
        UpdateDetail();
    }

    #endregion 进度加载

    #region 关卡交互

    /// <summary>选中关卡变化时刷新详情与按钮可用性。</summary>
    partial void OnSelectedLessonChanged(LessonItem? value) => UpdateDetail();

    /// <summary>刷新选中关卡详情。</summary>
    private void UpdateDetail()
    {
        LessonItem? item = SelectedLesson;
        if (item == null)
        {
            SelectedDetail = "选择左侧任意关卡查看详情";
            CanStart = false;
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"第 {item.Index} 关 · {item.Title}（{CourseLibrary.StageText(item.Lesson.Stage)}）");
        sb.AppendLine($"练习内容：{item.Description}");
        sb.AppendLine($"练习类型：{item.TypeText}　难度：{item.LevelText}");
        sb.AppendLine($"通关标准：{CourseLibrary.PassRuleText}");
        if (item.IsCleared)
        {
            sb.AppendLine($"当前成绩：{item.StarsText}　最佳正确率 {item.BestAccuracy:F1}%");
        }
        else if (!item.IsUnlocked)
        {
            sb.AppendLine($"尚未解锁：{item.LockHint}");
        }
        SelectedDetail = sb.ToString().TrimEnd();
        CanStart = item.IsUnlocked;
    }

    /// <summary>
    /// 开始挑战当前选中的关卡：把关卡交给会话对象，导航到打字练习页。
    /// </summary>
    [RelayCommand]
    private void StartLesson()
    {
        LessonItem? item = SelectedLesson;
        if (item == null || !item.IsUnlocked) return;

        _session.Begin(item.Lesson);
        StartRequested?.Invoke(this, item.Lesson);
        StatusMessage = $"开始挑战第 {item.Index} 关：{item.Title}";
    }

    /// <summary>开始挑战事件：由页面订阅后导航到打字练习页（VM 不直接引用页面）。</summary>
    public event EventHandler<CourseLesson>? StartRequested;

    /// <summary>重置全部闯关进度（重新开始闯关）。</summary>
    [RelayCommand]
    private void ResetProgress()
    {
        _store.Reset();
        _session.End();
        Reload();
        StatusMessage = "闯关进度已重置，从第一关重新开始";
    }

    /// <summary>
    /// 关卡结果产生后刷新列表：新解锁的关卡会立刻变为可挑战，
    /// 星级与进度条同步更新。
    /// </summary>
    private void OnLessonEvaluated(object? sender, LessonOutcome e)
    {
        Reload();
        StatusMessage = e.Passed
            ? $"第 {e.Lesson.StageOrder + 1} 关「{e.Lesson.Title}」通关！{GradeScale.ToText(e.Grade)} 级 · {e.Stars} 星"
            : $"第 {e.Lesson.StageOrder + 1} 关未达标：{GradeScale.ToText(e.Grade)} 级，正确率 {e.Accuracy:F1}%";
    }

    /// <summary>外部（页面重新进入时）触发的刷新。</summary>
    [RelayCommand]
    private void Refresh() => Reload();

    #endregion 关卡交互

    #region 文本工具

    /// <summary>练习类型中文名。</summary>
    private static string PracticeTypeText(PracticeType type) => type switch
    {
        PracticeType.English => "英文",
        PracticeType.Chinese => "中文",
        PracticeType.SpeedTest => "限时测速",
        PracticeType.Wubi => "五笔",
        PracticeType.EnglishWord => "英文单词",
        PracticeType.ChineseWord => "中文词组",
        _ => type.ToString()
    };

    /// <summary>难度中文名。</summary>
    private static string DifficultyText(Difficulty d) => d switch
    {
        Difficulty.Entry => "入门",
        Difficulty.Easy => "简单",
        Difficulty.Normal => "普通",
        Difficulty.Hard => "困难",
        Difficulty.Hell => "地狱",
        _ => d.ToString()
    };

    #endregion 文本工具
}

/// <summary>按阶段分组的关卡集合。</summary>
public class LessonGroup
{
    /// <summary>阶段枚举</summary>
    public CourseStage Stage { get; init; }

    /// <summary>阶段名</summary>
    public string StageName { get; init; } = string.Empty;

    /// <summary>阶段说明</summary>
    public string StageDescription { get; init; } = string.Empty;

    /// <summary>该阶段下的关卡</summary>
    public ObservableCollection<LessonItem> Items { get; init; } = new();
}