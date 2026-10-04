namespace TypeMaster.Core.Enums;

/// <summary>
/// 难度细分等级：在 <see cref="Difficulty"/>（入门 / 简单 / 普通 / 困难 / 地狱）五档之上再细分为 15 级。
/// 1 级最易、15 级最难；五档仍作为题库索引与历史成绩存档的维度，
/// 细分等级只影响练习文本长度、游戏刷怪节奏等"手感"参数，不改动数据库结构。
///
/// <para>
/// <b>等级区间划分</b>（由易到难）：
/// 入门 [1, 3]、简单 [4, 6]、普通 [7, 10]、困难 [11, 13]、地狱 [14, 15]。
/// </para>
/// <para>
/// <b>为什么档位不从 1 级开始均匀分</b>：难度不是线性的——
/// 初学者从"认识键盘"到"能连续打"跨越大，需要更细的台阶；
/// 而中高档之间差异主要体现在速度与准确率上，台阶可以更大。
/// 因此入门档给了独立的 1~3 级（其中 1 级比原来的最低级还容易）。
/// </para>
/// </summary>
public static class DifficultyScale
{
    #region 常量

    /// <summary>最小细分等级（最易）。</summary>
    public const int Min = 1;

    /// <summary>最大细分等级（最难）。</summary>
    public const int Max = 15;

    /// <summary>入门档的等级区间 [1, 3]。</summary>
    private const int EntryMax = 3;

    /// <summary>简单档的等级区间 [4, 6]。</summary>
    private const int EasyMax = 6;

    /// <summary>普通档的等级区间 [7, 10]。</summary>
    private const int NormalMax = 10;

    /// <summary>困难档的等级区间 [11, 13]。</summary>
    private const int HardMax = 13;

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
    /// 细分等级 → 难度档：1~3 入门，4~6 简单，7~10 普通，11~13 困难，14~15 地狱。
    /// </summary>
    /// <param name="level">细分等级（会自动收敛到合法区间）</param>
    /// <returns>对应的难度档</returns>
    public static Difficulty ToDifficulty(int level)
    {
        int lv = Clamp(level);
        if (lv <= EntryMax)
        {
            return Difficulty.Entry;
        }
        if (lv <= EasyMax)
        {
            return Difficulty.Easy;
        }
        if (lv <= NormalMax)
        {
            return Difficulty.Normal;
        }
        if (lv <= HardMax)
        {
            return Difficulty.Hard;
        }
        return Difficulty.Hell;
    }

    /// <summary>
    /// 难度档 → 该档的中间细分等级，用于点击档位按钮时同步滑动条。
    /// </summary>
    /// <param name="difficulty">难度档</param>
    /// <returns>该档对应的细分等级</returns>
    public static int FromDifficulty(Difficulty difficulty) => difficulty switch
    {
        // 取档位区间的中位数：太靠近边界会让"点一下档位"显得偏易或偏难
        Difficulty.Entry => 2,      // [1,3] 的中位
        Difficulty.Easy => 5,       // [4,6] 的中位
        Difficulty.Normal => 8,     // [7,10] 的中位偏易
        Difficulty.Hard => 12,      // [11,13] 的中位
        Difficulty.Hell => 14,      // [14,15] 的低位——刚进地狱档先从 14 级起步
        _ => Min
    };

    /// <summary>某个难度档对应的细分等级区间（含首尾）。</summary>
    /// <param name="difficulty">难度档</param>
    /// <returns>区间 [起始, 结束]</returns>
    public static (int From, int To) RangeOf(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Entry => (Min, EntryMax),
        Difficulty.Easy => (EntryMax + 1, EasyMax),
        Difficulty.Normal => (EasyMax + 1, NormalMax),
        Difficulty.Hard => (NormalMax + 1, HardMax),
        Difficulty.Hell => (HardMax + 1, Max),
        _ => (Min, Max)
    };

    #endregion 等级换算

    #region 档位内微调

    /// <summary>
    /// 计算细分等级在所属档位内的相对位置（0~1），供游戏参数做线性微调。
    /// 例：入门档 1/2/3 级分别返回 0 / 0.5 / 1。
    /// </summary>
    /// <param name="level">细分等级（会自动收敛到合法区间）</param>
    /// <returns>0~1 之间的微调系数</returns>
    public static double FineTune(int level)
    {
        int lv = Clamp(level);
        var (lo, hi) = RangeOf(ToDifficulty(lv));
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
    public static string ToText(int level) => Clamp(level) switch
    {
        1 => "1 级 · 认识键盘",
        2 => "2 级 · 入门",
        3 => "3 级 · 入门进阶",
        4 => "4 级 · 简单",
        5 => "5 级 · 简单进阶",
        6 => "6 级 · 轻松",
        7 => "7 级 · 普通",
        8 => "8 级 · 标准",
        9 => "9 级 · 进阶",
        10 => "10 级 · 熟练",
        11 => "11 级 · 困难",
        12 => "12 级 · 挑战",
        13 => "13 级 · 极限",
        14 => "14 级 · 地狱",
        _ => "15 级 · 炼狱"
    };

    /// <summary>
    /// 面向低龄/零基础用户的提示语，用于入门档下的界面引导。
    /// </summary>
    /// <param name="level">细分等级</param>
    /// <returns>提示文本；非入门档返回空串</returns>
    public static string BeginnerHint(int level)
        => ToDifficulty(level) == Difficulty.Entry
            ? "慢慢来，先看清键盘上的位置，不用着急提速。"
            : string.Empty;

    #endregion 界面展示
}
