using System.Collections.Generic;
using System.Linq;

namespace TypeMaster.Services;

/// <summary>
/// 韵母分组展示模型：分组名 + 该组韵母列表（供 ItemsControl 绑定）。
/// </summary>
public sealed record FinalGroup(string Group, string[] Finals);

/// <summary>
/// 拼音学习资料库：声母 / 韵母 / 整体认读音节与拼读小贴士，
/// 供学习园地页面展示（数据为小学语文通用内容）。
/// </summary>
public static class PinyinLibrary
{
    #region 声母韵母数据

    /// <summary>声母表（23 个）。</summary>
    public static readonly string[] Initials =
        { "b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h", "j", "q", "x", "zh", "ch", "sh", "r", "z", "c", "s", "y", "w" };

    /// <summary>单韵母表（6 个）。</summary>
    public static readonly string[] SimpleFinals = { "a", "o", "e", "i", "u", "ü" };

    /// <summary>复韵母表（9 个）。</summary>
    public static readonly string[] CompoundFinals = { "ai", "ei", "ui", "ao", "ou", "iu", "ie", "üe", "er" };

    /// <summary>前鼻韵母表（5 个）。</summary>
    public static readonly string[] FrontNasalFinals = { "an", "en", "in", "un", "ün" };

    /// <summary>后鼻韵母表（4 个）。</summary>
    public static readonly string[] BackNasalFinals = { "ang", "eng", "ing", "ong" };

    /// <summary>整体认读音节表（16 个）。</summary>
    public static readonly string[] WholeSyllables =
        { "zhi", "chi", "shi", "ri", "zi", "ci", "si", "yi", "wu", "yu", "ye", "yue", "yuan", "yin", "yun", "ying" };

    /// <summary>声调说明（含标调规则）。</summary>
    public static readonly string[] ToneTips =
    {
        "第一声（阴平）ˉ：高而平，如 mā（妈）",
        "第二声（阳平）ˊ：由中向高扬，如 má（麻）",
        "第三声（上声）ˇ：先降后升，如 mǎ（马）",
        "第四声（去声）ˋ：由高降到低，如 mà（骂）",
        "标调口诀：有 a 不放过，没 a 找 o e，i u 并列标在后，单个韵母不用说"
    };

    /// <summary>拼读小贴士（学习园地展示）。</summary>
    public static readonly string[] SpellingTips =
    {
        "两拼音节：声母 + 韵母快速连读，如 b—à→ba（爸）",
        "三拼音节：声母 + 介母（i/u/ü）+ 韵母，如 g—u—ā→guā（瓜）",
        "zh / ch / sh / r 是翘舌音，发音时舌尖翘起；z / c / s 是平舌音，舌尖抵住下齿背",
        "j / q / x 与 ü 相拼时，ü 上两点省略，写成 ju / qu / xu",
        "打字练习时配合拼音输入法，可先在录入框中练习常用音节，再过渡到整句输入"
    };

    /// <summary>
    /// 获取分组后的韵母数据：分组名 + 韵母列表，供界面 ItemsControl 绑定。
    /// </summary>
    /// <returns>韵母分组列表</returns>
    public static IReadOnlyList<FinalGroup> GetFinalGroups() => new List<FinalGroup>
    {
        new("单韵母（6 个）", SimpleFinals),
        new("复韵母（9 个）", CompoundFinals),
        new("前鼻韵母（5 个）", FrontNasalFinals),
        new("后鼻韵母（4 个）", BackNasalFinals),
    };

    #endregion 声母韵母数据

    #region 朗读文本

    /// <summary>
    /// 生成一组拼音符号的朗读文本（空格分隔交给语音引擎自然停顿）。
    ///
    /// 注意：**绝不能把拼音字母直接交给语音引擎**——实测会被读成英文字母名
    /// （"b p m f" 读作 bee/pee/em/eff）。此处统一转成呼读音汉字，
    /// 见 <see cref="PinyinSpeech"/>。
    /// </summary>
    /// <param name="symbols">拼音符号序列</param>
    /// <returns>朗读文本</returns>
    public static string GetSpeakText(IEnumerable<string> symbols)
    {
        if (symbols is null)
        {
            return string.Empty;
        }
        var parts = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(PinyinSpeech.GetSpeech);
        return string.Join("，", parts);
    }

    /// <summary>声母表的朗读文本。</summary>
    /// <returns>朗读文本</returns>
    public static string GetInitialsSpeakText() => GetSpeakText(Initials);

    /// <summary>整体认读音节表的朗读文本。</summary>
    /// <returns>朗读文本</returns>
    public static string GetWholeSyllablesSpeakText() => GetSpeakText(WholeSyllables);

    /// <summary>全部韵母（按分组顺序）的朗读文本。</summary>
    /// <returns>朗读文本</returns>
    public static string GetFinalsSpeakText()
    {
        var all = GetFinalGroups().SelectMany(g => g.Finals);
        return GetSpeakText(all);
    }

    /// <summary>
    /// 单个拼音符号的朗读文本（供点击胶囊朗读）。
    /// 先尝试「符号 + 界面显示名」的完整读法，读不出来至少读符号本身。
    /// </summary>
    /// <param name="symbol">拼音符号</param>
    /// <returns>朗读文本</returns>
    public static string GetSpeakText(string symbol) => PinyinSpeech.GetSpeech(symbol);

    /// <summary>
    /// 自检：返回所有缺少朗读映射的拼音符号。
    /// 供开发期验证数据完整性，避免新增符号时静默退化成字母读法。
    /// </summary>
    /// <returns>缺少映射的符号列表</returns>
    public static IReadOnlyList<string> FindMissingSpeechMappings()
    {
        var all = new List<string>();
        all.AddRange(Initials);
        all.AddRange(GetFinalGroups().SelectMany(g => g.Finals));
        all.AddRange(WholeSyllables);
        return all.Where(s => !PinyinSpeech.HasMapping(s)).Distinct().ToList();
    }

    #endregion 朗读文本
}
