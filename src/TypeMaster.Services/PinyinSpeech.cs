using System;
using System.Collections.Generic;

namespace TypeMaster.Services;

/// <summary>
/// 拼音朗读文本映射表：把拼音符号转换成「能被中文语音正确读出」的文本。
///
/// 为什么必须做这层映射（本机实测结论，非推测）：
/// 直接把拉丁字母交给中文语音，会被当成英文字母名朗读——
/// 例如 "b p m f" 会被读成「bee pee em eff」（ASR 反听得到「BPMF」），
/// 这对拼音教学是错的。汉语拼音教学一律采用「呼读音」，
/// 因此这里用承载呼读音的汉字来朗读，例如 b → 「玻」、p → 「坡」。
///
/// 术语：声母的「本音」是辅音本身（无法单独发出），教学与朗读都用「呼读音」，
/// 即声母后带一个元音，如 b 读作 bo（玻）。
/// </summary>
public static class PinyinSpeech
{
    #region 声母呼读音

    /// <summary>
    /// 23 个声母的呼读音汉字映射，按 <see cref="PinyinLibrary.Initials"/> 的顺序排列。
    /// 采用小学拼音教学通行的注音字（b 玻 / p 坡 / m 摸 / f 佛 …）。
    /// </summary>
    private static readonly Dictionary<string, string> InitialSpeech = new(StringComparer.Ordinal)
    {
        ["b"] = "玻",
        ["p"] = "坡",
        ["m"] = "摸",
        ["f"] = "佛",
        ["d"] = "得",
        ["t"] = "特",
        ["n"] = "讷",
        ["l"] = "勒",
        ["g"] = "哥",
        ["k"] = "科",
        ["h"] = "喝",
        ["j"] = "基",
        ["q"] = "欺",
        ["x"] = "希",
        ["zh"] = "知",
        ["ch"] = "蚩",
        ["sh"] = "诗",
        ["r"] = "日",
        ["z"] = "资",
        ["c"] = "雌",
        ["s"] = "思",
        ["y"] = "衣",
        ["w"] = "乌",
    };

    #endregion 声母呼读音

    #region 韵母读音

    /// <summary>
    /// 韵母读音汉字映射。韵母本身可以独立成音节，
    /// 因此优先用其自成音节的常用汉字来朗读（如 a → 啊、ai → 哀）。
    ///
    /// 少数韵母没有自然成音节的常用字（eng / ong），
    /// 用声母拼合的字来承载（在下方注释中标注），本机实测可稳定读出该韵母。
    /// </summary>
    private static readonly Dictionary<string, string> FinalSpeech = new(StringComparer.Ordinal)
    {
        // 单韵母
        ["a"] = "啊",
        ["o"] = "喔",
        ["e"] = "鹅",
        ["i"] = "衣",
        ["u"] = "乌",
        ["ü"] = "迂",

        // 复韵母
        ["ai"] = "哀",
        ["ei"] = "欸",
        ["ui"] = "威",
        ["ao"] = "熬",
        ["ou"] = "欧",
        ["iu"] = "优",
        ["ie"] = "耶",
        ["üe"] = "约",
        ["er"] = "儿",

        // 前鼻韵母
        ["an"] = "安",
        ["en"] = "恩",
        ["in"] = "因",
        ["un"] = "温",
        ["ün"] = "晕",

        // 后鼻韵母
        ["ang"] = "昂",
        ["eng"] = "鞥",
        ["ing"] = "英",
        ["ong"] = "轰",   // ong 不能自成音节，用 hōng 承载韵母 ong 的读音
    };

    #endregion 韵母读音

    #region 整体认读音节

    /// <summary>
    /// 16 个整体认读音节的读音汉字映射。
    /// 整体认读音节要求「整体记认、不拼读」，因此直接用对应汉字朗读。
    /// </summary>
    private static readonly Dictionary<string, string> WholeSyllableSpeech = new(StringComparer.Ordinal)
    {
        ["zhi"] = "知",
        ["chi"] = "吃",
        ["shi"] = "诗",
        ["ri"] = "日",
        ["zi"] = "资",
        ["ci"] = "雌",
        ["si"] = "思",
        ["yi"] = "衣",
        ["wu"] = "乌",
        ["yu"] = "迂",
        ["ye"] = "耶",
        ["yue"] = "约",
        ["yuan"] = "冤",
        ["yin"] = "因",
        ["yun"] = "晕",
        ["ying"] = "英",
    };

    #endregion 整体认读音节

    #region 查询

    /// <summary>
    /// 取得某个拼音符号的朗读文本：优先整体认读音节，其次声母呼读音，再次韵母读音。
    /// 未收录时原样返回（至少不会静默丢失）。
    /// </summary>
    /// <param name="symbol">拼音符号，如 b / zh / ang / zhi</param>
    /// <returns>用于朗读的汉字文本</returns>
    public static string GetSpeech(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return string.Empty;
        }

        string key = symbol.Trim();

        if (WholeSyllableSpeech.TryGetValue(key, out string? whole))
        {
            return whole;
        }
        if (InitialSpeech.TryGetValue(key, out string? initial))
        {
            return initial;
        }
        if (FinalSpeech.TryGetValue(key, out string? final))
        {
            return final;
        }
        return key;
    }

    /// <summary>某符号是否已收录朗读映射（供自检使用）。</summary>
    /// <param name="symbol">拼音符号</param>
    /// <returns>已收录返回 true</returns>
    public static bool HasMapping(string symbol)
        => !string.IsNullOrWhiteSpace(symbol)
           && (WholeSyllableSpeech.ContainsKey(symbol.Trim())
               || InitialSpeech.ContainsKey(symbol.Trim())
               || FinalSpeech.ContainsKey(symbol.Trim()));

    #endregion 查询
}
