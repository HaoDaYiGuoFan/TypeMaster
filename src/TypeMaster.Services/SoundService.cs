using System;
using System.Collections.Generic;
using TypeMaster.Core;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 音效服务：所有提示音由代码合成后交给共享音频引擎播放（基于 NAudio），
/// 不依赖任何外部音频文件，且能与背景音乐同时发声。
///
/// 为什么不再用 <c>Console.Beep</c>（本项目实测结论）：
/// 1) <c>Console.Beep</c> 在部分 Windows 11 与虚拟声卡 / 蓝牙耳机上完全不发声，
///    既有"音效开关形同失效"的问题；
/// 2) 它无法调节音量，也无法与背景音乐混音；
/// 3) 它依赖 kernel32 的硬件蜂鸣路径，行为在不同驱动下不一致。
///
/// 改动只发生在实现层：<see cref="ISoundService"/> 的 12 个方法签名保持不变，
/// 所有调用方（ViewModel / 页面）无需任何修改。
/// </summary>
public class SoundService : ISoundService
{
    #region 局部变量属性

    /// <summary>当前音效音量（0~1），由配置驱动。</summary>
    private static float Volume
    {
        get
        {
            int v = AppState.Current.SoundVolume;
            if (v < 0) v = 0;
            if (v > 100) v = 100;
            return v / 100f;
        }
    }

    #endregion 局部变量属性

    #region 打字练习音效

    public void PlayKey() => Play(new[] { new Tone(1046, 14) });

    public void PlayError() => Play(new[] { new Tone(311, 70), new Tone(233, 90, 10) });

    public void PlayComplete() => Play(new[]
    {
        new Tone(784, 90, 18), new Tone(988, 90, 18), new Tone(1319, 150)
    });

    #endregion 打字练习音效

    #region 小游戏音效

    /// <summary>锁定目标：短促的上行滑音，模拟发射。</summary>
    public void PlayShoot() => Play(new[] { new Tone(660, 40), new Tone(1180, 60) });

    /// <summary>消灭目标：清脆的双音叮咚。</summary>
    public void PlayHit() => Play(new[] { new Tone(1046, 50, 8), new Tone(1568, 85) });

    /// <summary>漏掉目标：低沉的下行音，提示失误。</summary>
    public void PlayMiss() => Play(new[] { new Tone(440, 85, 8), new Tone(294, 140) });

    /// <summary>
    /// 连击里程碑：以连击数为基准升高音调，连击越高越"燃"。
    /// </summary>
    /// <param name="combo">当前连击数</param>
    public void PlayCombo(int combo)
    {
        int step = Math.Min(8, Math.Max(0, combo / 5));
        int baseFreq = 784 + step * 80;
        Play(new[]
        {
            new Tone(baseFreq, 55, 8),
            new Tone(baseFreq + 200, 55, 8),
            new Tone(baseFreq + 400, 105)
        });
    }

    /// <summary>开局：上行三音，营造"准备开始"的仪式感。</summary>
    public void PlayStart() => Play(new[]
    {
        new Tone(523, 70, 18), new Tone(659, 70, 18), new Tone(880, 120)
    });

    /// <summary>游戏结束：下行三音，情绪收束。</summary>
    public void PlayGameOver() => Play(new[]
    {
        new Tone(784, 115, 18), new Tone(587, 115, 18), new Tone(392, 230)
    });

    /// <summary>地鼠冒头：极短的高音"啵"。</summary>
    public void PlayPop() => Play(new[] { new Tone(1245, 42) });

    /// <summary>青蛙吃虫：两声咀嚼感的短音。</summary>
    public void PlayEat() => Play(new[] { new Tone(700, 48), new Tone(480, 65) });

    /// <summary>抓到小偷：上行两音，表示成功。</summary>
    public void PlayCatch() => Play(new[] { new Tone(600, 52), new Tone(900, 78) });

    #endregion 小游戏音效

    #region 私有方法

    /// <summary>
    /// 把音符序列交给共享音频引擎播放。
    /// 引擎内部复用同一个输出设备与混音器，所以音效可以与 BGM 同时发声。
    /// </summary>
    /// <param name="tones">音符序列</param>
    private static void Play(IReadOnlyList<Tone> tones)
    {
        if (tones.Count == 0)
        {
            return;
        }

        // 音效开关关闭时直接返回
        if (!AppState.Current.EnableSound)
        {
            return;
        }

        float vol = Volume;
        if (vol <= 0f)
        {
            return;
        }

        try
        {
            var provider = new ToneSequenceProvider(tones, AudioEngine.SampleRate, AudioEngine.Channels, 0.55f);
            AudioEngine.Instance.PlayOneShot(provider, vol);
        }
        catch
        {
            // 音频设备不可用或被占用时静默忽略，绝不影响主流程
        }
    }

    #endregion 私有方法
}
