namespace TypeMaster.Core.Enums;

/// <summary>
/// 打字小游戏模式（玩法参考金山打字通经典小游戏）
/// </summary>
public enum GameMode
{
    /// <summary>太空大战：敌机携带单词从顶部下落，输入单词将其击落</summary>
    SpaceWar = 0,
    /// <summary>打地鼠：地鼠携带单词从地洞冒出，输入单词将其敲打</summary>
    WhackMole = 1,
    /// <summary>抓小偷：小偷携带单词横向逃窜，输入单词将其擒获</summary>
    CatchThief = 2,
    /// <summary>青蛙吃虫：虫子携带单词爬向青蛙，输入单词将其吃掉</summary>
    FrogBug = 3,
    /// <summary>打气球：气球携带单词从底部升起，输入单词将其扎破，飘出顶部扣生命</summary>
    BalloonPop = 4,
    /// <summary>生死时速：与电脑赛跑，完整输入单词驱动选手前进，先到终点者获胜</summary>
    LifeDeathSpeed = 5
}

/// <summary>
/// 游戏目标的可视化图形类型（映射到 MDIX PackIcon，避免使用 emoji 图标）
/// </summary>
public enum GameGlyph
{
    Ship = 0,
    Mole = 1,
    Thief = 2,
    Bug = 3,
    /// <summary>打气球的卡通气球</summary>
    Balloon = 4,
    /// <summary>生死时速的赛跑选手</summary>
    Runner = 5
}
