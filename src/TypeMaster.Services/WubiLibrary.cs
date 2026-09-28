using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>
/// 五笔练习的基本单元：一个汉字与其对应的五笔 86 版编码（取该字最短可用编码）。
/// </summary>
public sealed record WubiUnit(char Char, string Code);

/// <summary>
/// 五笔字根信息：一个字母键的键名字、助记口诀与主要字根，供学习园地展示。
/// </summary>
public sealed record WubiRootInfo(string Key, string KeyName, string Mnemonic, string Roots);

/// <summary>
/// 五笔 86 版内置字库：
/// 1) 提供约 400 个常用汉字的最短五笔编码（数据取自开源 rime-wubi86 码表）；
/// 2) 提供五笔字根表（25 个字母键的键名、口诀与主要字根），供学习园地页面展示；
/// 3) 按难度 / 细分等级生成五笔随机练习文本。
/// </summary>
public static class WubiLibrary
{
    #region 局部变量属性

    /// <summary>
    /// 常用汉字与其五笔 86 版最短编码（"字+码"逗号分隔），启动时一次性解析为字典。
    /// </summary>
    private const string CharCodeData =
        "一g,三dg,上h,下gh,不i,与gn,丑nfd,东ai,个wh,中k,为o,主y,么tc,义yq,之pp,乐qi,也"
        + "bn,习nu,书nnh,了b,事gk,二fg,于gf,云fcu,五gg,人w,从ww,他wb,们wu,件wrh,会wf,"
        + "传wfny,位wug,低wqa,体wsg,作wt,你wq,俗wwwk,保wk,修wht,假wnh,偏wyna,做wdt,"
        + "元fqb,兄kqb,光iq,入ty,公wc,关ud,其adw,具hw,内mw,写pgn,军pl,冬tuu,准uwy,出b"
        + "m,分wv,切av,删mmgj,到gc,制rmhj,前ue,剪uejv,动fcl,助egl,包qn,化wx,北ux,区a"
        + "q,医atd,十fgh,单ujfj,南fm,历dl,去fcu,又ccc,友dc,反rc,发v,叔hic,口kkkk,古d"
        + "gh,只kw,可sk,史kq,右dk,号kgn,合wgk,同m,后rg,和t,哀yeu,善uduk,喜fku,回lkd,"
        + "园lfq,困ls,国l,土ffff,在d,地f,场fnrt,坏fgi,域fakg,基ad,声fnr,复tjt,夏dht,"
        + "外qh,多qq,大dd,天gd,夹guw,奶ve,好vb,如vk,妹vfi,姐veg,姨vg,子bb,字pb,存dhb,"
        + "学ip,宇pgf,宋psu,宙pm,定pg,室pgc,家pe,容pww,宽pa,寓pjm,对cf,导nf,将uqf,小i"
        + "h,少it,就yi,尺nyi,局nnk,屏nua,属ntk,山mmm,工a,左da,市ymhj,师jgm,帮dt,幕aj"
        + "dh,平gu,年rh,序ycb,度ya,开ga,弟uxh,弯yox,当iv,录vi,影jyie,得tj,心ny,快nnw"
        + ",态dyn,怒vcn,恨nv,恶gogn,悲djdn,感dgkn,慢nj,戏ca,成dn,我q,所rn,手rt,打rs,"
        + "找ra,折rr,拼rua,指rxj,按rpv,换rq,捺rdfi,控rpw,提rj,撇rumt,撤ryc,改nty,故d"
        + "ty,效uqt,教ftbt,散aet,数ovt,整gkih,文yygy,新usr,方yy,旁upy,无fq,日jjjj,"
        + "旧hj,时jf,明je,星jtg,春dw,是j,昵jnx,显jo,普uo,暗ju,曲ma,替fwf,最jb,有e,朋ee"
        + ",木ssss,本sg,机sm,来go,板src,构sq,查sj,标sfi,栏suf,校suq,格st,桌hjs,档si,"
        + "椅sds,植sfhg,横sam,橙swgu,橡sqj,欢cqw,歇jqw,歌sksw,正ghd,此hx,步hi,母xgu"
        + ",气rnb,水ii,汉ic,江ia,没im,河isk,法if,泳iyni,洋iu,海itx,清ige,游iytb,湖id"
        + "e,火ooo,灰do,点hko,然qd,爱ep,父wqu,爷wqb,物tr,状udy,率yx,玻ghc,球gfi,理gj"
        + ",璃gyb,生tg,用et,田lll,电jn,画gl,白rrr,的r,皮hc,盘tel,看rhf,真fhw,着udh,短"
        + "tdg,码dcg,硬dgj,确dqe,示fi,神pyj,离yb,秋to,种tkh,科tu,称tq,程tkgg,空pw,窄"
        + "pwtf,窗pwt,竖jcu,站uh,章ujj,童ujff,笔tt,符twf,简tuj,管tp,篮tjtl,粉ow,粘o"
        + "h,系txi,紫hxx,红xa,线xg,练xan,经x,结xf,统xyc,绩xgm,绿xv,编xyna,置lfhf,美u"
        + "gdu,翘atgn,老ftx,者ftj,而dmj,能ce,脑eyb,自thd,舌tdd,舞rlg,航tey,色qc,节a"
        + "b,花awx,草ajj,菜ae,蓝ajt,行tf,表ge,西sghg,要s,见mqb,视pym,解qev,言yyy,计y"
        + "f,认yw,记yn,设ymc,词yngk,诗yff,话ytd,语ygk,说yu,读yfn,课yjs,谚yut,质rfm,"
        + "贴mhkg,起fhn,超fhv,足khu,跃khtd,跑khq,跳khi,蹈khev,车lg,转lfn,软lqw,轻lc"
        + ",载fa,输lwg,过fp,运fcp,近rp,还gip,这p,进fj,选tfqp,通cep,速gkip,道uthp,那v"
        + "fb,邮mb,部uk,都ftjb,里jfd,重tgj,金qqqq,钩qqc,钮qnf,铅qmk,银qve,销qie,键q"
        + "vfp,长ta,门uyh,闭uft,阿bs,院bpf,除bwt,难cw,雨fghy,雪fv,面dm,音ujf,韵ujqu"
        + ",页dmu,项adm,顺kd,题jghm,颜utem,风mq,首uth,高ym,黄amw,黑lfo,鼠vnu,鼻thl";

