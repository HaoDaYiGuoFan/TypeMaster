using System;

namespace TypeMaster.ViewModels;

/// <summary>对战页需要播放的视觉特效类型。</summary>
public enum GameEffectKind
{
    WordBurst,   // 消灭一个目标：星爆 + 扩散光环 + 飘字
    ComboPulse,  // 连击里程碑：分数/连击数字脉冲 + 庆祝闪光
    LifeLost,    // 漏掉目标：红圈闪烁
    WrongKey,    // 敲错：输入框抖动
    GameOver,    // 游戏结束：满屏彩屑
    ScreenShake  // 屏幕震动：Value 为强度（1 轻 / 2 重），漏怪与游戏结束时触发
}

/// <summary>对战页特效请求事件参数。</summary>
public class GameEffectEventArgs : EventArgs
{
    public GameEffectKind Kind { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public int Value { get; init; }
}
