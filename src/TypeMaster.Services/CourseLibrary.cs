using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>
/// 课程阶段：指法入门 → 单键 → 单词 → 句子 → 文章，对应金山打字通的渐进教学路线。
/// </summary>
public enum CourseStage
{
    /// <summary>指法入门：基准键与上/下排键的手位记忆</summary>
    Finger = 0,
    /// <summary>单键练习：分组反复击打同一批字母 / 数字 / 符号</summary>
    SingleKey = 1,
    /// <summary>单词练习：英文单词与中文词组</summary>
    Word = 2,
    /// <summary>句子练习：完整句子与标点</summary>
    Sentence = 3,
    /// <summary>文章练习：整篇文章与五笔短文</summary>
    Article = 4,

    /// <summary>
    /// 五笔入门（零基础支线）。
    ///
    /// 为什么单独成阶段而不插进主线的顺序里：
    ///   主线关卡按「前一关已通关」判定解锁，若把五笔关卡插到中间，
    ///   已通关用户的进度链会被打断（新插入的关卡永远解锁不了）。
    ///   独立成阶段并单独判定解锁，既不影响既有存档，也让想学五笔的
    ///   用户不必先做完 20 关英文/中文练习就能直接开始。
    /// </summary>
    WubiBasics = 5
}

/// <summary>
/// 关卡文本来源：决定 <see cref="CourseLibrary.GetText"/> 如何生成本关的对照文本。
/// </summary>
public enum LessonTextSource
{
    /// <summary>指法练习：按 <see cref="CourseLesson.SourceArg"/> 给定的字符集按分组节奏生成</summary>
    FingerDrill = 0,
    /// <summary>英文单词</summary>
    EnglishWords = 1,
    /// <summary>中文词组</summary>
    ChineseWords = 2,
    /// <summary>英文句子</summary>
    EnglishSentences = 3,
    /// <summary>中文句子</summary>
    ChineseSentences = 4,
    /// <summary>内置文章（按难度与等级取随机篇目）</summary>
    Article = 5,
    /// <summary>五笔字根短文</summary>
    Wubi = 6,

    /// <summary>
    /// 五笔按编码长度出字：SourceArg 给出码长（1~4）。
    /// 供「五笔入门」支线使用，从一码字开始循序渐进。
    /// </summary>
    WubiByCodeLength = 7
}

/// <summary>
/// 一个课程关卡的定义。关卡本身是纯数据，界面据此渲染列表与锁定状态。
/// </summary>
/// <param name="Id">关卡稳定 Id（进度存档以此为键，不要随意改动）</param>
/// <param name="Stage">所属阶段</param>
/// <param name="StageOrder">关卡在全局学习路线中的顺序（从 0 开始），用于解锁判定</param>
/// <param name="Title">关卡标题</param>
/// <param name="Description">关卡说明（告诉用户这一关练什么）</param>
/// <param name="PracticeType">练习类型</param>
/// <param name="Difficulty">难度档位</param>
/// <param name="Level">细分等级（1~10）</param>
/// <param name="Source">文本来源</param>
/// <param name="SourceArg">文本来源的参数（如指法字符集）</param>
public sealed record CourseLesson(
    string Id,
    CourseStage Stage,
    int StageOrder,
    string Title,
    string Description,
    PracticeType PracticeType,
    Difficulty Difficulty,
    int Level,
    LessonTextSource Source,
    string SourceArg = "");

/// <summary>
/// 课程库：内置「指法入门 → 单键 → 单词 → 句子 → 文章」五阶段关卡，
/// 并给出解锁判定所需的信息（达标线、前一关、文本生成）。
/// </summary>
public static class CourseLibrary
{
    #region 达标线

    /// <summary>通关所需的最低正确率（百分比）。</summary>
    public const double RequiredAccuracy = 90d;

    /// <summary>通关所需的最低评级。</summary>
    public const Grade RequiredGrade = Grade.B;

    /// <summary>
    /// 判断一次练习是否达到通关标准。
    /// </summary>
    /// <param name="accuracy">正确率（百分比）</param>
    /// <param name="grade">评级</param>
    /// <returns>是否通关</returns>
    public static bool IsPass(double accuracy, Grade grade)
        => accuracy >= RequiredAccuracy && grade >= RequiredGrade;

