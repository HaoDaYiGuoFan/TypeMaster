using System;
using System.Collections.Generic;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>
/// 背景音乐曲目清单：曲目标识 → 资源路径。
///
/// 音乐全部为本项目原创合成（Python 生成多声部循环乐段，
/// 再经 Windows Media Foundation 编码为 MP3），无任何第三方版权问题。
/// </summary>
public static class MusicLibrary
{
    #region 曲目标识

    /// <summary>游戏厅（游戏列表页）BGM。</summary>
    public const string Lobby = "lobby";

    /// <summary>太空大战 BGM：科幻、紧张。</summary>
    public const string SpaceWar = "space";

    /// <summary>打地鼠 BGM：俏皮、活泼。</summary>
    public const string WhackMole = "mole";

    /// <summary>抓小偷 BGM：悬疑、追逐。</summary>
    public const string CatchThief = "thief";

    /// <summary>青蛙吃虫 BGM：悠闲、轻快。</summary>
    public const string FrogBug = "frog";

    /// <summary>打气球 BGM：明亮、欢悦。</summary>
    public const string BalloonPop = "balloon";

    /// <summary>生死时速 BGM：竞速、激昂。</summary>
    public const string LifeDeathSpeed = "race";

    #endregion 曲目标识

    #region 清单

    /// <summary>
    /// 曲目标识 → 相对于「Music」资源目录的文件名。
    /// </summary>
    private static readonly Dictionary<string, string> Files = new(StringComparer.OrdinalIgnoreCase)
    {
        [Lobby] = "lobby.mp3",
        [SpaceWar] = "space.mp3",
        [WhackMole] = "mole.mp3",
        [CatchThief] = "thief.mp3",
        [FrogBug] = "frog.mp3",
        [BalloonPop] = "balloon.mp3",
        [LifeDeathSpeed] = "race.mp3",
    };

    /// <summary>全部曲目标识（供自检使用）。</summary>
    public static IReadOnlyCollection<string> AllTracks => Files.Keys;

    /// <summary>曲目中文名（供界面展示）。</summary>
    public static string DisplayName(string trackId) => trackId switch
    {
        Lobby => "游戏厅",
        SpaceWar => "太空大战",
        WhackMole => "打地鼠",
        CatchThief => "抓小偷",
        FrogBug => "青蛙吃虫",
        BalloonPop => "打气球",
        LifeDeathSpeed => "生死时速",
        _ => "背景音乐"
    };

    /// <summary>
    /// 取曲目对应的 MP3 文件名，未收录返回空串。
    /// </summary>
    /// <param name="trackId">曲目标识</param>
    /// <returns>文件名（不含目录）</returns>
    public static string FileNameOf(string trackId)
        => !string.IsNullOrWhiteSpace(trackId) && Files.TryGetValue(trackId, out string? f) ? f : string.Empty;

    /// <summary>
    /// 游戏模式 → 对应 BGM 曲目。
    /// </summary>
    /// <param name="mode">游戏模式</param>
    /// <returns>曲目标识</returns>
    public static string TrackFor(GameMode mode) => mode switch
    {
        GameMode.SpaceWar => SpaceWar,
        GameMode.WhackMole => WhackMole,
        GameMode.CatchThief => CatchThief,
        GameMode.FrogBug => FrogBug,
        GameMode.BalloonPop => BalloonPop,
        GameMode.LifeDeathSpeed => LifeDeathSpeed,
        _ => Lobby
    };

    #endregion 清单
}
