using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>打字输入反馈结果</summary>
public enum FeedResult
{
    None,
    Locked,        // 锁定了一个新目标
    Progress,      // 在当前目标上推进了一个字符
    WordCompleted, // 完整输入一个单词
    WrongKey,      // 已锁定目标但输入字符不匹配（不推进）
    NoMatch        // 没有任何目标以该字符开头
}

/// <summary>游戏结束事件参数</summary>
public class GameOverEventArgs : EventArgs
{
    public int Score { get; init; }
    public int MaxCombo { get; init; }

    /// <summary>是否因本局时间耗尽而结束（否则是生命耗尽 / 竞速分出胜负）。</summary>
    public bool TimedOut { get; init; }
}

/// <summary>完整输入一个单词时的事件参数（携带位置，供界面播放爆裂特效）</summary>
public class WordCompletedEventArgs : EventArgs
{
    public double X { get; init; }
    public double Y { get; init; }
    public int Combo { get; init; }
    public int Gained { get; init; }
}

/// <summary>
/// 打字小游戏核心引擎（与 UI 无关的纯逻辑）。
/// 由页面使用 DispatcherTimer 以约 30fps 调用 <see cref="Tick"/> 驱动；
/// Targets 集合会在 UI 线程上被修改（页面在 UI 线程调用本类方法）。
/// </summary>
public class TypingGameEngine
{
    private readonly Random _rand = new();
    private readonly List<string> _activeWords = new();
    private GameTarget? _locked;
    private string _buffer = string.Empty;
    private double _spawnAccumMs;
    private int _nextId = 1;
    private int _maxCombo;
    private int _level = DifficultyScale.Min;

    /// <summary>
    /// 生死时速：玩家每完整消灭一个单词前进的距离（像素），由 <see cref="ComputeRaceMetrics"/> 按赛道宽度折算。
    ///
    /// 历史缺陷：曾是固定 55px——赛道宽度用的是游戏区实际像素宽，
    /// 窗口一宽（最大化 / 大屏）玩家要打的单词数就线性变多，而电脑选手
    /// 速度不变、照样几十秒跑完，结果"打得很好仍然惜败"，一局还突然结束。
    /// 现改为按"固定单词数到终点"折算步长，与窗口宽度彻底解耦。
    /// </summary>
    private double _playerAdvancePx;

    /// <summary>生死时速：电脑选手每秒前进距离，由 <see cref="ComputeRaceMetrics"/> 按难度时限折算。</summary>
    private double _rivalSpeedPx;

    /// <summary>
    /// 单个游戏目标在界面上的实际高度（像素）。
    ///
    /// 为什么要在这里写死一个常量：目标模板由 GamePlayPage.xaml 的
    /// ItemsControl.ItemTemplate 定义，内容是「72px 精灵 + 约 39px 单词条」
    /// 再加少量外边距，合计约 120px。
    /// 引擎在计算"目标该出现在哪个 Y"时必须知道这个高度，否则会把
    /// 单词条算到可视区之外。
    ///
    /// 历史缺陷：青蛙吃虫原本用 <c>Y = Height - 70</c>，
    /// 于是目标底部落在 <c>Height + 39</c>，单词条被 PlayArea 的
    /// ClipToBounds 整条裁掉——表现为"只见虫子、不见单词"。
    /// </summary>
    private const double TargetVisualHeight = 120;

    /// <summary>
    /// 单个游戏目标在界面上的实际宽度（像素）。
    ///
    /// 与 GamePlayPage.xaml 目标模板里 Grid 的 Width 保持一致：
    /// 单词条为容纳最长 18 字母的单词（23 号字）已加宽到 230。
    /// 引擎按它计算目标 X 的取值范围与"到达/越界"判定，否则
    /// 右侧生成的目标会带着单词条伸出游戏区右缘，被 PlayArea 的
    /// ClipToBounds 裁掉——表现为"部分单词被遮挡"。
    /// </summary>
    private const double TargetVisualWidth = 230;

