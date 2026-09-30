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

    /// <summary>生死时速：每完整消灭一个单词，玩家选手前进的距离（像素）。</summary>
    private const double PlayerAdvancePx = 55;

    /// <summary>
    /// 单个游戏目标在界面上的实际高度（像素）。
    ///
    /// 为什么要在这里写死一个常量：目标模板由 GamePlayPage.xaml 的
    /// ItemsControl.ItemTemplate 定义，内容是「72px 精灵 + 约 37px 单词条」
    /// 再加少量外边距，合计约 109px。
    /// 引擎在计算"目标该出现在哪个 Y"时必须知道这个高度，否则会把
    /// 单词条算到可视区之外。
    ///
    /// 历史缺陷：青蛙吃虫原本用 <c>Y = Height - 70</c>，
    /// 于是目标底部落在 <c>Height + 39</c>，单词条被 PlayArea 的
    /// ClipToBounds 整条裁掉——表现为"只见虫子、不见单词"。
    /// </summary>
    private const double TargetVisualHeight = 109;

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
    public double Width { get; set; } = 800;
    public double Height { get; set; } = 500;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Combo { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsGameOver { get; private set; }

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
        IsRunning = true;
    }

    public void Stop() => IsRunning = false;

    /// <summary>每帧推进：dtSeconds 为距上一帧的秒数</summary>
    public void Tick(double dtSeconds)
    {
        if (!IsRunning) return;
        double dms = dtSeconds * 1000;

        // 生死时速：电脑选手按本局速度匀速前进，玩家每消灭一个单词前进一格
        #region 生死时速：选手推进与胜负判定

        if (Mode == GameMode.LifeDeathSpeed)
        {
            RivalX += _speedPx * dtSeconds;
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
                GameMode.SpaceWar => t.Y >= Height - 50,
                GameMode.CatchThief => t.X < -60 || t.X > Width + 60,
                // 危险判定：目标横向接近青蛙所在的中线。
                // 青蛙画在游戏区底部中央，目标也从底部中央穿过，
                // 因此只需横向距离判定即可（纵向已被上面的 Y 计算固定在底部带）。
                GameMode.FrogBug => Math.Abs(t.X - Width / 2) <= 40,
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
                    PlayerX += PlayerAdvancePx;
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

            (GameMode.WhackMole, Difficulty.Easy) => (8, 3000, 2, 0, 5200),
            (GameMode.WhackMole, Difficulty.Normal) => (7, 2400, 3, 0, 4400),
            (GameMode.WhackMole, Difficulty.Hard) => (6, 1900, 4, 0, 3600),

            (GameMode.CatchThief, Difficulty.Easy) => (8, 4600, 1, 26, 0),
            (GameMode.CatchThief, Difficulty.Normal) => (7, 3700, 2, 40, 0),
            (GameMode.CatchThief, Difficulty.Hard) => (6, 2900, 3, 56, 0),

            (GameMode.FrogBug, Difficulty.Easy) => (8, 4000, 1, 14, 0),
            (GameMode.FrogBug, Difficulty.Normal) => (7, 3200, 2, 21, 0),
            (GameMode.FrogBug, Difficulty.Hard) => (6, 2500, 3, 31, 0),

            // 打气球：气球从底部升起，速度 / 数量随难度递增
            (GameMode.BalloonPop, Difficulty.Easy) => (8, 4200, 2, 18, 0),
            (GameMode.BalloonPop, Difficulty.Normal) => (7, 3400, 3, 26, 0),
            (GameMode.BalloonPop, Difficulty.Hard) => (6, 2700, 4, 36, 0),

            // 生死时速：speed 为电脑选手前进速度；刷怪间隔即"下一个单词"出现节奏
            (GameMode.LifeDeathSpeed, Difficulty.Easy) => (3, 700, 1, 52, 0),
            (GameMode.LifeDeathSpeed, Difficulty.Normal) => (3, 550, 1, 78, 0),
            (GameMode.LifeDeathSpeed, Difficulty.Hard) => (3, 420, 1, 112, 0),

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
                t.X = 40 + _rand.NextDouble() * Math.Max(1, Width - 160);
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
                t.X = left ? -40 : Width + 40;
                t.Y = 40 + _rand.NextDouble() * (Height * 0.55);
                t.Vx = (left ? 1 : -1) * _speedPx;
                t.Vy = 0;
                break;

            case GameMode.FrogBug:
                bool l2 = _rand.Next(2) == 0;
                t.X = l2 ? -40 : Width + 40;
                // 让【整个目标】完整落在游戏区内：底部对齐到"高度 - 余量"，
                // 顶部再减去自身高度。这样单词条不会被裁掉。
                t.Y = Height - TargetVisualHeight - TargetBottomMargin;
                t.Vx = (l2 ? 1 : -1) * _speedPx;
                t.Vy = 0;
                break;

            case GameMode.BalloonPop:
                t.X = 40 + _rand.NextDouble() * Math.Max(1, Width - 160);
                t.Y = Height + 30;
                t.Vx = 0;
                t.Vy = -_speedPx;
                break;

            case GameMode.LifeDeathSpeed:
                // 竞速模式：单词固定显示在赛道中上方的"当前单词位"，不移动
                t.X = Width / 2 - 65;
                t.Y = Height * 0.32;
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
        double x = colW * ci + colW / 2 - 60;
        double y = Height * (0.20 + ri * 0.28);
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
        // 困难档（11~13 级）：不再限制
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

    private void EndGame()
    {
        IsRunning = false;
        IsGameOver = true;
        GameOver?.Invoke(this, new GameOverEventArgs { Score = Score, MaxCombo = _maxCombo });
    }
}
