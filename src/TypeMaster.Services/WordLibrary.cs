using System;
using System.Collections.Generic;
using System.Linq;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>
/// 单词 / 词组 / 句子题库：补齐金山打字通「单词练习」这一档。
///
/// 与 <see cref="TextLibrary"/>（整篇文章）的分工：
/// - TextLibrary 负责「文章」级练习；
/// - WordLibrary 负责「单词」与「句子」级练习，按难度分级、按细分等级决定篇幅。
/// </summary>
public static class WordLibrary
{
    #region 英文单词

    /// <summary>
    /// 英文单词表：按难度分级。
    /// 简单档为 3~4 字母高频词，普通档为 5~7 字母，困难档为 8 字母以上。
    /// </summary>
    private static readonly Dictionary<Difficulty, List<string>> EnglishWords = new()
    {
        // 入门档：3~4 字母、绝对常见、小学生认识的词
        [Difficulty.Entry] = new()
        {
            "cat", "dog", "sun", "cup", "bed", "pen", "hat", "box",
            "bus", "car", "run", "jump", "eat", "red", "blue", "big",
            "small", "hot", "cold", "new", "ball", "book", "tree", "bird",
            "fish", "cake", "milk", "hand", "foot", "door", "mom", "dad",
            "boy", "girl", "kid", "toy", "game", "home", "room", "food",
            "leg", "arm", "eye", "ear", "nose", "hair", "face", "head",
            "neck", "toe", "ant", "bee", "pig", "cow", "duck", "frog",
            "bear", "lion", "goat", "moon",
        },
        [Difficulty.Easy] = new()
        {
            "cat", "dog", "sun", "run", "red", "box", "cup", "pen", "hat", "map",
            "bag", "bed", "bus", "car", "cow", "egg", "fan", "fox", "gum", "hit",
            "ice", "jam", "key", "kid", "leg", "man", "net", "oil", "pig", "pot",
            "rat", "sea", "toy", "van", "web", "yes", "zoo", "arm", "bat", "cap",
            "dip", "ear", "fit", "gas", "hop", "ink", "job", "lip", "mud", "nap",
            "oak", "pea", "rib", "sad", "tap", "use", "vet", "win", "yap", "zip"
        },
        [Difficulty.Normal] = new()
        {
            "planet", "rocket", "garden", "window", "pencil", "orange", "monkey",
            "coffee", "forest", "silent", "bridge", "purple", "silver", "wallet",
            "castle", "button", "camera", "dragon", "flower", "guitar", "island",
            "mirror", "pepper", "rabbit", "school", "summer", "temple", "ticket",
            "valley", "winter", "yellow", "animal", "banana", "circle", "danger",
            "effort", "fabric", "galaxy", "hammer", "insect", "jungle", "kitten",
            "ladder", "magnet", "napkin", "object", "puzzle", "quartz", "reason",
            "saddle", "tailor", "unique", "vacuum", "wander", "yogurt", "zebra"
        },
        [Difficulty.Hard] = new()
        {
            "adventure", "chocolate", "umbrella", "knowledge", "wonderful",
            "beautiful", "dangerous", "education", "happiness", "important",
            "landscape", "mountain", "necessary", "operation", "president",
            "questions", "recognize", "situation", "technique", "understand",
            "vegetable", "wonderland", "xylophone", "yesterday", "achievement",
            "boundaries", "challenge", "delicious", "efficient", "friendship",
            "generation", "historical", "impossible", "journeying", "kindergarten",
            "leadership", "motivation", "navigation", "occasional", "photograph",
            "quotation", "remarkable", "successful", "tremendous", "university",
            "volunteer", "widespread", "experience", "thunderstorm", "conversation"
        }
    };

    #endregion 英文单词

    #region 中文词组