    /// <summary>目标与游戏区底部之间保留的余量，避免贴边或被圆角切到。</summary>
    private const double TargetBottomMargin = 6;

    // 当前局参数（由 Configure 填充）
    private int _lives;
    private double _spawnIntervalMs;
    private int _maxTargets;
    private double _speedPx;
    private double _moleLifetime;

    public ObservableCollection<GameTarget> Targets { get; } = new();

    public GameMode Mode { get; private set; }
    public Difficulty Difficulty { get; private set; } = Difficulty.Normal;

    /// <summary>难度细分等级（1~10），在所属档位内微调本局节奏。</summary>
    public int Level => _level;

    /// <summary>游戏区域尺寸（由页面在 Loaded / SizeChanged 时设置）</summary>
    private double _width = 800;

    /// <summary>游戏区域宽度（由页面在 Loaded / SizeChanged 时设置）。</summary>
    public double Width
    {
        get => _width;
        set
        {
            _width = value;
            // 竞速进行中窗口宽度变化：按新赛道宽度重算步长与电脑速度，
            // 保持"玩家单词数到终点 / 电脑用时"两个约定不随窗口尺寸漂移
            if (IsRunning && Mode == GameMode.LifeDeathSpeed)
            {
                ComputeRaceMetrics();
            }
        }
    }

