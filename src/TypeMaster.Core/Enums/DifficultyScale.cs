namespace TypeMaster.Core.Enums;

/// <summary>
/// 难度细分等级：在 <see cref="Difficulty"/>（简单 / 普通 / 困难）三档之上再细分为 10 级。
/// 1 级最易、10 级最难；三档仍作为题库索引与历史成绩存档的维度，
/// 细分等级只影响练习文本长度、游戏刷怪节奏等"手感"参数，不改动数据库结构。
/// </summary>
public static class DifficultyScale
{
    #region 常量

    /// <summary>最小细分等级（最易）。</summary>
    public const int Min = 1;

    /// <summary>最大细分等级（最难）。</summary>
    public const int Max = 10;

    /// <summary>简单档的等级区间 [1, 3]。</summary>
    private const int EasyMax = 3;

    /// <summary>普通档的等级区间 [4, 7]。</summary>
    private const int NormalMax = 7;

    #endregion 常量

    #region 等级换算

    /// <summary>
    /// 将细分等级收敛到合法区间 [Min, Max]，避免越界值影响题库索引与游戏参数。
    /// </summary>
    /// <param name="level">原始细分等级</param>
    /// <returns>位于 [Min, Max] 内的细分等级</returns>
    public static int Clamp(int level)
    {
        if (level < Min)
        {
            return Min;
        }
        if (level > Max)
        {
            return Max;
        }
        return level;
    }

    /// <summary>
    /// 细分等级 → 三档难度：1~3 简单，4~7 普通，8~10 困难。
    /// </summary>
    /// <param name="level">细分等级（会自动收敛到合法区间）</param>
    /// <returns>对应的三档难度枚举</returns>
    public static Difficulty ToDifficulty(int level)
    {
        int lv = Clamp(level);
        if (lv <= EasyMax)
        {
            return Difficulty.Easy;
        }
        if (lv <= NormalMax)
        {
            return Difficulty.Normal;
        }
        return Difficulty.Hard;
    }

    /// <summary>
    /// 三档难度 → 该档的中间细分等级，用于点击档位按钮时同步滑动条。
    /// </summary>
    /// <param name="difficulty">三档难度枚举</param>
    /// <returns>该档对应的细分等级</returns>
    public static int FromDifficulty(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 2,
            Difficulty.Normal => 5,
            Difficulty.Hard => 9,
            _ => Min
        };
    }

    #endregion 等级换算

    #region 档位内微调

    /// <summary>
    /// 计算细分等级在所属档位内的相对位置（0~1），供游戏参数做线性微调。
    /// 例：简单档 1/2/3 级分别返回 0 / 0.5 / 1。
    /// </summary>
    /// <param name="level">细分等级（会自动收敛到合法区间）</param>
    /// <returns>0~1 之间的微调系数</returns>
    public static double FineTune(int level)
    {
        int lv = Clamp(level);
        (int lo, int hi) = lv <= EasyMax ? (Min, EasyMax) : lv <= NormalMax ? (EasyMax + 1, NormalMax) : (NormalMax + 1, Max);
        if (hi <= lo)
        {
            return 0;
        }
        return (double)(lv - lo) / (hi - lo);
    }

    #endregion 档位内微调

    #region 界面展示

    /// <summary>
    /// 细分等级 → 中文描述，用于滑动条旁的文案展示。
    /// </summary>
    /// <param name="level">细分等级（会自动收敛到合法区间）</param>
    /// <returns>形如 "3 级 · 入门" 的描述文本</returns>
    public static string ToText(int level)
    {
        return Clamp(level) switch
        {
            1 => "1 级 · 入门",
            2 => "2 级 · 简单",
            3 => "3 级 · 轻松",
            4 => "4 级 · 普通",
            5 => "5 级 · 标准",
            6 => "6 级 · 进阶",
            7 => "7 级 · 熟练",
            8 => "8 级 · 困难",
            9 => "9 级 · 挑战",
            _ => "10 级 · 极限"
        };
    }

    #endregion 界面展示
}