    /// <summary>通关标准的说明文案（界面展示用）。</summary>
    public static string PassRuleText
        => $"正确率 ≥ {RequiredAccuracy:F0}% 且评级 ≥ {GradeScale.ToText(RequiredGrade)}";

    #endregion 达标线

    #region 关卡表

    /// <summary>全部关卡，已按学习路线顺序排列。</summary>
    private static readonly List<CourseLesson> Lessons = new()
    {
        // ---------- 第一阶段：指法入门 ----------
        new("finger-home", CourseStage.Finger, 0, "基准键 · ASDF JKL;",
            "双手食指放在 F 与 J 的凸起上，八个手指各归其位，反复击打基准键并回到原位。",
            PracticeType.English, Difficulty.Easy, 1, LessonTextSource.FingerDrill, "asdfjkl;"),

        new("finger-top", CourseStage.Finger, 1, "上排键 · QWERTY UIOP",
            "手指从基准键向上伸一个键位，击键后立刻回落，重点体会食指与中指的伸展。",
            PracticeType.English, Difficulty.Easy, 2, LessonTextSource.FingerDrill, "qwertyuiop"),

        new("finger-bottom", CourseStage.Finger, 2, "下排键 · ZXCV BNM,./",
            "手指向下弯曲够键，小指与无名指最容易偷懒，这一关专治下排键找不准。",
            PracticeType.English, Difficulty.Easy, 3, LessonTextSource.FingerDrill, "zxcvbnm,./"),

        new("finger-space", CourseStage.Finger, 3, "大拇指 · 空格与组合",
            "两根拇指轮流负责空格，与字母键交替击打，形成稳定的节奏感。",
            PracticeType.English, Difficulty.Easy, 4, LessonTextSource.FingerDrill, "asdf jkl"),

        new("finger-mix", CourseStage.Finger, 4, "综合指法 · 三排混合",
            "三排键位混合出现，练习手指在键盘上的整体定位能力。",
            PracticeType.English, Difficulty.Normal, 5, LessonTextSource.FingerDrill, "qazwsxedcrfvtgbyhnujmikolp"),

        // ---------- 第二阶段：单键练习 ----------
        new("key-left", CourseStage.SingleKey, 5, "左手字母区",
            "集中练习左手负责的字母，提升左手手指的独立性与准确率。",
            PracticeType.English, Difficulty.Easy, 2, LessonTextSource.FingerDrill, "qwertasdfgzxcvb"),

        new("key-right", CourseStage.SingleKey, 6, "右手字母区",
            "集中练习右手负责的字母，右手负责的键位更多，需要更多重复。",
            PracticeType.English, Difficulty.Easy, 3, LessonTextSource.FingerDrill, "yuiophjklmn"),

        new("key-number", CourseStage.SingleKey, 7, "主键盘数字行",
            "数字在键盘最上排，需要用手指伸展去够，练成之后打数据不用看键盘。",
            PracticeType.English, Difficulty.Normal, 5, LessonTextSource.FingerDrill, "1234567890"),

        new("key-symbol", CourseStage.SingleKey, 8, "常用符号键",
            "逗号、句号、分号、斜杠等符号在日常输入中出现频率很高，必须练熟。",
            PracticeType.English, Difficulty.Normal, 6, LessonTextSource.FingerDrill, ",.;'/"),

        // ---------- 第三阶段：单词练习 ----------
        new("word-en-easy", CourseStage.Word, 9, "英文单词 · 基础",
            "三到四字母的高频词，建立整词输入的节奏，不再逐字母停顿。",
            PracticeType.EnglishWord, Difficulty.Easy, 3, LessonTextSource.EnglishWords),

        new("word-en-normal", CourseStage.Word, 10, "英文单词 · 进阶",
            "五到七字母的常用词，练习长词内的连续击键。",
            PracticeType.EnglishWord, Difficulty.Normal, 5, LessonTextSource.EnglishWords),

        new("word-en-hard", CourseStage.Word, 11, "英文单词 · 挑战",
            "八字母以上的长词，考验手指的记忆与准确率。",
            PracticeType.EnglishWord, Difficulty.Hard, 8, LessonTextSource.EnglishWords),

        new("word-cn-easy", CourseStage.Word, 12, "中文词组 · 基础",
            "常用双字词，练习词组级别的连续输入与选词节奏。",
            PracticeType.ChineseWord, Difficulty.Easy, 3, LessonTextSource.ChineseWords),

        new("word-cn-normal", CourseStage.Word, 13, "中文词组 · 进阶",
            "三字词与常见成语，词组越长越依赖输入法的整体上屏。",
            PracticeType.ChineseWord, Difficulty.Normal, 5, LessonTextSource.ChineseWords),

        new("word-cn-hard", CourseStage.Word, 14, "中文词组 · 挑战",
            "四字成语与较长词组，考察长串输入的稳定性。",
            PracticeType.ChineseWord, Difficulty.Hard, 8, LessonTextSource.ChineseWords),

        // ---------- 第四阶段：句子练习 ----------
        new("sentence-en", CourseStage.Sentence, 15, "英文句子",
            "完整句子包含空格、标点与大小写，是速度测试前的重要过渡。",
            PracticeType.English, Difficulty.Normal, 6, LessonTextSource.EnglishSentences),

        new("sentence-cn", CourseStage.Sentence, 16, "中文句子",
            "中文句子引入标点与换气节奏，练习长句的稳定输出。",
            PracticeType.Chinese, Difficulty.Normal, 6, LessonTextSource.ChineseSentences),

        new("sentence-mix", CourseStage.Sentence, 17, "中英混排句子",
            "中英文夹杂的句子在日常办公中最常见，切换输入法是必修课。",
            PracticeType.English, Difficulty.Hard, 8, LessonTextSource.EnglishSentences),

        // ---------- 第五阶段：文章练习 ----------
        new("article-en", CourseStage.Article, 18, "英文短文",
            "整段英文短文，综合考察速度、准确率与持久力。",
            PracticeType.English, Difficulty.Hard, 9, LessonTextSource.Article),

        new("article-cn", CourseStage.Article, 19, "中文短文",
            "整段中文短文，是中文输入速度的最终检验。",
            PracticeType.Chinese, Difficulty.Hard, 9, LessonTextSource.Article),

        new("article-wubi", CourseStage.Article, 20, "毕业关 · 五笔短文",
            "用五笔编码完整输入一段中文，通关即代表五笔字根已经形成肌肉记忆。",
            PracticeType.Wubi, Difficulty.Hard, 9, LessonTextSource.Wubi),

        // ---------- 五笔入门支线（零基础，可独立开始练，不必先通关主线）----------
        //
        // 顺序按"编码击键数"由少到多：一码字 → 二码 → 三码 → 四码 → 常用字 → 短文。
        // 这样第一关就能打出完整的字，快速建立成就感；不用先背熟字根表。
        new("wubi-basic-1key", CourseStage.WubiBasics, 21, "五笔入门 · 一键成字",
            "一级简码只有 25 个字，每个字按一个字母键加空格就打出来。先体会「一击成字」的感觉，不必背字根。",
            PracticeType.Wubi, Difficulty.Easy, 1, LessonTextSource.WubiByCodeLength, "1"),

        new("wubi-basic-2key", CourseStage.WubiBasics, 22, "五笔入门 · 两码字",
            "两码成字是最常见的形式：第一码定位大方向，第二码锁定具体字。屏幕会逐码提示该按哪个键。",
            PracticeType.Wubi, Difficulty.Easy, 2, LessonTextSource.WubiByCodeLength, "2"),

        new("wubi-basic-3key", CourseStage.WubiBasics, 23, "五笔入门 · 三码字",
            "三码字数量最多。不用记拆解规则，照着屏幕上的逐码提示按即可，按多了自然形成手感。",
            PracticeType.Wubi, Difficulty.Normal, 4, LessonTextSource.WubiByCodeLength, "3"),

        new("wubi-basic-4key", CourseStage.WubiBasics, 24, "五笔入门 · 四码字",
            "需要四码的字手指移动范围更大，慢慢来，准确比快更重要。",
            PracticeType.Wubi, Difficulty.Normal, 5, LessonTextSource.WubiByCodeLength, "4"),

        new("wubi-basic-common", CourseStage.WubiBasics, 25, "五笔入门 · 常用字综合",
            "混合各种码长，练习最常用的字。到这一关应该已经不太需要看提示了。",
            PracticeType.Wubi, Difficulty.Normal, 6, LessonTextSource.Wubi),

        new("wubi-basic-text", CourseStage.WubiBasics, 26, "五笔入门 · 打一小段话",
            "用五笔完整打出几句话。做到这一步，日常用五笔记事已经没问题。",
            PracticeType.Wubi, Difficulty.Hard, 8, LessonTextSource.Wubi)
    };

