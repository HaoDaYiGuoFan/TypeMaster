using System;
using System.Collections.Generic;

namespace TypeMaster.Services;

/// <summary>提示语的情绪：决定图标与配色（Cheer 鼓励 / Oops 哎呀 / Wow 厉害）。</summary>
public enum TipMood
{
    Cheer,
    Oops,
    Wow
}

/// <summary>一条趣味提示事件参数。</summary>
public class FunTipEventArgs : EventArgs
{
    public string Message { get; init; } = string.Empty;
    public TipMood Mood { get; init; }
}

/// <summary>
/// 趣味提示语服务：集中管理面向玩家的诙谐提示（默认称呼“小胡”，八岁左右小朋友语气），
/// 在打字出错、消灭目标、连击、游戏结束等时刻随机播报。
/// 称呼由 <see cref="Nickname"/> 决定，首次启动让玩家自定，不填则默认“小胡”。
/// 纯逻辑、无 WPF 依赖，由界面层（FunTipHost）订阅 <see cref="TipRequested"/> 播放。
/// </summary>
public class FunTipService
{
    // —— 各类情景提示语（称呼统一为“小胡”，语气像给八九岁小朋友打气）——
    private static readonly string[] TypingError =
    {
        "小胡，这个键偷偷跑偏啦，再来一下抓住它！",
        "哎呀小胡，手指打滑了是不是？下次准能中！",
        "小胡别急，那个字母有点害羞，慢慢找它~",
        "小胡，敲错咯！它躲错位置了，换个键试试？",
        "小胡，键盘小精灵说你想念错它啦，嘿嘿",
        "小胡，差一点点！眼睛盯住对照的那一行~"
    };

    private static readonly string[] GameMiss =
    {
        "小胡，小怪兽溜走啦！下一只别让它跑哦~",
        "哎呀小胡，敌人逃跑成功，咱防线漏风咯！",
        "小胡快看，它跑啦！眼睛要盯紧屏幕呀",
        "小胡，它被你放走啦，下次手速再快点！",
        "小胡，漏掉一个！别担心，接着打回来！"
    };

    private static readonly string[] WordCompleted =
    {
        "小胡真棒！这个词被你一招搞定！",
        "哇小胡，命中！你就是打字小神射手~",
        "小胡厉害啦，这个词乖乖投降咯！",
        "小胡，漂亮！键盘都被你打得笑开花~",
        "小胡，干得漂亮！又消灭一个！"
    };

    private static readonly string[] Combo =
    {
        "小胡连击 x{0}！手速像火箭嗖嗖的！",
        "小胡 x{0} 连击！你是要起飞了吗哈哈",
        "小胡，连击 x{0}！手指头在跳舞呢~",
        "小胡 x{0} 连击啦！全场最靓的仔就是你"
    };

    private static readonly string[] GameOver =
    {
        "小胡，这局打完啦！再来一局肯定更厉害！",
        "小胡，game over～别灰心，你离高手只差练习啦",
        "小胡，战斗结束！喝口水，咱们再战一场！"
    };

    private static readonly string[] PracticeComplete =
    {
        "小胡，整篇打完啦！给自己鼓个掌啪啪啪~",
        "小胡完成挑战！你比昨天又厉害一点点哦",
        "小胡，全文通关！这波操作我给满分！"
    };

    private static readonly string[] Perfect =
    {
        "小胡，一个都没错！你是打字小天才本才！",
        "小胡满分通关！键盘都为你鼓掌啦！",
        "小胡，零失误！这水平可以去比赛咯！"
    };

    private static readonly string[] Start =
    {
        "小胡，准备好咯，手指头先热热身~",
        "小胡上场啦！让键盘见识下你的厉害！"
    };

    private readonly Random _rand = new();

    /// <summary>当前玩家昵称，提示语中的“小胡”会被替换成它。默认“用户1”。</summary>
    public string Nickname { get; set; } = "用户1";

    /// <summary>某条提示需要弹出时触发（界面层订阅并播放动画）。</summary>
    public event EventHandler<FunTipEventArgs>? TipRequested;

    public void Show(string message, TipMood mood = TipMood.Cheer)
        => TipRequested?.Invoke(this, new FunTipEventArgs { Message = message, Mood = mood });

    public void ShowTypingError() => Show(WithName(Pick(TypingError)), TipMood.Oops);
    public void ShowGameMiss() => Show(WithName(Pick(GameMiss)), TipMood.Oops);
    public void ShowWordCompleted() => Show(WithName(Pick(WordCompleted)), TipMood.Cheer);
    public void ShowCombo(int combo) => Show(WithName(string.Format(Pick(Combo), combo)), TipMood.Wow);
    public void ShowGameOver() => Show(WithName(Pick(GameOver)), TipMood.Cheer);
    public void ShowPracticeComplete() => Show(WithName(Pick(PracticeComplete)), TipMood.Cheer);
    public void ShowPerfect() => Show(WithName(Pick(Perfect)), TipMood.Wow);
    public void ShowStart() => Show(WithName(Pick(Start)), TipMood.Cheer);

    /// <summary>把提示语里的占位称呼“小胡”替换为当前昵称。</summary>
    private string WithName(string message) => message.Replace("小胡", Nickname);

    private string Pick(string[] arr) => arr[_rand.Next(arr.Length)];
}
