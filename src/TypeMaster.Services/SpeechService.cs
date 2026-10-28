using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using TypeMaster.Core;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 语音朗读服务实现：**双引擎**，全程离线、不联网。
///
/// <para>
/// <b>为什么是双引擎</b>：旧实现只用 System.Speech（SAPI 桌面引擎），
/// 只能枚举到 <c>Microsoft Huihui Desktop</c> 这类 Win7 时代的老语音——音质机械。
/// Win10/11 上更自然的语音（OneCore 康康/晓晓、Win11 可安装的"自然语音"真人级音色）
/// 被注册表隔离在 Speech_OneCore 下，SAPI 引擎根本看不到。
/// 因此改为：**优先 WinRT 语音合成**（Windows.Media.SpeechSynthesis，能看到全部新语音），
/// System.Speech 仅作兜底（WinRT 不可用时，如精简版系统）。
/// </para>
///
/// <para>
/// <b>WinRT 路径的声音播放</b>：合成结果是一个 WAV 流，
/// 用 NAudio（项目已有）的 WaveOutEvent 播放，独立于游戏音效的混音通道
/// （打字练习页没有 BGM，不存在设备抢占）。
/// </para>
///
/// <para>
/// <b>语音选择</b>：自动挑选中文语音，优先级为
/// 名字含 Natural（Win11 自然语音，真人级）&gt; 其他中文语音；
/// 找不到中文语音时降级 SAPI 再找一次，两处都没有则 <see cref="IsAvailable"/> 为 false。
/// </para>
///
/// <para>
/// 任何一步失败都静默降级——朗读只是辅助功能，绝不能影响主流程。
/// </para>
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public class SpeechService : ITextToSpeechService, IDisposable
{
    #region 局部变量属性

    private enum TtsEngine
    {
        None,
        WinRt,
        Sapi
    }

    private TtsEngine _engine = TtsEngine.None;

    /// <summary>WinRT 合成器（延迟创建并全程复用）。</summary>
    private global::Windows.Media.SpeechSynthesis.SpeechSynthesizer? _winRt;

    /// <summary>SAPI 合成器（兜底，延迟创建）。</summary>
    private System.Speech.Synthesis.SpeechSynthesizer? _sapi;

    /// <summary>
    /// WinRT 英文合成器（供打字游戏的英文鼓励语使用）。
    /// 与中文合成器分开实例，避免每次朗读都在中文/英文语音之间来回切换。
    /// </summary>
    private global::Windows.Media.SpeechSynthesis.SpeechSynthesizer? _winRtEn;

    /// <summary>SAPI 英文合成器（兜底路径的鼓励语用）。</summary>
    private System.Speech.Synthesis.SpeechSynthesizer? _sapiEn;

    /// <summary>SAPI 中文语音名（鼓励语切走后，下次中文朗读自动切回）。</summary>
    private string? _sapiZhVoiceName;

    /// <summary>WinRT 路径的播放设备（持久复用，避免频繁开关音频设备）。</summary>
    private WaveOutEvent? _waveOut;

    /// <summary>当前正在播放的 WAV（切换新 utterance 前释放旧实例）。</summary>
    private WaveStream? _currentReader;

    /// <summary>当前朗读任务的取消源（新朗读打断旧朗读）。</summary>
    private CancellationTokenSource? _synthCts;

    /// <summary>是否已尝试过初始化（失败后不再反复重试，避免每次都卡顿）。</summary>
    private bool _initAttempted;

    /// <summary>初始化失败原因（用于生成提示文案）。</summary>
    private string? _initError;

    /// <summary>并发保护：Speak / Stop 可能来自 UI 线程的不同事件。</summary>
    private readonly object _gate = new();

    private bool _disposed;

    /// <summary>当前引擎与语音的描述（诊断 / 验证用，不属于接口契约）。</summary>
    public string ActiveEngineDescription =>
        _engine switch
        {
            TtsEngine.WinRt => "WinRT（Windows.Media.SpeechSynthesis）：" + (_winRt?.Voice.DisplayName ?? "?"),
            TtsEngine.Sapi => "SAPI（System.Speech）：" + (_sapi?.Voice?.Name ?? "?"),
            _ => "无可用语音"
        };

    #endregion 局部变量属性

    #region 可用性

    public bool IsAvailable
    {
        get
        {
            EnsureInitialized();
            return _engine != TtsEngine.None;
        }
    }

    public string UnavailableHint =>
        _initError ?? "当前系统未安装中文语音包，暂时无法朗读。可在「设置 → 时间和语言 → 语音」中添加中文语音，"
        + "或在 Windows 11「设置 → 辅助功能 → 讲述人 → 添加自然语音」安装更自然的人声。";

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
        return _engine switch
        {
            TtsEngine.WinRt => SpeakWinRt(text, rateOffset),
            TtsEngine.Sapi => SpeakSapi(text, rateOffset),
            _ => false
        };
    }

    /// <summary>
    /// WinRT 路径：异步合成 WAV → NAudio 播放；新朗读会取消旧朗读（只听最新）。
    /// </summary>
    /// <param name="text">要朗读的文本</param>
    /// <param name="rateOffset">语速相对偏移（负数更慢）</param>
    /// <returns>任务已发起返回 true</returns>
    private bool SpeakWinRt(string text, int rateOffset, bool english = false)
    {
        try
        {
            lock (_gate)
            {
                var synth = english ? EnsureWinRtEn() : _winRt;
                if (_disposed || synth is null)
                {
                    return false;
                }

                // 打断上一次：取消未完成的合成 + 停止正在播放的音频
                _synthCts?.Cancel();
                _synthCts = new CancellationTokenSource();
                var cancelSource = _synthCts;
                var cancel = cancelSource.Token;

                // 语速：偏移 -5~5 映射到 0.6~1.4 倍速（1.0 为正常）
                synth.Options.SpeakingRate = (float)Math.Clamp(1.0 + rateOffset * 0.08, 0.5, 2.0);
                synth.Options.AudioPitch = 1.0f;

                var task = Task.Run(async () =>
                {
                    cancel.ThrowIfCancellationRequested();

                    using var winStream = await synth.SynthesizeTextToStreamAsync(text).AsTask(cancel);

                    // IRandomAccessStream → byte[]（用 DataReader，避免依赖互操作扩展方法）
                    var bytes = new byte[(int)winStream.Size];
                    using var streamReader = new Windows.Storage.Streams.DataReader(winStream);
                    await streamReader.LoadAsync((uint)bytes.Length).AsTask(cancel);
                    streamReader.ReadBytes(bytes);

                    cancel.ThrowIfCancellationRequested();

                    var wav = new MemoryStream(bytes);
                    var waveReader = new WaveFileReader(wav);
                    lock (_gate)
                    {
                        if (_disposed || cancel.IsCancellationRequested)
                        {
                            waveReader.Dispose();
                            return;
                        }

                        _currentReader?.Dispose();
                        _currentReader = waveReader;

                        _waveOut ??= new WaveOutEvent();
                        _waveOut.Stop();
                        _waveOut.Init(_currentReader);
                        _waveOut.Play();
                    }
                });

                return true;
            }
        }
        catch (Exception ex)
        {
            // 合成失败：静默降级，不影响主流程
            _initError = "语音播放失败：" + ex.Message;
            return false;
        }
    }

    /// <summary>SAPI 兜底路径：与旧实现一致（引擎内部直接出声）。</summary>
    /// <param name="text">要朗读的文本</param>
    /// <param name="rateOffset">语速相对偏移（负数更慢）</param>
    /// <returns>真正开始朗读返回 true</returns>
    private bool SpeakSapi(string text, int rateOffset)
    {
        if (_sapi is null)
        {
            return false;
        }

        try
        {
            lock (_gate)
            {
                if (_disposed || _sapi is null)
                {
                    return false;
                }

                // 防御：SAPI 中文朗读始终用初始化时选定的中文语音
                // （鼓励语走独立实例 _sapiEn，正常不会切走；此处仅为兜底）
                if (_sapiZhVoiceName is not null && _sapi.Voice?.Name != _sapiZhVoiceName)
                {
                    _sapi.SelectVoice(_sapiZhVoiceName);
                }

                // 语速：配置里是 -5~5 的档位，SAPI 的 Rate 正好也是 -10~10，直接映射。
                int rate = Math.Clamp(AppState.Current.SpeechRate + rateOffset, -5, 5);
                if (_sapi.Rate != rate)
                {
                    _sapi.Rate = rate;
                }

                // 打断上一段，保证「点哪个读哪个」，不排队
                _sapi.SpeakAsyncCancelAll();
                _sapi.SpeakAsync(text);
                return true;
            }
        }
        catch (Exception ex)
        {
            _initError = "语音播放失败：" + ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 说一句英文鼓励语：优先用系统里的**英文**语音来念（中文语音念英文很怪）。
    /// 与中文合成器分开实例，因此念完后的下一次中文朗读自动就是中文语音，互不串音。
    /// </summary>
    /// <param name="text">英文鼓励短语</param>
    /// <returns>任务已发起返回 true</returns>
    public bool SpeakPraise(string text) => SpeakPraise(text, 0);

    /// <summary>
    /// 以指定的相对语速说英文鼓励语。
    /// </summary>
    /// <param name="text">英文鼓励短语</param>
    /// <param name="rateOffset">语速相对偏移（负数更慢）</param>
    /// <returns>任务已发起返回 true</returns>
    public bool SpeakPraise(string text, int rateOffset)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!AppState.Current.EnableSpeech)
        {
            return false;
        }

        EnsureInitialized();
        return _engine switch
        {
            TtsEngine.WinRt => SpeakWinRt(text, rateOffset, english: true),
            TtsEngine.Sapi => SpeakSapiPraise(text, rateOffset),
            _ => false
        };
    }

    /// <summary>
    /// 取/建 WinRT 英文合成器：优先 Natural（自然音色）的英文语音。
    /// </summary>
    /// <returns>英文合成器；创建失败返回 null</returns>
    private global::Windows.Media.SpeechSynthesis.SpeechSynthesizer? EnsureWinRtEn()
    {
        if (_winRtEn is not null)
        {
            return _winRtEn;
        }

        try
        {
            var synth = new global::Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            var en = global::Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
                .OrderByDescending(v => v.DisplayName.Contains("Natural", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(v => v.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));

            if (en is null)
            {
                synth.Dispose();
                return null;
            }

            synth.Voice = en;
            _winRtEn = synth;
            return _winRtEn;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// SAPI 兜底的英文鼓励语：用独立的英文合成器实例（不动中文语音的选中态）。
    /// </summary>
    /// <param name="text">英文鼓励短语</param>
    /// <param name="rateOffset">语速相对偏移</param>
    /// <returns>真正开始朗读返回 true</returns>
    private bool SpeakSapiPraise(string text, int rateOffset)
    {
        try
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return false;
                }

                if (_sapiEn is null)
                {
                    var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                    var en = synth.GetInstalledVoices()
                        .Select(v => v.VoiceInfo)
                        .FirstOrDefault(i => i.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase));
                    if (en is null)
                    {
                        synth.Dispose();
                        // 没有英文语音：退化为中文语音念（音调可能怪，但比无声好）
                        return SpeakSapi(text, rateOffset);
                    }
                    synth.SelectVoice(en.Name);
                    _sapiEn = synth;
                }

                int rate = Math.Clamp(AppState.Current.SpeechRate + rateOffset, -5, 5);
                if (_sapiEn.Rate != rate)
                {
                    _sapiEn.Rate = rate;
                }

                _sapiEn.SpeakAsyncCancelAll();
                _sapiEn.SpeakAsync(text);
                return true;
            }
        }
        catch (Exception ex)
        {
            _initError = "语音播放失败：" + ex.Message;
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            lock (_gate)
            {
                _synthCts?.Cancel();
                if (_engine == TtsEngine.WinRt)
                {
                    _waveOut?.Stop();
                }
                else if (_engine == TtsEngine.Sapi)
                {
                    _sapi?.SpeakAsyncCancelAll();
                }
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
    /// 延迟初始化：优先 WinRT（能用到 OneCore / 自然语音），失败再降级 SAPI。
    /// 只尝试一次，成功后复用合成器。
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

            // ---------- 第一优先：WinRT ----------
            try
            {
                var synth = new global::Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                var zh = global::Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
                    .OrderByDescending(v => v.DisplayName.Contains("Natural", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(v => v.DisplayName.Contains("Online", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(v => v.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase));

                if (zh is not null)
                {
                    synth.Voice = zh;
                    _winRt = synth;
                    _engine = TtsEngine.WinRt;
                    return;
                }

                synth.Dispose();
                _initError = "Windows 语音组件里没有中文语音。";
            }
            catch (Exception ex)
            {
                _initError = "Windows 语音组件不可用：" + ex.Message;
            }

            // ---------- 兜底：SAPI ----------
            try
            {
                var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                var zh = synth.GetInstalledVoices()
                    .Select(v => v.VoiceInfo)
                    .FirstOrDefault(i => i.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase));

                if (zh is not null)
                {
                    synth.SelectVoice(zh.Name);
                    _sapiZhVoiceName = zh.Name;
                    _sapi = synth;
                    _engine = TtsEngine.Sapi;
                    return;
                }

                synth.Dispose();
                _initError = "当前系统未安装中文语音包，暂时无法朗读。可在「设置 → 时间和语言 → 语音」中添加中文语音，"
                    + "或在 Windows 11「设置 → 辅助功能 → 讲述人 → 添加自然语音」安装更自然的人声。";
            }
            catch (Exception ex)
            {
                _initError = "语音功能不可用：" + ex.Message;
            }

            _engine = TtsEngine.None;
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
                _synthCts?.Cancel();
                _currentReader?.Dispose();
                _waveOut?.Dispose();
                _winRt?.Dispose();
                _winRtEn?.Dispose();
                _sapi?.Dispose();
                _sapiEn?.Dispose();
            }
            catch
            {
                // 释放失败无需处理
            }
            _winRt = null;
            _winRtEn = null;
            _sapi = null;
            _sapiEn = null;
            _waveOut = null;
        }

        GC.SuppressFinalize(this);
    }

    #endregion 释放
}
