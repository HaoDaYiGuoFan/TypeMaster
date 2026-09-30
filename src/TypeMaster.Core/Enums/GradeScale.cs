using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMaster.Core.Enums;

/// <summary>
/// 评级换算规则：把「速度 + 正确率」折算成 <see cref="Grade"/>、星级与段位。
///
/// 设计要点：
/// 1) 速度不是绝对值达标，而是与「本练习类型 + 本细分等级」的基准速度相比的倍率，
///    因此低等级打得快、高等级打得慢都能得到合理评级。
/// 2) 正确率作为降级闸门：速度再快，正确率不到位也要降级，避免「乱敲刷速度」。
/// </summary>
public static class GradeScale
{
    #region 基准速度

    /// <summary>
    /// 各练习类型在 1 级时的基准速度（B 级对应的速度），
    /// 每升一级按 <see cref="SpeedStep"/> 递增。
    /// 单位与 TypingResult.Speed 一致（英文 WPM / 中文 KPM / 五笔 字每分钟）。
    /// </summary>
    private static double BaseSpeed(PracticeType type) => type switch
    {
        PracticeType.English or PracticeType.EnglishWord => 20d,
        PracticeType.Chinese or PracticeType.ChineseWord => 30d,
        PracticeType.Wubi => 10d,
        PracticeType.SpeedTest => 25d,
        _ => 20d
    };

    /// <summary>
    /// 入门档（<see cref="Difficulty.Entry"/>）的基准速度。
    ///
    /// 为什么单独给：入门档面向刚接触键盘的小学生，
    /// 若沿用「1 级 = 20 WPM」的门槛，他们几乎不可能拿到 B 级，
    /// 会一直在最低评级徘徊，打击积极性。
    /// 这里取常规基准的约三分之一，让"打对就能达标"。
    /// </summary>
    private static double EntryBaseSpeed(PracticeType type) => type switch
    {
        PracticeType.English or PracticeType.EnglishWord => 7d,
        PracticeType.Chinese or PracticeType.ChineseWord => 10d,
        PracticeType.Wubi => 4d,
        PracticeType.SpeedTest => 9d,
        _ => 7d
    };

    /// <summary>细分等级每升 1 级，基准速度的增量。</summary>
    private static double SpeedStep(PracticeType type) => type switch
    {
        PracticeType.English or PracticeType.EnglishWord => 3d,
        PracticeType.Chinese or PracticeType.ChineseWord => 6d,
        PracticeType.Wubi => 3d,
        PracticeType.SpeedTest => 4d,
        _ => 3d
    };

    /// <summary>
    /// 指定类型与细分等级的基准速度（该速度即 B 级的门槛）。
    /// </summary>
    /// <param name="type">练习类型</param>
    /// <param name="level">细分等级（1~10，越界自动收敛）</param>
    /// <returns>基准速度</returns>
    public static double GetBaselineSpeed(PracticeType type, int level)
    {
        int lv = DifficultyScale.Clamp(level);
        Difficulty diff = DifficultyScale.ToDifficulty(lv);

        if (diff == Difficulty.Entry)
        {
            // 入门档：低起点 + 小步长，1~3 级在 7~11 WPM（英文）之间平缓上升
            var (lo, _) = DifficultyScale.RangeOf(Difficulty.Entry);
            double entryStep = SpeedStep(type) * 0.5;
            return EntryBaseSpeed(type) + (lv - lo) * entryStep;
        }

        // 简单档起步即按"常规基准的 1 级水平"计算，保证与入门档衔接平滑
        var (simpleFrom, _) = DifficultyScale.RangeOf(Difficulty.Easy);
        return BaseSpeed(type) + (lv - simpleFrom + 1) * SpeedStep(type);
    }

    #endregion 基准速度

    #region 评级

    /// <summary>评级门槛：速度需达到基准速度的倍数。</summary>
    private static double SpeedRatio(Grade grade) => grade switch
    {
        Grade.S => 1.4d,
        Grade.A => 1.15d,
        Grade.B => 1.0d,
        Grade.C => 0.7d,
        _ => 0d
    };

    /// <summary>
    /// 计算评级。
    /// </summary>
    /// <param name="type">练习类型</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <param name="speed">实测速度</param>
    /// <param name="accuracy">实测正确率（百分比，0~100）</param>
    /// <returns>最终评级</returns>
    public static Grade Evaluate(PracticeType type, int level, double speed, double accuracy)
    {
        double baseline = GetBaselineSpeed(type, level);
        double ratio = baseline <= 0 ? 0d : speed / baseline;

        // 先按速度取一个初评级
        Grade grade = Grade.D;
        foreach (Grade g in new[] { Grade.S, Grade.A, Grade.B, Grade.C })
        {
            if (ratio >= SpeedRatio(g))
            {
                grade = g;
                break;
            }
        }

        // 再按正确率降级：正确率越低，降得越多
        int demote = AccuracyDemote(accuracy);
        int final = Math.Max(0, (int)grade - demote);
        return (Grade)final;
    }

    /// <summary>
    /// 正确率对应的降级档数：
    /// 大于等于 98% 不降级；95% 降 1 档；90% 降 2 档；80% 降 3 档；否则直接压到 D。
    /// </summary>
    private static int AccuracyDemote(double accuracy)
    {
        if (accuracy >= 98d) return 0;
        if (accuracy >= 95d) return 1;
        if (accuracy >= 90d) return 2;
        if (accuracy >= 80d) return 3;
        return 4;
    }

    /// <summary>评级英文名（用于界面与 CSV 导出）。</summary>
    public static string ToText(Grade grade) => grade switch
    {
        Grade.S => "S",
        Grade.A => "A",
        Grade.B => "B",
        Grade.C => "C",
        _ => "D"
    };

    /// <summary>评级中文说明（用于界面提示）。</summary>
    public static string ToChinese(Grade grade) => grade switch
    {
        Grade.S => "卓越",
        Grade.A => "优秀",
        Grade.B => "良好",
        Grade.C => "及格",
        _ => "待提高"
    };

    /// <summary>评级折算星级（1~5 星）。</summary>
    public static int ToStars(Grade grade) => (int)grade + 1;

    /// <summary>把持久化的整数还原为评级枚举（越界自动收敛）。</summary>
    public static Grade FromInt(int value)
        => Enum.IsDefined(typeof(Grade), value) ? (Grade)value : Grade.D;

    #endregion 评级

    #region 段位

    /// <summary>达到 S 级多少次可授予「大师」段位。</summary>
    private const int MasterThreshold = 5;

    /// <summary>
    /// 由历史成绩的评级分布推导段位。
    /// </summary>
    /// <param name="grades">历史成绩的评级序列</param>
    /// <returns>段位</returns>
    public static TypingRank GetRank(IEnumerable<Grade> grades)
    {
        List<Grade> list = grades?.ToList() ?? new List<Grade>();
        int s = list.Count(g => g == Grade.S);
        if (s >= MasterThreshold) return TypingRank.Master;
        if (s >= 1) return TypingRank.Expert;
        if (list.Any(g => g == Grade.A)) return TypingRank.Skilled;
        if (list.Any(g => g == Grade.B)) return TypingRank.Beginner;
        return TypingRank.Novice;
    }

    /// <summary>段位中文名。</summary>
    public static string ToText(TypingRank rank) => rank switch
    {
        TypingRank.Master => "大师",
        TypingRank.Expert => "高手",
        TypingRank.Skilled => "熟练",
        TypingRank.Beginner => "入门",
        _ => "新手"
    };

    #endregion 段位
}