    #endregion 关卡表

    #region 公开接口

    /// <summary>全部关卡（按学习路线顺序）。</summary>
    public static IReadOnlyList<CourseLesson> GetAll() => Lessons;

    /// <summary>按阶段取关卡。</summary>
    /// <param name="stage">课程阶段</param>
    /// <returns>该阶段下的关卡列表</returns>
    public static IReadOnlyList<CourseLesson> GetByStage(CourseStage stage)
        => Lessons.Where(l => l.Stage == stage).OrderBy(l => l.StageOrder).ToList();

    /// <summary>按 Id 取关卡；不存在返回 null。</summary>
    /// <param name="id">关卡 Id</param>
    /// <returns>关卡定义或 null</returns>
    public static CourseLesson? GetById(string id)
        => Lessons.FirstOrDefault(l => l.Id == id);

    /// <summary>
    /// 取某关的前一关（按学习路线顺序）；第一关返回 null。
    /// </summary>
    /// <param name="lesson">当前关卡</param>
    /// <returns>前一关或 null</returns>
    public static CourseLesson? GetPrevious(CourseLesson lesson)
    {
        int idx = Lessons.FindIndex(l => l.Id == lesson.Id);
        return idx > 0 ? Lessons[idx - 1] : null;
    }

    /// <summary>
    /// 判断关卡是否已解锁：第一关永远开放，其余关卡要求前一关已通关。
    /// </summary>
    /// <param name="lesson">待判断的关卡</param>
    /// <param name="progress">当前进度（可为 null，视为全新进度）</param>
    /// <returns>是否已解锁</returns>
    public static bool IsUnlocked(CourseLesson lesson, CourseProgress? progress)
    {
        // 五笔入门支线独立判定：只依赖本支线的前一关，不要求通关主线。
        //
        // 理由：支线的目标是让零基础用户（尤其是中老年学习者）能直接开始学五笔，
        // 而不必先做完 20 关英文指法/单词/句子练习。
        // 若沿用主线的"前一关已通关"规则，第一关五笔的前置会变成主线毕业关，
        // 等于把门槛设成了做完整个主线，与支线的用意矛盾。
        if (lesson.Stage == CourseStage.WubiBasics)
        {
            return IsUnlockedInWubiBasics(lesson, progress);
        }

        CourseLesson? prev = GetPrevious(lesson);
        if (prev == null)
        {
            return true;
        }
        return progress != null && progress.IsCleared(prev.Id);
    }

