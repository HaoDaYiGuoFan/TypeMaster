namespace TypeMaster.Core.Enums;

/// <summary>
/// 难度等级：入门 / 简单 / 普通 / 困难。
///
/// <para>
/// <b>取值约定（重要）</b>：本枚举的整数值会写进数据库（<c>TypingRecord.Difficulty</c>），
/// 因此**只能追加新档、不能改动已有档的取值**。
/// </para>
/// <para>
/// 「入门」是在简单档之下新增的最易档，为不破坏既有成绩记录，
/// 它被追加为 <c>3</c> 而不是插到 <c>0</c>：
/// 若设为 0，历史上所有 <c>Difficulty=0</c> 的「简单」成绩都会被读成「入门」。
/// </para>
/// </summary>
public enum Difficulty
{
    /// <summary>简单（历史取值，勿改）</summary>
    Easy = 0,

    /// <summary>普通（历史取值，勿改）</summary>
    Normal = 1,

    /// <summary>困难（历史取值，勿改）</summary>
    Hard = 2,

    /// <summary>
    /// 入门：比简单更低的一档，面向刚接触键盘的小学生初学者。
    /// 追加为 3 以保证旧成绩语义不变。
    /// </summary>
    Entry = 3
}

/// <summary>
/// <see cref="Difficulty"/> 的显示与排序辅助。
///
/// 引入原因：枚举的整数顺序（Easy=0, Normal=1, Hard=2, Entry=3）与
/// 人类认知的难度顺序（入门 &lt; 简单 &lt; 普通 &lt; 困难）不一致——
/// 这是为了兼容既有存档而做的取舍。所有"按难度排序/取上下档"的逻辑
/// 都必须走这里，不能直接依赖枚举的整数值排序。
/// </summary>
public static class DifficultyOrder
{
    /// <summary>由易到难的全部档次（界面按此顺序展示）。</summary>
    public static readonly Difficulty[] All =
    {
        Difficulty.Entry,
        Difficulty.Easy,
        Difficulty.Normal,
        Difficulty.Hard
    };

    /// <summary>得到某档在"由易到难"序列中的序号（0 最易）。</summary>
    /// <param name="difficulty">难度档</param>
    /// <returns>序号；未知档返回 -1</returns>
    public static int Rank(Difficulty difficulty)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i] == difficulty)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 把数据库里读到的整数还原为难度档。
    /// 兼容处理：遇到历史遗留的越界值（如旧版本删档）时，按就近原则收敛，
    /// 避免历史页因未知取值而显示空白。
    /// </summary>
    /// <param name="value">数据库中的整数值</param>
    /// <returns>难度档</returns>
    public static Difficulty FromInt(int value)
    {
        if (Enum.IsDefined(typeof(Difficulty), value))
        {
            return (Difficulty)value;
        }
        // 越界值：负数为最易、过大为最难
        return value < 0 ? Difficulty.Entry : Difficulty.Hard;
    }

    /// <summary>中文名称。</summary>
    /// <param name="difficulty">难度档</param>
    /// <returns>中文档名</returns>
    public static string ToText(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Entry => "入门",
        Difficulty.Easy => "简单",
        Difficulty.Normal => "普通",
        Difficulty.Hard => "困难",
        _ => "未知"
    };

    /// <summary>
    /// 该档是否面向"零基础/低龄"用户（用于文案与默认参数微调）。
    /// </summary>
    /// <param name="difficulty">难度档</param>
    /// <returns>是否为基础档</returns>
    public static bool IsBeginner(Difficulty difficulty)
        => difficulty is Difficulty.Entry or Difficulty.Easy;
}
