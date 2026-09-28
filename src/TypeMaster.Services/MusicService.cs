using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TypeMaster.Core;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 背景音乐服务实现：基于 NAudio，支持 MP3 循环、音量调节与淡入淡出。
///
/// 设计要点：
/// 1) 与音效共享同一音频引擎（<see cref="AudioEngine"/>），确保两者能真正叠加播放；
/// 2) 同一曲目重复 Play 不会重启，避免每次切回页面都从头播放；
/// 3) 音量由 <see cref="AppState.Current"/> 的配置驱动，设置变更后实时生效；
/// 4) 音乐文件缺失、解码失败、无音频设备等一律静默降级，不影响游戏。
/// </summary>
public class MusicService : IMusicService, IDisposable
{
    #region 局部变量属性

    private readonly object _gate = new();

    private LoopSampleProvider? _loop;
    private AudioFileLoopProvider? _file;

    private string _currentId = string.Empty;
    private bool _disposed;

    /// <summary>淡入淡出定时器（后台线程，间隔 30ms）。用 Timer 而非 DispatcherTimer，
    /// 以便服务层完全不依赖 WPF。</summary>
    private Timer? _fadeTimer;
    private float _fadeFrom;
    private float _fadeTo;
    private int _fadeElapsedMs;
    private int _fadeDurationMs;

    /// <summary>淡入淡出过程中的互斥锁（Timer 回调在后台线程执行）。</summary>
    private readonly object _fadeGate = new();

    /// <summary>目标音乐音量（0~1），淡入结束后即为此值。</summary>
    private float _targetVolume;

    #endregion 局部变量属性

    #region 属性

    public bool IsPlaying
    {
        get
        {
            lock (_gate)
            {
                return _loop is not null && _targetVolume > 0f;
            }
        }
    }

    public string CurrentTrackId
    {
        get
        {
            lock (_gate)
            {
                return _currentId;
            }
        }
    }

    #endregion 属性

    #region 播放

    public bool Play(string trackId)
    {
        if (_disposed || string.IsNullOrWhiteSpace(trackId))
        {
            return false;
        }

        // 音乐开关关闭时不播放
        if (!AppState.Current.EnableMusic)
        {
            return false;
        }

        lock (_gate)
        {
            // 已是当前曲目：只确保音量正确，不重启（避免重复进页面时反复从头播放）
            if (string.Equals(_currentId, trackId, StringComparison.OrdinalIgnoreCase) && _loop is not null)
            {
                _targetVolume = TargetVolume();
                AudioEngine.Instance.SetMusicVolume(_targetVolume);
                return true;
            }
        }

        return SwitchTo(trackId, fadeIn: true);
    }

    public void Stop()
    {
        lock (_gate)
        {
            DetachLocked();
            _currentId = string.Empty;
            _targetVolume = 0f;
        }
        AudioEngine.Instance.SetMusicInput(null, 0f);
    }

    public void ApplySettings()
    {
        if (_disposed)
        {
            return;
        }

        // 关闭开关 → 立即停止
        if (!AppState.Current.EnableMusic)
        {
            Stop();
            return;
        }

        lock (_gate)
        {
            if (_loop is null)
            {
                return;
            }
            _targetVolume = TargetVolume();
        }
        AudioEngine.Instance.SetMusicVolume(TargetVolume());
    }

    #endregion 播放

    #region 内部实现

    /// <summary>当前应使用的音乐音量（0~1）。</summary>
    private static float TargetVolume()
    {
        int v = AppState.Current.MusicVolume;
        if (v < 0) v = 0;
        if (v > 100) v = 100;
        return v / 100f * 0.7f;   // 上限压到 0.7：BGM 不应盖过音效与提示
    }

    /// <summary>
    /// 切换到指定曲目：淡出旧曲 → 加载新曲 → 淡入。
    /// </summary>
    /// <param name="trackId">曲目标识</param>
    /// <param name="fadeIn">是否淡入</param>
    private bool SwitchTo(string trackId, bool fadeIn)
    {
        string? path = ResolvePath(trackId);
        if (path is null)
        {
            return false;
        }

        LoopSampleProvider? newLoop = null;
        AudioFileLoopProvider? newFile = null;
        try
        {
            newFile = new AudioFileLoopProvider(path);
            newLoop = new LoopSampleProvider(newFile);
        }
        catch
        {
            newFile?.Dispose();
            return false;
        }

        float target = TargetVolume();

        lock (_gate)
        {
            DetachLocked();
            _file = newFile;
            _loop = newLoop;
            _currentId = trackId;
            _targetVolume = target;
        }

        // 淡入：起始音量为 0，逐渐升到目标值
        float start = fadeIn ? 0f : target;
        if (!AudioEngine.Instance.SetMusicInput(newLoop, start))
        {
            // 设备不可用：保留状态但不会有声音，避免反复重试
            return false;
        }

        if (fadeIn && target > 0f)
        {
            StartFade(start, target, 420);
        }
        return true;
    }