    /// <summary>
    /// 汉字 → 最短五笔编码字典（从 CharCodeData 解析而来）。
    /// </summary>
    private static readonly Dictionary<char, string> CharCodes = BuildCharCodes();

    /// <summary>
    /// 五笔 86 版字根表：25 个字母键的键名字、助记口诀与主要字根。
    /// </summary>
    private static readonly List<WubiRootInfo> RootTable = new()
    {
        new("G", "王", "王旁青头戋（兼）五一", "王 一 五 戋 龶（青头）"),
        new("F", "土", "土士二干十寸雨", "土 士 二 干 十 寸 雨"),
        new("D", "大", "大犬三羊古石厂", "大 犬 三 古 石 厂 𠦶（羊去尾）"),
        new("S", "木", "木丁西", "木 丁 西"),
        new("A", "工", "工戈草头右框七", "工 戈 艹 匚 七 弋 廾"),
        new("H", "目", "目具上止卜虎皮", "目 上 止 卜 且 具 虔（虎头） 皮"),
        new("J", "日", "日早两竖与虫依", "日 曰 早 刂 虫 川（两竖）"),
        new("K", "口", "口与川，字根稀", "口 川"),
        new("L", "田", "田甲方框四车力", "田 甲 囗 四 车 力 皿"),
        new("M", "山", "山由贝，下框几", "山 由 贝 冂 几"),
        new("T", "禾", "禾竹一撇双人立，反文条头共三一", "禾 竹 丿 彳（双人立） 攵（反文） 夂（条头）"),
        new("R", "白", "白手看头三二斤", "白 手 扌 斤 𠂒（看头）"),
        new("E", "月", "月彡乃用家衣底", "月 用 乃 彡 豕（家底） 𧘇（衣底）"),
        new("W", "人", "人和八，三四里", "人 亻 八 癶"),
        new("Q", "金", "金勺缺点无尾鱼，犬旁留叉儿一点夕，氏无七", "金 钅 勹 鱼 犭 儿 夕 氏（无七）"),
        new("Y", "言", "言文方广在四一，高头一捺谁人去", "言 讠 文 方 广 亠 圭 丶"),
        new("U", "立", "立辛两点六门疒", "立 辛 冫 六 门 疒 丬"),
        new("I", "水", "水旁兴头小倒立", "水 氵 小 ⺌ 兴（兴头）"),
        new("O", "火", "火业头，四点米", "火 业 米 灬"),
        new("P", "之", "之字军盖建道底，摘礻衣", "之 宀 冖 辶 廴 礻 衤"),
        new("N", "已", "已半巳满不出己，左框折尸心和羽", "已 巳 己 乙 尸 ⺕ 心 忄 羽"),
        new("B", "子", "子耳了也框向上", "子 了 耳 阝 也 凵 孑"),
        new("V", "女", "女刀九臼山朝西", "女 刀 九 臼 彐（山朝西）"),
        new("C", "又", "又巴马，丢矢矣", "又 巴 马 厶"),
        new("X", "纟", "慈母无心弓和匕，幼无力", "纟 幺 弓 匕 母（无心） 幺"),
    };