    public double Height { get; set; } = 500;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Combo { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsGameOver { get; private set; }

    /// <summary>
    /// 本局剩余时间（秒）。一局共有三种结束方式：生命耗尽、竞速分出胜负、时间耗尽——
    /// 时间上限保证"高手不死也能打完一局"，一局时长可控（用户反馈此前一局太长）。
    /// </summary>
    public double TimeLeftSeconds { get; private set; }

    /// <summary>生死时速：玩家选手的横向位置（像素），供界面渲染赛道位置。</summary>
    public double PlayerX { get; private set; }

    /// <summary>生死时速：电脑选手的横向位置（像素），供界面渲染赛道位置。</summary>
    public double RivalX { get; private set; }

    /// <summary>生死时速：本局玩家是否获胜（ true=玩家先到终点，false=电脑先到或生命耗尽）。</summary>
    public bool PlayerWon { get; private set; }

    /// <summary>当前已输入（锁定目标的前缀），供输入框回显</summary>
    public string Buffer => _buffer;

    public event EventHandler<GameOverEventArgs>? GameOver;
    public event EventHandler? LifeLost;
    public event EventHandler<WordCompletedEventArgs>? WordCompleted;
    public event EventHandler? WrongKey;

    /// <summary>锁定了一个目标（界面可播放"发射"音效）。</summary>
    public event EventHandler? TargetLocked;

    /// <summary>新目标出现（界面可按模式播放冒头 / 出现音效）。</summary>
    public event EventHandler? TargetSpawned;

    // ---- 配置 ----

    public void Configure(GameMode mode, Difficulty difficulty)
    {
        Mode = mode;
        Difficulty = difficulty;
        _level = DifficultyScale.FromDifficulty(difficulty);
        LoadParams();
        Lives = _lives;
        IsRunning = false;
        IsGameOver = false;
    }

    /// <summary>
    /// 按游戏模式 + 三档难度 + 细分等级（1~10）配置本局参数。
    /// 细分等级在所属档位内线性微调刷怪间隔、下落速度与地鼠停留时长。
    /// </summary>
    /// <param name="mode">游戏模式</param>
    /// <param name="difficulty">三档难度</param>
    /// <param name="level">细分等级（1~10）</param>
    public void Configure(GameMode mode, Difficulty difficulty, int level)
    {
        Mode = mode;
        Difficulty = difficulty;
        _level = DifficultyScale.Clamp(level);
        LoadParams();
        Lives = _lives;
        IsRunning = false;
        IsGameOver = false;
    }

    public void Start()
    {
        Score = 0;
        Combo = 0;
        _maxCombo = 0;
        Lives = _lives;
        Targets.Clear();
        _activeWords.Clear();
        _locked = null;
        _buffer = string.Empty;
        _spawnAccumMs = 0;
        _nextId = 1;
        PlayerX = 20;
        RivalX = 20;
        PlayerWon = false;
        IsGameOver = false;
        TimeLeftSeconds = RoundSeconds;
        // 竞速模式的步长 / 电脑速度依赖赛道宽度，开局时折算一次
        if (Mode == GameMode.LifeDeathSpeed)
        {
            ComputeRaceMetrics();
        }
        IsRunning = true;
    }

    public void Stop() => IsRunning = false;

    /// <summary>
    /// 折算生死时速的竞速参数：玩家完整打满 <see cref="RaceWordsToFinish"/> 个单词到终点，
    /// 电脑选手用 <see cref="RaceRivalSeconds"/> 秒跑完全程。
    /// 两个量都按"赛道实际宽度"折算成像素，窗口再宽，需要打的单词数也不变。
    /// </summary>
    private void ComputeRaceMetrics()
    {
        double distance = Math.Max(200, Width - 60);
        _playerAdvancePx = distance / RaceWordsToFinish;
        _rivalSpeedPx = distance / RaceRivalSeconds;
    }

    /// <summary>生死时速：玩家到终点需要完整输入的单词数（按难度递增，与窗口宽度无关）。</summary>
    private int RaceWordsToFinish => Difficulty switch
    {
        // 入门档：12 个短词，允许平均每个词 5 秒（60 秒时限内电脑才跑完），刚学的孩子努力一下能赢
        Difficulty.Entry => 12,
        Difficulty.Easy => 15,
        Difficulty.Normal => 18,
        Difficulty.Hard => 20,
        _ => 24
    };

    /// <summary>生死时速：电脑选手跑完全程的秒数（玩家节奏慢于它即落败），随难度收紧。</summary>
    private double RaceRivalSeconds => Difficulty switch
    {
        Difficulty.Entry => 60,
        Difficulty.Easy => 55,
        Difficulty.Normal => 50,
        Difficulty.Hard => 45,
        _ => 40
    };

    /// <summary>
    /// 本局时限（秒）：入门 120 / 简单 105 / 普通 90 / 困难 80 / 地狱 70。
    ///
    /// 为什么随难度递减：低龄玩家打得慢，给足时间才能攒到分数、玩得尽兴；
    /// 高档位玩家手速快，更短的一局反而更紧凑刺激。竞速模式由终点线
    /// 自然收尾（电脑选手最快二十来秒即到），时限只作兜底。
    /// </summary>
    private int RoundSeconds => Difficulty switch
    {
        Difficulty.Entry => 120,
        Difficulty.Easy => 105,
        Difficulty.Normal => 90,
        Difficulty.Hard => 80,
        _ => 70
    };

    /// <summary>每帧推进：dtSeconds 为距上一帧的秒数</summary>
    public void Tick(double dtSeconds)
    {
        if (!IsRunning) return;
        double dms = dtSeconds * 1000;

        // 本局倒计时：时间耗尽同样结束一局
        TimeLeftSeconds -= dtSeconds;
        if (TimeLeftSeconds <= 0)
        {
            TimeLeftSeconds = 0;
            EndGame(timedOut: true);
            return;
        }

        // 生死时速：电脑选手按本局速度匀速前进，玩家每消灭一个单词前进一格
        #region 生死时速：选手推进与胜负判定

        if (Mode == GameMode.LifeDeathSpeed)
        {
            RivalX += _rivalSpeedPx * dtSeconds;
            double finish = Width - 40;
            if (RivalX >= finish)
            {
                PlayerWon = false;
                EndGame();
                return;
            }
            if (PlayerX >= finish)
            {
                PlayerWon = true;
                EndGame();
                return;
            }
        }

        #endregion 生死时速：选手推进与胜负判定

        foreach (var t in Targets.ToList())
        {
            t.AgeMs += dms;
            t.X += t.Vx * dtSeconds;
            t.Y += t.Vy * dtSeconds;

            bool danger = Mode switch
            {
                // 落地线：目标【精灵】触及地面即出局（旧值 Height - 50 不含单词条高度，
                // 最后约 70px 的下落里单词逐渐沉入底边被裁，而玩家还要照着它输入）。
                // 换算后精灵底部恰好落在 Height - 50 的地面线上，单词条完整可见。
                GameMode.SpaceWar => t.Y >= Height - TargetVisualHeight - TargetBottomMargin,
                // 小偷逃逸判定按"整个目标完全离开游戏区"计算（t.X 是目标左缘）
                GameMode.CatchThief => t.X < -TargetVisualWidth - 20 || t.X > Width + 20,
                // 危险判定：目标【中心】横向接近青蛙所在的中线。
                // 青蛙画在游戏区底部中央，目标也从底部中央穿过，
                // 因此只需横向距离判定即可（纵向已被上面的 Y 计算固定在底部带）。
                // 注意 t.X 是目标左缘，需先换算到中心（+ TargetVisualWidth / 2），
                // 否则虫子会明显跑过青蛙才被判"到达"。
                GameMode.FrogBug => Math.Abs(t.X + TargetVisualWidth / 2 - Width / 2) <= 40,
                GameMode.WhackMole => t.AgeMs >= t.LifetimeMs,
                GameMode.BalloonPop => t.Y <= -60,   // 气球飘出顶部
                _ => false
            };

            if (danger) RemoveTarget(t, penalize: Mode != GameMode.WhackMole);
        }

        _spawnAccumMs += dms;
        if (_spawnAccumMs >= _spawnIntervalMs && Targets.Count < _maxTargets)
        {
            _spawnAccumMs = 0;
            Spawn();
        }
    }

    /// <summary>喂入一个字符（英文小写匹配）</summary>
    public FeedResult Feed(char c)
    {
        if (!IsRunning || IsGameOver) return FeedResult.None;
        char ch = char.ToLowerInvariant(c);

        if (_locked == null)
        {
            var candidates = Targets
                .Where(t => t.Word.Length > 0 && char.ToLowerInvariant(t.Word[0]) == ch)
                .ToList();
            if (candidates.Count == 0)
            {
                WrongKey?.Invoke(this, EventArgs.Empty);
                return FeedResult.NoMatch;
            }

            var target = candidates.OrderBy(DistanceToDanger).First();
            _locked = target;
            target.IsLocked = true;
            target.MatchedLength = 1;
            _buffer = ch.ToString();
            TargetLocked?.Invoke(this, EventArgs.Empty);
            return FeedResult.Locked;
        }

        char expected = char.ToLowerInvariant(_locked.Word[_locked.MatchedLength]);
        if (expected == ch)
        {
            _locked.MatchedLength++;
            _buffer += ch;
            if (_locked.MatchedLength >= _locked.Word.Length)
            {
                double bx = _locked.X, by = _locked.Y;
                int gained = (int)(_locked.Word.Length * 10 * (1 + Combo * 0.1));
                Score += gained;
                Combo++;
                if (Combo > _maxCombo) _maxCombo = Combo;

                // 生死时速：每完整输入一个单词，玩家选手向终点推进一格
                if (Mode == GameMode.LifeDeathSpeed)
                {
                    PlayerX += _playerAdvancePx;
                }

                _locked.IsLocked = false;
                Targets.Remove(_locked);
                _activeWords.Remove(_locked.Word);
                _locked = null;
                _buffer = string.Empty;
                WordCompleted?.Invoke(this, new WordCompletedEventArgs
                {
                    X = bx,
                    Y = by,
                    Combo = Combo,
                    Gained = gained
                });
                return FeedResult.WordCompleted;
            }
            return FeedResult.Progress;
        }

        WrongKey?.Invoke(this, EventArgs.Empty);
        return FeedResult.WrongKey;
    }

    /// <summary>清空当前锁定（输入回退 / 目标逃逸时调用）</summary>
    public void ResetLock()
    {
        if (_locked != null)
        {
            _locked.IsLocked = false;
            _locked.MatchedLength = 0;
            _locked = null;
        }
        _buffer = string.Empty;
    }

    // ---- 内部 ----

    private void LoadParams()
    {
        // 面向刚接触电脑的三四年级小朋友：速度很慢、刷怪很稀、同屏很少、生命很多。
        (int lives, double spawn, int max, double speed, double mole) p = (Mode, Difficulty) switch
        {
            // ---------- 入门档：面向刚学键盘的孩子，节奏最慢、生命最多 ----------
            (GameMode.SpaceWar, Difficulty.Entry) => (10, 5600, 1, 9, 0),
            (GameMode.WhackMole, Difficulty.Entry) => (10, 4200, 1, 0, 7000),
            (GameMode.CatchThief, Difficulty.Entry) => (10, 6000, 1, 18, 0),
            (GameMode.FrogBug, Difficulty.Entry) => (10, 5600, 1, 10, 0),
            (GameMode.BalloonPop, Difficulty.Entry) => (10, 5800, 1, 12, 0),
            (GameMode.LifeDeathSpeed, Difficulty.Entry) => (3, 1100, 1, 34, 0),

            (GameMode.SpaceWar, Difficulty.Easy) => (8, 4200, 2, 15, 0),
            (GameMode.SpaceWar, Difficulty.Normal) => (7, 3400, 3, 23, 0),
            (GameMode.SpaceWar, Difficulty.Hard) => (6, 2700, 4, 33, 0),
            (GameMode.SpaceWar, Difficulty.Hell) => (5, 2200, 5, 42, 0),

            (GameMode.WhackMole, Difficulty.Easy) => (8, 3000, 2, 0, 5200),
            (GameMode.WhackMole, Difficulty.Normal) => (7, 2400, 3, 0, 4400),
            (GameMode.WhackMole, Difficulty.Hard) => (6, 1900, 4, 0, 3600),
            (GameMode.WhackMole, Difficulty.Hell) => (5, 1500, 5, 0, 2800),

            (GameMode.CatchThief, Difficulty.Easy) => (8, 4600, 1, 26, 0),
            (GameMode.CatchThief, Difficulty.Normal) => (7, 3700, 2, 40, 0),
            (GameMode.CatchThief, Difficulty.Hard) => (6, 2900, 3, 56, 0),
            (GameMode.CatchThief, Difficulty.Hell) => (5, 2400, 4, 70, 0),

            (GameMode.FrogBug, Difficulty.Easy) => (8, 4000, 1, 14, 0),
            (GameMode.FrogBug, Difficulty.Normal) => (7, 3200, 2, 21, 0),
            (GameMode.FrogBug, Difficulty.Hard) => (6, 2500, 3, 31, 0),
            (GameMode.FrogBug, Difficulty.Hell) => (5, 2000, 4, 40, 0),

            // 打气球：气球从底部升起，速度 / 数量随难度递增
            (GameMode.BalloonPop, Difficulty.Easy) => (8, 4200, 2, 18, 0),
            (GameMode.BalloonPop, Difficulty.Normal) => (7, 3400, 3, 26, 0),
            (GameMode.BalloonPop, Difficulty.Hard) => (6, 2700, 4, 36, 0),
            (GameMode.BalloonPop, Difficulty.Hell) => (5, 2200, 5, 46, 0),

            // 生死时速：speed 字段已不再直接使用——电脑选手速度改由
            // ComputeRaceMetrics 按"跑完全程的秒数"折算（与赛道宽度解耦）；
            // spawn 保留为下一个单词的出现节奏
            (GameMode.LifeDeathSpeed, Difficulty.Easy) => (3, 700, 1, 52, 0),
            (GameMode.LifeDeathSpeed, Difficulty.Normal) => (3, 550, 1, 78, 0),
            (GameMode.LifeDeathSpeed, Difficulty.Hard) => (3, 420, 1, 112, 0),
            (GameMode.LifeDeathSpeed, Difficulty.Hell) => (3, 350, 1, 140, 0),

            _ => (8, 4000, 2, 22, 0),
        };

        // 细分等级微调：档位内等级越高，刷怪越密、速度越快、地鼠停留越短（幅度已收窄，避免儿童难度陡增）
        double tune = DifficultyScale.FineTune(_level);
        _lives = p.lives;
        _spawnIntervalMs = p.spawn * (1 - 0.14 * tune);
        _maxTargets = p.max;
        _speedPx = p.speed * (1 + 0.20 * tune);
        _moleLifetime = p.mole * (1 - 0.12 * tune);
    }

    private void Spawn()
    {
        var word = PickWord();
        _activeWords.Add(word);
        var t = new GameTarget { Id = _nextId++, Word = word, Glyph = GlyphFor(Mode) };

        switch (Mode)
        {
            case GameMode.SpaceWar:
                // 左右各留 40 边距，保证整个目标（含 230 宽单词条）完整落在游戏区内
                t.X = 40 + _rand.NextDouble() * Math.Max(1, Width - TargetVisualWidth - 80);
                t.Y = -30;
                t.Vx = 0;
                t.Vy = _speedPx;
                break;

            case GameMode.WhackMole:
                var (hx, hy) = PickHole();
                t.X = hx;
                t.Y = hy;
                t.Vx = 0;
                t.Vy = 0;
                t.LifetimeMs = _moleLifetime;
                break;

            case GameMode.CatchThief:
                bool left = _rand.Next(2) == 0;
                t.X = left ? -TargetVisualWidth + 40 : Width + 40;
                // 纵向带钳制：矮窗口下不许越过"完整容纳一个目标"的下限，
                // 否则小偷横向穿场期间单词条一直被下边缘裁掉
                t.Y = Math.Min(40 + _rand.NextDouble() * (Height * 0.55),
                               Height - TargetVisualHeight - TargetBottomMargin);
                t.Vx = (left ? 1 : -1) * _speedPx;
                t.Vy = 0;
                break;

            case GameMode.FrogBug:
                bool l2 = _rand.Next(2) == 0;
                // 与 CatchThief 一致：从"整个目标完全在屏外"的位置入场，
                // 两个方向的入场时机才对称
                t.X = l2 ? -TargetVisualWidth + 40 : Width + 40;
                // 让【整个目标】完整落在游戏区内：底部对齐到"高度 - 余量"，
                // 顶部再减去自身高度。这样单词条不会被裁掉。
                t.Y = Height - TargetVisualHeight - TargetBottomMargin;
                t.Vx = (l2 ? 1 : -1) * _speedPx;
                t.Vy = 0;
                break;

            case GameMode.BalloonPop:
                // 左右各留 40 边距，保证整个目标（含 230 宽单词条）完整落在游戏区内
                t.X = 40 + _rand.NextDouble() * Math.Max(1, Width - TargetVisualWidth - 80);
                // 出生即完整可见（底部对齐）：气球自下而上飘，若从屏幕下方整格升起，
                // 位于目标底部的单词条要等约 150px 才完全进入视野，
                // 期间单词一直半截被裁而玩家无法输入。入场感由模板的缩放动画补足。
                t.Y = Height - TargetVisualHeight - TargetBottomMargin;
                t.Vx = 0;
                t.Vy = -_speedPx;
                break;

            case GameMode.LifeDeathSpeed:
                // 竞速模式：单词固定显示在赛道中上方的"当前单词位"，不移动
                // （t.X 是目标左缘，减去半个占位让单词居中于赛道；矮窗口下同样钳制）
                t.X = Width / 2 - TargetVisualWidth / 2;
                t.Y = Math.Min(Height * 0.32, Height - TargetVisualHeight - TargetBottomMargin);
                t.Vx = 0;
                t.Vy = 0;
                break;
        }

        Targets.Add(t);
        TargetSpawned?.Invoke(this, EventArgs.Empty);
    }

    private (double x, double y) PickHole()
    {
        const int cols = 3, rows = 2;
        int ci = _rand.Next(cols);
        int ri = _rand.Next(rows);
        double colW = Width / cols;
        // 目标左缘 = 列中心 - 半个占位，让精灵与单词恰好落在洞口正上方
        double x = colW * ci + colW / 2 - TargetVisualWidth / 2;
        // 行位钳制：游戏区偏矮时，按比例算出的行位会让单词条沉到下边缘之外
        //（表现为地鼠的单词长期半截被裁），此时改为贴着"完整容纳一个目标"的下限
        double y = Math.Min(Height * (0.20 + ri * 0.28),
                            Height - TargetVisualHeight - TargetBottomMargin);
        return (x, y);
    }

    private GameGlyph GlyphFor(GameMode mode) => mode switch
    {
        GameMode.SpaceWar => GameGlyph.Ship,
        GameMode.WhackMole => GameGlyph.Mole,
        GameMode.CatchThief => GameGlyph.Thief,
        GameMode.FrogBug => GameGlyph.Bug,
        GameMode.BalloonPop => GameGlyph.Balloon,
        GameMode.LifeDeathSpeed => GameGlyph.Runner,
        _ => GameGlyph.Ship
    };

    /// <summary>
    /// 按细分等级限制单词最大长度：等级越低单词越短，方便低龄儿童跟打。
    /// </summary>
    private int MaxWordLength => _level switch
    {
        // 入门档（1~3 级）：单词更短，刚学键盘的孩子不必去找长词
        1 => 3,
        2 => 4,
        3 => 4,
        // 简单档（4~6 级）
        4 or 5 => 6,
        6 => 7,
        // 普通档（7~10 级）
        7 or 8 => 8,
        9 or 10 => 10,
        // 困难档（11~13 级）与地狱档（14~15 级）：不再限制
        _ => int.MaxValue
    };

    private string PickWord()
    {
        int maxLen = MaxWordLength;
        var pool = TextLibrary.GetGameWords(Difficulty).ToList();
        var candidates = pool.Where(w => w.Length <= maxLen && !_activeWords.Contains(w)).ToList();
        if (candidates.Count == 0) candidates = pool.Where(w => !_activeWords.Contains(w)).ToList();
        if (candidates.Count == 0) candidates = pool;
        return candidates[_rand.Next(candidates.Count)];
    }

    /// <summary>到危险线（被惩罚）的剩余距离，越小越紧急</summary>
    private double DistanceToDanger(GameTarget t)
    {
        return Mode switch
        {
            GameMode.SpaceWar => Height - (t.Y + 30),
            GameMode.WhackMole => t.LifetimeMs - t.AgeMs,
            GameMode.CatchThief => t.Vx > 0 ? (Width + 60 - t.X) : (t.X + 60),
            GameMode.FrogBug => Math.Abs(t.X - Width / 2),
            GameMode.BalloonPop => t.Y + 30,
            GameMode.LifeDeathSpeed => (Width - 40) - RivalX,
            _ => double.MaxValue
        };
    }

    private void RemoveTarget(GameTarget t, bool penalize)
    {
        Targets.Remove(t);
        _activeWords.Remove(t.Word);
        if (_locked == t)
        {
            t.IsLocked = false;
            _locked.MatchedLength = 0;
            _locked = null;
            _buffer = string.Empty;
        }

        if (!penalize) return;

        Lives--;
        Combo = 0;
        LifeLost?.Invoke(this, EventArgs.Empty);
        if (Lives <= 0) EndGame();
    }

    private void EndGame(bool timedOut = false)
    {
        IsRunning = false;
        IsGameOver = true;
        GameOver?.Invoke(this, new GameOverEventArgs { Score = Score, MaxCombo = _maxCombo, TimedOut = timedOut });
    }
}
