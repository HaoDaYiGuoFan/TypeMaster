using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TypeMaster.Services;

/// <summary>
/// 共享音频引擎：整个应用共用一个输出设备与一个混音器。
///
/// 为什么要共用而不是每个声音各开一个设备（本项目实测结论）：
/// 1) Windows 上同时打开多个 WaveOut 设备容易互相抢占，出现爆音或某个声音直接丢失；
/// 2) 共用混音器后，BGM 与音效可以真正叠加播放，并各自独立控制音量；
/// 3) 音效播放完毕由混音器自动摘除，无需手工管理生命周期。
///
/// 线程安全：所有公开方法都加锁，允许 UI 线程与后台线程同时调用。
/// </summary>
internal sealed class AudioEngine : IDisposable
{
    #region 常量

    /// <summary>统一采样率（混音器与所有输入必须一致）。</summary>
    public const int SampleRate = 44100;

    /// <summary>统一声道数：立体声。</summary>
    public const int Channels = 2;

    #endregion 常量

    #region 单例

    private static readonly Lazy<AudioEngine> Lazy = new(() => new AudioEngine(), isThreadSafe: true);

    /// <summary>全局唯一实例。</summary>
    public static AudioEngine Instance => Lazy.Value;

    #endregion 单例

    #region 局部变量属性

    private readonly object _gate = new();

    /// <summary>混音器：BGM 与所有音效都挂在它上面。</summary>
    private readonly MixingSampleProvider _mixer;

    /// <summary>输出设备（延迟创建：无音频设备时不应让程序崩溃）。</summary>
    private WaveOutEvent? _output;

    /// <summary>当前是否已成功打开输出设备。</summary>
    private bool _outputReady;

    /// <summary>输出设备初始化失败原因（供上层给出提示）。</summary>
    private string? _deviceError;

    private bool _disposed;

    #endregion 局部变量属性

    #region 构造

    private AudioEngine()
    {
        // ReadFully = false：输入流读完后由混音器自动移除，这正是音效所需的"放完即走"行为
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels))
        {
            ReadFully = false
        };
    }

    /// <summary>输出设备是否可用。为 false 时所有播放请求都会被安静忽略。</summary>
    public bool IsAvailable
    {
        get
        {
            EnsureOutput();
            return _outputReady;
        }
    }

    /// <summary>输出设备不可用时的原因说明。</summary>
    public string DeviceError => _deviceError ?? string.Empty;

    #endregion 构造

    #region 播放控制

    /// <summary>
    /// 添加一个音效输入：播放完毕后由混音器自动移除，调用方无需关心释放。
    /// </summary>
    /// <param name="source">音频源（必须为 44100Hz 立体声）</param>
    /// <param name="volume">该音效的音量（0~1）</param>
    /// <returns>成功加入返回 true；设备不可用返回 false</returns>
    public bool PlayOneShot(ISampleProvider source, float volume)
    {
        if (source is null || _disposed)
        {
            return false;
        }
        EnsureOutput();
        if (!_outputReady)
        {
            return false;
        }

        try
        {
            var normalized = Normalize(source);
            var withVolume = new VolumeSampleProvider(normalized) { Volume = Clamp01(volume) };
            lock (_gate)
            {
                _mixer.AddMixerInput(withVolume);
                // ReadFully=false 的混音器在所有输入被移除后会把输出设备带入
                // Stopped 态（如音乐 Stop 之后）。重新有输入时必须显式恢复播放，
                // 否则后续所有声音（音效同理）都永远不会响起。
                _output?.Play();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置 BGM 输入：先移除旧的，再加入新的。传入 null 表示只移除。
    /// BGM 需要常驻，因此不由混音器自动回收（循环流永远读不完）。
    /// </summary>
    /// <param name="source">BGM 音频源（需已是循环流）</param>
    /// <param name="volume">BGM 音量（0~1）</param>
    /// <returns>成功设置返回 true</returns>
    public bool SetMusicInput(ISampleProvider? source, float volume)
    {
        if (_disposed)
        {
            return false;
        }

        lock (_gate)
        {
            if (_musicInput is not null)
            {
                try { _mixer.RemoveMixerInput(_musicInput); } catch { /* 已被移除 */ }
                _musicInput = null;
            }

            if (source is null)
            {
                return true;
            }

            EnsureOutput();
            if (!_outputReady)
            {
                return false;
            }

            try
            {
                var withVolume = new VolumeSampleProvider(Normalize(source)) { Volume = Clamp01(volume) };
                _musicInput = withVolume;
                _mixer.AddMixerInput(withVolume);
                // 关键修复：Stop 之后输出设备可能已因"混音器无输入"自动停止
                //（ReadFully=false 的 NAudio 行为），重新挂上 BGM 后必须
                // 显式恢复播放，否则"停了再播"永远无声——这正是
                // "回首页再进游戏乐园，BGM 消失"的根因。
                _output?.Play();
                return true;
            }
            catch
            {
                _musicInput = null;
                return false;
            }
        }
    }

    /// <summary>当前 BGM 输入（用于实时改音量）。</summary>
    private VolumeSampleProvider? _musicInput;

    /// <summary>实时调整当前 BGM 音量（不重启曲目）。</summary>
    /// <param name="volume">音量（0~1）</param>
    public void SetMusicVolume(float volume)
    {
        lock (_gate)
        {
            if (_musicInput is not null)
            {
                _musicInput.Volume = Clamp01(volume);
            }
        }
    }

    #endregion 播放控制

    #region 设备管理

    /// <summary>
    /// 延迟打开输出设备。没有音频设备（如无声服务器）时记录原因并保持静默。
    /// </summary>
    private void EnsureOutput()
    {
        if (_outputReady || _disposed)
        {
            return;
        }

        lock (_gate)
        {
            if (_outputReady || _disposed)
            {
                return;
            }

            try
            {
                if (WaveOut.DeviceCount <= 0)
                {
                    _deviceError = "未检测到音频输出设备。";
                    return;
                }

                var output = new WaveOutEvent { DesiredLatency = 120 };
                output.Init(_mixer);
                output.Play();
                _output = output;
                _outputReady = true;
            }
            catch (Exception ex)
            {
                _deviceError = "音频输出初始化失败：" + ex.Message;
                _outputReady = false;
            }
        }
    }

    /// <summary>
    /// 把任意格式的输入规整为引擎统一的 44100Hz 立体声，避免混音器抛格式异常。
    /// </summary>
    /// <param name="source">原始音频源</param>
    /// <returns>规整后的音频源</returns>
    private static ISampleProvider Normalize(ISampleProvider source)
    {
        ISampleProvider result = source;

        // 声道数：单声道转立体声；立体声保持不变
        if (result.WaveFormat.Channels == 1)
        {
            result = new MonoToStereoSampleProvider(result);
        }

        // 采样率：非 44100 时重采样（本项目自制音频均为 44100，这里只做兜底）
        if (result.WaveFormat.SampleRate != SampleRate)
        {
            result = new WdlResamplingSampleProvider(result, SampleRate);
        }

        return result;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    #endregion 设备管理

    #region 释放

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            try
            {
                _output?.Stop();
                _output?.Dispose();
            }
            catch
            {
                // 释放失败无需处理
            }
            _output = null;
            _outputReady = false;
        }
    }

    #endregion 释放
}
