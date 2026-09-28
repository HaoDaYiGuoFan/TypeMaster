using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace TypeMaster.Services;

/// <summary>
/// 单音定义：频率（Hz）、持续时长（毫秒）、与下一音之间的间隔（毫秒）。
/// </summary>
internal readonly record struct Tone(int Frequency, int DurationMs, int GapMs = 0);

/// <summary>
/// 音效合成器：把一串音符渲染成立体声采样，供混音器播放。
///
/// 为什么自己合成而不是用 <c>Console.Beep</c>（本项目实测结论）：
/// 1) <c>Console.Beep</c> 在部分 Windows 11 与虚拟声卡上完全不发声，
///    且音量无法调节、无法与其它声音混音；
/// 2) 自合成可精确控制音色与包络，也便于与 BGM 同时播放。
///
/// 实现方式：一次性把整段音符渲染进内存缓冲再流式读出——
/// 音效都很短（最长约 0.5 秒），预渲染最简单也最稳。
/// </summary>
internal sealed class ToneSequenceProvider : ISampleProvider
{
    #region 常量

    /// <summary>音头淡入时长（毫秒）：消除起音爆音。</summary>
    private const int AttackMs = 3;

    /// <summary>音尾淡出时长（毫秒）：消除收音爆音。</summary>
    private const int ReleaseMs = 12;

    #endregion 常量

    #region 局部变量属性

    private readonly float[] _data;
    private int _position;

    public WaveFormat WaveFormat { get; }

    #endregion 局部变量属性

    #region 构造

    /// <summary>
    /// 渲染一串音符。
    /// </summary>
    /// <param name="tones">音符序列</param>
    /// <param name="sampleRate">采样率</param>
    /// <param name="channels">声道数</param>
    /// <param name="gain">整体增益（0~1）</param>
    public ToneSequenceProvider(IReadOnlyList<Tone> tones, int sampleRate, int channels, float gain = 1f)
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

        int total = 0;
        foreach (var t in tones)
        {
            total += MsToSamples(t.DurationMs, sampleRate) + MsToSamples(t.GapMs, sampleRate);
        }

        _data = new float[Math.Max(0, total) * channels];

        int cursor = 0;
        foreach (var t in tones)
        {
            int dur = MsToSamples(t.DurationMs, sampleRate);
            int gap = MsToSamples(t.GapMs, sampleRate);
            RenderTone(_data, cursor, dur, t.Frequency, sampleRate, channels, gain);
            cursor += dur + gap;
        }
    }

    #endregion 构造

    #region 读取

    public int Read(float[] buffer, int offset, int count)
    {
        int remaining = _data.Length - _position;
        if (remaining <= 0)
        {
            return 0;   // 播放结束：混音器会自动移除本输入
        }

        int n = Math.Min(count, remaining);
        Array.Copy(_data, _position, buffer, offset, n);
        _position += n;
        return n;
    }

    #endregion 读取

    #region 私有工具

    private static int MsToSamples(int ms, int sampleRate)
        => (int)Math.Round(ms / 1000.0 * sampleRate);

    /// <summary>
    /// 渲染单个音符：正弦波 + 淡入淡出包络。
    /// </summary>
    private static void RenderTone(float[] data, int start, int durationSamples,
                                   int frequency, int sampleRate, int channels, float gain)
    {
        if (durationSamples <= 0 || frequency <= 0)
        {
            return;
        }

        int attack = Math.Min(MsToSamples(AttackMs, sampleRate), durationSamples);
        int release = Math.Min(MsToSamples(ReleaseMs, sampleRate), Math.Max(0, durationSamples - attack));

        double step = 2.0 * Math.PI * frequency / sampleRate;
        double phase = 0;

        for (int i = 0; i < durationSamples; i++)
        {
            // 包络：仅在音头与音尾做淡入淡出，中间保持满幅
            float env = 1f;
            if (i < attack && attack > 0)
            {
                env = (float)i / attack;
            }
            else if (i >= durationSamples - release && release > 0)
            {
                env = (float)(durationSamples - i) / release;
            }

            float v = (float)Math.Sin(phase) * env * gain;
            phase += step;

            int baseIdx = (start + i) * channels;
            for (int c = 0; c < channels; c++)
            {
                if (baseIdx + c < data.Length)
                {
                    data[baseIdx + c] = v;
                }
            }
        }
    }

    #endregion 私有工具
}