    #endregion 局部变量属性

    #region 编码查询

    /// <summary>
    /// 尝试获取某个汉字的五笔 86 版最短编码。
    /// </summary>
    /// <param name="ch">待查询的汉字</param>
    /// <param name="code">查询成功时输出编码，失败时为空串</param>
    /// <returns>字库中存在该字返回 true，否则返回 false</returns>
    public static bool TryGetCode(char ch, out string code)
    {
        if (CharCodes.TryGetValue(ch, out var found))
        {
            code = found;
            return true;
        }
        code = string.Empty;
        return false;
    }

    /// <summary>
    /// 将一段中文文本转换为五笔练习单元序列：
    /// 只保留字库中存在编码的汉字，其余字符（标点 / 生僻字等）自动跳过。
    /// </summary>
    /// <param name="text">原始中文文本</param>
    /// <returns>五笔练习单元列表（可能为空）</returns>
    public static List<WubiUnit> BuildUnits(string text)
    {
        var units = new List<WubiUnit>();
        if (string.IsNullOrEmpty(text))
        {
            return units;
        }
        foreach (char ch in text)
        {
            if (CharCodes.TryGetValue(ch, out string? code))
            {
                units.Add(new WubiUnit(ch, code));
            }
        }
        return units;
    }

    #endregion 编码查询

    #region 练习文本生成

