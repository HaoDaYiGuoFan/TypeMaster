namespace TypeMaster.Core.Entities;

/// <summary>
/// 用户档案（轻量持久化，存于独立 JSON，避免改动 EF AppConfig 表结构）。
/// 目前仅承载昵称，供所有趣味提示语统一称呼玩家。
/// </summary>
public class UserProfile
{
    /// <summary>玩家昵称，默认“用户1”</summary>
    public string Nickname { get; set; } = "用户1";

    /// <summary>是否已初始化（首次启动弹窗后记为 true，避免重复打扰）</summary>
    public bool Initialized { get; set; }

    /// <summary>是否已看过新手引导（看完或跳过后记为 true，避免每次启动都弹）</summary>
    public bool HasSeenGuide { get; set; }
}
