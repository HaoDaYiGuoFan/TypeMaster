namespace TypeMaster.Core.Enums;

/// <summary>
/// 练习类型：英文 / 中文 / 限时测速 / 五笔 / 英文单词 / 中文词组。
/// 注意：新类型一律追加在末尾，保证已存档成绩里的整数编号语义不变。
/// </summary>
public enum PracticeType
{
    English = 0,
    Chinese = 1,
    SpeedTest = 2,

    /// <summary>五笔 86 版字根练习：屏幕显示汉字，按五笔编码键入</summary>
    Wubi = 3,

    /// <summary>英文单词练习：逐个单词键入，词表按长度与常见度分级</summary>
    EnglishWord = 4,

    /// <summary>中文词组练习：逐个词组键入，词表按年级分级</summary>
    ChineseWord = 5
}