using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.ViewModels;

/// <summary>趋势图上的一个数据点（一次练习）。</summary>
public class TrendPoint
{
    /// <summary>练习时间</summary>
    public DateTime Time { get; init; }

    /// <summary>速度</summary>
    public double Speed { get; init; }

    /// <summary>正确率</summary>
    public double Accuracy { get; init; }

    /// <summary>评级整数（0~4），用于着色</summary>
    public int Grade { get; init; }

    /// <summary>该点在序列中的序号（从 0 开始），用于横轴定位</summary>
    public int Index { get; init; }

    /// <summary>鼠标悬停提示</summary>
    public string Tooltip { get; init; } = string.Empty;
}

/// <summary>分类汇总的一行：某个练习类型的统计结果。</summary>
public class SummaryRow
{
    /// <summary>练习类型中文名</summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>练习类型枚举（用于筛选）</summary>
    public PracticeType Type { get; init; }

    /// <summary>练习次数</summary>
    public int Count { get; init; }

    /// <summary>平均速度</summary>
    public double AvgSpeed { get; init; }

    /// <summary>最佳速度</summary>
    public double BestSpeed { get; init; }

    /// <summary>平均正确率</summary>
    public double AvgAccuracy { get; init; }

    /// <summary>最佳评级字母</summary>
    public string BestGrade { get; init; } = "-";

    /// <summary>速度单位</summary>
    public string SpeedUnit { get; init; } = "WPM";
}

/// <summary>热力图上的一个键位。</summary>
public class HeatKey
{
    /// <summary>按键标签</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>显示字符（Space 等特殊键用文字）</summary>
    public string Display { get; init; } = string.Empty;

    /// <summary>错误率（百分比）</summary>
    public double ErrorRate { get; init; }

    /// <summary>总击键次数</summary>
    public int Total { get; init; }

    /// <summary>错误次数</summary>
    public int Wrong { get; init; }

    /// <summary>是否为基准键（ASDF JKL;），用于界面加标记</summary>
    public bool IsHomeKey { get; init; }

    /// <summary>热力等级 0~4：0 表示无数据，4 表示错误率最高。</summary>
    public int HeatLevel { get; init; }

    /// <summary>是否没有任何数据（界面显示为灰键）</summary>
    public bool HasNoData => Total == 0;

    /// <summary>悬停提示</summary>
    public string Tooltip => HasNoData
        ? $"{Display}：暂无数据"
        : $"{Display}：错误 {Wrong} / 共 {Total}，错误率 {ErrorRate:F1}%";
}

/// <summary>热力图的一行（对应键盘的一排）。</summary>
public class HeatRow
{
    /// <summary>该排的键位</summary>
    public ObservableCollection<HeatKey> Keys { get; init; } = new();
}

/// <summary>薄弱键位排名的一行。</summary>
public class WeakKeyRow
{
    /// <summary>排名</summary>
    public int Rank { get; init; }

    /// <summary>按键显示</summary>
    public string Display { get; init; } = string.Empty;

    /// <summary>错误次数</summary>
    public int Wrong { get; init; }

    /// <summary>总次数</summary>
    public int Total { get; init; }

    /// <summary>错误率</summary>
    public double ErrorRate { get; init; }

    /// <summary>建议文案</summary>
    public string Advice { get; init; } = string.Empty;
}

