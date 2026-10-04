using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.ViewModels;

/// <summary>
/// 单个打字小游戏的视图模型：包装 <see cref="TypingGameEngine"/>，
/// 暴露分数 / 生命 / 连击 / 运行状态给界面，并负责音效与导航。
/// </summary>
public partial class GameViewModel : ObservableObject
{
    private readonly TypingGameEngine _engine;
    private readonly ISoundService _sound;
    private readonly ITextToSpeechService _speech;
    private readonly INavigationService _nav;
    private readonly FunTipService _fun;

    #region 局部变量属性

    private int _lastMilestone;
    private DateTime _lastErrorTip = DateTime.MinValue;
    private DateTime _lastPraise = DateTime.MinValue;
    private int _praiseIndex;

    /// <summary>
    /// 打对单词时轮换播报的英文鼓励语。
    ///
    /// 为什么不每次都播同一条：孩子对固定表扬很快麻木，轮换能持续给新鲜感；
    /// 为什么不播中文：英文短词更短促，不打断打字节奏，也更像游戏里的配音。
    /// </summary>
    private static readonly string[] PraiseWords =
        { "Good", "Nice", "Great", "Cool", "Awesome", "Well done", "Perfect", "Excellent", "Amazing", "You did it" };

    #endregion 局部变量属性

    #region 绑定属性

    /// <summary>界面层订阅此事件以播放粒子/抖动/彩屑等特效。</summary>
    public event EventHandler<GameEffectEventArgs>? EffectRequested;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _lives;
    [ObservableProperty] private int _combo;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private string _statusMessage = "按“开始游戏”进入战斗";

    /// <summary>本局剩余时间的展示文本，整秒变化时才刷新（避免每帧 40 次重绘）。</summary>
    [ObservableProperty] private string _timeLeftText = "--";

    [ObservableProperty] private Difficulty _selectedDifficulty = Difficulty.Normal;

    /// <summary>难度细分等级（1~10），与三档难度按钮双向联动。</summary>
    [ObservableProperty] private int _difficultyLevel = DifficultyScale.FromDifficulty(Difficulty.Normal);

    /// <summary>难度细分等级的中文描述，滑动条旁展示。</summary>
    [ObservableProperty] private string _difficultyLevelText = DifficultyScale.ToText(DifficultyScale.FromDifficulty(Difficulty.Normal));

    [ObservableProperty] private Brush _accentBrush = Brushes.Blue;

    #endregion 绑定属性

    public GameMode Mode { get; private set; }

    /// <summary>活动目标集合（与引擎共享同一引用，供 Canvas 绑定）</summary>
    public System.Collections.ObjectModel.ObservableCollection<GameTarget> Targets => _engine.Targets;

    /// <summary>当前已输入（锁定目标的前缀），供输入框回显</summary>
    public string CurrentBuffer => _engine.Buffer;

    /// <summary>生死时速：玩家选手的横向位置（像素），供界面渲染赛道。</summary>
    public double PlayerX => _engine.PlayerX;

    /// <summary>生死时速：电脑选手的横向位置（像素），供界面渲染赛道。</summary>
    public double RivalX => _engine.RivalX;

    /// <summary>生死时速：本局玩家是否获胜。</summary>
    public bool PlayerWon => _engine.PlayerWon;

    #region 构造函数