    /// <summary>
    /// 按难度 / 细分等级生成一段五笔随机练习文本（仅含字库内汉字，含少量标点分隔）。
    /// 细分等级决定文本长度：等级越高，练习单元越多。
    /// </summary>
    /// <param name="difficulty">三档难度（影响随机池，目前统一使用常用字池）</param>
    /// <param name="level">细分等级（1~10）</param>
    /// <returns>练习用中文文本</returns>
    public static string GetRandomPractice(Difficulty difficulty, int level)
    {
        // 目标单元数：1 级约 8 字，10 级约 40 字，线性递增
        int unitCount = 6 + DifficultyScale.Clamp(level) * 4;

        var keys = CharCodes.Keys.ToList();
        var rand = new System.Random();
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < unitCount; i++)
        {
            builder.Append(keys[rand.Next(keys.Count)]);
            // 每 6~8 个字插入一个顿号分隔，方便换字重打
            if ((i + 1) % (6 + rand.Next(3)) == 0)
            {
                builder.Append('，');
            }
        }
        return builder.ToString();
    }

    #endregion 练习文本生成

    #region 字根表

    /// <summary>获取五笔 86 版字根表（25 个字母键，按键盘分区排序）。</summary>
    /// <returns>字根信息列表</returns>
    public static IReadOnlyList<WubiRootInfo> GetRoots() => RootTable;

    /// <summary>获取五笔 86 版字根口诀总述（学习园地顶部展示）。</summary>
    /// <returns>口诀说明文本</returns>
    public static string GetMnemonicIntro() =>
        "五笔字型 86 版将 25 个字母键分为 5 个区：横（G F D S A）、竖（H J K L M）、" +
        "撇（T R E W Q）、捺（Y U I O P）、折（N B V C X）。" +
        "每句口诀描述该键上的主要字根，记熟口诀即可快速定位字根所在键位。";

    #endregion 字根表

    #region 朗读文本

    /// <summary>
    /// 口诀中会被语音读错的生僻字根 → 同音常用字。
    ///
    /// 背景（本机实测结论）：Windows 内置中文语音对字根类生僻字常读错，
    /// 例如「戋」被读成 jīn（巾）、「疒」被读成 bìng（病）、「彡」被读成 sān（三）。
    /// 这里用同音常用字替代，**只影响朗读，不影响界面显示的文字**。
    /// </summary>
    private static readonly Dictionary<char, char> SpeechOverrides = new()
    {
        ['戋'] = '兼',   // jiān，避免被读成“巾”
        ['疒'] = '讷',   // nè，避免被读成“病”
        ['彡'] = '山',   // shān，避免被读成“三”
        ['纟'] = '丝',   // sī，避免被读成“减 / 角”（X 键的键名）
        ['礻'] = '示',   // shì，避免被读成“十”（P 键口诀中的示字旁）
        ['衤'] = '衣',   // yī，衣字旁与“衣”同音
    };

    /// <summary>
    /// 生成某条口诀的朗读文本：剥离括号内的读音提示，并对生僻字根做同音替换。
    ///
    /// 为什么要剥离括号：口诀里的「戋（兼）」括号内是给读者看的读音提示，
    /// 实测朗读时会连同提示一起读出（读成「…尖，尖…」），必须去掉。
    /// </summary>
    /// <param name="mnemonic">口诀原文</param>
    /// <returns>适合朗读的文本</returns>
    public static string GetSpeakText(string mnemonic)
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
            return string.Empty;
        }

        return ApplyOverrides(StripBrackets(mnemonic));
    }

    /// <summary>
    /// 生成单个键位字根的朗读文本：先读字母键与键名，再读口诀。
    /// 字母键（如 G）用英文字母读法即可——它本身就是键盘上的字母键；
    /// 键名（如「纟」）同样需要注音覆盖，否则会被语音读错。
    /// </summary>
    /// <param name="root">字根信息</param>
    /// <returns>朗读文本</returns>
    public static string GetSpeakText(WubiRootInfo root)
    {
        if (root is null)
        {
            return string.Empty;
        }
        return ApplyOverrides($"{root.Key} 键，{root.KeyName}，{StripBrackets(root.Mnemonic)}");
    }

    /// <summary>
    /// 生成全部 25 条口诀的朗读文本（供「朗读全部口诀」按钮使用）。
    /// </summary>
    /// <returns>连贯的口诀朗读文本</returns>
    public static string GetRootsSpeakText()
    {
        var parts = RootTable.Select(r => GetSpeakText(r.Mnemonic));
        return string.Join("，", parts);
    }

    /// <summary>
    /// 生成口诀总述的朗读文本（剥离括号，其中的 G F D S A 等是键盘字母键，按英文读即可）。
    /// </summary>
    /// <returns>朗读文本</returns>
    public static string GetIntroSpeakText() => GetSpeakText(GetMnemonicIntro());

    #endregion 朗读文本

    #region 朗读私有工具

    /// <summary>
    /// 剥离括号内的读音提示。
    /// 口诀里「戋（兼）」的括号内容是给读者看的读音提示，实测朗读时会连带读出，
    /// 因此生成朗读文本时必须去掉。
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>去掉括号提示后的文本</returns>
    private static string StripBrackets(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        bool inBracket = false;

        foreach (char c in text)
        {
            // 全角 / 半角括号内的内容一律跳过（那是读音提示，不是口诀本身）
            if (c is '（' or '(')
            {
                inBracket = true;
                continue;
            }
            if (c is '）' or ')')
            {
                inBracket = false;
                continue;
            }
            if (!inBracket)
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 把生僻字根替换为同音常用字，见 <see cref="SpeechOverrides"/>。
    /// 只影响朗读，不影响界面显示的文字。
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>替换后的文本</returns>
    private static string ApplyOverrides(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            builder.Append(SpeechOverrides.TryGetValue(c, out char fixedChar) ? fixedChar : c);
        }
        return builder.ToString();
    }

    #endregion 朗读私有工具

    #region 私有工具

    /// <summary>
    /// 解析 CharCodeData 常量为"汉字 → 编码"字典。
    /// 数据中的若干词组片段（如"前后"这类无编码项）会被自动跳过。
    /// </summary>
    /// <returns>汉字编码字典</returns>
    private static Dictionary<char, string> BuildCharCodes()
    {
        var map = new Dictionary<char, string>();
        foreach (string part in CharCodeData.Split(','))
        {
            // 每段应为"1 个汉字 + 若干编码字母"，长度不足或超出的片段直接忽略
            if (part.Length < 2 || part.Length > 5)
            {
                continue;
            }
            char ch = part[0];
            string code = part[1..];
            if (!IsAsciiLower(code) || map.ContainsKey(ch))
            {
                continue;
            }
            map[ch] = code;
        }
        return map;
    }

    /// <summary>判断字符串是否全部由小写字母组成（五笔编码仅含 a~y）。</summary>
    /// <param name="text">待检查的字符串</param>
    /// <returns>全部为小写字母返回 true，否则返回 false</returns>
    private static bool IsAsciiLower(string text)
    {
        foreach (char c in text)
        {
            if (c is < 'a' or > 'z')
            {
                return false;
            }
        }
        return true;
    }

    #endregion 私有工具
}
