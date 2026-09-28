using TypeMaster.Core.Enums;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 导航目标
/// </summary>
public enum NavigationTarget
{
    Home = 0,
    Typing = 1,
    History = 2,
    Settings = 3,
    GameCenter = 4,

    /// <summary>学习园地：五笔字根表 / 口诀 / 拼音声韵母说明</summary>
    Learn = 5,

    /// <summary>课程中心：指法入门 → 单键 → 单词 → 句子 → 文章 五阶段闯关</summary>
    Course = 6
}

/// <summary>
/// 导航服务（由 App 层实现，解耦 ViewModel 与 View）
/// </summary>
public interface INavigationService
{
    void Navigate(NavigationTarget target);

    /// <summary>进入指定模式的打字小游戏对战页</summary>
    void NavigateGame(GameMode mode);
}