    /// <summary>
    /// 中文词组表：按难度分级。
    /// 简单档为二年级以内的双字词，普通档为三字词与成语，困难档为四字成语与较长词组。
    /// </summary>
    private static readonly Dictionary<Difficulty, List<string>> ChineseWords = new()
    {
        // 入门档：双字词为主，都是日常最基础的
        [Difficulty.Entry] = new()
        {
            "妈妈", "爸爸", "爷爷", "奶奶", "哥哥", "姐姐", "弟弟", "妹妹",
            "老师", "同学", "上学", "放学", "上课", "下课", "作业", "考试",
            "语文", "数学", "体育", "音乐", "早上", "中午", "晚上", "今天",
            "明天", "昨天", "现在", "时间", "上午", "下午", "苹果", "香蕉",
            "西瓜", "米饭", "面条", "鸡蛋", "牛奶", "面包", "青菜", "水果",
            "小猫", "小狗", "小鸟", "小鱼", "老虎", "大象", "猴子", "兔子",
            "蝴蝶", "蜜蜂", "太阳", "月亮", "星星", "白云", "下雨", "下雪",
            "春天", "夏天", "秋天", "冬天", "学校", "教室", "家里", "公园",
            "医院", "商店", "马路", "车站", "房间", "大门", "红色", "黄色",
            "蓝色", "绿色", "白色", "黑色", "大小", "多少", "高矮", "长短",
        },
        [Difficulty.Easy] = new()
        {
            "同学", "老师", "教室", "黑板", "铅笔", "橡皮", "书包", "作业", "语文", "数学",
            "音乐", "画画", "跑步", "快乐", "朋友", "爸爸", "妈妈", "爷爷", "奶奶", "哥哥",
            "姐姐", "弟弟", "妹妹", "早上", "中午", "晚上", "今天", "明天", "昨天", "春天",
            "夏天", "秋天", "冬天", "太阳", "月亮", "星星", "白云", "小河", "大树", "花朵",
            "小鸟", "蝴蝶", "青蛙", "熊猫", "老虎", "兔子", "苹果", "香蕉", "西瓜", "牛奶",
            "面包", "鸡蛋", "米饭", "青菜", "回家", "上学", "读书", "写字", "唱歌", "跳舞"
        },
        [Difficulty.Normal] = new()
        {
            "图书馆", "运动会", "机器人", "望远镜", "指南针", "万里长城", "天安门",
            "科学家", "数学家", "美术课", "体育课", "少先队", "红领巾", "国旗",
            "努力", "坚持", "勇敢", "认真", "仔细", "专心", "团结", "友爱", "诚实",
            "守信", "勤劳", "节约", "环保", "健康", "安全", "文明", "礼貌",
            "三心二意", "一心一意", "聚精会神", "全神贯注", "日积月累", "勤学苦练",
            "春天的脚步", "金色的秋天", "丰收的季节", "快乐的节日", "美丽的校园",
            "碧绿的草地", "清澈的小溪", "广阔的草原", "遥远的星空", "温暖的阳光"
        },
        [Difficulty.Hard] = new()
        {
            "锲而不舍", "持之以恒", "废寝忘食", "精益求精", "一丝不苟", "孜孜不倦",
            "融会贯通", "举一反三", "温故知新", "学以致用", "水滴石穿", "铁杵成针",
            "闻鸡起舞", "悬梁刺股", "囊萤映雪", "凿壁偷光", "博览群书", "出口成章",
            "落笔生花", "妙笔生辉", "行云流水", "栩栩如生", "惟妙惟肖", "活灵活现",
            "气象万千", "波澜壮阔", "气势磅礴", "层峦叠嶂", "青山绿水", "鸟语花香",
            "万紫千红", "春暖花开", "秋高气爽", "冰天雪地", "繁星点点", "月朗星稀",
            "孜孜以求", "自强不息", "厚积薄发", "脚踏实地"
        }
    };

    #endregion 中文词组

    #region 英文句子

