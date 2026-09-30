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
/// 一个字的逐码按键提示：把编码拆成"第几码 → 按哪个键（键名是什么）"。
///
/// 用途：面向初学五笔的中老年用户。他们最直接的困难是"这个字该按哪个键"，
/// 而五笔编码的每一位本身就对应一个键位，因此可以确定地给出指引，
/// 不需要掌握字根拆解推理。
/// </summary>
/// <param name="Char">汉字</param>
/// <param name="Code">完整最短编码</param>
/// <param name="Keys">逐位键位提示</param>
public sealed record WubiCharHint(char Char, string Code, IReadOnlyList<WubiKeyStep> Keys);

/// <summary>
/// 逐码提示中的一步：第几位、按哪个字母键、该键的键名字。
/// </summary>
/// <param name="Index">第几码（从 1 开始）</param>
/// <param name="Key">字母键（大写，如 E）</param>
/// <param name="KeyName">该键的键名字（如「月」），取自 25 键字根表</param>
public sealed record WubiKeyStep(int Index, string Key, string KeyName);

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
    ///
    /// 规模：6489 字。来源与口径：
    ///   字表 —— 《通用规范汉字表》(2013) 一级字表(3500) + 二级字表(3000)，
    ///           三级字表(1605) 为专业领域生僻字，刻意**不收录**，
    ///           避免初学者练习时碰到「龘 / 甪」这类字。
    ///   编码 —— rime-wubi86 官方码表，取该字最短可用编码（含一级/二级/三级简码），
    ///           与玩家在实际五笔输入法中的击键一致。
    ///
    /// 为什么要扩到这么大：早期版本仅 404 字，最常用 140 字只覆盖 74%，
    /// 缺「她 以 把 第 样 想 情 女 很 给 名 间 知 世 两 身 被 高 已 常 活」等，
    /// 学习者连一句完整的话都打不出来。
    /// </summary>
    private const string CharCodeData =
        "一g,三dg,上h,下gh,不i,与gn,丑nfd,东ai,个wh,中k,为o,主y,么tc,义yq,之pp,乐qi,也bn,习nu,书nnh,了b,事gk,二fg,"
        + "于gf,云fcu,五gg,人w,从ww,他wb,们wu,件wrh,会wf,传wfny,位wug,低wqa,体wsg,作wt,你wq,俗wwwk,保wk,修wht,假wnh,偏wyna,做wdt,元fqb,"
        + "兄kqb,光iq,入ty,公wc,关ud,其adw,具hw,内mw,写pgn,军pl,冬tuu,准uwy,出bm,分wv,切av,删mmgj,到gc,制rmhj,前ue,剪uejv,动fcl,助egl,"
        + "包qn,化wx,北ux,区aq,医atd,十fgh,单ujfj,南fm,历dl,去fcu,又ccc,友dc,反rc,发v,叔hic,口kkkk,古dgh,只kw,可sk,史kq,右dk,号kgn,"
        + "合wgk,同m,后rg,和t,哀yeu,善uduk,喜fku,回lkd,园lfq,困ls,国l,土ffff,在d,地f,场fnrt,坏fgi,域fakg,基ad,声fnr,复tjt,夏dht,外qh,"
        + "多qq,大dd,天gd,夹guw,奶ve,好vb,如vk,妹vfi,姐veg,姨vg,子bb,字pb,存dhb,学ip,宇pgf,宋psu,宙pm,定pg,室pgc,家pe,容pww,宽pa,"
        + "寓pjm,对cf,导nf,将uqf,小ih,少it,就yi,尺nyi,局nnk,屏nua,属ntk,山mmm,工a,左da,市ymhj,师jgm,帮dt,幕ajdh,平gu,年rh,序ycb,度ya,"
        + "开ga,弟uxh,弯yox,当iv,录vi,影jyie,得tj,心ny,快nnw,态dyn,怒vcn,恨nv,恶gogn,悲djdn,感dgkn,慢nj,戏ca,成dn,我q,所rn,手rt,打rs,"
        + "找ra,折rr,拼rua,指rxj,按rpv,换rq,捺rdfi,控rpw,提rj,撇rumt,撤ryc,改nty,故dty,效uqt,教ftbt,散aet,数ovt,整gkih,文yygy,新usr,方yy,旁upy,"
        + "无fq,日jjjj,旧hj,时jf,明je,星jtg,春dw,是j,昵jnx,显jo,普uo,暗ju,曲ma,替fwf,最jb,有e,朋ee,木ssss,本sg,机sm,来go,板src,"
        + "构sq,查sj,标sfi,栏suf,校suq,格st,桌hjs,档si,椅sds,植sfhg,横sam,橙swgu,橡sqj,欢cqw,歇jqw,歌sksw,正ghd,此hx,步hi,母xgu,气rnb,水ii,"
        + "汉ic,江ia,没im,河isk,法if,泳iyni,洋iu,海itx,清ige,游iytb,湖ide,火ooo,灰do,点hko,然qd,爱ep,父wqu,爷wqb,物tr,状udy,率yx,玻ghc,"
        + "球gfi,理gj,璃gyb,生tg,用et,田lll,电jn,画gl,白rrr,的r,皮hc,盘tel,看rhf,真fhw,着udh,短tdg,码dcg,硬dgj,确dqe,示fi,神pyj,离yb,"
        + "秋to,种tkh,科tu,称tq,程tkgg,空pw,窄pwtf,窗pwt,竖jcu,站uh,章ujj,童ujff,笔tt,符twf,简tuj,管tp,篮tjtl,粉ow,粘oh,系txi,紫hxx,红xa,"
        + "线xg,练xan,经x,结xf,统xyc,绩xgm,绿xv,编xyna,置lfhf,美ugdu,翘atgn,老ftx,者ftj,而dmj,能ce,脑eyb,自thd,舌tdd,舞rlg,航tey,色qc,节ab,"
        + "花awx,草ajj,菜ae,蓝ajt,行tf,表ge,西sghg,要s,见mqb,视pym,解qev,言yyy,计yf,认yw,记yn,设ymc,词yngk,诗yff,话ytd,语ygk,说yu,读yfn,"
        + "课yjs,谚yut,质rfm,贴mhkg,起fhn,超fhv,足khu,跃khtd,跑khq,跳khi,蹈khev,车lg,转lfn,软lqw,轻lc,载fa,输lwg,过fp,运fcp,近rp,还gip,这p,"
        + "进fj,选tfqp,通cep,速gkip,道uthp,那vfb,邮mb,部uk,都ftjb,里jfd,重tgj,金qqqq,钩qqc,钮qnf,铅qmk,银qve,销qie,键qvfp,长ta,门uyh,闭uft,阿bs,"
        + "院bpf,除bwt,难cw,雨fghy,雪fv,面dm,音ujf,韵ujqu,乙nnl,丁sgh,厂dgt,七ag,卜hhy,八wty,儿qt,匕xtn,几mt,九vt,刁ngd,刀vn,力lt,乃etn,"
        + "干fggh,亏fnv,士fghg,才ft,寸fghy,丈dyi,万dnv,巾mhk,千tfk,乞tnb,川kthh,亿wn,夕qtny,久qy,勺qyi,凡my,丸vyi,及ey,广yygt,亡ynv,丫uhk,尸nngt,"
        + "己nng,已nnnn,巳nngn,弓xng,卫bg,女vvv,刃vyi,飞nui,叉cyi,马cn,乡xte,丰dh,王ggg,井fjk,夫fw,专fny,丐ghn,扎rnn,艺anb,支fc,厅ds,犬dgty,"
        + "太dy,歹gqi,尤dnv,匹aqv,巨and,牙ah,屯gb,戈agnt,比xx,互gx,瓦gny,止hh,曰jhng,贝mhny,冈mqi,午tfj,牛rhk,毛tfn,壬tfd,升tak,夭tdi,仁wfg,"
        + "什wfh,片thg,仆why,仇wvn,币tmh,仍we,仅wcy,斤rtt,爪rhyi,介wj,仑wxb,今wynb,凶qb,乏tpi,仓wbb,月eee,氏qa,勿qre,欠qw,风mq,丹myd,匀qu,"
        + "乌qng,勾qci,凤mc,六uy,亢ymb,斗ufk,忆nn,订ys,户yne,冗pmb,讥ymn,引xh,巴cnh,孔bnn,队bw,办lw,以c,允cq,予cbj,邓cb,劝cl,双cc,"
        + "幻xnn,玉gy,刊fjh,未fii,末gs,击fmk,巧agnn,扑rhy,卉faj,扒rwy,功al,扔re,甘afd,世an,艾aqu,术sy,丙gmw,厉ddn,石dgtg,布dmh,夯dlb,戊dny,"
        + "龙dx,灭goi,轧lnn,卡hhu,占hk,凸hgm,卢hn,业og,帅jmh,归jv,旦jgf,目hhhh,且eg,叶kf,甲lhnh,申jhk,叮ksh,由mh,叭kwy,央md,叽kmn,叼kng,"
        + "叫kn,叩kbh,叨kvn,另kl,叹kcy,冉mfd,皿lhn,凹mmgd,囚lwi,四lh,矢tdu,失rw,乍thf,禾ttt,丘rgd,付wfy,仗wdyy,代wa,仙wm,仪wyq,仔wbg,斥ryi,"
        + "瓜rcy,乎tuh,丛wwg,令wyc,甩en,印qgb,尔qiu,句qkd,匆qry,册mm,卯qtbh,犯qtb,处th,鸟qyng,务tl,饥qnm,立uu,冯uc,玄yxu,闪uw,兰uff,半uf,"
        + "汁ifh,汇ian,头udi,宁ps,穴pwu,它px,讨yfy,让yh,礼pynn,训yk,议yyq,必nt,讯ynf,永yni,司ngk,尼nx,民n,弗xjk,弘xcy,辽bp,奴vcy,召vkf,"
        + "加lk,边lp,孕ebf,圣cff,台ck,矛cbt,纠xnh,幼xln,丝xxg,邦dtb,式aa,迂gfp,刑gajh,戎ade,扛rag,寺ff,吉fk,扣rk,考ftg,托rta,巩amy,圾fe,"
        + "执rvy,扩ry,扫rv,扬rnr,耳bgh,芋agf,共aw,芒ayn,亚gog,芝ap,朽sgnn,朴shy,权sc,臣ahn,吏gkq,再gmf,协fl,压dfy,厌ddi,戌dgn,百dj,页dmu,"
        + "匠ar,夸dfn,夺df,达dp,列gq,死gqx,夷gxw,轨lv,邪ahtb,尧atgq,划aj,迈dnp,毕xxf,至gcf,贞hm,尘iff,尖id,劣itl,早jh,吁kgfh,吐kfg,吓kgh,"
        + "虫jhny,团lft,吕kk,吊kmh,吃ktn,因ld,吸ke,吗kcg,吆kxy,屿mgn,屹mtnn,岁mqu,帆mhm,岂mn,则mj,刚mqj,网mqq,肉mww,朱ri,先tfq,丢tfc,廷tfpd,"
        + "竹ttg,迁tfp,乔tdj,迄tnp,伟wfn,乒rgt,乓rgy,休ws,伍wgg,伏wdy,优wdn,臼vth,伐wat,延thp,仲wkhh,任wtf,伤wtl,价wwj,伦wwx,份wwv,华wxf,仰wqbh,"
        + "仿wyn,伙wo,伪wyl,伊wvt,血tld,向tm,似wny,舟tei,全wg,杀qsu,兆iqv,企whf,众www,伞wuh,创wbj,肌em,肋el,朵ms,杂vs,危qdb,旬qj,旨xj,"
        + "旭vj,负qm,匈qqb,名qk,各tk,争qv,壮ufg,冲ukh,妆uv,冰ui,庄yfd,庆yd,亦you,刘yj,齐yjj,交uq,衣ye,次uqw,产u,决un,亥yntw,充yc,"
        + "妄ynvf,问ukd,闯ucd,羊udj,并ua,米oy,灯os,州ytyh,汗ifh,污ifn,汛inf,池ib,汝ivg,汤inr,忙nynn,兴iw,守pf,宅pta,安pv,讲yfj,讳yfnh,讶yah,"
        + "许ytf,讹ywxn,论ywx,讼ywc,农pei,讽ymq,访yyn,诀ynwy,寻vf,迅nfp,尽nyu,异naj,弛xb,孙bi,阵bl,阳bj,收nh,阶bwj,阴be,防by,奸vfh,妇vv,"
        + "妃vnn,她vbn,妈vc,羽nny,观cm,买nudu,驮cdy,纤xtf,驯ckh,约xq,级xe,纪xn,驰cbn,纫xvy,巡vp,寿dtf,弄gaj,麦gtu,玖gqy,玛gcg,形gae,戒aak,"
        + "吞gdk,远fqp,违fnhp,韧fnhy,扶rfw,抚rfq,坛ffc,技rfc,抠raq,扰rdn,扼rdb,拒ran,批rx,址fhg,扯rhg,走fhu,抄rit,贡am,汞aiu,坝fmy,攻at,赤fo,"
        + "抓rrhy,扳rrc,抡rwx,扮rwv,抢rwb,孝ftb,坎fqw,均fqu,抑rqb,抛rvl,投rmc,坟fy,坑fym,抗rymn,坊fyn,抖rufh,护ryn,壳fpm,志fn,块fnw,扭rnf,把rcn,"
        + "报rb,拟rny,却fcb,抒rcb,劫fcln,芙afwu,芜afqb,苇afn,芽aah,芹arj,芥awj,芬awv,苍awb,芳ay,严god,芦aynr,芯anu,劳apl,克dq,芭ac,苏alw,杆sfh,"
        + "杠sag,杜sfg,材sft,村sf,杖sdy,杏skf,杉set,巫aww,极se,李sb,杨sn,求fiy,甫geh,匣alk,更gjq,束gki,吾gkf,豆gku,两gmww,酉sgd,丽gmy,辰dfe,"
        + "励ddnl,否gik,尬dnw,歼gqt,连lpk,轩lf,卤hl,坚jcf,肖ie,旱jfj,盯hs,呈kg,吴kgd,县egc,呆ks,吱kfc,吠kdy,呕kaqy,旷jyt,围lfnh,呀ka,吨kgb,"
        + "男ll,吵ki,串kkh,员km,呐kmw,听kr,吟kwyn,吩kwv,呛kwb,吻kqr,吹kqw,呜kqng,吭kym,吧kc,邑kcb,吼kbn,囤lgb,别klj,吮kcq,岖maq,岗mmq,帐mht,"
        + "财mf,针qf,钉qs,牡trfg,告tfkf,乱tdn,利tjh,秃tmb,秀te,私tcy,每txg,兵rgw,估wd,何wsk,佐wda,佑wdk,但wjg,伸wjh,佃wl,伯wr,伶wwyc,佣weh,"
        + "住wygg,伴wuf,身tmd,皂rab,伺wng,佛wxj,囱tlqi,彻tavn,役tmc,返rcp,余wtu,希qdm,坐wwf,谷wwk,妥ev,含wynk,邻wycb,岔wvmj,肝ef,肛ea,肚efg,肘efy,"
        + "肠enr,龟qjn,甸ql,免qkq,狂qtg,犹qtdn,狈qtmy,角qe,条ts,彤mye,卵qyt,灸qyo,岛qynm,刨qnjh,迎qbp,饭qnr,饮qnq,冻uai,亩ylf,况ukq,床ysi,库ylk,"
        + "庇yxx,疗ubk,吝ykf,应yid,冷uwyc,庐yyne,辛uygh,弃yca,冶uck,忘ynnu,闰ug,闲usi,间uj,闷uni,判udjh,兑ukqb,灶of,灿om,灼oqy,汪ig,沐isy,沛igmh,"
        + "汰idy,沥idl,沙iit,汽irn,沃itdy,沦iwx,汹iqbh,泛itp,沧iwb,沟iqc,沪iyn,沈ipq,沉ipm,沁in,怀ng,忧ndn,忱np,完pfq,宏pdc,牢prh,究pwv,穷pwl,"
        + "灾po,良yv,证ygh,启ynk,评ygu,补puh,初puv,社py,祀pynn,识ykw,诈yth,诉yr,罕pwf,诊ywe,译ycf,君vtkd,灵vo,即vcb,层nfc,屁nxx,尿nii,尾ntf,"
        + "迟nyp,张xt,忌nnu,际bf,陆bfm,陈ba,阻begg,附bwf,坠bwff,妓vfc,妙vit,妖vtd,姊vtnt,妨vy,妒vynt,努vcl,忍vynu,劲cal,矣ct,鸡cqy,纬xfnh,驱caq,"
        + "纯xgb,纱xi,纲xm,纳xmw,驳cqq,纵xww,纷xwv,纸xqa,纹xyy,纺xy,驴cyn,纽xnf,奉dwf,玩gfq,环ggi,武gah,青gef,责gmu,现gm,玫gt,规fwm,抹rgs,"
        + "卦ffhy,坷fsk,坯fgig,拓rd,拢rdx,拔rdc,坪fgu,拣ranw,坦fjg,担rjg,坤fjhh,押rl,抽rm,拐rkl,拖rtb,拍rrg,顶sdm,拆rry,拎rwyc,拥reh,抵rqa,拘rqk,"
        + "势rvyl,抱rqn,拄ryg,垃fug,拉ru,拦ruf,幸fuf,拌rufh,拧rps,拂rxjh,拙rbm,招rvk,坡fhc,披rhc,拨rnt,择rcf,抬rck,拇rxg,拗rxl,取bc,茉ags,苦adf,"
        + "昔ajf,苛as,若adk,茂adn,苹agu,苗alf,英amd,苟aqkf,苑aqb,苞aqn,范aib,直fh,茁abm,茄alkf,茎aca,苔ack,茅acbt,枉sgg,林ss,枝sfc,杯sgi,枢saq,"
        + "柜san,枚sty,析sr,松swc,枪swb,枫smq,杭sym,杰so,述syp,枕spq,丧fue,或ak,卧ahnh,刺gmi,枣gmiu,卖fnud,郁deb,矾dmy,矿dyt,厕dmjk,奈dfi,奔dfa,"
        + "奇dskf,奋dlf,欧aqq,殴aqm,垄dxf,妻gv,轰lcc,顷xd,斩lr,轮lwx,非djd,歧hfc,肯he,齿hwb,些hxf,卓hjj,虎ha,虏halv,肾jce,贤jcm,尚imkf,旺jgg,"
        + "味kfi,果js,昆jx,哎kaq,咕kdg,昌jj,呵ksk,畅jhnr,易jqr,咙kdx,昂jqb,迪mp,典maw,固ldd,忠khn,呻kjh,咒kkm,咋kthf,咐kwf,呼kt,鸣kqy,咏kyn,"
        + "呢knx,咄kbm,咖klk,岸mdfj,岩mdf,帖mhh,罗lq,帜mhkw,帕mhr,岭mwyc,凯mnm,败mty,账mta,贩mr,贬mtp,购mqc,贮mpg,图ltu,钓qqy,知td,迭rwp,氛rnw,"
        + "垂tga,牧trt,乖tfu,刮tdjh,秆tfh,季tb,委tv,秉tgv,佳wffg,侍wff,岳rgm,供waw,使wgkq,例wgq,侠wgu,侥watq,版thgc,侄wgcf,侦whm,侣wkk,侧wmj,凭wtfm,"
        + "侨wtd,佩wmg,货wxm,侈wqq,依wye,卑rtfj,迫rpd,欣rqw,征tgh,往tyg,爬rhyc,彼thc,径tca,舍wfk,刹qsj,命wgkb,肴qde,斧wqr,爸wqc,采es,觅emq,受epc,"
        + "乳ebn,贪wynm,念wynn,贫wvm,忿wvnu,肤efw,肺egm,肢efc,肿ek,胀eta,股emc,肮eym,肪eyn,肥ec,服eb,胁elw,周mfk,昏qajf,鱼qgf,兔qkqy,狐qtr,忽qrn,"
        + "狗qtq,狞qtp,备tlf,饰qnth,饱qnqn,饲qnnk,变yo,京yiu,享ybf,庞ydx,店yhk,夜ywt,庙ymd,府ywf,底yqa,疟uagd,疙utn,疚uqy,剂yjjh,卒ywwf,郊uqb,庚yvw,"
        + "废ynty,净uqv,盲ynh,放yt,刻ynt,育yce,氓ynna,闸ulk,闹uym,郑udb,券udv,卷udbb,炬oan,炒oi,炊oqw,炕oym,炎oo,炉oyn,沫igs,浅igt,泄iann,沽idg,"
        + "沾ihk,泪ihg,沮ieg,油img,泊ir,沿imk,泡iqn,注iy,泣iug,泞ips,泻ipgg,泌int,泥inx,沸ixj,沼ivk,波ihc,泼inty,泽icf,治ick,怔ngh,怯nfcy,怖ndm,"
        + "性ntg,怕nr,怜nwyc,怪nc,怡nck,宝pgy,宗pfi,宠pdx,宜peg,审pj,官pn,帘pwm,宛pq,实pu,试yaa,郎yvcb,肩yned,房yny,诚ydn,衬puf,衫pue,祈pyr,"
        + "诞ythp,诡yqd,询yqj,该yynw,详yud,建vfhp,肃vij,隶vii,帚vpm,屉nan,居nd,届nm,刷nmh,屈nbm,弧xrc,弥xqi,弦xyx,承bd,孟blf,陋bgm,陌bdj,孤br,"
        + "陕bgu,降bt,函bib,限bv,姑vd,姓vtg,妮vnx,始vck,姆vx,迢vkp,驾lkc,叁cdd,参cd,艰cv,组xeg,绅xjh,细xl,驶ckq,织xkw,驹cqk,终xtu,驻cy,"
        + "绊xuf,驼cp,绍xvk,绎xcf,贯xfm,契dhv,贰afm,奏dwg,玷ghk,珍gw,玲gwy,珊gmm,毒gxgu,型gajf,拭raa,挂rffg,封fffy,持rf,拷rft,拱raw,项adm,垮fdfn,"
        + "挎rdfn,城fd,挟rgu,挠ratq,政ght,赴fhh,赵fhq,挡riv,拽rjx,哉fak,挺rtfp,括rtd,垢fr,拴rwg,拾rwgk,挑riq,垛fms,垫rvyf,挣rqvh,挤ryj,挖rpwn,挥rpl,"
        + "挪rvf,拯rbi,某afs,甚adwn,荆aga,茸abf,革af,茬adhf,荐adh,巷awn,带gkp,茧aju,茵ald,茶aws,荒aynq,茫aiy,荡ain,荣aps,荤aplj,荧apo,胡de,荫abe,"
        + "荔all,药ax,栈sgt,柑saf,枯sd,柄sgm,栋sai,相sh,柏srg,栅smm,柳sqt,柱syg,柿symh,柠sps,树scf,勃fpb,柬gli,咸dgk,威dgv,歪gig,研dga,砖dfny,"
        + "厘djfd,厚djb,砌dav,砂di,泵diu,砚dmq,砍dqw,耐dmjf,耍dmjv,牵dpr,鸥aqqg,残gqg,殃gqm,轴lm,鸦ahtg,皆xxr,韭djdg,背uxe,战hka,虐haa,临jty,览jtyq,"
        + "省ith,削iej,尝ipf,昧jfi,盹hgb,盼hwv,眨htp,哇kff,哄kaw,哑kgo,冒jhf,映jmd,昨jt,咧kgq,昭jvk,畏lge,趴khw,胃le,贵khgm,界lwj,虹ja,虾jghy,"
        + "蚁jyq,思ln,蚂jcg,虽kj,品kkk,咽kld,骂kkc,勋kml,哗kwx,咱kth,响ktm,哈kwg,哆kqq,咬kuq,咳kynw,咪ko,哪kv,哟kx,炭mdo,峡mgu,罚ly,贱mgt,"
        + "贻mck,骨me,幽xxm,钙qgh,钝qgbn,钞qit,钟qkhh,钢qmq,钠qmw,钥qeg,钦qqw,钧qqug,卸rhb,缸rma,拜rdfh,矩tda,毡tfnk,氢rnc,怎thfn,牲trtg,适tdp,秒ti,"
        + "香tjf,竿tfj,段wdm,便wgj,俩wgm,贷wam,顺kd,俏wie,促wkh,俄wtr,俐wtj,侮wtx,俭wwgi,俘web,信wy,皇rgf,泉riu,鬼rqc,侵wvp,禹tkm,侯wnt,追wnnp,"
        + "俊wcw,盾rfh,待tffy,徊tlk,衍tif,律tvfh,很tve,须ed,叙wtc,剑wgi,逃iqp,食wyv,盆wvl,胚egi,胧edx,胆ej,胜etg,胞eqn,胖euf,脉eyni,胎eck,勉qkql,"
        + "狭qtgw,狮qtjh,独qtj,狰qtqh,狡qtu,狱qtyd,狠qtv,贸qyv,怨qbn,急qvn,饵qnbg,饶qna,蚀qnj,饺qnuq,饼qnu,峦yom,奖uqd,亭yps,亮ypm,迹yop,庭ytfp,疮uwb,"
        + "疯umq,疫umc,疤ucv,咨uqwk,姿uqwv,亲us,帝up,施ytb,闺uffd,闻ub,闽uji,阀uwa,阁utk,差uda,养udyj,姜ugv,叛udrc,送udp,类od,迷op,籽ob,娄ov,"
        + "首uth,逆ubt,兹uxx,总ukn,炼oanw,炸oth,烁oqi,炮oq,炫oyx,烂oufg,剃uxhj,洼iffg,洁ifk,洪iaw,洒is,柒ias,浇iat,浊ij,洞imgk,测imj,洗itf,活itd,"
        + "派ire,洽iwg,染ivs,洛itk,浏iyjh,济iyj,洲iyt,浑ipl,浓ipe,津ivfh,恃nff,恒ngj,恢ndo,恍niq,恬ntd,恤ntl,恰nwgk,恼nyb,举iwf,觉ipmq,宣pgj,宦pah,"
        + "宫pk,宪ptf,突pwd,穿pwat,窃pwav,客pt,诫yaah,冠pfqf,诬yaw,扁ynma,袄put,祖pye,祝pyk,祠pynk,误ykg,诱yte,诲ytx,诵yceh,垦vef,退vep,既vca,屋ngc,"
        + "昼nyj,屎noi,费xjm,陡bfh,逊bip,眉nhd,孩bynw,陨bkm,险bwg,娃vff,姥vft,姻vld,娇vtdj,姚viq,娜vvf,架lks,贺lkm,盈ecl,勇cel,怠ckn,癸wgd,蚤cyj,"
        + "柔cbts,垒cccf,绑xdt,绒xad,绕xat,骄ctdj,绘xwf,给xw,绚xqj,骆ctk,络xtk,绝xqc,绞xuq,骇cynw,耕dif,耘difc,耗ditn,耙dic,艳dhq,泰dwiu,秦dwt,珠gr,"
        + "班gyt,素gxi,匿aadk,蚕gdj,顽fqd,盏glf,匪adjd,捞rap,栽fas,捕rge,埂fgj,捂rgkg,振rdf,赶fhfk,盐fhl,捎rie,捍rjf,捏rjfg,埋fjf,捉rkh,捆rls,捐rke,"
        + "损rkm,袁fke,捌rklj,哲rrk,逝rrp,捡rwgi,挫rww,挽rqkq,挚rvyr,热rvyo,恐amyn,捣rqym,壶fpo,捅rce,埃fct,挨rct,耻bh,耿bo,耽bpq,聂bcc,恭awnu,莽ada,"
        + "莱ago,莲alp,莫ajd,莉atj,荷awsk,获aqt,晋gogj,莹apgy,莺apq,框sagg,梆sdt,桂sff,桔sfk,栖ssg,桐smgk,株sri,桥std,桦swx,栓swg,桃siq,桩syf,核synw,"
        + "样su,根sve,索fpx,哥sks,逗gkup,栗ssu,贾smu,酌sgq,配sgn,翅fcn,辱dfef,唇dfek,砸damh,砰dgu,砾dqi,础dbm,破dhc,原dr,套ddu,逐epi,烈gqjo,殊gqr,"
        + "殉gqq,顾db,轿ltd,较lu,顿gbnm,毙xxgx,致gcft,柴hxs,虑han,监jtyl,紧jc,党ipk,逞kgp,晒jsg,眠hna,晓jat,哮kft,唠kap,鸭lqy,晃ji,哺kge,晌jtm,"
        + "剔jqrj,晕jp,蚌jdh,畔luf,蚣jwc,蚊jyy,蚪jufh,蚓jxh,哨kie,哩kjf,圃lgey,哭kkdu,哦ktr,恩ldn,鸯mdq,唤kqm,唁kyg,哼kyb,唧kvcb,啊kb,唉kct,唆kcw,"
        + "罢lfc,峭mi,峨mtr,峰mtd,圆lkmi,峻mcw,贼madt,贿mde,赂mtk,赃myf,钱qg,钳qaf,钻qhk,钾qlh,铁qr,铃qwyc,缺rmn,氧rnu,氨rnp,特trf,牺trs,造tfkp,"
        + "乘tux,敌tdt,秤tgu,租teg,积tkw,秧tmdy,秩trw,秘tn,透tep,笑ttd,笋tvt,债wgmy,借waj,值wfhg,倚wds,俺wdjn,倾wxd,倒wgc,倘wim,俱whw,倡wjjg,候whn,"
        + "赁wtfm,俯wyw,倍wuk,倦wud,健wvf,臭thdu,射tmdf,躬tmdx,息thn,倔wnb,徒tfhy,徐twt,殷rvn,舰temq,舱tew,般tem,途wtp,拿wgkr,耸wwb,爹wqqq,舀evf,豺eef,"
        + "豹eeqy,颁wvd,颂wcd,翁wcn,胰egx,脆eqd,脂ex,胸eq,胳etk,脏eyf,脐eyj,胶eu,脓epe,逛qtgp,狸qtjf,狼qty,卿qtvb,逢tdh,鸵qynx,留qyvl,鸳qbq,皱qvhc,"
        + "饿qnt,馁qne,凌ufw,凄ugvv,恋yon,桨uqs,浆uqi,衰ykge,衷ykhe,高ym,郭ybb,席yam,座yww,症ugh,病ugm,疾utd,斋ydm,疹uwe,疼utu,疲uhc,脊iwe,紊yxiu,"
        + "唐yvh,瓷uqwn,资uqwm,凉uyiy,剖ukj,竞ukqb,旅ytey,畜yxl,阅uuk,羞udn,羔ugo,瓶uag,拳udr,料ou,益uwl,兼uvo,烤oft,烘oaw,烦odm,烧oat,烛oj,烟ol,"
        + "烙otk,递uxhp,涛idt,浙irr,涝iap,浦igey,酒isgg,涉ihi,消iie,涡ikm,浩itfk,涂iwt,浴iww,浮ieb,涣iqm,涤its,流iyc,润iugg,涧iujg,涕iuxt,浪iyv,浸ivp,"
        + "涨ix,烫inro,涩ivy,涌ice,悖nfpb,悟ngkg,悄ni,悍njf,悔ntx,悯nuy,悦nuk,害pd,宵pi,宴pjv,宾pr,窍pwan,宰puj,案pvs,请yge,朗yvc,诸yft,诺yad,"
        + "扇ynnd,诽ydj,袜pug,袖pum,袍puq,被puhc,祥pyu,冥pju,谁ywyg,调ymf,冤pqk,谅yyi,谆yybg,谈yoo,谊ype,剥vijh,恳venu,展nae,剧ndj,屑nied,弱xu,陵bfw,"
        + "祟bmf,陶bqr,陷bqv,陪buk,娱vkgd,娟vke,恕vkn,娥vtr,娘vyv,预cbd,桑cccs,绢xke,绣xten,验cwg,继xo,骏ccw,琐gim,琉gyc,琅gyv,捧rdw,堵fft,措raj,"
        + "描ral,掩rdjn,捷rgv,排rdj,焉ghg,掉rhj,捶rtgf,赦fot,堆fwy,推rwyg,埠fwn,掀rrq,授rep,捻rwyn,掏rqr,掐rqv,掠ryiy,掂ryh,培fuk,接ruv,掷rudb,探rpws,"
        + "据rnd,掘rnbm,掺rcd,职bk,聆bwyc,勘adwl,聊bqt,娶bcv,著aft,菱afwt,勒afl,黄amw,菲adj,萌aje,萝alq,菌alt,萎atv,萄aqr,菊aqo,菩auk,萍aigh,菠aih,"
        + "萤apj,营apk,乾fjt,萧avi,萨abu,菇avd,械sa,彬sse,梦ssq,婪ssv,梗sgjq,梧sgk,梢sie,梅stx,检sw,梳syc,梯sux,桶sce,梭scw,救fiyt,曹gma,副gkl,"
        + "票sfiu,酝sgf,酗sgqb,厢dsh,戚dhi,硅dff,硕ddm,奢dft,盔dol,爽dqq,聋dxb,袭dxy,盛dnnl,匾ayna,辅lgey,辆lgm,颅hndm,虚hao,彪hame,雀iwyf,堂ipkf,常ipkh,"
        + "眶hag,匙jghx,晨jd,睁hqv,眯ho,眼hv,悬egcn,野jfc,啪krr,啦kru,曼jlc,晦jtx,晚jq,啄keyy,啡kdj,距kha,趾khh,啃khe,略ltk,蚯jrgg,蛀jyg,蛇jpx,"
        + "唬kham,累lx,鄂kkfb,唱kjj,患kkhn,啰klqy,唾ktg,唯kwyg,啤krt,啥kwfk,啸kvi,崖mdff,崎mds,崭ml,逻lqp,崔mwy,帷mhw,崩mee,崇mpf,崛mnbm,婴mmv,圈lud,"
        + "铐qftn,铛qiv,铝qkk,铜qmgk,铭qqk,铲qut,矫tdtj,甜tdaf,秸tfkg,梨tjs,犁tjr,秽tmq,移tqq,笨tsg,笼tdx,笛tmf,笙ttgf,第tx,敏txgt,袋waye,悠whtn,偿wi,"
        + "偶wjm,偎wlge,偷wwgj,您wqin,售wyk,停wyp,躯tmdq,兜qrnq,衅tlu,徘tdjd,徙thh,衔tqf,舶ter,船temk,舵tepx,斜wtuf,盒wgkl,鸽wgkg,敛wgit,悉ton,欲wwkw,彩ese,"
        + "领wycm,脚efcb,脖efp,脯ege,豚eey,脸ew,脱euk,象qje,够qkqq,逸qkqp,猜qtge,猪qtfj,猎qta,猫qtal,凰mrg,猖qtjj,猛qtbl,祭wfi,馅qnqv,馆qnp,凑udw,减udg,"
        + "毫ypt,烹ybo,庶yao,麻yss,庵ydjn,痊uwg,痒uud,痕uve,廊yyv,康yvi,庸yveh,鹿ynj,盗uqwl,竟ujq,商um,族ytt,旋ytn,望yneg,阎uqvd,阐uuj,羚udwc,盖ugl,"
        + "眷udhf,粗oe,粒oug,断on,兽ulg,焊ojf,焕oqm,添igd,鸿iaqg,淋iss,涯idf,淹idj,渠ians,渐il,淑ihic,淌iim,混ijx,淮iwy,淆iqd,渊ito,淫iet,渔iqgg,"
        + "淘iqr,淳iyb,液iyw,淤iywu,淡io,淀ipgh,深ipw,涮inm,涵ibi,婆ihcv,梁ivw,渗icd,情nge,惜najg,惭nl,悼nhjh,惧nhw,惕njq,惟nwy,惊nyiy,惦nyh,悴nywf,"
        + "惋npqb,惨ncd,惯nxf,寇pfqc,寅pgm,寄pds,寂ph,宿pwdj,窒pwg,窑pwr,密pnt,谋yaf,谍yan,谎yay,谐yxxr,袱puwd,祷pyd,祸pykw,谓yle,谜yopy,逮vip,敢nb,"
        + "尉nfif,屠nft,弹xuj,隋bda,堕bdef,随bde,蛋nhj,隅bjm,隆btg,隐bq,婚vq,婶vpj,婉vpq,颇hcd,颈cad,绪xft,续xfn,骑cds,绰xhj,绳xkjn,维xwy,绵xr,"
        + "绷xee,绸xmf,综xp,绽xpg,缀xcc,巢vjs,琴ggw,琳gss,琢gey,琼gyiy,斑gyg,揍rdwd,款ffi,堪fad,塔fawk,搭rawk,堰fajv,揩rxxr,越fha,趁fhwe,趋fhqv,揽rjt,"
        + "堤fjgh,博fge,揭rjq,彭fkue,揣rmd,插rtf,揪rto,搜rvh,煮ftjo,援ref,搀rqku,裁fay,搁rut,搓rud,搂ro,搅ripq,壹fpg,握rng,搔rcyj,揉rcbs,斯adwr,期adwe,"
        + "欺adww,联bu,葫adef,惹adkn,葬agq,募ajdl,葛ajq,董atg,葡aqg,敬aqk,葱aqrn,蒋auq,蒂aup,落ait,韩fjfh,朝fje,辜duj,葵awg,棒sdw,棱sfw,棋sad,椰sbb,"
        + "森sss,焚sso,椒shi,棵sjs,棍sjx,椎swy,棉srm,棚see,棕sp,棺spn,榔syv,椭sbd,惠gjh,惑akgn,逼gklp,粟sou,棘gmii,酣sgaf,酥sgty,厨dgkf,厦ddh,硝die,"
        + "硫dyc,雁dww,殖gqf,裂gqje,雄dcw,颊guwm,雳fdlb,暂lrj,雅ahty,辈djdl,凿ogu,辉iqpl,敞imkt,棠ipks,赏ipkm,掌ipkr,晴jge,睐hgo,暑jft,晰jsr,量jg,鼎hnd,"
        + "喷kfa,喳ksj,晶jjj,喇kgk,遇jm,喊kdgt,遏jqwp,晾jyiy,景jy,畴ldt,践khg,跋khdc,跌khr,跛khhc,遗khgp,蛙jff,蛛jri,蜓jtfp,蜒jthp,蛤jw,喝kjq,鹃keq,"
        + "喂klg,喘kmd,喉kwn,喻kwgj,啼ku,喧kp,嵌maf,幅mhg,帽mhj,赋mga,赌mftj,赎mfn,赐mjq,赔muk,黑lfo,铸qdt,铺qge,链qlp,锁qim,锄qegl,锅qkm,锈qten,"
        + "锋qtd,锌quh,锐quk,甥tgll,掰rwvr,智tdkj,氮rno,毯tfno,氯rnv,鹅trng,剩tuxj,稍tie,稀tqd,税tuk,筐tag,等tffu,筑tam,策tgm,筛tjgh,筒tmgk,筏twa,答tw,"
        + "筋telb,筝tqvh,傲wgqt,傅wge,牌thgf,堡wksf,集wys,焦wyo,傍wup,储wyf,皓rtfk,皖rpf,粤tlo,奥tmo,街tffh,惩tghn,御trh,循trfh,艇tet,舒wfkb,逾wgep,番tol,"
        + "释toc,禽wyb,腊eaj,脾ert,腋eywy,腔epw,腕epq,鲁qgj,猩qtjg,猬qtle,猾qtm,猴qtw,惫tln,馈qnk,馋qnqu,装ufy,蛮yoj,敦ybt,斌yga,痘ugku,痢utj,痪uqm,"
        + "痛uce,竣ucw,阔uit,翔udng,羡ugu,粪oawu,尊usg,奠usgd,遂uep,曾ul,焰oqv,港iawn,滞igk,湘ishg,渣isjg,渤ifp,渺ihit,湿ijo,温ijl,渴ijq,溃ikh,溅imgt,"
        + "滑ime,湃ird,渝iwgj,湾iyo,渡iya,滋iux,渲ipgg,溉ivc,愤nfa,慌nay,惰nda,愕nkk,愣nly,惶nrgg,愧nrq,愉nw,慨nvc,割pdhj,寒pfj,富pgk,窜pwk,窝pwkw,"
        + "窖pwtk,窘pwvk,遍ynm,雇ynwy,裕puw,裤puy,裙puvk,禅pyuf,禄pyv,谢ytm,谣yer,谤yup,谦yuv,犀nir,屡no,强xk,粥xox,疏nhy,隔bgk,隙bij,隘buw,媒vaf,"
        + "絮vkx,嫂vvh,媚vnh,婿vnhe,登wgku,缅xdmd,缆xjt,缉xkb,缎xwd,缓xef,缔xup,缕xov,骗cyna,骚ccyj,缘xxe,瑟ggn,鹉gahg,瑞gmd,瑰grq,瑙gvt,魂fcr,肆dv,"
        + "摄rbcc,摸rajd,填ffh,搏rgef,塌fjn,鼓fkuc,摆rlf,携rwye,搬rte,摇rer,搞rym,塘fyv,摊rcw,聘bmg,斟adwf,蒜afi,勤akgl,靴afwx,靶afc,鹊ajqg,墓ajdf,蓬atdp,"
        + "蓄ayx,蒲aigy,蓉apw,蒙apg,蒸abi,献fmud,椿sdwj,禁ssf,楚ssn,楷sx,榄sjtq,想shn,槐srq,榆swgj,楼sov,概svc,赖gkim,酪sgtk,酬sgyh,碍djg,碘dma,碑drt,"
        + "碎dyw,碰duo,碗dpq,碌dvi,尴dnjl,雷flf,零fwyc,雾ftl,雹fqn,辐lgk,辑lkb,督hich,频hid,龄hwbc,鉴jtyq,睛hg,睹hft,睦hf,瞄hal,睫hgv,睡ht,睬hes,"
        + "嗜kftj,鄙kfl,嗦kfpi,愚jmhn,暖jef,盟jel,暇jnh,照jvko,畸lds,跨khd,跷khaq,跺khm,跪khqb,路kht,跤khuq,跟khv,遣khgp,蜈jkg,蜗jkm,蛾jtr,蜂jtd,蜕juk,"
        + "嗅kthd,嗡kwc,嗓kcc,署lftj,罪ldj,罩lhj,蜀lqj,幌mhjq,错qaj,锚qal,锡qjq,锣qlq,锤qtgf,锥qwy,锦qrm,锯qnd,锰qbl,矮tdtv,辞tduh,稚twy,稠tmfk,颓tmdm,"
        + "愁tonu,筹tdtf,签twgi,筷tnn,毁va,舅vl,鼠vnu,催wmw,傻wtlt,像wqj,躲tmds,魁rqcf,衙tgk,微tmg,愈wgen,遥er,腻eaf,腰esv,腥ejt,腮elny,腹etj,腺eri,"
        + "鹏eeq,腾eud,腿eve,鲍qgq,猿qtfe,颖xtd,触qejy,煞qvt,雏qvw,馍qnad,馏qnql,酱uqsg,禀ylki,痹ulgj,廓yyb,痴utdk,痰uoo,廉yuvo,靖uge,意ujn,誊udyf,粮oyv,"
        + "煎uejo,塑ubtf,慈uxxn,煤oa,煌or,满iagw,漠iaj,滇ifhw,源idr,滤iha,滥ijt,滔iev,溪iex,溜iqyl,漓iybc,滚iuc,溢iuw,溯iub,滨ipr,溶ipwk,溺ixu,粱ivwo,"
        + "滩icw,慎nfh,誉iwyf,塞pfjf,寞paj,窥pwfq,窟pwn,寝puvc,谨yak,褂pufh,裸pujs,福pyg,谬ynwe,群vtk,殿naw,辟nku,障buj,媳vthn,嫉vut,嫌vu,嫁vpe,叠cccg,"
        + "缚xge,缝xtdp,缠xyj,缤xpr,剿vjsj,静geq,碧grd,赘gqtm,熬gqto,墙ffuk,墟fhag,嘉fkuk,摧rmw,赫fof,截faw,誓rryf,境fuj,摘rum,摔ryx,聚bct,慕ajdn,暮ajdj,"
        + "摹ajdr,蔓ajl,蔑aldt,蔡awf,蔗aya,蔽aum,蔼ayj,熙ahko,蔚anf,兢dqd,模saj,槛sjt,榴sqy,榜sup,榨spw,榕spwk,遭gmap,酵sgfb,酷sgtk,酿sgye,酸sgc,碟dan,"
        + "碱ddg,碳dmd,磁du,愿drin,需fdm,辖lpdk,辗lna,雌hxw,裳ipke,颗jsd,瞅hto,墅jfcf,嗽kgkw,踊khc,蜻jgeg,蜡jaj,蝇jk,蜘jtdk,蝉jujf,嘛ky,嘀kum,赚muv,"
        + "锹qto,锻qwd,镀qya,舔tdgn,稳tqv,熏tgl,箕tad,算tha,箩tlq,箫tvij,舆wfl,僚wdu,僧wul,鼻thl,魄rrqc,魅rqci,貌eerq,膜eajd,膊egef,膀eup,鲜qgu,疑xtdh,"
        + "孵qytb,馒qnjc,裹yjse,敲ymkc,豪ypeu,膏ypk,遮yaop,腐ywfw,瘩uaw,瘟ujl,瘦uvh,辣ugk,彰uje,竭ujqn,端umd,旗yta,精oge,粹oyw,歉uvow,弊umia,熄othn,熔opw,"
        + "煽oynn,潇iavj,漆isw,漱igkw,漂isf,漫ijlc,滴ium,漾iugi,演ipg,漏infy,慷nyv,寨pfjs,赛pfjm,寡pde,察pwfi,蜜pntj,寥pnw,谭ysj,肇ynth,褐pujn,褪puvp,谱yuo,"
        + "隧bue,嫩vgk,翠nywf,熊cexo,凳wgkm,骡clx,缩xpw,慧dhd,撵rfwl,撕rad,撒rae,撩rdu,趣fhb,趟fhi,撑rip,撮rjb,撬rtfn,播rtol,擒rwyc,墩fyb,撞ruj,增fu,"
        + "撰rnnw,聪bukn,鞋afff,鞍afp,蕉awy,蕊ann,蔬anh,蕴axj,槽sgmj,樱smmv,樟suj,橄snb,敷geht,豌gkub,飘sfiq,醋sga,醇sgyb,醉sgy,磕dfc,磊ddd,磅dup,碾dna,"
        + "震fdf,霄fie,霉ftxu,瞒hagw,题jghm,暴jaw,瞎hp,嘻kfk,嘶kad,嘲kfj,嘹kdui,踢khj,踏khij,踩khes,踪khp,蝶jan,蝴jde,蝠jgkl,蝎jjq,蝌jtu,蝗jr,蝙jyna,"
        + "嘿klf,嘱knt,幢mhu,墨lfof,镇qfhw,镐qym,镑qup,靠tfkd,稽tdnj,稻tev,黎tqt,稿tym,稼tpe,箱tsh,篓tov,箭tue,篇tyna,僵wgl,躺tmdk,僻wnk,德tfl,艘tevc,"
        + "膝esw,膛ei,鲤qgjf,鲫qgvb,熟ybv,摩yssr,褒ywk,瘪uthx,瘤uqyl,瘫ucwy,凛uyl,颜utem,毅uem,糊ode,遵usgp,憋umin,潜ifw,澎ifke,潮ifj,潭isj,鲨iitg,澳itm,"
        + "潘itol,澈iyct,澜iugi,澄iwgu,懂nat,憔nwyo,懊ntm,憎nul,额ptkm,翩ynmn,褥pudf,谴ykhp,鹤pwy,憨nbtn,慰nfi,劈nkuv,履ntt,豫cbq,缭xdu,撼rdgn,擂rfl,操rkk,"
        + "擅ryl,燕au,蕾aflf,薯alfj,薛awnu,薇atm,擎aqkr,薪aus,薄aig,颠fhwm,翰fjw,噩gkkk,橱sdgf,橘scbk,融gkm,瓢sfiy,醒sgj,霍fwyf,霎fuv,辙lyc,冀uxl,餐hq,"
        + "嘴khx,踱khyc,蹄khuh,蹂khcs,蟆jajd,螃jup,器kkd,噪kkks,鹦mmvg,赠mu,默lfod,黔lfon,镜quj,赞tfqm,穆tri,篡thdc,篷ttdp,篱tyb,儒wfd,邀rytp,衡tqdh,膨efk,"
        + "雕mfky,鲸qgy,磨yssd,瘾ubq,瘸ulkw,凝uxt,辨uyt,辩uyu,糙otf,糖oyvk,糕ougo,燃oqdo,濒ihim,澡ik,激iry,懒ngkm,憾ndgn,懈nq,窿pwb,壁nkuf,避nk,缰xgl,"
        + "缴xry,戴falw,擦rpwi,藉adi,鞠afq,藏adnt,藐aee,檬sap,檐sqdy,檀syl,礁dwy,磷doq,霜fs,霞fnhc,瞭hdui,瞧hwy,瞬hep,瞳hu,瞩hnt,瞪hwg,曙jl,蹋khjn,"
        + "螺jlx,蟋jto,蟀jyx,嚎kyp,赡mqd,穗tgjn,魏tvr,簧tamw,簇tyt,繁txgi,徽tmgt,爵elv,朦eap,臊ekks,鳄qgkn,癌ukk,辫uxu,赢ynky,糟ogmj,糠oyvi,燥okk,懦nfdj,"
        + "豁pdhk,臀nawe,臂nkue,翼nla,骤cbc,藕adiy,鞭afw,藤aeu,覆stt,瞻hqd,蹦khme,嚣kkdk,镰qyu,翻toln,鳍qgfj,鹰ywwg,瀑ija,襟pus,璧nkuy,戳nwya,孽awnb,警aqky,"
        + "蘑ays,藻aik,攀sqq,曝jja,蹲khuf,蹭khuj,蹬khwu,巅mfh,簸tadc,簿tig,蟹qevj,颤ylkm,靡yssd,癣uqg,瓣ur,羹ugod,鳖umig,爆oja,疆xfg,鬓depw,壤fyk,馨fnm,"
        + "耀iqny,躁khks,蠕jfdj,嚼kel,嚷kyk,巍mtv,籍tdij,鳞qgo,魔yssc,糯ofd,灌iak,譬nkuy,蠢dwjj,霸faf,露fkhk,霹fnk,躏khay,黯lfoj,髓med,赣ujt,囊gkh,镶qyk,"
        + "瓤ykky,罐rmay,矗fhfh,乂qty,乜nnv,兀gqv,弋agny,孑bnhg,孓byi,幺xnny,亓fjj,韦fnh,廿agh,丏ghnn,卅gkk,仄dwi,厄dbv,仃wsh,仉wmn,仂wln,兮wgnb,刈qjh,"
        + "爻qqu,卞yhu,闩ugd,讣yhy,尹vte,夬nwi,爿nhde,毋xde,邗fbh,邛abh,艽avb,艿aeb,札snn,叵akd,匝amh,丕gigf,匜abv,劢dnl,卟khy,叱kxn,叻kln,仨wdg,"
        + "仕wfg,仟wtfh,仡wtn,仫wtcy,仞wvy,卮rgbv,氐qay,犰qtvn,刍qvf,邝ybh,邙ynb,汀ish,讦yfh,讧yag,讪ymh,讫ytnn,尻nvv,阡btf,尕eiu,弁caj,驭ccy,匡agd,"
        + "耒dii,玎gsh,玑gmn,邢gab,圩fgf,圬ffn,圭fff,扦rtfh,圪ftn,圳fkh,圹fyt,扪run,圮fnn,圯fnn,芊atf,芍aqy,芄avy,芨aey,芑anb,芎axb,芗axt,亘gjg,"
        + "厍dlk,夼dkj,戍dynt,尥dnq,乩hkn,旯jvb,曳jxe,岌meyu,屺mnn,凼ibk,囡lvd,钇qnn,缶rmk,氘rnj,氖rne,牝trx,伎wfcy,伛waqy,伢wah,佤wgn,仵wtfh,伥wta,"
        + "伧wwbn,伉wym,伫wpg,囟tlqi,汆tyiu,刖ejh,夙mgq,旮vjf,刎qrj,犷qtyt,犸qtcg,舛qah,凫qynm,邬qngb,饧qnnr,汕imh,汔itn,汐iqy,汲iey,汜inn,汊icyy,忖nfy,"
        + "忏ntfh,讴yaq,讵yang,祁pyb,讷ymw,聿vfhk,艮vei,厾nfci,阱bfj,阮bfq,阪brcy,丞big,妁vqy,牟cr,纡xgf,纣xfy,纥xtnn,纨xvyy,玕gfh,玙ggng,抟rfn,抔rgiy,"
        + "圻frh,坂frc,坍fmyg,坞fqng,抃ryhy,抉rnwy,芫afqb,邯afb,芸afcu,芾agm,苈adl,苣aan,芷ahf,芮amwu,苋amq,芼atfn,苌ata,苁awwu,芩awyn,芪aqa,芡aqw,芟amc,"
        + "苄ayh,苎apgf,苡any,杌sgqn,杓sqyy,杞snn,杈scyy,忑ghnu,孛fpbf,邴gmwb,邳gigb,矶dmn,奁daq,豕egt,忒ani,欤gngw,轫lvy,迓ahtp,邶uxb,忐hnu,卣hln,邺ogb,"
        + "旰jfh,呋kfw,呒kfq,呓kan,呔kdyy,呖kdl,呃kdb,旸jnrt,吡kxx,町lsh,虬jnn,呗kmy,吽krhh,吣kny,吲kxh,帏mhf,岐mfc,岈mah,岘mmqn,岑mwyn,岚mmqu,兕mmgq,"
        + "囵lwxv,囫lqr,钊qjh,钋qhy,钌qbh,迕tfpk,氙rnm,氚rnkj,牤tryn,佞wfv,邱rgb,攸whty,佚wrw,佝wqk,佟wtuy,佗wpx,伽wlk,彷tyn,佘wfiu,佥wgif,孚ebf,豸eer,"
        + "坌wvff,肟efn,邸qayb,奂qmd,劬qkl,狄qtoy,狁qtc,鸠vqyg,邹qvb,饨qngn,饩qnrn,饪qntf,饫qntd,饬qntl,亨ybj,庑yfq,庋yfc,疔usk,疖ubk,肓ynef,闱ufn,闳udc,"
        + "闵uyi,羌udnb,炀onrt,沣idh,沅ifq,沔igh,沤iaq,沌igb,沏iav,沚ihg,汩ijg,汨ijg,沂irh,汾iwv,沨imqy,汴iyh,汶iyy,沆iym,沩iyl,泐ibl,怃nfq,怄naq,"
        + "忡nkh,忤ntfh,忾nrn,怅nta,忻nrh,忪nwc,怆nwb,忭nyhy,忸nnf,诂ydg,诃ysk,诅yeg,诋yqay,诌yqvg,诏yvk,诒yck,孜bty,陇bdx,陀bpx,陂bhc,陉bca,妍vga,"
        + "妩vfq,妪vaq,妣vxx,妊vtf,妗vwy,妫vyl,妞vnf,姒vny,妤vcbh,邵vkb,劭vkl,刭cajh,甬cej,邰ckb,纭xfc,纰xxxn,纴xtfg,纶xwx,纾xcb,玮gfn,玡gaht,玭gxxn,"
        + "玠gwjh,玢gwv,玥geg,玦gnwy,盂gfl,忝gdn,匦alv,坩fafg,抨rguh,拤rhhy,坫fhkg,拈rhkg,垆fhnt,抻rjh,劼fkln,拃rthf,拊rwf,坼fry,坻fqa,坨fpxn,坭fnx,抿rna,"
        + "坳fx,耶bbh,苷aaf,苯asg,苤agi,茏adx,苫ahk,苜ahf,苴aeg,苒amf,苘amk,茌awff,苻awfu,苓awyc,茚aqgb,茆aqtb,茑aqyg,茓apwu,茔apff,茕apn,茀axjj,苕avkf,"
        + "枥sdl,枇sxxn,杪sit,杳sjf,枧smqn,杵stfh,枨sta,枞sww,枋syn,杻snfg,杷scn,杼scb,矸dfh,砀dnr,刳dfnj,奄djn,瓯aqgn,殁gqmc,郏guwb,轭ldb,郅gcfb,鸢aqyg,"
        + "盱hgf,昊jgd,昙jfcu,杲jsu,昃jdw,咂kam,呸kgi,昕jrh,昀jqu,旻jyu,昉jyn,炅jou,咔khhy,畀lgj,虮jmn,咀keg,呷klh,黾kjn,呱krc,呤kwyc,咚ktuy,咆kqn,"
        + "咛kps,呶kvc,呣kxgu,呦kxl,咝kxxg,岢msk,岿mjv,岬mlh,岫mmg,帙mhrw,岣mqk,峁mqt,刿mqjh,迥mk,岷mna,剀mnj,帔mhhc,峄mcf,沓ijf,囹lwy,罔muy,钍qfg,"
        + "钎qtf,钏qkh,钒qmyy,钕qvg,钗qcy,邾rib,迮thfp,牦trtn,竺tff,迤tbp,佶wfkg,佬wft,佰wdj,侑wde,侉wdf,臾vw,岱wamj,侗wmgk,侃wkq,侏wri,侩wwfc,佻wiq,"
        + "佾wwe,侪wyj,佼wuq,佯wudh,侬wp,帛rmh,阜wnnf,侔wcr,徂tegg,刽wfcj,郄qdc,怂wwn,籴tyo,瓮wcg,戗wba,肼efj,肽edy,肱edc,肫egb,剁msj,迩qip,郇qjb,"
        + "狙qteg,狎qtl,狍qtqn,狒qtx,咎thk,炙qo,枭qyns,饯qngt,饴qnc,冽ugq,冼utf,庖yqn,疠udnv,疝umk,疡unr,兖ucq,妾uvf,劾yntl,炜ofn,炖ogbn,炘orh,炝owb,"
        + "炔onw,泔iaf,沭isyy,泷idx,泸ihn,泱imdy,泅ilw,泗ilg,泠iwyc,泺iqi,泖iqt,泫iyx,泮iuf,沱ipx,泯ina,泓ixc,泾ica,怙ndg,怵ns,怦ngu,怛njg,怏nmdy,"
        + "怍nth,怩nnx,怫nxj,怿ncfh,宕pdf,穹pwx,宓pntr,诓yagg,诔ydiy,诖yffg,诘yfk,戾ynd,诙ydo,戽ynu,郓plb,衩puc,祆pygd,祎pyfh,祉pyh,祇pyqa,诛yri,诜ytfq,"
        + "诟yrg,诠ywg,诣yxj,诤yqvh,诧ypta,诨ypl,诩yng,戕nhda,孢bqn,亟bkc,陔bynw,妲vjg,妯vm,姗vmm,帑vcm,弩vcx,孥vcbf,驽vcc,虱ntj,迦lkp,迨ckp,绀xaf,"
        + "绁xann,绂xdc,驷clg,驸cwf,绉xqv,绌xbm,驿ccf,骀cck,甾vlf,珏ggy,珐gfc,珂gsk,珑gdx,玳gwa,珀grg,顸fdmy,珉gna,珈glk,拮rfk,垭fgo,挝rfp,垣fgjg,"
        + "挞rdp,垤fgc,赳fhnh,贲fam,垱fivg,垌fmg,郝fob,垧ftm,垓fynw,挦rvfy,垠fve,茜asf,荚aguw,荑agx,贳anm,荜axxf,莒akkf,茼amg,茴alkf,茱ari,莛atfp,荞atdj,"
        + "茯awd,荏awtf,荇atfh,荃awgf,荟awfc,荀aqj,茗aqkf,荠ayjj,茭auqu,茨auqw,垩gogf,荥api,荦apr,荨avf,荩anyu,剋dqjk,荪abiu,茹avk,荬anud,荮axf,柰sfiu,栉sab,"
        + "柯ssk,柘sdg,栊sdx,柩saqy,枰sgu,栌shnt,柙slh,枵skg,柚smg,枳skw,柞sth,柝sryy,栀srgb,柢sqa,栎sqi,枸sqk,柈sufh,柁spx,枷slk,柽scfg,剌gkij,酊sgs,"
        + "郦gmyb,甭gie,砗dlh,砘dgb,砒dxx,斫drh,砭dtp,砜dmqy,奎dfff,耷dbf,虺gqji,殂gqe,殇gqtr,殄gqwe,殆gqc,轱ldg,轲lsk,轳lhnt,轶lrw,轸lwe,虿dnju,毖xxnt,"
        + "觇hkm,尜idi,哐kag,眄hgh,眍haq,郢kgbh,眇hit,眊htfn,眈hpq,禺jmhy,哂ksg,咴kdo,曷jqwn,昴jqt,昱juf,咦kgx,哓kat,哔kxxf,畎ldy,毗lxx,呲khxn,胄mef,"
        + "畋lty,畈lrc,虼jtn,虻jyn,盅khl,咣kiq,哕kmq,剐kmwj,郧kmb,咻kws,囿lde,咿kwvt,哌kre,哙kwfc,哚kms,咯ktk,咩kud,咤kpta,哝kpe,哏kve,哞kcr,峙mff,"
        + "峣matq,罘lgi,帧mhhm,峒mmgk,峤mtdj,峋mqjg,峥mqv,贶mkq,钚qgiy,钛qdy,钡qmy,钣qrc,钤qwyn,钨qqn,钫qyn,钯qcn,氡rntu,氟rnx,牯trdg,郜tfkb,秕txx,秭ttnt,"
        + "竽tgf,笈teyu,笃tcf,俦wdtf,俨wgo,俅wfiy,俪wgmy,叟vh,垡waff,牮war,俣wkg,俚wjf,皈rrcy,俑wce,俟wct,逅rgkp,徇tqj,徉tud,舢temh,俞wgej,郗qdmb,俎wweg,"
        + "郤wwkb,爰eft,郛ebb,瓴wycn,胨eai,胪ehnt,胛elh,胂ejhh,胙eth,胍erc,胗ewe,胝eqa,朐eqk,胫eca,鸨xfq,匍qgey,狨qtad,狯qtwc,飑mqqn,狩qtpf,狲qtbi,訇qyd,"
        + "逄tah,昝thj,饷qntk,饸qnwk,饹qntk,胤txen,孪yob,娈yov,弈yoa,奕yod,庥yws,疬udl,疣udnv,疥uwj,疭uwwi,庠yudk,竑udcy,彦uter,飒umqy,闼udpi,闾ukkd,闿umnv,"
        + "阂uyn,羑ugqy,迸uap,籼omh,酋usgf,炳ogm,炻odg,炽ok,炯omk,烀otu,炷oyg,烃oc,洱ibg,洹igj,洧ideg,洌igq,浃igu,洇ildy,洄ilk,洙iri,涎ithp,洎ithg,"
        + "洫itlg,浍iwfc,洮iiq,洵iqj,浒iytf,浔ivfy,浕inyu,洳ivkg,恸nfcl,恓nsg,恹nddy,恫nmg,恺nmn,恻nmj,恂nqj,恪ntkg,恽npl,宥pdef,扃ynmk,衲pumw,衽putf,衿puwn,"
        + "袂pun,祛pyfc,祜pydg,祓pydc,祚pyt,诮yie,祗pyqy,祢pyq,诰ytfk,诳yqt,鸩pqq,昶ynij,郡vtkb,咫nyk,弭xbg,牁nhdk,胥nhe,陛bx,陟bhi,娅vgo,姮vgjg,娆vat,"
        + "姝vr,姣vuq,姘vua,姹vpt,怼cfn,羿naj,炱cko,矜cbtn,绔xdf,骁catq,骅cwx,绗xtfh,绛xtah,骈cu,耖diit,挈dhvr,珥gbg,珙gaw,顼gdm,珰givg,珩gtf,珧giq,"
        + "珣gqjg,珞gtk,琤gqvh,珲gpl,敖gqty,恚ffnu,埔fgey,埕fkg,埘fjfy,埙fkmy,埚fkm,挹rkc,耆ftxj,耄ftxn,埒fef,捋refy,贽rvym,垸fpf,捃rvt,盍fclf,荸afpb,莆age,"
        + "莳ajfu,莴akm,莪atr,莠ate,莓atx,莜awh,莅awuf,荼awt,莩aebf,荽aev,莸aqtn,荻aqto,莘auj,莎aiit,莞apfq,莨ayv,鸪dqyg,莼axg,栲sftn,栳sftx,郴ssb,桓sgjg,"
        + "桡sat,桎sgcf,桢shm,桤smnn,梃stfp,栝stdg,桕svg,桁stfh,桧swf,桅sqd,栟suah,桉spv,栩sng,逑fiyp,逋gehp,彧akge,鬲gkmh,豇gkua,酐sgfh,逦gmyp,厝daj,孬giv,"
        + "砝dfcy,砹daqy,砺dddn,砧dhkg,砷djh,砟dth,砼dwa,砥dqay,砣dpx,剞dskj,砻dxd,轼la,轾lgc,辂ltkg,鸫aiq,趸dnk,龀hwbx,鸬hnq,虔hay,逍iep,眬hdxn,唛kgt,"
        + "晟jdn,眩hy,眙hck,哧kfo,哽kgj,唔kgkg,晁jiqb,晏jpv,鸮kgng,趵khqy,趿khey,畛lwet,蚨jfw,蚜jah,蚍jxxn,蚋jmw,蚬jmq,蚝jtf,蚧jwj,唢kim,圄lgkd,唣kra,"
        + "唏kqd,盎mdl,唑kww,崂map,崃mgo,罡lgh,罟ldf,峪mwwk,觊mnmq,赅myn,钰qgyy,钲qghg,钴qdg,钵qsg,钹qdcy,钺qant,钽qjg,钼qhg,钿qlg,铀qmg,铂qrg,铄qqi,"
        + "铆qqt,铈qymh,铉qyx,铊qpx,铋qntt,铌qnx,铍qhc,铎qcf,氩rngg,氤rnl,氦rnyw,毪tfnh,舐tdqa,秣tgs,秫tsy,盉tlf,笄tgaj,笕tmqb,笊trhy,笏tqr,笆tcb,俸wdwh,"
        + "倩wgeg,俵wgey,偌wad,俳wdjd,俶whic,倬whjh,倏whtd,恁wtfn,倭wtv,倪wvq,俾wrt,倜wmf,隼wyfj,隽wyeb,倌wpn,倥wpw,臬ths,皋rdfj,郫rtfb,倨wnd,衄tlnf,颀rdm,"
        + "徕tgo,舫teyn,釜wqf,奚exd,衾wyne,胯edf,胱eiq,胴emg,胭eld,脍ewf,胼eua,朕eudy,脒eoy,胺epv,鸱qayg,玺qig,鸲qkqg,狷qtke,猁qtt,狳qtwt,猃qtwi,狺qtyg,"
        + "逖qtop,桀qahs,袅qyne,饽qnfb,凇usw,栾yos,挛yor,亳ypta,疳uaf,疴uskd,疸ujg,疽ueg,痈uek,疱uqn,痂ulkd,痉uca,衮uceu,凋umf,颃ymdm,恣uqwn,旆ytg,旄yttn,"
        + "旃ytmy,阃uls,阄uqj,訚uyd,阆uyv,恙ugn,粑ocn,朔ubte,郸ujfb,烜ogjg,烨owx,烩owf,烊oud,剡ooj,郯oob,烬ony,涑igki,浯igkg,涞igo,涟ilp,娑iitv,涅ijfg,"
        + "涠ilf,浞ikhy,涓ike,浥ikcn,涔imw,浜irgw,浠iqdh,浣ipfq,浚icwt,悚ngki,悭njc,悝njfg,悒nkc,悌nux,悛ncw,宸pdfe,窈pwxl,剜pqbj,诹ybc,冢pey,诼yey,袒pujg,"
        + "袢puu,祯pyhm,诿ytv,谀yvwy,谂ywyn,谄yqv,谇yyw,屐ntfc,屙nbs,陬bbc,勐bll,奘nhdd,牂nhdd,蚩bhgj,陲btgf,姬vah,娠vdf,娌vjfg,娉vmgn,娲vkm,娩vqk,娴vus,"
        + "娣vux,娓vntn,婀vbs,畚cdl,逡cwt,绠xgj,骊cg,绡xie,骋cmg,绥xev,绦xts,绨xuxt,骎cvpc,邕vkc,鸶xxgg,彗dhdv,耜din,焘dtfo,舂dwv,琏glp,琇gten,麸gtfw,"
        + "揶rbb,埴ffhg,埯fdj,捯rgcj,掳rha,掴rlgy,埸fjq,埵ftgf,赧fobc,埤frt,捭rrt,逵fwfp,埝fwyn,堋fee,堍fqk,掬rqo,鸷rvyg,掖ryw,捽rywf,掊ruk,堉fyce,掸rujf,"
        + "捩rynd,掮ryne,悫fpmn,埭fvi,埽fvp,掇rcc,掼rxf,聃bmfg,菁agef,萁aadw,菘asw,堇akgf,萘adfi,萋agv,菽ahi,菖ajjf,萜amhk,萸avw,萑awyf,棻awvs,菔aebc,菟aqky,"
        + "萏aqvf,萃ayw,菏ais,菹aie,菪apd,菅apnn,菀apqb,萦apx,菰abr,菡abib,梵ssm,梿slpy,梏stfk,觋awwq,桴seb,桷sqe,梓suh,棁sukq,桫sii,棂svo,啬fulk,郾ajv,"
        + "匮akh,敕gkit,豉gkuc,鄄sfb,酞sgdy,酚sgw,戛dha,硎dgaj,硭day,硒dsg,硖dguw,硗dat,硐dmg,硇dtl,硌dtk,鸸dmjg,瓠dfny,匏dfnn,厩dvc,龚dxa,殒gqk,殓gqw,"
        + "殍gqeb,赉gom,雩ffnb,辄lbn,堑lrf,眭hff,眦hhx,啧kgm,晡jgey,晤jgk,眺hiq,眵hqq,眸hcr,圊lged,喏kadk,喵kal,啉kss,勖jhl,晞jqdh,唵kdjn,晗jwyk,冕jqkq,"
        + "啭klfy,畦lff,趺khf,啮khwb,跄khwb,蚶jaf,蛄jdg,蛎jdd,蛆jegg,蚰jmg,蛊jlf,圉lfu,蚱jthf,蛉jwyc,蛏jcfg,蚴jxl,啁kmf,啕kqrm,唿kqrn,啐kyw,唼kuv,唷kyc,"
        + "啖koo,啵kih,啶kpgh,啷kyv,唳kynd,唰knm,啜kccc,帻mhgm,崚mfwt,崦mdj,帼mhl,崮mld,崤mqde,崆mpw,赇mfi,赈mdfe,赊mwf,铑qftx,铒qbg,铗qguw,铙qat,铟qldy,"
        + "铠qmn,铡qmj,铢qri,铣qtfq,铤qtfp,铧qwx,铨qwg,铩qqs,铪qwgk,铫qiq,铬qtk,铮qqv,铯qqcn,铰quq,铱qye,铳qyc,铵qpv,铷qvk,氪rndq,牾trgk,鸹tdq,秾tpey,"
        + "逶tvp,笺tgr,筇tab,笸takf,笪tjgf,笮tth,笠tuf,笥tng,笤tvk,笳tlkf,笾tlp,笞tck,偾wfa,偃wajv,偕wxxr,偈wjq,傀wrq,偬wqrn,偻wov,皑rmnn,皎ruq,鸻tfhg,"
        + "徜tim,舸tes,舻teh,舴tetf,舷teyx,龛wgkx,翎wycn,脬eeb,脘epf,脲eni,匐qgk,猗qtdk,猡qtlq,猞qtwk,猝qtyf,斛qeu,猕qtxi,馗vuth,馃qnjs,馄qnjx,鸾yoq,孰ybvy,"
        + "庹yany,庾yvwi,痔uffi,痍ugxw,疵uhx,翊ung,旌yttg,旎ytnx,袤ycbe,阇uftj,阈uak,阉udjn,阊ujjd,阋uvq,阍uqa,阏uywu,羟udca,粝odd,粕org,敝umi,焐ogk,烯oqd,"
        + "焓owy,烽ot,焖oun,烷opf,焗onnk,渍igm,渚ift,淇iadw,淅isr,淞iswc,渎ifnd,涿ieyy,淖ihj,挲iitr,淠ilgj,涸ild,渑ikj,淦iqg,淝iec,淬iywf,涪iuk,淙ipfi,"
        + "涫ipn,渌ivi,淄ivl,惬nag,悻nfuf,悱ndjd,惝nim,惘nmu,悸ntb,惆nmf,惚nqr,惇nybg,惮nuj,窕pwi,谌yadn,谏ygl,扈ynkc,皲plh,谑yha,裆puiv,袷puwk,裉puve,"
        + "谒yjq,谔ykkn,谕ywgj,谖yef,谗yqk,谙yuj,谛yuph,谝yyna,逯vipi,郿nhbh,隈blge,粜bmo,隍brg,隗brq,婧vge,婊vgey,婕vgv,娼vjj,婢vrt,婵vuj,胬vcmw,袈lky,"
        + "翌nuf,恿cen,欸ctdw,绫xfw,骐cadw,绮xds,绯xdjd,绱xim,骒cj,绲xjx,骓cwyg,绶xep,绺xth,绻xudb,绾xpn,骖ccd,缁xvl,耠diw,琫gdwh,琵ggx,琶ggc,琪gad,"
        + "瑛gam,琦gds,琥gha,琨gjx,靓gem,琰goo,琮gpf,琯gpnn,琬gpq,琛gpw,琚gnd,辇fwfl,鼋fqkn,揳rdhd,堞fan,搽raws,揸rsj,揠rajv,堙fsf,趄fhe,揖rkb,颉fkd,"
        + "塄fly,揿rqq,耋ftxf,揄rwgj,蛩amyj,蛰rvyj,塆fyox,摒rnua,揆rwgd,掾rxe,聒btd,葑afff,葚aadn,靰afgq,靸afey,葳adg,葺akb,葸alnu,萼akkn,葆awk,葩arc,葶ayp,"
        + "蒌ao,萱apgg,戟fja,葭anhc,楮sftj,棼ssw,椟sfn,棹shj,椤slq,棰stg,赍fww,椋syiy,椁syb,椪suog,棣svi,椐snd,鹁fpbg,覃sjj,酤sgdg,酢sgtf,酡sgp,鹂gmyg,"
        + "厥dubw,殚gqu,殛gqb,雯fyu,雱fyb,辊lj,辋lmu,椠lrs,辍lccc,辎lvl,斐djdy,睄hieg,睑hwgi,睇huxt,睃hcw,戢kbnt,喋kans,嗒kawk,喃kfm,喱kdjf,喹kdf,晷jthk,"
        + "喈kxxr,跖khdg,跗khwf,跞khqi,跚khmg,跎khpx,跏khlk,跆khck,蛱jgu,蛲jatq,蛭jgc,蛳jjg,蛐jma,蛔jlk,蛞jtdg,蛴jyj,蛟juq,蛘jud,喁kjm,喟kle,啾kto,嗖kvh,"
        + "喑kuj,嗟kuda,喽kov,嗞kuxx,喀kpt,喔kngf,喙kxe,嵘maps,嵖msjg,崴mdgt,遄mdm,詈lyf,嵎mjmy,崽mln,嵬mrq,嵛mwg,嵯mud,嵝mov,嵫mux,幄mhnf,嵋mnh,赕moo,"
        + "铻qgkg,铼qgoy,铿qjc,锃qkg,锂qjf,锆qtfk,锇qtrt,锉qww,锏qujg,锑qux,锒qyve,锔qnnk,锕qbs,掣rmhr,矬tdw,氰rnge,毳tfnn,毽tfnp,犊trfd,犄trd,犋trhw,鹄tfkg,"
        + "犍trv,嵇tdnm,黍twi,稃tebg,稂tyv,筚txxf,筵tthp,筌twgf,傣wdw,傈wss,舄vqo,牍thgd,傥wipq,傧wpr,遑rgp,傩wcwy,遁rfhp,徨trg,媭edmv,畲wfil,弑qsa,颌wgkm,"
        + "翕wgkn,釉tom,鹆wwkg,舜epqh,貂eev,腈egeg,腌edjn,腓edjd,腆ema,腴evw,腑eyw,腚epg,腱evfp,鱿qgd,鲀qggn,鲂qgyn,颍xid,猢qtde,猹qts,猥qtle,飓mqh,觞qetr,"
        + "觚qer,猱qtcs,颎xodm,飧qwye,馇qns,馊qnvc,亵yrv,脔yomw,裒yveu,痣ufni,痨uapl,痦ugkd,痞ugi,痤uww,痫uus,痧uii,赓yvwm,竦ugki,瓿ukg,啻upmk,颏yntm,鹇usq,"
        + "阑ugli,阒uhd,阕uwgd,粞osg,遒usgp,孳uxxb,焯ohj,焜ojxx,焙ouk,焱ooou,鹈uxhg,湛iad,渫ians,湮isfg,湎idm,湜ijgh,渭ile,湍imd,湫itoy,溲ivh,湟irgg,溆iwtc,"
        + "湲iefc,湔iue,湉intd,渥ing,湄inh,滁ibw,愠njlg,惺njt,愦nkhm,惴nmdj,愀nto,愎ntjt,愔nujg,喾ipt,寐pnhi,谟yaj,扉yndd,裢pul,裎puk,裥puuj,祾pyft,祺pya,"
        + "谠yip,幂pjd,谡ylw,谥yuw,谧yntl,遐nhf,孱nbb,弼xdj,巽nna,骘bhic,媪vjl,媛vefc,婷vyp,巯cay,翚nplj,皴cwtc,婺cbtv,骛cbtc,缂xafh,缃xsh,缄xdg,彘xgx,"
        + "缇xjg,缈xhi,缌xlny,缑xwn,缒xwnp,缗xna,飨xtw,耢dial,瑚gde,瑁gjhg,瑜gwg,瑗gefc,瑄gpgg,瑕gnh,遨gqtp,骜gqtc,韫fnhl,髡degq,塬fdr,鄢ghgb,趔fhgj,趑fhuw,"
        + "摅rhan,摁rld,蜇rrj,搋rrhm,搪ryv,搐ryxl,搛ruvo,搠rub,摈rpr,彀fpgc,毂fpl,搦rxu,搡rccs,蓁adwt,戡adwa,蓍aftj,鄞akgb,靳afr,蓐adff,蓦ajdc,鹋alqg,蒽aldn,"
        + "蓓awuk,蓖atl,蓊awc,蒯aeej,蓟aqgj,蓑ayk,蒿aym,蒺aut,蓠aybc,蒟auqk,蒡aupy,蒹auv,蒴aub,蒗aiye,蓥apqf,颐ahkm,楔sdh,楠sfm,楂ssj,楝sgl,楫skb,楸sto,"
        + "椴swd,槌swn,楯srfh,皙srr,榈suk,槎suda,榉siw,楦spg,楣snh,楹sec,椽sxe,裘fiye,剽sfij,甄sfgn,酮sgmk,酰sgtq,酯sgx,酩sgqk,蜃dfej,碛dgm,碓dwyg,硼dee,"
        + "碉dmf,碚duk,碇dpgh,碜dcd,鹌djng,辏ldw,龃hwbg,龅hwbn,訾hxy,粲hqco,虞hak,睚hd,嗪kdwt,韪jghh,嗷kgqt,嗉kgxi,睨hvq,睢hwyg,雎egw,睥hr,嘟kftb,嗑kfcl,"
        + "嗫kbc,嗬kawk,嗔kfhw,嗝kgkh,戥jtga,嗄kdht,煦jqko,暄jpg,遢jnp,暌jwgd,跬khff,跶khdp,跸khxf,跐khhx,跣khtq,跹khtp,跻khyj,蛸jie,蜊jtj,蜍jwt,蜉jeb,蜣judn,"
        + "畹lpq,蛹jceh,嗣kma,嗯kldn,嗥krd,嗲kwq,嗳kep,嗌kuw,嗍kub,嗨kitu,嗐kpdk,嗤kbhj,嗵kce,罨ldjn,嵊mtu,嵩mym,嵴miw,骰mem,锗qft,锛qdf,锜qdsk,锝qjgf,"
        + "锞qjs,锟qjx,锢qldg,锨qrq,锩qudb,锭qp,锱qvl,雉tdwy,氲rnjl,犏trya,歃tfvw,稞tjsy,稗trtf,稔twyn,筠tfqu,筢trc,筮taw,筲tief,筱twh,牒thgs,煲wkso,敫ryty,"
        + "徭term,愆tifn,艄teie,觎wgeq,毹wgen,貊eed,貅eew,貉eetk,颔wynm,腠edw,腩efm,腼edmd,腭ekk,腧ewgj,塍eudf,媵eudv,詹qdw,鲅qgdc,鲆qgg,鲇qghk,鲈qghn,稣qgty,"
        + "鲋qgw,鲐qgc,肄xtdh,鹐qvqg,飕mqvc,觥qei,遛qyvp,馐qnuf,鹑ybq,亶ylkg,瘃uey,痱udjd,痼uld,痿utv,瘐uvw,瘁uyw,瘆ucde,麂ynjm,裔yem,歆ujqw,旒ytyq,雍yxt,"
        + "阖ufc,阗ufh,阙uub,羧udct,豢ude,粳ogj,猷usgd,煳odeg,煜oju,煨olg,煅owd,煊opg,煸oyna,煺ove,滟idhc,溱idw,溘ifcl,漭iada,滢iapy,溥igef,溧issy,溽idff,"
        + "裟iite,溻ijn,溷iley,滗itt,滫iwhe,溴ithd,滏iwq,滃iwcn,滦iyos,溏iyvk,滂iup,滓ipu,溟ipju,滪icbm,愫ngx,慑nbc,慊nuv,鲎ipqg,骞pfjc,窦pwfd,窠pwj,窣pwyf,"
        + "裱puge,褚pufj,裨pur,裾pund,裰pucc,禊pydd,谩yjl,谪yum,媾vfj,嫫vajd,媲vtl,嫒vepc,嫔vpr,媸vbh,缙xgoj,缜xfh,缛xdff,辔xlx,骝cqyl,缟xym,缡xyb,缢xuw,"
        + "缣xuv,骟cynn,耥diik,璈ggqt,瑶ger,瑭gyvk,獒gqtd,觏fjgq,慝aadn,嫠fit,韬fnhv,叆fcec,髦detn,摽rsfi,墁fjl,撂rlt,摞rlx,撄rmm,翥ftjn,踅rrkh,摭rya,墉fyvh,"
        + "墒fum,榖fptc,綦adwi,蔫agho,蔷afu,靺afgs,靼afjg,鞅afmd,靿afxl,甍alpn,蔸aqrq,蔟ayt,蔺auw,戬goga,蕖aias,蔻apfl,蓿apwj,斡fjwf,鹕deq,蓼anw,榛sdwt,榧sadd,"
        + "榻sjn,榫swyf,榭stm,槔srd,榱syk,槁symk,槟spr,槠syfj,榷spwy,僰gmiw,酽sggd,酶sgtu,酹sge,厮dadr,碡dgx,碴dsj,碣djq,碲duph,磋dud,臧dnd,豨eqdh,殡gqp,"
        + "霆ftf,霁fyj,辕lfk,蜚djdj,裴djde,翡djdn,龇hwbx,龈hwbe,睿hpgh,睽hwgd,嘞kaf,嘈kgmj,嘌ksf,嘁kdht,嘎kdh,暧jep,暝jpju,踌khdf,踉khye,蜞jad,蜥jsrh,蜮jak,"
        + "蝈jlg,蜴jjqr,蜱jrt,蜩jmfk,蜷judb,蜿jpq,螂jyv,蜢jbl,嘘khag,嘡kipf,鹗kkfg,嘣kme,嘤kmm,嘚ktjf,嗾kyt,嘧kpn,罴lfco,罱lfm,幔mhjc,嶂muj,幛mhuj,赙mge,"
        + "罂mmr,骷medg,骶meqy,鹘meq,锲qdh,锴qxx,锶qln,锷qkkn,锸qtfv,锵quqf,镁qug,镂qov,犒tryk,箐tge,箦tgmu,箧tagw,箍tra,箸tft,箬tadk,箅tlg,箪tujf,箔tir,"
        + "箜tpw,箢tpq,箓tviu,毓txgq,僖wfkk,儆waqt,僳wso,僭waqj,劁wyoj,僮wuj,魃rqcc,魆rqct,睾tlff,艋tebl,鄱tolb,膈egk,膑epr,鲑qgff,鲔qgde,鲚qgyj,鲛qguq,鲟qgv,"
        + "獐qtuj,觫qegi,雒tkwy,夤qpgw,馑qnag,銮yoqf,塾ybvf,麽yssc,瘌ugkj,瘊uwn,瘘uov,瘙ucy,廖ynw,韶ujv,旖ytdk,膂ytee,阚unb,鄯udub,鲞udqg,粿ojsy,粼oqab,粽opfi,"
        + "糁ocd,槊ubts,鹚uxxg,熘oqyl,熥ocep,潢iam,漕igmj,滹ihah,漯ilx,漶ikkn,潋iwgt,潴iqtj,漪iqtk,漉iynx,漳iuj,漩iyth,澉inb,潍ixw,慵nyvh,搴pfjr,窨pwuj,寤pnhk,"
        + "綮ynti,谮yaqj,褡pua,褙puue,褓puws,褛puo,褊puya,谯ywyo,谰yug,谲ycbk,暨vcag,屣nthh,鹛nhq,嫣vgh,嫱vfuk,嫖vsf,嫦viph,嫚vjlc,嫘vlx,嫡vum,鼐eh,翟nwyf,"
        + "瞀cbth,鹜cbtg,骠cs,缥xs,缦xjl,缧xlxi,缨xmm,骢ctl,缪xnw,缫xvj,耦dij,耧dio,瑾gakg,璜gamw,璀gmwy,璎gmmv,璁gtl,璋guj,璇gyth,奭ddjj,髯dem,髫devk,"
        + "撷rfkm,撅rduw,赭fofj,撸rqg,鋆fquq,撙rus,撺rpwh,墀fni,聩bkh,觐akgq,鞑afdp,蕙agj,鞒aftj,蕈asj,蕨adu,蕤aetg,蕞ajb,蕺akbt,瞢alph,蕃ato,蕲aujr,赜ahkm,"
        + "槿sak,樯sfu,槭sdht,樗sffn,樘sip,樊sqqd,槲sqef,醌sgjx,醅sguk,靥dddl,魇ddr,餍ddw,磔dqas,磙duc,霈fig,辘lyn,龉hwbk,龊hwbh,觑haoq,瞌hfcl,瞋hfhw,瞑hpj,"
        + "嘭kfke,噎kfp,噶kaj,颙jmhm,暹jwy,噘kdu,踔khhj,踝khjs,踟khtk,踒khtv,踬khrm,踮khyk,踯khub,踺khvp,踞khnd,蝽jdwj,蝾japs,蝻jfm,蝰jdff,蝮jtjt,螋jvh,蝓jwgj,"
        + "蝣jytb,蝼jov,噗kog,嘬kjb,颚kkfm,噍kwyo,噢ktmd,噙kwyc,噜kqg,噌kul,噔kwgu,颛mdmm,幞mho,幡mhtl,嶙mo,嶝mwgu,骺mer,骼met,骸mey,镊qbc,镉qgkh,镌qwye,"
        + "镍qth,镏qqyl,镒quw,镓qpe,镔qpr,稷tlw,箴tdgt,篑tkhm,篁trgf,篌twn,篆txe,牖thgy,儋wqd,徵tmgt,磐temd,虢efhm,鹞ermg,膘esf,滕eudi,鲠qggq,鲡qggy,鲢qglp,"
        + "鲣qgjf,鲥qgjf,鲧qgti,鲩qgp,獗qtdw,獠qtdi,觯qeuf,馓qnat,馔qnnw,麾yssn,廛yjf,瘛udhn,瘼uajd,瘢utec,瘠uiw,齑ydjj,羯udjn,羰udm,遴oqa,糌othj,糍oux,糅ocb,"
        + "熜otln,熵oum,熠onrg,澍ifkf,澌iadr,潸isse,潦idui,潲iti,鋈itdq,潟ivqo,潼iujf,潺inbb,憬njy,憧nujf,寮pdu,窳pwry,谳yfm,褴pujl,褟pujn,褫purm,谵yqdy,熨nfio,"
        + "屦ntov,嬉vfk,勰llln,戮nwe,蝥cbtj,缬xfkm,缮xud,缯xul,骣cnb,畿xxa,耩diff,耨did,耪diuy,璞gogy,璟gjyi,靛gep,璠gtol,璘goqh,聱gqtb,螯gqtj,髻defk,髭deh,"
        + "髹dew,擀rfj,熹fkuo,甏fkun,擞rovt,縠fpgc,磬fnmd,颞bccm,蕻adaw,鞘afie,颟agmm,薤agqg,薨alpx,檠aqks,薏aujn,薮aovt,薜ank,薅avdf,樾sfht,橛sdu,橇stf,樵swyo,"
        + "檎swyc,橹sqg,樽susf,樨snih,橼sxxe,墼gjff,橐gkhs,翮gkmn,醛sgag,醐sgde,醍sgjh,醚sgo,磲dias,赝dwwm,飙dddq,殪gqfu,霖fss,霏fdjd,霓fvq,錾lrq,辚lo,臻gcft,"
        + "遽hae,氅imkn,瞟hsf,瞠hip,瞰hnb,嚄kawc,嚆kay,噤kssi,暾jyb,蹀khas,踹khmj,踵khtf,踽khty,蹉khua,蹁khya,螨jagw,蟒jada,螈jdr,螅jthn,螭jybc,螠juwl,螟jpj,"
        + "噱khae,噬kta,噫kujn,噻kpf,噼knk,罹lnw,圜llg,镖qsf,镗qipf,镘qjl,镚qmee,镛qyvh,镝qum,镞qytd,镠qnwe,氇tfnj,氆tfnj,憩tdtn,穑tfuk,篝tfjf,篥tss,篦ttlx,"
        + "篪trhm,篙tymk,盥qgi,劓thlj,翱rdf,魉rqcw,魈rqce,徼try,歙wgkw,膳eudk,膦eo,膙exkj,鲮qgft,鲱qgdd,鲲qgjx,鲳qgjj,鲴qgld,鲵qgvq,鲷qgm,鲻qgvl,獴qtae,獭qtgm,"
        + "獬qtqh,邂qevp,鹧yaog,廨yqe,赟ygam,瘰ulx,廪yyli,瘿umm,瘵uwf,瘴uujk,癃ubtg,瘳unwe,斓yugi,麇ynjt,麈ynjg,嬴ynky,壅yxtf,羲ugt,糗othd,瞥umih,甑uljn,燎odui,"
        + "燠otm,燔oto,燧oue,濑igkm,濉ihw,潞ikhk,澧ima,澹iqdy,澥iqeh,澶iylg,濂iyu,褰pfje,寰plg,窸pwtn,褶punr,禧pyfk,嬖nkuv,犟xkjh,隰bjx,嬗vylg,颡cccm,缱xkhp,"
        + "缲xkk,缳xlge,璨ghq,璩ghae,璐gkhk,璪gkks,螫fotj,擤rth,壕fyp,觳fpgc,罄fnmm,擢rnwy,薹afkf,鞡afru,鞬afvp,薷afdj,薰atgo,藓aqgd,藁ayms,檄sry,檩syli,懋scbn,"
        + "醢sgdl,翳atdn,礅dyb,磴dwgu,鹩dujg,龋hwby,龌hwbf,豳eem,壑hpg,黻oguc,嚏kfph,嚅kfd,蹑khb,蹒khaw,蹊khed,蟥jam,螬jgmj,螵jsf,疃luj,螳jip,蟑jujh,嚓kpw,"
        + "羁laf,罽ldoj,罾lul,嶷mx,黜lfom,黝lfol,髁mej,髀merf,镡qsjh,镢qduw,镣qdu,镦qyb,镧qugi,镩qpw,镪qx,镫qwgu,罅rmhh,黏twik,簌tgkw,篾tldt,篼tqrq,簖tonr,"
        + "簋tvel,鼢vnuv,黛wal,儡wll,鹪wyog,鼾thlf,皤rtol,魍rqcn,龠wgka,繇ermi,貘eea,邈eerp,貔eetx,臌efkc,膻eyl,臆euj,臃eyx,鲼qgfm,鲽qga,鳀qgjh,鳃qgl,鳅qgto,"
        + "鳇qgr,鳊qgya,螽tujj,燮oyo,鹫yidg,襄ykk,糜ysso,縻yssi,膺ywwe,癍ugy,麋ynjo,懑iagn,濡ifd,濮iwo,濞ithj,濠iyp,濯inw,蹇pfjh,謇pfjy,邃pwup,襁pux,檗nkus,"
        + "擘nkur,孺bfd,隳bdan,嬷vys,蟊cbtj,鹬cbtg,鍪cbtq,鏊gqtq,鳌gqtg,鬈deu,鬃dep,瞽fkuh,鞯afa,鞨afjn,鞫afqy,鞧afug,鞣afcs,藜atq,藠arrr,藩aitl,醪sgne,蹙dhih,"
        + "礓dgl,燹eeo,餮gqwe,瞿hhwy,曛jtgo,颢jyim,曜jnw,躇khaj,蹚khif,鹭khtg,蟛jfke,蟪jgjn,蟠jtol,蟮judk,鹮lgkg,黠lfok,黟lfoq,髅meo,髂mep,镬qawc,镭qfl,镯qlqj,"
        + "馥tjtt,簟tsj,簪taq,鼬vnum,雠wyy,艟teuf,鳎qgjn,鳏qgli,鳐qgem,癞ugkm,癔uujn,癜una,癖unk,糨ox,蹩umih,鎏iycq,懵nal,彝xgo,邋vlq,鬏deto,攉rfwy,攒rtfm,"
        + "鞲afff,鞴afae,藿afwy,蘧aha,蘅atqh,麓ssyx,醮sgwo,醯sgyl,酃fkk,霪fief,霭fyjn,霨fnff,黼oguy,嚯kfwy,蹰khdf,蹶khdw,蹽khdi,蹼kho,蹴khyn,蹾khyt,蹿khph,蠖jawc,"
        + "蠓jap,蟾jqd,蠊jyu,黢lfot,髋mepq,髌mepw,镲qpwi,籀trql,籁tgkm,齁thlk,魑rqcc,艨teae,鳓qgal,鳔qgs,鳕qgfv,鳗qgjc,鳙qgyh,麒ynjw,鏖ynjq,羸ynky,瀚ifjn,瀣ihq,"
        + "瀛iyny,襦pufj,谶ywwg,襞nkue,骥cux,缵xtfm,瓒gtfm,攘ryk,蘩atxi,蘖awns,醴sgmu,霰fae,酆dhdb,矍hhw,曦jug,躅khlj,鼍kkl,巉mqky,黩lfod,黥lfoi,黪lfoe,镳qyno,"
        + "镴qvln,黧tqto,纂thdi,璺wfm,鼯vnuk,臜etfm,鳜qgdw,鳝qguk,鳟qguf,獾qtay,孀vfs,骧cyk,瓘gaky,鼙fkuf,醺sgto,礴dai,颦hidf,曩jyk,鳢qgmu,癫ufhm,麝ynjf,夔uht,"
        + "爝oel,灏ijym,禳pyye,鐾nkuq,羼nudd,蠡xej,耱diy,懿fpgn,蘸asgo,鹳akkg,霾feef,氍hhwn,饕kgne,躐khvn,髑mel,镵qqky,穰tyk,饔yxte,鬻xoxh,鬟del,趱fht,攫rhh,"
        + "攥rthi,颧akk,躜khtm,鼹vnuv,癯uhh,麟ynjh,蠲uwlj,蠹gkhj,躞khoc,衢thhh,鑫qqq,灞ifa,襻pusr,纛gxf,鬣devn,攮rgke,囔kgke,馕qnge,戆ujtn,爨wfmo,齉thle";

    /// <summary>
    /// 汉字 → 最短五笔编码字典（从 CharCodeData 解析而来）。
    /// </summary>
    private static readonly Dictionary<char, string> CharCodes = BuildCharCodes();

    /// <summary>
    /// 一级常用字（《通用规范汉字表》一级字表，共 3500 字）中本项目字库已收录的部分。
    ///
    /// 用途：五笔入门课程（<see cref="GetPracticeByCodeLength"/>）优先从这批字里抽，
    /// 避免初学者（尤其是不熟悉生僻字的中老年用户）抽到「肼 / 棰 / 琏」这类字而受挫。
    /// 自由练习仍从全库抽，不受此表限制。
    /// </summary>
    private const string Level1CommonChars =
        "一乙二十丁厂七卜八人入儿匕几九刁了刀力乃又三干于亏工土士才下寸大丈与万上小口山巾千乞川亿个夕久么勺凡丸及广亡门丫义之尸己"
        + "已巳弓子卫也女刃飞习叉马乡丰王开井天夫元无云专丐扎艺木五支厅不犬太区历歹友尤匹车巨牙屯戈比互切瓦止少曰日中贝冈内水见午牛"
        + "手气毛壬升夭长仁什片仆化仇币仍仅斤爪反介父从仑今凶分乏公仓月氏勿欠风丹匀乌勾凤六文亢方火为斗忆计订户认冗讥心尺引丑巴孔队"
        + "办以允予邓劝双书幻玉刊未末示击打巧正扑卉扒功扔去甘世艾古节本术可丙左厉石右布夯戊龙平灭轧东卡北占凸卢业旧帅归旦目且叶甲申"
        + "叮电号田由只叭史央兄叽叼叫叩叨另叹冉皿凹囚四生矢失乍禾丘付仗代仙们仪白仔他斥瓜乎丛令用甩印尔乐句匆册卯犯外处冬鸟务包饥主"
        + "市立冯玄闪兰半汁汇头汉宁穴它讨写让礼训议必讯记永司尼民弗弘出辽奶奴召加皮边孕发圣对台矛纠母幼丝邦式迂刑戎动扛寺吉扣考托老"
        + "巩圾执扩扫地场扬耳芋共芒亚芝朽朴机权过臣吏再协西压厌戌在百有存而页匠夸夺灰达列死成夹夷轨邪尧划迈毕至此贞师尘尖劣光当早吁"
        + "吐吓虫曲团吕同吊吃因吸吗吆屿屹岁帆回岂则刚网肉年朱先丢廷舌竹迁乔迄伟传乒乓休伍伏优臼伐延仲件任伤价伦份华仰仿伙伪自伊血向"
        + "似后行舟全会杀合兆企众爷伞创肌肋朵杂危旬旨旭负匈名各多争色壮冲妆冰庄庆亦刘齐交衣次产决亥充妄闭问闯羊并关米灯州汗污江汛池"
        + "汝汤忙兴宇守宅字安讲讳军讶许讹论讼农讽设访诀寻那迅尽导异弛孙阵阳收阶阴防奸如妇妃好她妈戏羽观欢买红驮纤驯约级纪驰纫巡寿弄"
        + "麦玖玛形进戒吞远违韧运扶抚坛技坏抠扰扼拒找批址扯走抄贡汞坝攻赤折抓扳抡扮抢孝坎均抑抛投坟坑抗坊抖护壳志块扭声把报拟却抒劫"
        + "芙芜苇芽花芹芥芬苍芳严芦芯劳克芭苏杆杠杜材村杖杏杉巫极李杨求甫匣更束吾豆两酉丽医辰励否还尬歼来连轩步卤坚肖旱盯呈时吴助县"
        + "里呆吱吠呕园旷围呀吨足邮男困吵串员呐听吟吩呛吻吹呜吭吧邑吼囤别吮岖岗帐财针钉牡告我乱利秃秀私每兵估体何佐佑但伸佃作伯伶佣"
        + "低你住位伴身皂伺佛囱近彻役返余希坐谷妥含邻岔肝肛肚肘肠龟甸免狂犹狈角删条彤卵灸岛刨迎饭饮系言冻状亩况床库庇疗吝应这冷庐序"
        + "辛弃冶忘闰闲间闷判兑灶灿灼弟汪沐沛汰沥沙汽沃沦汹泛沧没沟沪沈沉沁怀忧忱快完宋宏牢究穷灾良证启评补初社祀识诈诉罕诊词译君灵"
        + "即层屁尿尾迟局改张忌际陆阿陈阻附坠妓妙妖姊妨妒努忍劲矣鸡纬驱纯纱纲纳驳纵纷纸纹纺驴纽奉玩环武青责现玫表规抹卦坷坯拓拢拔坪"
        + "拣坦担坤押抽拐拖者拍顶拆拎拥抵拘势抱拄垃拉拦幸拌拧拂拙招坡披拨择抬拇拗其取茉苦昔苛若茂苹苗英苟苑苞范直茁茄茎苔茅枉林枝杯"
        + "枢柜枚析板松枪枫构杭杰述枕丧或画卧事刺枣雨卖郁矾矿码厕奈奔奇奋态欧殴垄妻轰顷转斩轮软到非叔歧肯齿些卓虎虏肾贤尚旺具味果昆"
        + "国哎咕昌呵畅明易咙昂迪典固忠呻咒咋咐呼鸣咏呢咄咖岸岩帖罗帜帕岭凯败账贩贬购贮图钓制知迭氛垂牧物乖刮秆和季委秉佳侍岳供使例"
        + "侠侥版侄侦侣侧凭侨佩货侈依卑的迫质欣征往爬彼径所舍金刹命肴斧爸采觅受乳贪念贫忿肤肺肢肿胀朋股肮肪肥服胁周昏鱼兔狐忽狗狞备"
        + "饰饱饲变京享庞店夜庙府底疟疙疚剂卒郊庚废净盲放刻育氓闸闹郑券卷单炬炒炊炕炎炉沫浅法泄沽河沾泪沮油泊沿泡注泣泞泻泌泳泥沸沼"
        + "波泼泽治怔怯怖性怕怜怪怡学宝宗定宠宜审宙官空帘宛实试郎诗肩房诚衬衫视祈话诞诡询该详建肃录隶帚屉居届刷屈弧弥弦承孟陋陌孤陕"
        + "降函限妹姑姐姓妮始姆迢驾叁参艰线练组绅细驶织驹终驻绊驼绍绎经贯契贰奏春帮玷珍玲珊玻毒型拭挂封持拷拱项垮挎城挟挠政赴赵挡拽"
        + "哉挺括垢拴拾挑垛指垫挣挤拼挖按挥挪拯某甚荆茸革茬荐巷带草茧茵茶荒茫荡荣荤荧故胡荫荔南药标栈柑枯柄栋相查柏栅柳柱柿栏柠树勃"
        + "要柬咸威歪研砖厘厚砌砂泵砚砍面耐耍牵鸥残殃轴轻鸦皆韭背战点虐临览竖省削尝昧盹是盼眨哇哄哑显冒映星昨咧昭畏趴胃贵界虹虾蚁思"
        + "蚂虽品咽骂勋哗咱响哈哆咬咳咪哪哟炭峡罚贱贴贻骨幽钙钝钞钟钢钠钥钦钧钩钮卸缸拜看矩毡氢怎牲选适秒香种秋科重复竿段便俩贷顺修"
        + "俏保促俄俐侮俭俗俘信皇泉鬼侵禹侯追俊盾待徊衍律很须叙剑逃食盆胚胧胆胜胞胖脉胎勉狭狮独狰狡狱狠贸怨急饵饶蚀饺饼峦弯将奖哀亭"
        + "亮度迹庭疮疯疫疤咨姿亲音帝施闺闻闽阀阁差养美姜叛送类迷籽娄前首逆兹总炼炸烁炮炫烂剃洼洁洪洒柒浇浊洞测洗活派洽染洛浏济洋洲"
        + "浑浓津恃恒恢恍恬恤恰恼恨举觉宣宦室宫宪突穿窃客诫冠诬语扁袄祖神祝祠误诱诲说诵垦退既屋昼屏屎费陡逊眉孩陨除险院娃姥姨姻娇姚"
        + "娜怒架贺盈勇怠癸蚤柔垒绑绒结绕骄绘给绚骆络绝绞骇统耕耘耗耙艳泰秦珠班素匿蚕顽盏匪捞栽捕埂捂振载赶起盐捎捍捏埋捉捆捐损袁捌"
        + "都哲逝捡挫换挽挚热恐捣壶捅埃挨耻耿耽聂恭莽莱莲莫莉荷获晋恶莹莺真框梆桂桔栖档桐株桥桦栓桃格桩校核样根索哥速逗栗贾酌配翅辱"
        + "唇夏砸砰砾础破原套逐烈殊殉顾轿较顿毙致柴桌虑监紧党逞晒眠晓哮唠鸭晃哺晌剔晕蚌畔蚣蚊蚪蚓哨哩圃哭哦恩鸯唤唁哼唧啊唉唆罢峭峨"
        + "峰圆峻贼贿赂赃钱钳钻钾铁铃铅缺氧氨特牺造乘敌秤租积秧秩称秘透笔笑笋债借值倚俺倾倒倘俱倡候赁俯倍倦健臭射躬息倔徒徐殷舰舱般"
        + "航途拿耸爹舀爱豺豹颁颂翁胰脆脂胸胳脏脐胶脑脓逛狸狼卿逢鸵留鸳皱饿馁凌凄恋桨浆衰衷高郭席准座症病疾斋疹疼疲脊效离紊唐瓷资凉"
        + "站剖竞部旁旅畜阅羞羔瓶拳粉料益兼烤烘烦烧烛烟烙递涛浙涝浦酒涉消涡浩海涂浴浮涣涤流润涧涕浪浸涨烫涩涌悖悟悄悍悔悯悦害宽家宵"
        + "宴宾窍窄容宰案请朗诸诺读扇诽袜袖袍被祥课冥谁调冤谅谆谈谊剥恳展剧屑弱陵祟陶陷陪娱娟恕娥娘通能难预桑绢绣验继骏球琐理琉琅捧"
        + "堵措描域捺掩捷排焉掉捶赦堆推埠掀授捻教掏掐掠掂培接掷控探据掘掺职基聆勘聊娶著菱勒黄菲萌萝菌萎菜萄菊菩萍菠萤营乾萧萨菇械彬"
        + "梦婪梗梧梢梅检梳梯桶梭救曹副票酝酗厢戚硅硕奢盔爽聋袭盛匾雪辅辆颅虚彪雀堂常眶匙晨睁眯眼悬野啪啦曼晦晚啄啡距趾啃跃略蚯蛀蛇"
        + "唬累鄂唱患啰唾唯啤啥啸崖崎崭逻崔帷崩崇崛婴圈铐铛铝铜铭铲银矫甜秸梨犁秽移笨笼笛笙符第敏做袋悠偿偶偎偷您售停偏躯兜假衅徘徙"
        + "得衔盘舶船舵斜盒鸽敛悉欲彩领脚脖脯豚脸脱象够逸猜猪猎猫凰猖猛祭馅馆凑减毫烹庶麻庵痊痒痕廊康庸鹿盗章竟商族旋望率阎阐着羚盖"
        + "眷粘粗粒断剪兽焊焕清添鸿淋涯淹渠渐淑淌混淮淆渊淫渔淘淳液淤淡淀深涮涵婆梁渗情惜惭悼惧惕惟惊惦悴惋惨惯寇寅寄寂宿窒窑密谋谍"
        + "谎谐袱祷祸谓谚谜逮敢尉屠弹隋堕随蛋隅隆隐婚婶婉颇颈绩绪续骑绰绳维绵绷绸综绽绿缀巢琴琳琢琼斑替揍款堪塔搭堰揩越趁趋超揽堤提"
        + "博揭喜彭揣插揪搜煮援搀裁搁搓搂搅壹握搔揉斯期欺联葫散惹葬募葛董葡敬葱蒋蒂落韩朝辜葵棒棱棋椰植森焚椅椒棵棍椎棉棚棕棺榔椭惠"
        + "惑逼粟棘酣酥厨厦硬硝确硫雁殖裂雄颊雳暂雅翘辈悲紫凿辉敞棠赏掌晴睐暑最晰量鼎喷喳晶喇遇喊遏晾景畴践跋跌跑跛遗蛙蛛蜓蜒蛤喝鹃"
        + "喂喘喉喻啼喧嵌幅帽赋赌赎赐赔黑铸铺链销锁锄锅锈锋锌锐甥掰短智氮毯氯鹅剩稍程稀税筐等筑策筛筒筏答筋筝傲傅牌堡集焦傍储皓皖粤"
        + "奥街惩御循艇舒逾番释禽腊脾腋腔腕鲁猩猬猾猴惫然馈馋装蛮就敦斌痘痢痪痛童竣阔善翔羡普粪尊奠道遂曾焰港滞湖湘渣渤渺湿温渴溃溅"
        + "滑湃渝湾渡游滋渲溉愤慌惰愕愣惶愧愉慨割寒富寓窜窝窖窗窘遍雇裕裤裙禅禄谢谣谤谦犀属屡强粥疏隔隙隘媒絮嫂媚婿登缅缆缉缎缓缔缕"
        + "骗编骚缘瑟鹉瑞瑰瑙魂肆摄摸填搏塌鼓摆携搬摇搞塘摊聘斟蒜勤靴靶鹊蓝墓幕蓬蓄蒲蓉蒙蒸献椿禁楚楷榄想槐榆楼概赖酪酬感碍碘碑碎碰"
        + "碗碌尴雷零雾雹辐辑输督频龄鉴睛睹睦瞄睫睡睬嗜鄙嗦愚暖盟歇暗暇照畸跨跷跳跺跪路跤跟遣蜈蜗蛾蜂蜕嗅嗡嗓署置罪罩蜀幌错锚锡锣锤"
        + "锥锦键锯锰矮辞稚稠颓愁筹签简筷毁舅鼠催傻像躲魁衙微愈遥腻腰腥腮腹腺鹏腾腿鲍猿颖触解煞雏馍馏酱禀痹廓痴痰廉靖新韵意誊粮数煎"
        + "塑慈煤煌满漠滇源滤滥滔溪溜漓滚溢溯滨溶溺粱滩慎誉塞寞窥窟寝谨褂裸福谬群殿辟障媳嫉嫌嫁叠缚缝缠缤剿静碧璃赘熬墙墟嘉摧赫截誓"
        + "境摘摔撇聚慕暮摹蔓蔑蔡蔗蔽蔼熙蔚兢模槛榴榜榨榕歌遭酵酷酿酸碟碱碳磁愿需辖辗雌裳颗瞅墅嗽踊蜻蜡蝇蜘蝉嘛嘀赚锹锻镀舞舔稳熏箕"
        + "算箩管箫舆僚僧鼻魄魅貌膜膊膀鲜疑孵馒裹敲豪膏遮腐瘩瘟瘦辣彰竭端旗精粹歉弊熄熔煽潇漆漱漂漫滴漾演漏慢慷寨赛寡察蜜寥谭肇褐褪"
        + "谱隧嫩翠熊凳骡缩慧撵撕撒撩趣趟撑撮撬播擒墩撞撤增撰聪鞋鞍蕉蕊蔬蕴横槽樱橡樟橄敷豌飘醋醇醉磕磊磅碾震霄霉瞒题暴瞎嘻嘶嘲嘹影"
        + "踢踏踩踪蝶蝴蝠蝎蝌蝗蝙嘿嘱幢墨镇镐镑靠稽稻黎稿稼箱篓箭篇僵躺僻德艘膝膛鲤鲫熟摩褒瘪瘤瘫凛颜毅糊遵憋潜澎潮潭鲨澳潘澈澜澄懂"
        + "憔懊憎额翩褥谴鹤憨慰劈履豫缭撼擂操擅燕蕾薯薛薇擎薪薄颠翰噩橱橙橘整融瓢醒霍霎辙冀餐嘴踱蹄蹂蟆螃器噪鹦赠默黔镜赞穆篮篡篷篱"
        + "儒邀衡膨雕鲸磨瘾瘸凝辨辩糙糖糕燃濒澡激懒憾懈窿壁避缰缴戴擦藉鞠藏藐檬檐檀礁磷霜霞瞭瞧瞬瞳瞩瞪曙蹋蹈螺蟋蟀嚎赡穗魏簧簇繁徽"
        + "爵朦臊鳄癌辫赢糟糠燥懦豁臀臂翼骤藕鞭藤覆瞻蹦嚣镰翻鳍鹰瀑襟璧戳孽警蘑藻攀曝蹲蹭蹬巅簸簿蟹颤靡癣瓣羹鳖爆疆鬓壤馨耀躁蠕嚼嚷"
        + "巍籍鳞魔糯灌譬蠢霸露霹躏黯髓赣囊镶瓤罐矗";

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

    /// <summary>
    /// 生成一个字的逐码按键提示，供五笔练习页面向初学者展示。
    ///
    /// 返回结果同时包含完整编码与逐位键位（含键名字），例如「明」：
    ///     编码 je  第1码 J键(日)  第2码 E键(月)
    /// </summary>
    /// <param name="ch">汉字</param>
    /// <returns>提示信息；字库中无此字时返回 null</returns>
    public static WubiCharHint? GetCharHint(char ch)
    {
        if (!CharCodes.TryGetValue(ch, out string? code) || string.IsNullOrEmpty(code))
        {
            return null;
        }

        var steps = new List<WubiKeyStep>(code.Length);
        for (int k = 0; k < code.Length; k++)
        {
            string key = char.ToUpperInvariant(code[k]).ToString();
            steps.Add(new WubiKeyStep(k + 1, key, GetKeyName(key)));
        }
        return new WubiCharHint(ch, code, steps);
    }

    /// <summary>
    /// 取某个字母键的键名字（如 E → 「月」）；未知键返回空串。
    /// 键名字取自 25 键字根表，与该键的助记口诀一一对应。
    /// </summary>
    /// <param name="key">字母键（大小写均可）</param>
    /// <returns>键名字</returns>
    public static string GetKeyName(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }
        string upper = key.ToUpperInvariant();
        foreach (var info in RootTable)
        {
            if (info.Key == upper)
            {
                return info.KeyName;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 生成逐码提示的一行说明文字，供界面直接显示或语音朗读。
    /// 例：「明，编码 J E，第 1 码按 J 键，日；第 2 码按 E 键，月」
    /// </summary>
    /// <param name="ch">汉字</param>
    /// <param name="forSpeech">true 时生成适合朗读的文本（更口语化，键名用于注音）</param>
    /// <returns>说明文字；字库中无此字时返回空串</returns>
    public static string GetHintText(char ch, bool forSpeech = false)
    {
        WubiCharHint? hint = GetCharHint(ch);
        if (hint is null)
        {
            return string.Empty;
        }

        if (forSpeech)
        {
            // 朗读版：键名放在字母后作为读音提示，避免字母被读成英文单词
            var speakParts = hint.Keys.Select(s =>
                string.IsNullOrEmpty(s.KeyName)
                    ? $"第{s.Index}码按{s.Key}键"
                    : $"第{s.Index}码按{s.Key}键，{s.KeyName}");
            return $"{hint.Char}，编码 {string.Join(" ", hint.Code.Select(c => char.ToUpperInvariant(c)))}，" +
                   string.Join("，", speakParts);
        }

        var parts = hint.Keys.Select(s =>
            string.IsNullOrEmpty(s.KeyName)
                ? $"第{s.Index}码 {s.Key}键"
                : $"第{s.Index}码 {s.Key}键（{s.KeyName}）");
        return $"编码 {hint.Code.ToUpperInvariant()}    " + string.Join("    ", parts);
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
        // 目标单元数：按档给量。入门档只给 6~10 字，
        // 五笔初学者每字要按 1~4 个键，10 字已经是不小的量。
        int unitCount = DifficultyScale.Clamp(level) switch
        {
            1 => 6,
            2 => 8,
            3 => 10,
            4 => 14,
            5 => 16,
            6 => 18,
            7 => 22,
            8 => 26,
            9 => 30,
            10 => 34,
            11 => 38,
            12 => 42,
            _ => 46
        };

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

    /// <summary>
    /// 按【编码长度】取一组练习字，供五笔入门课程使用。
    ///
    /// 为什么按编码长度分级：五笔的难易与击键数直接相关——
    ///   1 码字（一级简码 25 个字）一击成字，最适合建立最初的成就感；
    ///   2 / 3 / 4 码依次增加手指负担与记忆量。
    /// 这比"随机抽字"更符合从易到难的学习顺序，避免初学者一上手
    /// 就碰到需要四码的生僻字而受挫。
    /// </summary>
    /// <param name="codeLength">编码长度（1~4）</param>
    /// <param name="count">取多少个字</param>
    /// <returns>练习用中文文本（字之间用顿号分隔，便于逐字重打）</returns>
    public static string GetPracticeByCodeLength(int codeLength, int count)
    {
        int len = Math.Clamp(codeLength, 1, 4);
        int n = Math.Max(1, count);

        // 优先用一级常用字（3500 字表）；该码长下不足时再回退到全库。
        // 这是为了让课程"只出现日常会用到的字"，避免初学者被生僻字劝退。
        var common = Level1CommonChars
            .Where(c => CharCodes.TryGetValue(c, out string? code) && code.Length == len)
            .ToList();

        var pool = common.Count >= 8
            ? common
            : CharCodes.Where(kv => kv.Value.Length == len).Select(kv => kv.Key).ToList();

        if (pool.Count == 0)
        {
            return string.Empty;
        }

        var rand = new System.Random();
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            builder.Append(pool[rand.Next(pool.Count)]);
            // 每 5 个字加一个顿号：给初学者留出换字与停顿的节奏
            if ((i + 1) % 5 == 0 && i < n - 1)
            {
                builder.Append('、');
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 面向课程的随机练习文本：只从一级常用字（3500 字表）中取字。
    ///
    /// 与 <see cref="GetRandomPractice"/> 的区别：后者从全库随机，适合自由练习；
    /// 本方法限定常用字，供五笔入门课程使用——课程要让学习者建立信心，
    /// 不能出现「肼 / 棰 / 琏」这类平时用不到的字。
    /// </summary>
    /// <param name="count">字数</param>
    /// <param name="maxCodeLength">最大编码长度（0 表示不限）</param>
    /// <returns>练习用中文文本</returns>
    public static string GetCommonPractice(int count, int maxCodeLength = 0)
    {
        int n = Math.Max(1, count);
        var pool = Level1CommonChars
            .Where(c => CharCodes.ContainsKey(c))
            .Where(c => maxCodeLength <= 0 || CharCodes[c].Length <= maxCodeLength)
            .ToList();

        if (pool.Count == 0)
        {
            return string.Empty;
        }

        var rand = new System.Random();
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            builder.Append(pool[rand.Next(pool.Count)]);
            if ((i + 1) % 5 == 0 && i < n - 1)
            {
                builder.Append('、');
            }
        }
        return builder.ToString();
    }

    /// <summary>某个编码长度下可用的字数（课程等级据此决定练习量）。</summary>
    /// <param name="codeLength">编码长度（1~4）</param>
    /// <returns>可用字数</returns>
    public static int CountByCodeLength(int codeLength)
    {
        int len = Math.Clamp(codeLength, 1, 4);
        return CharCodes.Count(kv => kv.Value.Length == len);
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
