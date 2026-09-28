using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.Core;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 学习园地页：提供五笔字根表 / 口诀、拼音声韵母与拼读规则、打字指法三个学习板块。
/// 页面数据由 <see cref="WubiLibrary"/> 与 <see cref="PinyinLibrary"/> 提供；
/// 朗读由 <see cref="ITextToSpeechService"/> 提供（Windows 内置中文语音，离线）。
///
/// 朗读设计要点（基于本机 ASR 反听实测）：
/// 1) 拼音**不能**把拉丁字母直接交给语音引擎（会被读成英文字母名），
///    统一经 <see cref="PinyinSpeech"/> 转成呼读音汉字；
/// 2) 五笔口诀需先剥离括号内读音提示（否则提示也会被读出），
///    并对个别生僻字根做同音替换，见 <see cref="WubiLibrary.GetSpeakText(string)"/>。
/// </summary>
public partial class LearnCenterPage : UserControl
{
    #region 局部变量属性

    private readonly ITextToSpeechService _speech;

    #endregion 局部变量属性

    #region 构造函数

    public LearnCenterPage()
    {
        InitializeComponent();

        _speech = App.ServiceProvider.GetRequiredService<ITextToSpeechService>();

        LoadLearnData();
        ApplySpeechAvailability();
    }

    #endregion 构造函数

    #region 私有方法

    /// <summary>
    /// 注入学习板块数据：五笔字根表（来自 WubiLibrary）与拼音 / 指法资料（来自 PinyinLibrary）。
    /// </summary>
    private void LoadLearnData()
    {
        // 五笔字根：25 个键位的键名 / 口诀 / 主要字根
        WubiRootsList.ItemsSource = WubiLibrary.GetRoots();
        WubiIntroText.Text = WubiLibrary.GetMnemonicIntro();

        // 拼音：声母 / 韵母分组 / 整体认读 / 声调与拼读贴士
        InitialsList.ItemsSource = PinyinLibrary.Initials;
        FinalGroupsList.ItemsSource = PinyinLibrary.GetFinalGroups();
        WholeSyllablesList.ItemsSource = PinyinLibrary.WholeSyllables;
        ToneTipsList.ItemsSource = PinyinLibrary.ToneTips;
        SpellingTipsList.ItemsSource = PinyinLibrary.SpellingTips;

        // 指法要领（与金山打字通指法练习一致的通用规则）
        FingerTipsList.ItemsSource = new[]
        {
            "手腕悬空、手指自然弯曲成弧形，指尖轻放在基准键位上，手掌不压键盘",
            "每个手指负责自己那一列的按键，击键后立即回到基准键位，不要停留在别的键上",
            "拇指专门负责空格键；击键要轻快有弹性，像小鸡啄米，不要用力敲击",
            "眼睛看屏幕或文稿，做到\"盲打\"，不低头看键盘；刚开始慢一点，准确率比速度更重要",
            "保持良好坐姿：身体坐正、双脚放平，屏幕与眼睛保持约一臂距离，每练习 30 分钟远眺放松"
        };
    }

    /// <summary>
    /// 根据系统是否具备中文语音，决定朗读入口是否可用。
    /// 缺少语音包时只给提示、禁用按钮，不影响页面其它功能。
    /// </summary>
    private void ApplySpeechAvailability()
    {
        bool ok = _speech.IsAvailable;
        if (!ok)
        {
            SpeechHintText.Text = _speech.UnavailableHint;
            SpeechHintBar.Visibility = Visibility.Visible;
        }
    }

    /// <summary>朗读失败时把原因显示到提示条，避免用户以为按钮没反应。</summary>
    /// <param name="started">是否成功开始朗读</param>
    private void ReportIfFailed(bool started)
    {
        if (started)
        {
            return;
        }

        if (!AppState.Current.EnableSpeech)
        {
            SpeechHintText.Text = "语音朗读已在「系统设置」中关闭。可开启后继续使用朗读功能。";
        }
        else
        {
            SpeechHintText.Text = _speech.UnavailableHint;
        }
        SpeechHintBar.Visibility = Visibility.Visible;
    }

    #endregion 私有方法

    #region 朗读事件（五笔口诀）

    /// <summary>朗读五笔字型分区说明。</summary>
    private void SpeakWubiIntro_Click(object sender, RoutedEventArgs e)
        => ReportIfFailed(_speech.Speak(WubiLibrary.GetIntroSpeakText()));

    /// <summary>依次朗读 25 个键位的字根口诀。</summary>
    private void SpeakAllWubi_Click(object sender, RoutedEventArgs e)
        => ReportIfFailed(_speech.Speak(WubiLibrary.GetRootsSpeakText()));

    /// <summary>朗读单张字根卡片对应的键位口诀。</summary>
    private void SpeakWubiCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: WubiRootInfo root })
        {
            ReportIfFailed(_speech.Speak(WubiLibrary.GetSpeakText(root)));
        }
    }

    #endregion 朗读事件（五笔口诀）

    #region 朗读事件（拼音）

    /// <summary>朗读 23 个声母（呼读音）。</summary>
    private void SpeakAllInitials_Click(object sender, RoutedEventArgs e)
        => ReportIfFailed(_speech.Speak(PinyinLibrary.GetInitialsSpeakText()));

    /// <summary>朗读该组韵母。</summary>
    private void SpeakFinalGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: IEnumerable<string> finals })
        {
            ReportIfFailed(_speech.Speak(PinyinLibrary.GetSpeakText(finals)));
        }
    }

    /// <summary>朗读 16 个整体认读音节。</summary>
    private void SpeakAllWholeSyllables_Click(object sender, RoutedEventArgs e)
        => ReportIfFailed(_speech.Speak(PinyinLibrary.GetWholeSyllablesSpeakText()));

    /// <summary>点击拼音胶囊朗读单个拼音。</summary>
    private void SpeakPinyinChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string symbol } && symbol.Length > 0)
        {
            ReportIfFailed(_speech.Speak(PinyinLibrary.GetSpeakText(symbol)));
        }
    }

    /// <summary>朗读一条小贴士原文（声调规则 / 拼读小贴士 / 指法要领）。</summary>
    private void SpeakTip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string text } && text.Length > 0)
        {
            ReportIfFailed(_speech.Speak(text));
        }
    }

    #endregion 朗读事件（拼音）
}
