namespace TypeMaster.Core.Entities;

/// <summary>
/// 软件配置（AppConfig 表，单行，Id 固定为 1）
/// </summary>
public class AppConfig
{
    /// <summary>主键，固定为 1</summary>
    public int Id { get; set; }

    /// <summary>主题：0亮色 / 1护眼绿 / 2暗黑</summary>
    public int Theme { get; set; }

    /// <summary>是否显示虚拟键盘</summary>
    public bool ShowVirtualKeyboard { get; set; }

    /// <summary>是否开启按键音效</summary>
    public bool EnableSound { get; set; } = true;

    /// <summary>是否开启学习园地的语音朗读（五笔口诀 / 拼音声韵母）。</summary>
    public bool EnableSpeech { get; set; } = true;

    /// <summary>语音朗读语速档位（-5 慢 ~ 5 快，0 为正常）。</summary>
    public int SpeechRate { get; set; } = 0;

    /// <summary>是否开启游戏背景音乐（BGM）。</summary>
    public bool EnableMusic { get; set; } = true;

    /// <summary>背景音乐音量（0~100）。</summary>
    public int MusicVolume { get; set; } = 55;

    /// <summary>音效音量（0~100）。</summary>
    public int SoundVolume { get; set; } = 80;

    /// <summary>打字区字体</summary>
    public string FontFamily { get; set; } = "Consolas";

    /// <summary>打字区字号</summary>
    public double FontSize { get; set; } = 22;

    /// <summary>窗口宽度</summary>
    // ---------------- 长辈模式（面向中老年学习者的整体预设）----------------
    //
    // 设计意图：把"适合长辈"的一组设置打包成一个开关，一键生效，
    // 不需要逐项去调。各子项仍可单独覆盖，方便按个人习惯微调。

    /// <summary>
    /// 长辈模式总开关。开启后自动应用大字号、慢节奏，
    /// 并默认打开五笔逐码按键提示与大字显示。
    /// </summary>
    public bool ElderMode { get; set; }

    /// <summary>
    /// 五笔练习时显示逐码按键提示（如「第2码 E键（月）」）。
    /// 面向初学五笔的用户；熟练后可关闭。
    /// </summary>
    public bool ShowWubiKeyHint { get; set; } = true;

    /// <summary>
    /// 五笔练习时用大字显示当前字与编码，方便看清。
    /// </summary>
    public bool ShowWubiBigChar { get; set; } = true;

    /// <summary>
    /// 切换到新的练习字时自动朗读该字的编码与按键提示。
    /// 依赖系统中文语音引擎；未安装时自动跳过，不影响其它功能。
    /// </summary>
    public bool SpeakWubiHint { get; set; }

    /// <summary>进入长辈模式前的字号，用于关闭长辈模式时恢复原值。</summary>
    public double FontSizeBeforeElderMode { get; set; } = 22;

    /// <summary>进入长辈模式前的语音语速，用于关闭时恢复。</summary>
    public int SpeechRateBeforeElderMode { get; set; }

    public int WindowWidth { get; set; } = 1100;

    /// <summary>窗口高度</summary>
    public int WindowHeight { get; set; } = 720;
}
