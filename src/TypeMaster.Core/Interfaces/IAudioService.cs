namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 背景音乐服务：为游戏厅与各款小游戏播放循环 BGM。
///
/// 设计要点：
/// 1) 实现基于 NAudio，与本机音频设备交互，支持 MP3 解码与循环播放；
/// 2) 与 <see cref="ISoundService"/> 相互独立但共享音频设备，
///    音量由各自的配置项单独控制，便于用户"关音乐留音效"；
/// 3) 切换曲目时自动淡出旧曲再淡入新曲，避免生硬的断点；
/// 4) 任何失败都静默降级——背景音乐是氛围功能，绝不能影响游戏主流程。
/// </summary>
public interface IMusicService
{
    /// <summary>当前是否正在播放背景音乐。</summary>
    bool IsPlaying { get; }

    /// <summary>当前正在播放的曲目标识（无则返回空串）。</summary>
    string CurrentTrackId { get; }

    /// <summary>
    /// 播放指定曲目并循环。若已是同一曲目则不重启（避免每次进页面都从头开始）。
    /// </summary>
    /// <param name="trackId">曲目标识，取值见 MusicLibrary</param>
    /// <returns>真正开始播放（或已在播放同一曲目）返回 true</returns>
    bool Play(string trackId);

    /// <summary>停止播放（带淡出）。</summary>
    void Stop();

    /// <summary>
    /// 应用最新的音量 / 开关配置。设置变更后调用，无需重启曲目。
    /// 音乐开关关闭时应立即停止播放。
    /// </summary>
    void ApplySettings();
}
