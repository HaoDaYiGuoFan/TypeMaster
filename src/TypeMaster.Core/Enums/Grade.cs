namespace TypeMaster.Core.Enums;

/// <summary>
/// 打字评级：由「相对速度基线」与「正确率」共同判定，S 最高、D 最低。
/// 数值刻意设计为 D=0，使旧成绩（无评级列）默认落到 D，语义上等同「未达标」。
/// </summary>
public enum Grade
{
    /// <summary>D：未达基准速度，或正确率过低</summary>
    D = 0,
    /// <summary>C：及格</summary>
    C = 1,
    /// <summary>B：良好</summary>
    B = 2,
    /// <summary>A：优秀</summary>
    A = 3,
    /// <summary>S：卓越</summary>
    S = 4
}

/// <summary>
/// 打字段位：由历史成绩中的评级分布推导的长期水平标签。
/// </summary>
public enum TypingRank
{
    /// <summary>新手：尚无达标成绩</summary>
    Novice = 0,
    /// <summary>入门：至少一次 B 级</summary>
    Beginner = 1,
    /// <summary>熟练：至少一次 A 级</summary>
    Skilled = 2,
    /// <summary>高手：至少一次 S 级</summary>
    Expert = 3,
    /// <summary>大师：五次以上 S 级</summary>
    Master = 4
}