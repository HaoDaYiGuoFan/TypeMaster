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
    public int WindowWidth { get; set; } = 1100;

    /// <summary>窗口高度</summary>
    public int WindowHeight { get; set; } = 720;
}