    public GameViewModel(TypingGameEngine engine, ISoundService sound, ITextToSpeechService speech, INavigationService nav, FunTipService fun)
    {
        _engine = engine;
        _sound = sound;
        _speech = speech;
        _nav = nav;
        _fun = fun;

        _engine.GameOver += (_, e) =>
        {
            IsGameOver = true;
            IsRunning = false;
            if (AppState.Current.EnableSound) _sound.PlayGameOver();
            // 结算文案：超时到点 / 生命耗尽 / 竞速胜负 三种收尾各有措辞
            if (Mode == GameMode.LifeDeathSpeed)
            {
                StatusMessage = _engine.PlayerWon
                    ? $"你赢啦！率先冲过终点，本局得分 {e.Score}，最高连击 {e.MaxCombo}"
                    : $"惜败！电脑选手先到了终点，本局得分 {e.Score}，最高连击 {e.MaxCombo}";
            }
            else if (e.TimedOut)
            {
                StatusMessage = $"时间到！本局得分 {e.Score}，最高连击 {e.MaxCombo}";
            }
            else
            {
                StatusMessage = $"游戏结束！本局得分 {e.Score}，最高连击 {e.MaxCombo}";
            }
            _fun.ShowGameOver();
            RaiseEffect(GameEffectKind.ScreenShake, value: 2);
            RaiseEffect(GameEffectKind.GameOver);
        };
        _engine.LifeLost += (_, _) =>
        {
            Lives = _engine.Lives;
            Combo = _engine.Combo;
            if (AppState.Current.EnableSound) _sound.PlayMiss();
            _fun.ShowGameMiss();
            RaiseEffect(GameEffectKind.ScreenShake, value: 2);
            RaiseEffect(GameEffectKind.LifeLost);
        };
        _engine.WordCompleted += (_, e) =>
        {
            Score = _engine.Score;
            Combo = _engine.Combo;
            if (AppState.Current.EnableSound)
            {
                _sound.PlayHit();
                // 按游戏模式补一句"角色动作音"，让画面与声音对上
                switch (Mode)
                {
                    case GameMode.FrogBug:
                        _sound.PlayEat();
                        break;
                    case GameMode.CatchThief:
                        _sound.PlayCatch();
                        break;
                }
            }
            PlayPraise();
            _fun.ShowWordCompleted();
            RaiseEffect(GameEffectKind.WordBurst, e.X, e.Y, e.Gained);
            if (e.Combo > 0 && e.Combo % 5 == 0 && e.Combo != _lastMilestone)
            {
                _lastMilestone = e.Combo;
                if (AppState.Current.EnableSound) _sound.PlayCombo(e.Combo);
                _fun.ShowCombo(e.Combo);
                RaiseEffect(GameEffectKind.ComboPulse, value: e.Combo);
            }
        };
        _engine.TargetLocked += (_, _) =>
        {
            if (AppState.Current.EnableSound) _sound.PlayShoot();
        };
        _engine.TargetSpawned += (_, _) =>
        {
            if (AppState.Current.EnableSound && Mode == GameMode.WhackMole) _sound.PlayPop();
        };
        _engine.WrongKey += (_, _) =>
        {
            if (AppState.Current.EnableSound) _sound.PlayError();
            // 敲错较频繁，限流避免提示语刷屏（同一条最多每 2.5 秒一次）
            if ((DateTime.Now - _lastErrorTip).TotalSeconds >= 2.5)
            {
                _lastErrorTip = DateTime.Now;
                _fun.ShowTypingError();
            }
            RaiseEffect(GameEffectKind.WrongKey);
        };
    }

    #endregion 构造函数

    #region 特效

    private void RaiseEffect(GameEffectKind kind, double x = 0, double y = 0, int value = 0)
        => EffectRequested?.Invoke(this, new GameEffectEventArgs { Kind = kind, X = x, Y = y, Value = value });

    /// <summary>
    /// 打对单词后说一句英文鼓励语（Good / Nice / Great…轮换）。
    ///
    /// 节流：至少间隔 2 秒才播——地狱档后期单词很短，高频连打时
    /// 句句都播会互相打断（TTS 是"取消上一段再说"），听起来像结巴。
    /// 语音开关（设置里的"语音朗读"）关闭时不出声。
    /// </summary>
    private void PlayPraise()
    {
        if (!AppState.Current.EnableSpeech)
        {
            return;
        }
        if ((DateTime.Now - _lastPraise).TotalSeconds < 2)
        {
            return;
        }
        _lastPraise = DateTime.Now;
        _praiseIndex = (_praiseIndex + 1) % PraiseWords.Length;
        _speech.SpeakPraise(PraiseWords[_praiseIndex]);
    }

    #endregion 特效

    #region 难度调节

    /// <summary>
    /// 难度细分等级变化时：同步三档难度与等级文案，并在未开局时立即重配引擎。
    /// </summary>
    /// <param name="value">新的细分等级</param>
    partial void OnDifficultyLevelChanged(int value)
    {
        int level = DifficultyScale.Clamp(value);
        DifficultyLevelText = DifficultyScale.ToText(level);
        SelectedDifficulty = DifficultyScale.ToDifficulty(level);
        // 对局进行中不重配，避免打断当前局；新的等级在下一局开始时生效
        if (!IsRunning)
        {
            _engine.Configure(Mode, SelectedDifficulty, level);
        }
    }