/// <summary>
/// 成绩统计页 ViewModel：趋势曲线、分类汇总、薄弱键位与热力图、CSV 导出。
/// 所有图表数据都在这里算好后交给页面绘制，页面只负责画（不引入第三方图表库）。
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    #region 局部变量属性

    private readonly IUnitOfWork _uow;

    /// <summary>最近一次加载的全部成绩（升序），筛选与统计都基于它计算。</summary>
    private List<TypingRecord> _all = new();

    /// <summary>QWERTY 键盘布局（热力图用），与虚拟键盘保持一致。</summary>
    private static readonly string[][] KeyboardLayout =
    {
        new[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
        new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" },
        new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'" },
        new[] { "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/" },
        new[] { "Space" }
    };

    private static readonly HashSet<string> HomeKeys = new() { "A", "S", "D", "F", "J", "K", "L", ";" };

    #endregion 局部变量属性

    #region 绑定属性

    /// <summary>筛选后的成绩明细（表格显示）。</summary>
    [ObservableProperty]
    private ObservableCollection<TypingRecord> _records = new();

    /// <summary>趋势曲线数据点。</summary>
    [ObservableProperty]
    private ObservableCollection<TrendPoint> _trend = new();

    /// <summary>分类汇总行。</summary>
    [ObservableProperty]
    private ObservableCollection<SummaryRow> _summary = new();

    /// <summary>热力图行。</summary>
    [ObservableProperty]
    private ObservableCollection<HeatRow> _heatRows = new();

    /// <summary>薄弱键位排名。</summary>
    [ObservableProperty]
    private ObservableCollection<WeakKeyRow> _weakKeys = new();

    /// <summary>类型筛选选项。null 表示全部。</summary>
    public IReadOnlyList<TypeFilterOption> TypeFilters { get; } = new List<TypeFilterOption>
    {
        new() { Type = null, Name = "全部类型" },
        new() { Type = PracticeType.English, Name = "英文" },
        new() { Type = PracticeType.Chinese, Name = "中文" },
        new() { Type = PracticeType.SpeedTest, Name = "限时测速" },
        new() { Type = PracticeType.Wubi, Name = "五笔" },
        new() { Type = PracticeType.EnglishWord, Name = "英文单词" },
        new() { Type = PracticeType.ChineseWord, Name = "中文词组" }
    };

    /// <summary>当前类型筛选。</summary>
    [ObservableProperty]
    private TypeFilterOption? _selectedTypeFilter;

    /// <summary>时间范围选项。</summary>
    public IReadOnlyList<RangeFilterOption> RangeFilters { get; } = new List<RangeFilterOption>
    {
        new() { Days = 7, Name = "近 7 天" },
        new() { Days = 30, Name = "近 30 天" },
        new() { Days = 90, Name = "近 90 天" },
        new() { Days = 0, Name = "全部时间" }
    };

    /// <summary>当前时间范围筛选。</summary>
    [ObservableProperty]
    private RangeFilterOption? _selectedRangeFilter;

    /// <summary>状态提示。</summary>
    [ObservableProperty]
    private string _statusMessage = "点击加载成绩开始分析";

    #region 概览卡片

    /// <summary>筛选后的练习次数。</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>平均速度。</summary>
    [ObservableProperty]
    private string _avgSpeedText = "-";

    /// <summary>最佳速度。</summary>
    [ObservableProperty]
    private string _bestSpeedText = "-";

    /// <summary>平均正确率。</summary>
    [ObservableProperty]
    private string _avgAccuracyText = "-";

    /// <summary>总练习时长（便于看投入）。</summary>
    [ObservableProperty]
    private string _totalTimeText = "-";

    /// <summary>段位文本。</summary>
    [ObservableProperty]
    private string _rankText = "新手";

    /// <summary>评级分布文案（S/A/B/C/D 各多少次）。</summary>
    [ObservableProperty]
    private string _gradeDistributionText = "-";

    /// <summary>最近一次练习的评级字母。</summary>
    [ObservableProperty]
    private string _latestGradeText = "-";

    #endregion 概览卡片

    #region 图表状态

    /// <summary>趋势图是否为空（无数据时页面显示占位提示）。</summary>
    [ObservableProperty]
    private bool _hasTrend;

    /// <summary>热力图是否为空。</summary>
    [ObservableProperty]
    private bool _hasHeatData;

    /// <summary>弱点分析结论（一句话）。</summary>
    [ObservableProperty]
    private string _weaknessSummary = "暂无键位数据，先做几次英文练习吧";

    #endregion 图表状态

    #endregion 绑定属性

    #region 构造函数

    public HistoryViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        SelectedTypeFilter = TypeFilters[0];
        SelectedRangeFilter = RangeFilters[3];   // 默认「全部时间」，首次进入就能看到全部历史
    }

    #endregion 构造函数

    #region 数据加载

    /// <summary>
    /// 加载成绩并按当前筛选条件重建所有统计结果。
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        var list = await _uow.TypingRecords.GetAllAsync();
        // 统一升序保存：趋势曲线与时间范围计算都以升序为前提
        _all = list.OrderBy(x => x.CreateTime).ToList();
        Rebuild();
    }

    /// <summary>清空记录（会同时清空所有统计）。</summary>
    [RelayCommand]
    private async Task ClearAsync()
    {
        await _uow.TypingRecords.ClearAsync();
        _all.Clear();
        Rebuild();
        StatusMessage = "已清空全部成绩";
    }

    /// <summary>筛选条件变化时立即重建统计，不需要再点一次加载。</summary>
    partial void OnSelectedTypeFilterChanged(TypeFilterOption? value) => Rebuild();

    partial void OnSelectedRangeFilterChanged(RangeFilterOption? value) => Rebuild();

    #endregion 数据加载

    #region 统计重建

    /// <summary>
    /// 按当前筛选条件重建明细、趋势、汇总、热力图与薄弱键位。
    /// 所有派生指标都在此统一刷新，避免各视图各自口径不一致。
    /// </summary>
    private void Rebuild()
    {
        List<TypingRecord> filtered = ApplyFilters(_all);

        Records = new ObservableCollection<TypingRecord>(filtered.OrderByDescending(r => r.CreateTime));
        Trend = new ObservableCollection<TrendPoint>(BuildTrend(filtered));
        Summary = new ObservableCollection<SummaryRow>(BuildSummary(filtered));
        BuildKeyAnalysis(filtered);

        HasTrend = Trend.Count > 1;
        TotalCount = filtered.Count;
        BuildOverview(filtered);

        StatusMessage = filtered.Count == 0
            ? "当前筛选条件下暂无成绩记录"
            : $"共 {filtered.Count} 条记录（全部 {_all.Count} 条）";
    }

    /// <summary>按类型与时间范围过滤。</summary>
    private List<TypingRecord> ApplyFilters(List<TypingRecord> source)
    {
        IEnumerable<TypingRecord> q = source;

        if (SelectedTypeFilter?.Type is { } t)
        {
            q = q.Where(r => r.PracticeType == (int)t);
        }

        int days = SelectedRangeFilter?.Days ?? 0;
        if (days > 0)
        {
            DateTime from = DateTime.Now.AddDays(-days);
            q = q.Where(r => r.CreateTime >= from);
        }

        return q.ToList();
    }

    /// <summary>概览卡片：次数、平均/最佳速度、平均正确率、总时长、段位、评级分布。</summary>
    private void BuildOverview(List<TypingRecord> filtered)
    {
        if (filtered.Count == 0)
        {
            AvgSpeedText = "-";
            BestSpeedText = "-";
            AvgAccuracyText = "-";
            TotalTimeText = "-";
            GradeDistributionText = "-";
            LatestGradeText = "-";
            RankText = GradeScale.ToText(TypingRank.Novice);
            return;
        }

        AvgSpeedText = filtered.Average(r => r.Speed).ToString("F1");
        BestSpeedText = filtered.Max(r => r.Speed).ToString("F1");
        AvgAccuracyText = filtered.Average(r => r.Accuracy).ToString("F1") + "%";

        int totalSec = filtered.Sum(r => r.UseSecond);
        TotalTimeText = totalSec >= 3600
            ? $"{totalSec / 3600} 小时 {totalSec % 3600 / 60} 分"
            : $"{totalSec / 60} 分 {totalSec % 60} 秒";

        var grades = filtered.Select(r => GradeScale.FromInt(r.Grade)).ToList();
        RankText = GradeScale.ToText(GradeScale.GetRank(grades));
        LatestGradeText = GradeScale.ToText(grades[^1]);

        var counts = new Dictionary<Grade, int> { [Grade.S] = 0, [Grade.A] = 0, [Grade.B] = 0, [Grade.C] = 0, [Grade.D] = 0 };
        foreach (var g in grades)
        {
            counts[g]++;
        }
        GradeDistributionText = string.Join("　", new[] { Grade.S, Grade.A, Grade.B, Grade.C, Grade.D }
            .Select(g => $"{GradeScale.ToText(g)}:{counts[g]}"));
    }

    /// <summary>
    /// 构造趋势数据：按时间顺序输出每次练习的速度与正确率。
    /// 数据点很多时按等间隔抽样，避免曲线糊成一团（同时保留最后一点）。
    /// </summary>
    private static List<TrendPoint> BuildTrend(List<TypingRecord> filtered)
    {
        var ordered = filtered.OrderBy(r => r.CreateTime).ToList();
        if (ordered.Count == 0) return new List<TrendPoint>();

        const int maxPoints = 120;
        List<TypingRecord> sampled;
        if (ordered.Count <= maxPoints)
        {
            sampled = ordered;
        }
        else
        {
            int step = (int)Math.Ceiling(ordered.Count / (double)maxPoints);
            sampled = ordered.Where((_, i) => i % step == 0).ToList();
            if (!ReferenceEquals(sampled[^1], ordered[^1]))
            {
                sampled.Add(ordered[^1]);
            }
        }

        var points = new List<TrendPoint>(sampled.Count);
        for (int i = 0; i < sampled.Count; i++)
        {
            TypingRecord r = sampled[i];
            Grade g = GradeScale.FromInt(r.Grade);
            points.Add(new TrendPoint
            {
                Time = r.CreateTime,
                Speed = r.Speed,
                Accuracy = r.Accuracy,
                Grade = r.Grade,
                Index = i,
                Tooltip = $"{r.CreateTime:MM-dd HH:mm}　速度 {r.Speed:F1}　正确率 {r.Accuracy:F1}%　{GradeScale.ToText(g)} 级"
            });
        }
        return points;
    }

    /// <summary>按练习类型分组汇总。</summary>
    private static List<SummaryRow> BuildSummary(List<TypingRecord> filtered)
    {
        return filtered
            .GroupBy(r => r.PracticeType)
            .Select(g =>
            {
                var type = (PracticeType)g.Key;
                bool isCjkType = type is PracticeType.Chinese or PracticeType.ChineseWord or PracticeType.Wubi;
                List<Grade> grades = g.Select(r => GradeScale.FromInt(r.Grade)).ToList();
                Grade best = grades.OrderByDescending(x => (int)x).First();
                return new SummaryRow
                {
                    Type = type,
                    TypeName = PracticeTypeText(type),
                    Count = g.Count(),
                    AvgSpeed = Math.Round(g.Average(r => r.Speed), 1),
                    BestSpeed = Math.Round(g.Max(r => r.Speed), 1),
                    AvgAccuracy = Math.Round(g.Average(r => r.Accuracy), 1),
                    BestGrade = GradeScale.ToText(best),
                    SpeedUnit = isCjkType ? "字/分" : "WPM"
                };
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    /// <summary>
    /// 键位分析：聚合逐键对错，生成热力图数据与薄弱键位排名。
    /// 中文类练习不采集键位，因此只统计有键位数据的记录。
    /// </summary>
    private void BuildKeyAnalysis(List<TypingRecord> filtered)
    {
        var stats = KeyStatsCodec.Aggregate(filtered);
        var dict = stats.ToDictionary(s => s.Key, s => s, StringComparer.Ordinal);

        // 热力图：按键盘布局铺满，没有数据的键也要显示（灰键），方便用户看出哪些键还没练到
        double maxError = stats.Count == 0 ? 0d : stats.Max(s => s.Wrong);
        var rows = new ObservableCollection<HeatRow>();
        foreach (string[] line in KeyboardLayout)
        {
            var row = new HeatRow();
            foreach (string key in line)
            {
                dict.TryGetValue(key, out KeyStatEntry? e);
                row.Keys.Add(new HeatKey
                {
                    Key = key,
                    Display = key == "Space" ? "空格" : key,
                    ErrorRate = e?.ErrorRate ?? 0d,
                    Total = e?.Total ?? 0,
                    Wrong = e?.Wrong ?? 0,
                    IsHomeKey = HomeKeys.Contains(key),
                    HeatLevel = HeatLevel(e?.Wrong ?? 0, maxError)
                });
            }
            rows.Add(row);
        }
        HeatRows = rows;

        int keysWithData = stats.Count(s => s.Total > 0);
        HasHeatData = keysWithData > 0;

        // 薄弱键位：只看有足够样本的键，避免「打错一次就是 100%」的噪声
        const int minSample = 5;
        var weak = stats
            .Where(s => s.Total >= minSample && s.Wrong > 0)
            .OrderByDescending(s => s.Wrong)
            .ThenByDescending(s => s.ErrorRate)
            .Take(8)
            .Select((s, i) => new WeakKeyRow
            {
                Rank = i + 1,
                Display = s.Key == "Space" ? "空格" : s.Key,
                Wrong = s.Wrong,
                Total = s.Total,
                ErrorRate = Math.Round(s.ErrorRate, 1),
                Advice = Advice(s)
            })
            .ToList();
        WeakKeys = new ObservableCollection<WeakKeyRow>(weak);

        WeaknessSummary = BuildWeaknessSummary(weak, stats, keysWithData);
    }

    /// <summary>把错误次数映射到 0~4 的热力等级。</summary>
    private static int HeatLevel(int wrong, double maxWrong)
    {
        if (wrong <= 0 || maxWrong <= 0) return 0;
        double ratio = wrong / maxWrong;
        if (ratio >= 0.75) return 4;
        if (ratio >= 0.5) return 3;
        if (ratio >= 0.25) return 2;
        return 1;
    }

    /// <summary>针对单个按键给出练习建议。</summary>
    private static string Advice(KeyStatEntry s)
    {
        string key = s.Key == "Space" ? "空格" : s.Key;
        if (HomeKeys.Contains(s.Key))
        {
            return $"{key} 属基准键，练「指法入门 · 基准键」并养成击键后归位习惯";
        }
        if (s.Key.Length == 1 && char.IsDigit(s.Key[0]))
        {
            return $"数字键 {key} 需要手指从基准键伸展，练「单键练习 · 数字行」";
        }
        if (s.Key is "," or "." or "/" or ";" or "'" or "[" or "]" or "\\" or "-" or "=" or "`")
        {
            return $"符号键 {key} 多由小指负责，练「单键练习 · 常用符号」";
        }
        return $"{key} 错误偏多，可在「单键练习」里针对这一区反复击打";
    }

    /// <summary>生成一句话弱点结论，让用户不必自己看表也能知道下一步练什么。</summary>
    private static string BuildWeaknessSummary(List<WeakKeyRow> weak, List<KeyStatEntry> stats, int keysWithData)
    {
        if (keysWithData == 0)
        {
            return "暂无键位数据（中文练习走输入法，不采集物理按键），先做几次英文练习吧";
        }
        if (weak.Count == 0)
        {
            return "键位错误率都很低，可以尝试提高难度等级或挑战更长的文本";
        }

        WeakKeyRow top = weak[0];
        double overallError = stats.Where(s => s.Total > 0).Sum(s => s.Wrong) * 100d
                             / Math.Max(1, stats.Where(s => s.Total > 0).Sum(s => s.Total));
        return $"最需要加强的是 {top.Display}（错误 {top.Wrong} 次，错误率 {top.ErrorRate:F1}%）；" +
               $"整体错误率 {overallError:F1}%，建议从薄弱键位所在的指法分组开始练。";
    }

    #endregion 统计重建

    #region 导出

    /// <summary>
    /// 把当前筛选后的成绩导出为 CSV（UTF-8 BOM，保证 Excel 打开不乱码）。
    /// </summary>
    /// <returns>导出文件路径；无数据时返回空串</returns>
    public string ExportCsv()
    {
        List<TypingRecord> filtered = ApplyFilters(_all);
        if (filtered.Count == 0)
        {
            StatusMessage = "当前筛选条件下没有可导出的记录";
            return string.Empty;
        }

        // 导出到「当前用户」的目录，避免多用户共用电脑时把成绩导到一处
        string dir = AppDataPaths.ExportDirectory;
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, $"typing_records_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("记录时间,练习类型,难度,细分等级,评级,总字符,正确,错误,速度,速度单位,正确率(%),耗时(秒)");
        foreach (TypingRecord r in filtered.OrderBy(r => r.CreateTime))
        {
            var type = (PracticeType)r.PracticeType;
            bool isCjkType = type is PracticeType.Chinese or PracticeType.ChineseWord or PracticeType.Wubi;
            sb.AppendLine(string.Join(",",
                r.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                PracticeTypeText(type),
                DifficultyText(r.Difficulty),
                r.Level.ToString(),
                GradeScale.ToText(GradeScale.FromInt(r.Grade)),
                r.TotalCharCount.ToString(),
                r.RightCharCount.ToString(),
                r.WrongCharCount.ToString(),
                r.Speed.ToString("F1"),
                isCjkType ? "字/分" : "WPM",
                r.Accuracy.ToString("F1"),
                r.UseSecond.ToString()));
        }

        try
        {
            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
            StatusMessage = $"已导出 {filtered.Count} 条记录：{file}";
            return file;
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败：{ex.Message}";
            return string.Empty;
        }
    }

    /// <summary>
    /// 导出完成事件：把文件路径交给页面，由页面弹窗提示并可选打开所在文件夹。
    /// （VM 不直接弹对话框，保持与 View 解耦。）
    /// </summary>
    public event EventHandler<string>? ExportRequested;

    /// <summary>导出 CSV 命令（路径通过 <see cref="ExportRequested"/> 交给页面提示用户）。</summary>
    [RelayCommand]
    private void Export() => ExportRequested?.Invoke(this, ExportCsv());

    #endregion 导出

    #region 文本工具

    /// <summary>练习类型中文名（与课程页保持同一套文案）。</summary>
    public static string PracticeTypeText(PracticeType type) => type switch
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
    public static string DifficultyText(int difficulty) => difficulty switch
    {
        // 与 Difficulty 枚举取值一致：0简单 / 1普通 / 2困难 / 3入门 / 4地狱
        0 => "简单",
        1 => "普通",
        2 => "困难",
        3 => "入门",
        4 => "地狱",
        _ => difficulty.ToString()
    };

    #endregion 文本工具
}

/// <summary>练习类型筛选选项。</summary>
public class TypeFilterOption
{
    /// <summary>类型；null 表示不限</summary>
    public PracticeType? Type { get; init; }

    /// <summary>显示名</summary>
    public string Name { get; init; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>时间范围筛选选项。</summary>
public class RangeFilterOption
{
    /// <summary>天数；0 表示不限</summary>
    public int Days { get; init; }

    /// <summary>显示名</summary>
    public string Name { get; init; } = string.Empty;

    public override string ToString() => Name;
}