    /// <summary>英文句子：用于「句子」级练习，长度随难度增加。</summary>
    private static readonly Dictionary<Difficulty, List<string>> EnglishSentences = new()
    {
        // 入门档：单句、三到四词、无复杂标点
        [Difficulty.Entry] = new()
        {
            "I am a boy.", "She is my mom.", "It is a cat.", "We like to play.", "The sun is hot.", "I can see it.",
            "He is my dad.", "They are at home.", "This is my pen.", "That is a tree.", "I have a ball.", "You are my friend.",
            "The dog can run.", "My book is new.", "We go to school.", "It is very cold.", "She has a red hat.", "I like my room.",
            "The bird can fly.", "Let us go now.",
        },
        [Difficulty.Easy] = new()
        {
            "I have a red bag.",
            "The cat sits on the mat.",
            "We go to school by bus.",
            "She likes to read books.",
            "My dog can run very fast.",
            "The sun is big and bright.",
            "He plays with his toys.",
            "We eat rice and fish.",
            "The bird sings in the tree.",
            "I love my happy family."
        },
        [Difficulty.Normal] = new()
        {
            "Every morning I walk to school with my best friend.",
            "Our teacher tells us a story about a brave little rabbit.",
            "The library is quiet, so we can read books there.",
            "My mother cooks delicious noodles on Sunday evening.",
            "We planted some flowers in the school garden last week.",
            "The robot can clean the floor and wash the dishes.",
            "Reading good books helps us learn many new words.",
            "It is important to keep our classroom clean and tidy.",
            "The train travels through the mountains and over the river.",
            "Practice every day, and your typing will become faster."
        },
        [Difficulty.Hard] = new()
        {
            "Knowledge is a treasure that follows its owner everywhere, so we should never stop learning.",
            "Although the journey was long and difficult, the travelers never gave up their hope.",
            "The scientist explained the experiment carefully so that everyone could understand it.",
            "Reading widely is one of the best ways to improve both your vocabulary and your writing.",
            "If you keep practicing with patience and care, you will surely make remarkable progress.",
            "The ancient city has many beautiful buildings that attract visitors from around the world.",
            "Our environment needs protection, and small daily habits can make a great difference.",
            "There is no shortcut to success; it comes from steady effort and constant improvement.",
            "The library contains thousands of volumes covering history, science, and literature.",
            "Nature reminds us to stay humble, curious, and grateful for everything we have."
        }
    };

    #endregion 英文句子

    #region 中文句子

    /// <summary>中文句子：用于「句子」级练习，长度随难度增加。</summary>
    private static readonly Dictionary<Difficulty, List<string>> ChineseSentences = new()
    {
        // 入门档：四到八字，一句话说完
        [Difficulty.Entry] = new()
        {
            "我爱我的家。", "今天天气很好。", "小猫在睡觉。", "我们一起去玩。", "妈妈做的饭很香。", "我在写作业。",
            "树上有只小鸟。", "天上有白云。", "弟弟在学走路。", "我喜欢看书。", "爷爷在浇花。", "放学回家吧。",
            "这个苹果很甜。", "我会自己穿衣服。", "老师夸我了。", "下雨了要打伞。", "我们一起唱歌。", "小狗跑得很快。",
            "晚上早点睡。", "明天见。",
        },
        [Difficulty.Easy] = new()
        {
            "我喜欢在春天里放风筝。",
            "小河边的柳树发出了嫩芽。",
            "妈妈做的红烧肉特别香。",
            "下课以后我们在操场上跑步。",
            "天上的星星一闪一闪的。",
            "爷爷每天早晨都去公园打拳。",
            "教室里的黑板擦得干干净净。",
            "小猫趴在窗台上晒太阳。",
            "秋天的果园里挂满了红苹果。",
            "我们一起把书包整理好吧。"
        },
        [Difficulty.Normal] = new()
        {
            "清晨的阳光穿过树叶，在石板路上投下细碎的光斑。",
            "图书馆里静悄悄的，只能听见翻书页的沙沙声。",
            "他把作业本摊开，一笔一画地写下了今天的日记。",
            "山脚下的小村庄被薄薄的雾气轻轻笼罩着。",
            "老师告诉我们，遇到困难时不要着急，慢慢想办法。",
            "秋天的田野一片金黄，稻谷被风吹得像波浪一样起伏。",
            "我们班的同学一起把教室后面的墙报布置得十分漂亮。",
            "雨过天晴，天边挂起了一道弯弯的彩虹。",
            "每天坚持练字二十分钟，慢慢地就会看到进步。",
            "火车站里人来人往，广播里反复播报着列车信息。"
        },
        [Difficulty.Hard] = new()
        {
            "阅读经典文学作品，不仅能开阔我们的视野，还能让我们的心灵变得更加丰盈。",
            "科学技术的发展日新月异，它正在悄悄地改变着我们每一个人的生活。",
            "只要目标明确并且持之以恒地努力，再遥远的路也会一点点被走完。",
            "秋天的白洋淀，芦苇在风中轻轻摇曳，水面上偶尔掠过几只水鸟。",
            "老师常常鼓励我们多提问题，因为善于思考的人才能发现更多的可能。",
            "这座城市既保存着古朴的街巷，又矗立着现代化的摩天大楼。",
            "保护环境并不是一句口号，它体现在我们日常生活的每一个细节里。",
            "千百年来，无数能工巧匠用双手留下了令人惊叹的建筑与器物。",
            "真正的成长，是在一次次尝试与失败之后依然愿意继续向前。",
            "历史告诉我们，脚踏实地的努力远比一时的聪明更加可靠。"
        }
    };