    /// <summary>解出曲目的绝对路径，找不到文件返回 null。</summary>
    /// 音乐以 EmbeddedResource 打进程序集（保证单文件发布不丢资源），
    /// 解出后写成临时文件供 NAudio 读取（NAudio 需要文件路径）。</summary>
    private static string? ResolvePath(string trackId)
    {
        string name = MusicLibrary.FileNameOf(trackId);
        if (name.Length == 0)
        {
            return null;
        }

        // 优先：程序集内嵌资源（单文件发布下的唯一可靠来源）
        string? cached = ExtractEmbedded(name);
        if (cached is not null)
        {
            return cached;
        }

        // 兑底：输出目录旁的散落文件（便于开发期直接替换音乐试听）
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Music", name),
            Path.Combine(AppContext.BaseDirectory, "Music", name),
        };
        foreach (string p in candidates)
        {
            if (File.Exists(p))
            {
                return p;
            }
        }
        return null;
    }

    /// <summary>已释放出的内嵌音乐缓存（文件名 -> 临时文件路径）。</summary>
    private static readonly Dictionary<string, string> ExtractedCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object ExtractGate = new();

    /// <summary>
    /// 从程序集内嵌资源中释放指定音乐到临时目录，返回路径；不存在时返回 null。
    /// 同一首只释放一次，后续复用。
    /// </summary>
    private static string? ExtractEmbedded(string fileName)
    {
        lock (ExtractGate)
        {
            if (ExtractedCache.TryGetValue(fileName, out string? hit) && File.Exists(hit))
            {
                return hit;
            }

            try
            {
                // 音乐以内嵌资源形式打入 App 程序集（见 TypeMaster.App.csproj），
                // 而本服务位于 Services 程序集，因此两个程序集都要查一遍。
                var assemblies = new[]
                {
                    System.Reflection.Assembly.GetEntryAssembly(),
                    typeof(MusicService).Assembly,
                };

                // 资源名形如 TypeMaster.App.Assets.Music.lobby.mp3
                string suffix = ".Assets.Music." + fileName;
                string? resName = null;
                System.Reflection.Assembly? owner = null;

                foreach (var asm in assemblies)
                {
                    if (asm is null)
                    {
                        continue;
                    }
                    resName = asm.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                    if (resName is not null)
                    {
                        owner = asm;
                        break;
                    }
                }

                if (resName is null || owner is null)
                {
                    return null;
                }

                string dir = Path.Combine(Path.GetTempPath(), "TypeMaster", "Music");
                Directory.CreateDirectory(dir);
                string outPath = Path.Combine(dir, fileName);

                using (var src = owner.GetManifestResourceStream(resName))
                {
                    if (src is null)
                    {
                        return null;
                    }
                    using var dst = File.Create(outPath);
                    src.CopyTo(dst);
                }

                ExtractedCache[fileName] = outPath;
                return outPath;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>解除当前曲目的挂接并释放资源（必须在持锁状态下调用）。</summary>
    private void DetachLocked()
    {
        _loop = null;
        try { _file?.Dispose(); } catch { /* 忽略 */ }
        _file = null;
    }

    #endregion 内部实现

    #region 淡入淡出

    /// <summary>
    /// 在指定时长内把音量从 from 平滑过渡到 to。
    /// 使用后台 <see cref="Timer"/>，避免服务层依赖 WPF 的 DispatcherTimer。
    /// </summary>
    /// <param name="from">起始音量</param>
    /// <param name="to">目标音量</param>
    /// <param name="durationMs">过渡时长（毫秒）</param>
    private void StartFade(float from, float to, int durationMs)
    {
        lock (_fadeGate)
        {
            _fadeFrom = from;
            _fadeTo = to;
            _fadeElapsedMs = 0;
            _fadeDurationMs = Math.Max(1, durationMs);

            _fadeTimer ??= new Timer(OnFadeTick, null, Timeout.Infinite, Timeout.Infinite);
            _fadeTimer.Change(0, 30);
        }
    }

    private void OnFadeTick(object? state)
    {
        float target;
        float v;
        lock (_fadeGate)
        {
            _fadeElapsedMs += 30;
            float t = Math.Min(1f, (float)_fadeElapsedMs / _fadeDurationMs);
            v = _fadeFrom + (_fadeTo - _fadeFrom) * t;
            target = _fadeTo;
            if (t >= 1f)
            {
                _fadeTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        AudioEngine.Instance.SetMusicVolume(v);

        if (v == target)
        {
            AudioEngine.Instance.SetMusicVolume(target);
        }
    }

    #endregion 淡入淡出

    #region 释放

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        try { _fadeTimer?.Dispose(); } catch { /* 忽略 */ }

        Stop();
        AudioEngine.Instance.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion 释放
}
