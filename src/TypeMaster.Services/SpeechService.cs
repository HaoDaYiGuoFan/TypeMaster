using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using TypeMaster.Core;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 语音朗读服务实现：使用 Windows 内置语音合成（SAPI），全程离线、不联网。
///
/// 关键行为：
/// 1) 语音对象延迟创建并全程复用（<see cref="SpeechSynthesizer"/> 构造较慢，且持有系统资源）；
/// 2) 自动挑选中文（zh-CN 优先）语音；找不到中文语音时 <see cref="IsAvailable"/> 为 false，
///    调用方应禁用按钮并展示提示，而不是抛异常；
/// 3) 重复点击直接打断上一段（先取消再朗读），不会排队堆积；
/// 4) 任何一步失败都静默降级——朗读只是辅助功能，绝不能影响主流程。
///
/// 平台说明：SAPI 仅 Windows 提供，整个类标注 <see cref="SupportedOSPlatformAttribute"/>，
/// 使平台分析器（CA1416）不再对内部调用逐条报警。
/// </summary>
[SupportedOSPlatform("windows")]
public class SpeechService : ITextToSpeechService, IDisposable
{
    #region 局部变量属性

    /// <summary>朗读用的语音对象（延迟创建）。</summary>
    private SpeechSynthesizer? _synth;

    /// <summary>是否已尝试过初始化（失败后不再反复重试，避免每次都卡顿）。</summary>
    private bool _initAttempted;

    /// <summary>初始化失败原因（用于生成提示文案）。</summary>
    private string? _initError;

    /// <summary>并发保护：Speak / Stop 可能来自 UI 线程的不同事件。</summary>
    private readonly object _gate = new();

    private bool _disposed;

    #endregion 局部变量属性

    #region 可用性

    public bool IsAvailable
    {
        get
        {
            EnsureInitialized();
            return _synth is not null;
        }
    }

    public string UnavailableHint =>
        _initError ?? "当前系统未安装中文语音包，暂时无法朗读。可在「设置 → 时间和语言 → 语音」中添加中文语音。";

    #endregion 可用性

    #region 朗读

    public bool Speak(string text) => Speak(text, 0);

    public bool Speak(string text, int rateOffset)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // 朗读开关关闭时直接返回，不产生任何声音
        if (!AppState.Current.EnableSpeech)
        {
            return false;
        }

        EnsureInitialized();
        if (_synth is null)
        {
            return false;
        }

        try
        {
            lock (_gate)
            {
                if (_disposed || _synth is null)
                {
                    return false;
                }

                // 语速：配置里是 -5~5 的档位，SAPI 的 Rate 正好也是 -10~10，直接映射。
                // rateOffset 用于按场景微调（如五笔编码提示需要更慢），叠加后再收敛。
                int rate = Math.Clamp(AppState.Current.SpeechRate + rateOffset, -5, 5);
                if (_synth.Rate != rate)
                {
                    _synth.Rate = rate;
                }

                // 打断上一段，保证「点哪个读哪个」，不排队
                _synth.SpeakAsyncCancelAll();
                _synth.SpeakAsync(text);
                return true;
            }
        }
        catch (Exception ex)
        {
            // 语音设备被占用 / 被策略禁用等：静默降级，不影响界面
            _initError = "语音播放失败：" + ex.Message;
            return false;
        }
    }

    public void Stop()
    {
        if (_synth is null)
        {
            return;
        }

        try
        {
            lock (_gate)
            {
                _synth.SpeakAsyncCancelAll();
            }
        }
        catch
        {
            // 停止失败无需处理
        }
    }

    #endregion 朗读

    #region 初始化

    /// <summary>
    /// 延迟初始化：只尝试一次，成功后复用语音对象。
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initAttempted)
        {
            return;
        }

        lock (_gate)
        {
            if (_initAttempted)
            {
                return;
            }
            _initAttempted = true;

            try
            {
                var synth = new SpeechSynthesizer();

                // 优先中文语音；没有中文就退回系统默认（英文语音读中文会很怪，但至少可用）
                var zh = synth.GetInstalledVoices()
                    .Select(v => v.VoiceInfo)
                    .FirstOrDefault(i => i.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase));

                if (zh is not null)
                {
                    synth.SelectVoice(zh.Name);
                }
                else
                {
                    _initError = "当前系统未安装中文语音包，暂时无法朗读。可在「设置 → 时间和语言 → 语音」中添加中文语音。";
                }

                _synth = synth;
            }
            catch (Exception ex)
            {
                _initError = "语音功能不可用：" + ex.Message;
                _synth = null;
            }
        }
    }

    #endregion 初始化

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
                _synth?.SpeakAsyncCancelAll();
                _synth?.Dispose();
            }
            catch
            {
                // 释放失败无需处理
            }
            _synth = null;
        }

        GC.SuppressFinalize(this);
    }

    #endregion 释放
}