    /// <summary>选择三档难度：把滑动条同步到该档的代表等级。</summary>
    /// <param name="d">目标难度档位</param>
    [RelayCommand]
    private void SelectDifficulty(Difficulty d)
    {
        DifficultyLevel = DifficultyScale.FromDifficulty(d);
    }

    #endregion 难度调节

    #region 对局控制

    public void Initialize(GameMode mode)
    {
        Mode = mode;
        (Title, Description, AccentBrush) = MetaFor(mode);
        _engine.Configure(mode, SelectedDifficulty, DifficultyLevel);
        SyncView();
        IsRunning = false;
        IsGameOver = false;
        StatusMessage = "按“开始游戏”进入战斗";
    }

    private void SyncView()
    {
        Score = _engine.Score;
        Lives = _engine.Lives;
        Combo = _engine.Combo;
    }

    public void SetBounds(double w, double h)
    {
        _engine.Width = w;
        _engine.Height = h;
    }

    /// <summary>由页面 DispatcherTimer 每帧调用</summary>
    public void Tick(double dtSeconds)
    {
        if (!_engine.IsRunning) return;
        _engine.Tick(dtSeconds);
        SyncView();
        RefreshTimeLeft();
    }

    /// <summary>同步剩余时间文本：只在整数秒变化时更新绑定，减少无谓的界面刷新。</summary>
    private void RefreshTimeLeft()
    {
        int seconds = (int)Math.Ceiling(_engine.TimeLeftSeconds);
        string text = $"{seconds} 秒";
        if (TimeLeftText != text)
        {
            TimeLeftText = text;
        }
    }

    public void Feed(char c)
    {
        if (!_engine.IsRunning) return;
        _engine.Feed(c);
        SyncView();
    }

    public void ResetLock() => _engine.ResetLock();

    [RelayCommand]
    private void Start()
    {
        _engine.Configure(Mode, SelectedDifficulty, DifficultyLevel);
        _engine.Start();
        IsRunning = true;
        IsGameOver = false;
        _lastMilestone = 0;
        _lastErrorTip = DateTime.MinValue;
        SyncView();
        RefreshTimeLeft();
        StatusMessage = "开始！输入屏幕上单词的首字母锁定目标，继续输入将其消灭";
        if (AppState.Current.EnableSound) _sound.PlayStart();
        _fun.ShowStart();
    }

    [RelayCommand]
    private void Restart() => Start();

    [RelayCommand]
    private void Back() => _nav.Navigate(NavigationTarget.GameCenter);

    #endregion 对局控制

    #region 私有工具

    private static (string title, string desc, Brush accent) MetaFor(GameMode mode) => mode switch
    {
        GameMode.SpaceWar => ("太空大战",
            "敌机携带单词从天而降，输入单词将其击落，别让它们着陆！",
            new SolidColorBrush(Color.FromRgb(0x29, 0xB6, 0xF6))),
        GameMode.WhackMole => ("打地鼠",
            "地鼠顶着单词冒出洞口，抢在缩回去之前把它敲掉。",
            new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x6A))),
        GameMode.CatchThief => ("抓小偷",
            "小偷带着单词横穿屏幕，输入单词将其擒获，别让他溜走！",
            new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26))),
        GameMode.FrogBug => ("青蛙吃虫",
            "虫子爬向青蛙，输入单词让它一口吃掉，守住中线！",
            new SolidColorBrush(Color.FromRgb(0x26, 0xA6, 0x9A))),
        GameMode.BalloonPop => ("打气球",
            "气球带着单词从篮子里往上飘，输入单词把它扎破，别让它飞出屏幕！",
            new SolidColorBrush(Color.FromRgb(0xEC, 0x6f, 0x8F))),
        GameMode.LifeDeathSpeed => ("生死时速",
            "和电脑赛跑！完整输入屏幕上的单词驱动选手前进，先到终点的人获胜。",
            new SolidColorBrush(Color.FromRgb(0x8E, 0x5B, 0xE9))),
        _ => ("打字小游戏", "输入单词完成挑战。", Brushes.Blue)
    };

    #endregion 私有工具
}