    /// <summary>
    /// 五笔入门支线内部的解锁判定：支线第一关始终可玩，其余要求支线内前一关已通关。
    /// </summary>
    /// <param name="lesson">待判断的关卡（须属于五笔入门支线）</param>
    /// <param name="progress">当前进度</param>
    /// <returns>是否已解锁</returns>
    private static bool IsUnlockedInWubiBasics(CourseLesson lesson, CourseProgress? progress)
    {
        var chain = Lessons.Where(l => l.Stage == CourseStage.WubiBasics)
                           .OrderBy(l => l.StageOrder)
                           .ToList();
        int idx = chain.FindIndex(l => l.Id == lesson.Id);
        if (idx <= 0)
        {
            // 支线第一关（或未找到）：直接开放
            return true;
        }
        return progress != null && progress.IsCleared(chain[idx - 1].Id);
    }

    /// <summary>阶段中文名。</summary>
    public static string StageText(CourseStage stage) => stage switch
    {
        CourseStage.Finger => "指法入门",
        CourseStage.SingleKey => "单键练习",
        CourseStage.Word => "单词练习",
        CourseStage.Sentence => "句子练习",
        CourseStage.WubiBasics => "五笔入门（零基础）",
        _ => "文章练习"
    };

    /// <summary>阶段说明文案。</summary>
    public static string StageDescription(CourseStage stage) => stage switch
    {
        CourseStage.Finger => "先把八个手指放到正确的位置，这是所有速度的起点。",
        CourseStage.SingleKey => "分组反复击打同一批按键，让手指记住每个键的距离。",
        CourseStage.Word => "从单个字母走向整词输入，开始建立连击节奏。",
        CourseStage.Sentence => "加入空格与标点，为整篇输入做准备。",
        CourseStage.WubiBasics => "专为零基础准备，从一键成字开始。屏幕会逐码提示该按哪个键，不必先背字根。",
        _ => "整篇对照输入，检验速度、准确率与持久力。"
    };