    #endregion 中文句子

    #region 公开接口

    /// <summary>
    /// 取一批英文单词（供单词练习使用）：按难度抽取，数量由细分等级决定。
    /// </summary>
    /// <param name="difficulty">难度档位</param>
    /// <param name="level">细分等级（1~10），等级越高单词越多</param>
    /// <returns>以空格连接的单词串</returns>
    public static string GetEnglishWordText(Difficulty difficulty, int level)
    {
        List<string> pool = EnglishWords[difficulty];
        int count = GetWordCount(level);
        return string.Join(" ", PickRandom(pool, count));
    }

    /// <summary>
    /// 取一批中文词组（供词组练习使用）。
    /// </summary>
    /// <param name="difficulty">难度档位</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>以空格连接的中文词组串</returns>
    public static string GetChineseWordText(Difficulty difficulty, int level)
    {
        List<string> pool = ChineseWords[difficulty];
        int count = GetWordCount(level);
        return string.Join(" ", PickRandom(pool, count));
    }

    /// <summary>
    /// 取一批英文句子（供句子级练习使用）。
    /// </summary>
    /// <param name="difficulty">难度档位</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>拼接后的英文句子串</returns>
    public static string GetEnglishSentenceText(Difficulty difficulty, int level)
        => JoinSentences(EnglishSentences[difficulty], level);

    /// <summary>
    /// 取一批中文句子（供句子级练习使用）。
    /// </summary>
    /// <param name="difficulty">难度档位</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>拼接后的中文句子串</returns>
    public static string GetChineseSentenceText(Difficulty difficulty, int level)
        => JoinSentences(ChineseSentences[difficulty], level);

    /// <summary>
    /// 取一个英文单词（供游戏题库之类的轻量场景使用）。
    /// </summary>
    /// <param name="difficulty">难度档位</param>
    /// <returns>单个英文单词</returns>
    public static string GetRandomEnglishWord(Difficulty difficulty)
    {
        List<string> pool = EnglishWords[difficulty];
        return pool[Random.Shared.Next(pool.Count)];
    }

    /// <summary>英文单词表可读快照（供测试与工具使用）。</summary>
    public static IReadOnlyDictionary<Difficulty, List<string>> EnglishWordTable => EnglishWords;

    /// <summary>中文词组表可读快照（供测试与工具使用）。</summary>
    public static IReadOnlyDictionary<Difficulty, List<string>> ChineseWordTable => ChineseWords;

    #endregion 公开接口

    #region 私有工具

    /// <summary>
    /// 细分等级 → 本轮单词个数。
    ///
    /// 分档给量（而非线性）：入门档要给得少——刚接触键盘的孩子
    /// 一次打 4~6 个词刚好，多了会疲劳也会挫败。
    /// </summary>
    private static int GetWordCount(int level) => DifficultyScale.Clamp(level) switch
    {
        // 入门档（1~3 级）：少而精
        1 => 4,
        2 => 5,
        3 => 6,
        // 简单档（4~6 级）
        4 => 10,
        5 => 12,
        6 => 14,
        // 普通档（7~10 级）
        7 => 18,
        8 => 22,
        9 => 26,
        10 => 30,
        // 困难档（11~13 级）
        11 => 36,
        12 => 40,
        _ => 44
    };

    /// <summary>
    /// 细分等级 → 本轮句子条数。入门档只给 2 句，先打完再继续。
    /// </summary>
    private static int GetSentenceCount(int level) => DifficultyScale.Clamp(level) switch
    {
        1 => 2,
        2 => 2,
        3 => 3,
        4 => 4,
        5 => 5,
        6 => 5,
        7 => 6,
        8 => 7,
        9 => 7,
        10 => 8,
        11 => 9,
        12 => 10,
        _ => 11
    };

    /// <summary>从池中不放回地随机抽取 count 个（count 超过池容量时取全池）。</summary>
    private static List<string> PickRandom(List<string> pool, int count)
    {
        int n = Math.Min(count, pool.Count);
        return pool.OrderBy(_ => Random.Shared.Next()).Take(n).ToList();
    }

    /// <summary>拼接若干句子，句间以空格分隔。</summary>
    private static string JoinSentences(List<string> pool, int level)
    {
        int count = Math.Min(GetSentenceCount(level), pool.Count);
        var picked = pool.OrderBy(_ => Random.Shared.Next()).Take(count);
        return string.Join(" ", picked);
    }

    #endregion 私有工具
}