    /// <summary>
    /// 生成本关的对照文本。每次调用都会重新随机抽取，便于「换一篇」。
    /// </summary>
    /// <param name="lesson">关卡</param>
    /// <returns>对照文本</returns>
    public static string GetText(CourseLesson lesson)
    {
        return lesson.Source switch
        {
            LessonTextSource.FingerDrill => BuildDrill(lesson.SourceArg, lesson.Level),
            LessonTextSource.EnglishWords => WordLibrary.GetEnglishWordText(lesson.Difficulty, lesson.Level),
            LessonTextSource.ChineseWords => WordLibrary.GetChineseWordText(lesson.Difficulty, lesson.Level),
            LessonTextSource.EnglishSentences => WordLibrary.GetEnglishSentenceText(lesson.Difficulty, lesson.Level),
            LessonTextSource.ChineseSentences => WordLibrary.GetChineseSentenceText(lesson.Difficulty, lesson.Level),
            LessonTextSource.Article => TextLibrary.GetRandomBuiltIn(lesson.Difficulty, lesson.Level),
            // 自由练习用的五笔随机短文
            LessonTextSource.Wubi => lesson.Stage == CourseStage.WubiBasics
                ? WubiLibrary.GetCommonPractice(26, maxCodeLength: 4)
                : WubiLibrary.GetRandomPractice(lesson.Difficulty, lesson.Level),
            // 五笔入门支线：SourceArg 给出编码长度（1~4），从一码字循序渐进
            LessonTextSource.WubiByCodeLength => WubiLibrary.GetPracticeByCodeLength(
                ParseCodeLength(lesson.SourceArg), CodeLengthPracticeCount(lesson.StageOrder)),
            _ => string.Empty
        };
    }

    #endregion 公开接口

    #region 五笔入门支线工具

    /// <summary>解析五笔入门关卡的字长参数；非法值回退到 1（一级简码）。</summary>
    /// <param name="arg">关卡 SourceArg</param>
    /// <returns>1~4 的编码长度</returns>
    private static int ParseCodeLength(string arg)
        => int.TryParse(arg, out int n) ? Math.Clamp(n, 1, 4) : 1;

    /// <summary>
    /// 各关的练习字数：前期少而精，后期逐步加量，避免一上来就疲劳。
    /// </summary>
    /// <param name="stageOrder">关卡序号</param>
    /// <returns>本次练习的字数</returns>
    private static int CodeLengthPracticeCount(int stageOrder) => stageOrder switch
    {
        21 => 12,   // 一码字：少一些，先把"一击成字"的手感建立起来
        22 => 16,
        23 => 20,
        24 => 20,
        _ => 24
    };

    #endregion 五笔入门支线工具

    #region 私有工具

    /// <summary>
    /// 生成指法 / 单键练习文本：从给定字符集中随机取字，每 4 个字符插入一个空格分组，
    /// 便于眼睛分组扫视、手指分组动作。总长度由细分等级决定。
    /// </summary>
    /// <param name="chars">可用字符集</param>
    /// <param name="level">细分等级</param>
    /// <returns>练习文本</returns>
    private static string BuildDrill(string chars, int level)
    {
        string pool = new string((chars ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).Distinct().ToArray());
        if (pool.Length == 0)
        {
            return string.Empty;
        }

        int target = TextLibrary.GetTargetLength(level);
        var sb = new StringBuilder(target + target / 4 + 1);
        int groupSize = 4;
        int inGroup = 0;
        while (sb.Length < target)
        {
            sb.Append(pool[Random.Shared.Next(pool.Length)]);
            inGroup++;
            if (inGroup >= groupSize)
            {
                sb.Append(' ');
                inGroup = 0;
            }
        }
        return sb.ToString().TrimEnd();
    }

    #endregion 私有工具
}