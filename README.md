# TypeMaster · 打字通

> 一款面向 Windows 的**中文打字练习软件**，对标金山打字通的核心体验，用 .NET 8 + WPF 从零实现。
> 覆盖指法入门到文章练习的完整学习路径，并配有 6 款打字小游戏、五笔与拼音学习园地。
> **内置面向中老年学习者的「长辈模式」**：一键放大字号、放慢语速、开启五笔逐码提示。
> **支持多使用者**：同一台电脑上多人使用，成绩与进度各自独立、互不干扰。

<p>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white">
  <img alt="WPF" src="https://img.shields.io/badge/WPF-Windows-0078D4">
  <img alt="Platform" src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green">
</p>

---

## 目录

- [功能一览](#功能一览)
- [快速开始](#快速开始)
- [项目结构](#项目结构)
- [技术栈](#技术栈)
- [题库与词库规模](#题库与词库规模)
- [数据存放位置](#数据存放位置)
- [从源码构建](#从源码构建)
- [常见问题](#常见问题)
- [授权](#授权)

---

## 功能一览

软件分为 **7 大模块**，从导航栏可直达。

### 🏠 首页
- 学习概览与快捷入口
- 昵称设置与首次使用引导
- 当前水平等级展示（新手 → 入门 → 熟练 → 高手 → 大师）

### ⌨️ 打字练习
核心练习页，覆盖 6 种练习类型：

| 练习类型 | 说明 |
| --- | --- |
| 英文 | 英文单词、句子、段落 |
| 中文 | 中文短文（小学题库，一到六年级分级） |
| 英文单词 | 整词输入，按长度与常见度分级 |
| 中文词组 | 双字词到四字成语，按年级分级 |
| 五笔 | 86 版五笔字根练习；**逐码提示该按哪个键**，不必先背熟字根 |
| 限时测速 | 1 / 3 / 5 分钟限时，时间到自动交卷 |

配套能力：

- **10 级难度细分**——在三档难度之上再做 10 级微调，等级越高文本越长
- **逐字实时着色**——正确=绿、错误=红、未输入=灰、当前字符=橙色高亮
- **自动续接**——打到文本接近末尾时自动接上**新的**内容，练习可以一直进行，想停时点「提交成绩」即可
- **自动滚动**——对照文本超出一屏时，视口自动跟随当前输入位置
- **虚拟键盘**——跟随物理按键实时高亮，基准键 `ASDF JKL;` 以浅绿底色提示指法
- **自定义文章**——可导入自己的文本作为练习素材
- **实时统计**——正确数 / 错误数 / 正确率 / 速度(WPM 或 字/分) / 耗时 / 实时评级

### 📊 成绩统计
- 历史成绩列表与筛选
- **键位热力图**——按正确率标出薄弱键位
- 正确率与速度趋势
- 成绩等级评定（D / C / B / A / S）

### 🎮 游戏乐园
6 款打字小游戏，把单词输入变成游戏操作：

| 游戏 | 玩法 |
| --- | --- |
| 太空大战 | 敌机携带单词从顶部下落，输入单词将其击落 |
| 打地鼠 | 地鼠携带单词从地洞冒出，输入单词将其敲打 |
| 抓小偷 | 小偷携带单词横向逃窜，输入单词将其擒获 |
| 青蛙吃虫 | 虫子携带单词爬向青蛙，输入单词将其吃掉 |
| 打气球 | 气球携带单词从底部升起，输入单词将其扎破 |
| 生死时速 | 与电脑赛跑，完整输入单词驱动选手前进 |

游戏化增强：

- **9 个原创卡通精灵**（256×256 PNG）+ **7 首原创 BGM**（按游戏各自节奏）
- 动画效果：`BackEase` 缩放入场、待机浮动、锁定脉动、被消灭时旋转缩小上飘消散
- **音频引擎**：NAudio 共享单例混音器，BGM 与音效可同时播放不抢占设备
- BGM / 音效音量独立可调

### 📚 学习园地
- **五笔字根表**——86 版字根口诀，按分区展示
- **五笔逐码提示**——练习时显示「第2码 E键（月）」，把编码逐位翻译成按键
- **拼音表**——23 声母 / 24 韵母 / 16 整体认读，共 63 个拼音胶囊
- **语音朗读**——点击即可朗读字根口诀与拼音（离线 TTS，无需联网）
  - 拼音用**呼读音汉字表**发音（直接读字母会被读成英文字母名）
  - 口诀自动**剥离括号提示**，生僻字用同音字替换表纠正

### 🎓 课程中心
**27 关**渐进式课程，分 6 个阶段：

| 阶段 | 关卡 | 内容 |
| --- | --- | --- |
| 指法入门 | 5 关 | 基准键 → 上排键 → 下排键 → 空格组合 → 三排混合 |
| 单键练习 | 4 关 | 左手字母区 → 右手字母区 → 数字行 → 符号键 |
| 单词练习 | 6 关 | 英文单词（基础/进阶/挑战）、中文词组（基础/进阶/挑战） |
| 句子练习 | 3 关 | 英文句子 → 中文句子 → 中英混排 |
| 文章练习 | 3 关 | 英文短文 → 中文短文 → 毕业关（五笔短文） |
| **五笔入门** | **6 关** | **零基础支线：一键成字 → 两码 → 三码 → 四码 → 常用字 → 打一段话** |

> **五笔入门是独立支线**：不必先做完前面 20 关英文/中文练习即可直接开始，
> 且只从**最常用 3500 字**中出题，不会碰到生僻字。

- 通关标准：**正确率 ≥ 90%** 且评级 **B 级以上**
- 闯关进度自动保存，逐关解锁
- 五笔入门支线内部单独解锁，第一关始终可玩

### ⚙️ 系统设置
- **使用者管理**——切换 / 新建 / 重命名 / 删除；多人共用一台电脑时数据互不影响
- **显示适配**——界面缩放（0.8~1.6 倍）、启动自动适配屏幕、窗口尺寸按屏幕约束
- **长辈模式**（一键预设）——字号放大、语速放慢、五笔提示与语音朗读全开、切护眼绿主题
- **五笔学习辅助**（可单项开关）——逐码按键提示 / 大字显示当前字 / 切换到新字时朗读编码
- **三套主题**：亮色 / 护眼绿 / 暗黑
- 字体与字号（默认 Consolas 22）
- 虚拟键盘开关
- 音效开关与音量、BGM 开关与音量
- 语音朗读开关与语速

---

## 快速开始

### 方式一：直接运行（推荐给普通用户）

1. 从 [Releases](../../releases) 页面下载 `TypeMaster-v1.0.928-win-x64.zip`
2. 解压到任意目录
3. 双击 `TypeMaster.exe` 即可运行

> **无需安装 .NET 运行时**——发布包是自包含（self-contained）单文件，
> 原生库已一并打包，单独拷走 exe 也能运行。

**系统要求**：Windows 10 / 11（64 位）

### 方式二：从源码构建

见 [从源码构建](#从源码构建)。

---

## 项目结构

```
TypeMaster/
├─ TypeMaster.sln
├─ global.json                      # 固定 SDK 版本（见下方说明）
├─ src/
│  ├─ TypeMaster.Core               # 核心模型、枚举、接口（不依赖任何外层）
│  │  ├─ Entities/                  # TypingRecord / AppConfig / UserProfile / CourseProgress / CustomArticle
│  │  ├─ Enums/                     # PracticeType / Difficulty / DifficultyScale / GameMode / Grade / GradeScale / ThemeType
│  │  ├─ Interfaces/                # 服务契约（打字、键盘钩子、音效、语音、导航、仓储…）
│  │  ├─ AppState.cs                # 运行时全局配置单例
│  │  ├─ AppDataPaths.cs            # 用户数据目录定位
│  │  └─ AppVersion.cs              # 版本信息
│  │
│  ├─ TypeMaster.Data               # 数据访问层（EF Core + SQLite）
│  │  ├─ DbContext/                 # TypeMasterDbContext / SqliteSchemaUpgrader
│  │  └─ Repositories/              # TypingRecordRepository / AppConfigRepository / UnitOfWork
│  │
│  ├─ TypeMaster.Services           # 业务逻辑层（无 UI 依赖）
│  │  ├─ TypingService.cs           # 核心打分算法
│  │  ├─ TypingGameEngine.cs        # 游戏引擎（6 种玩法的统一驱动）
│  │  ├─ TextLibrary.cs             # 内置中英文题库（73 篇文章）
│  │  ├─ WordLibrary.cs             # 单词 / 词组 / 句子库
│  │  ├─ GameWords.cs               # 游戏词库（4700 词，按难度分档）
│  │  ├─ WubiLibrary.cs             # 五笔字根表 + 6489 常用字编码 + 逐码提示
│  │  ├─ PinyinLibrary.cs           # 拼音声韵母表
│  │  ├─ CourseLibrary.cs           # 27 关课程定义（含五笔入门支线）
│  │  ├─ AudioEngine.cs             # NAudio 共享混音器
│  │  ├─ MusicLibrary.cs            # BGM 曲目表
│  │  ├─ SpeechService.cs           # 语音朗读（System.Speech）
│  │  ├─ KeyboardHookService.cs     # 全局键盘钩子（WH_KEYBOARD_LL）
│  │  └─ ...
│  │
│  ├─ TypeMaster.ViewModels         # MVVM 视图模型
│  │  ├─ MainViewModel / TypingViewModel / HistoryViewModel
│  │  ├─ GameCenterViewModel / GameViewModel
│  │  ├─ CourseViewModel / SettingsViewModel
│  │  └─ ...
│  │
│  └─ TypeMaster.App                # 程序入口 + 视图 + 资源
│     ├─ App.xaml(.cs)              # 入口、DI 容器、数据库初始化、主题应用
│     ├─ Views/Pages/               # 7 个页面
│     ├─ Views/Windows/             # 主窗口 + 导入文章 / 昵称 / 新手引导窗口
│     ├─ Views/UserControls/        # 虚拟键盘 / 毛玻璃标题栏 / 趣味提示
│     ├─ Assets/Music/              # 7 首原创 BGM（mp3）
│     ├─ Assets/Sprites/            # 9 个卡通精灵（png）
│     └─ Themes/ Resources/         # 主题与资源字典
├─ docs/                            # 文档
└─ dist/                            # 发布包输出（git 忽略）
```

### 架构原则

- **严格分层**：`Core` ← `Data` ← `Services` ← `ViewModels` ← `App`，依赖单向
- **依赖注入**：所有服务经 DI 容器注册，生命周期统一管理
- **MVVM**：`CommunityToolkit.Mvvm` 的 `[ObservableProperty]` / `[RelayCommand]`
- **UI 规范**：图标只用 MDIX 矢量 `PackIcon`（**不使用 emoji**）；强调色为纯色实心
- **WPF 通用行为用附加属性实现**，不继承自定义基类（避免样式继承带来的连锁问题）

---

## 技术栈

| 分类 | 组件 | 版本 | 用途 |
| --- | --- | --- | --- |
| 运行时 | .NET | 8.0 | `net8.0-windows` |
| UI 框架 | WPF | — | 桌面窗口与控件 |
| UI 美化 | MaterialDesignThemes | 4.9.0 | 卡片、按钮、矢量图标、弹性动画 |
| UI 美化 | MaterialDesignColors | 2.1.4 | 配色 |
| MVVM | CommunityToolkit.Mvvm | 8.2.2 | 属性与命令生成 |
| 行为 | Microsoft.Xaml.Behaviors.Wpf | 1.1.122 | 交互行为 |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | 8.0.0 | 服务容器 |
| 数据持久化 | Microsoft.EntityFrameworkCore.Sqlite | 8.0.8 | 成绩与配置存储 |
| 音频 | NAudio | 2.2.1 | BGM 播放、音效合成、混音 |
| 语音 | System.Speech | 8.0.0 | 离线 TTS 朗读 |

### 为什么这样选

- **BGM 与音效共用一个 `WaveOutEvent`**：Windows 上多个 WaveOut 设备会互相抢占，BGM 与音效无法真正叠加。因此用共享单例 `AudioEngine` + `MixingSampleProvider` 统一混音。
- **BGM 为原创合成**：不引入授权不明的第三方音乐素材。7 首曲子由多声部合成生成，各有独立 BPM（112~152）。
- **语音用离线 TTS**：`System.Speech` 调用系统已安装的中文语音引擎（如 `Microsoft Huihui Desktop`），无网络请求。
- **`TypeMaster.Services` 目标框架为 `net8.0-windows`**：NAudio 的 `WaveOut` 与 SAPI 都是 Windows 专有 API；但服务层不依赖 WPF（淡入淡出用 `System.Threading.Timer` 而非 `DispatcherTimer`）。

---

## 题库与词库规模

| 内容 | 规模 |
| --- | --- |
| 内置文章 | **73 篇**（小学一到六年级分级 + 英文分级） |
| 游戏词库 | **4700 词**（简单 1500 / 普通 2000 / 困难 1200） |
| 英文单词 / 句子 | 166 词 · 30 句 |
| 中文词组 / 句子 | 147 词 · 30 句 |
| 五笔字库 | **6489 字**（《通用规范汉字表》一级+二级，最常用 140 字 100% 覆盖） |
| 五笔字根口诀 | 86 版完整字根表（25 键） |
| 拼音表 | 23 声母 · 24 韵母 · 16 整体认读 |
| 课程关卡 | 27 关 / 6 阶段（含五笔入门支线 6 关） |
| 原创音频 | 7 首 BGM |
| 原创图形 | 9 个卡通精灵 |

### 游戏词库说明

词库基于 [google-10000-english](https://github.com/first20hours/google-10000-english)（Google Web Trillion Word Corpus 词频表）整理，处理规则：

- 按字母数分三档，与游戏的字长限制对齐
- 过滤：1-2 字母词、缩写、技术网络词、品牌机构名、地名人名日期、敏感与消极词
- 形态规则：剔除含长辅音簇、元音占比过低的非自然词
- 每档只保留**词频最高**的若干个，避免长尾生僻词影响体验

> 该词表的授权条款见 [授权](#授权) 一节。

---

## 多使用者（多人共用一台电脑）

数据按使用者**物理分目录**存放，而不是在同一份数据里加标记区分——
后者需要所有查询都带上过滤条件，漏一处就会串数据；分目录天然不会串。

```
%LOCALAPPDATA%\TypeMaster\
├─ users.json              使用者清单（有哪些人、当前是谁）
└─ users\
   ├─ u1\                 使用者 1
   │  ├─ typemaster.db        成绩
   │  ├─ user_profile.json    昵称
   │  ├─ course_progress.json 闯关进度
   │  └─ custom_articles.json 自定义文章
   └─ u2\                 使用者 2（与 u1 完全隔离）
```

### 使用方式

- **启动时**：若有多个使用者，会弹出选择窗口（双击名字即可进入）
- **设置页 → 使用者**：可切换 / 新建 / 重命名 / 删除

### 几点说明

| 事项 | 行为 |
| --- | --- |
| 切换使用者 | **需要重启应用**（数据库连接已绑定到原目录，重启可确保不串数据） |
| 重命名 | 只改显示昵称，**数据目录不动**，成绩不会"搬家" |
| 删除使用者 | 需两次确认；会**一并删除其全部数据且不可恢复** |
| 删除限制 | 不能删除当前使用者，也不能删到只剩零位 |
| 从旧版升级 | 旧数据**自动迁移**到第一个使用者目录；迁移失败会弹窗告知并保留原文件 |

> 典型场景：爷爷学五笔、奶奶学拼音，各练各的，成绩与闯关进度互不干扰。

---

## 数据存放位置

用户数据存放在 **当前 Windows 用户** 的本地应用数据目录，而非 exe 所在目录——
这样单文件分发时，移动或删除 exe 都不会丢失数据。

具体子目录见上一节「多使用者」。简言之：

```
%LOCALAPPDATA%\TypeMaster\
├─ users.json              使用者清单
└─ users\<使用者>\         该使用者的全部数据
   ├─ typemaster.db
   ├─ user_profile.json
   ├─ custom_articles.json
   ├─ course_progress.json
   └─ exports\            导出的成绩 CSV
```

**备份方法**：复制整个 `%LOCALAPPDATA%\TypeMaster` 目录（含所有使用者）；
只备份某一位，则复制对应的 `users\<使用者>` 子目录。

### 数据库升级

数据库结构变更采用**非破坏性升级**：启动时通过 `PRAGMA table_info` 检查列是否存在，
缺失则用 `ALTER TABLE ADD COLUMN` 补列，**不会清空已有数据**。
因此从旧版本升级时，历史成绩与设置都会完整保留。

---

## 从源码构建

### 环境要求

- Windows 10 / 11
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)（建议 **8.0.4xx**）

> **关于 `global.json`**：项目根目录的 `global.json` 将 SDK 固定为 `8.0.425`。
> 如果你的机器上装了多个 SDK（例如同时装了 .NET 9 / 10），没有这个文件时
> `dotnet build` 可能选中更高版本 SDK，导致构建行为不一致。
> 若你本机没有 8.0.425，可修改该文件中的 `version`，或将其删除。

### 构建

```powershell
git clone <你的仓库地址>
cd TypeMaster

dotnet build TypeMaster.sln -c Release
```

预期输出：**0 个警告，0 个错误**。

### 运行

```powershell
dotnet run --project src/TypeMaster.App/TypeMaster.App.csproj -c Release
```

### 发布单文件 exe

```powershell
dotnet publish src/TypeMaster.App/TypeMaster.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false `
  -o publish
```

> **`IncludeNativeLibrariesForSelfExtract=true` 不能省**。
> `PublishSingleFile` 默认**不打包原生库**，只把托管程序集打进 exe。
> 不加上这个开关时，单独拷走 exe 运行会因缺少原生库而崩溃。
> 加上之后，WPF 渲染与 SQLite 所需的原生 DLL 会一起打包。

产物体积约 **175 MB**（自包含 + 原生库），单独拷到任意目录均可运行。

更多构建细节与排错见 [`docs/BUILD.md`](docs/BUILD.md)。

---

## 显示适配（多分辨率 / 缩放）

支持从 1080p 到 4K、系统缩放 100% 至 200% 的各种组合。

### 已做的适配

| 能力 | 说明 |
| --- | --- |
| Per-Monitor V2 DPI 感知 | manifest 声明，跨不同缩放比的显示器拖动时自动适配 |
| 启动自动适配 | 窗口尺寸按当前屏幕可用区自动选择（可在设置中关闭） |
| 屏幕约束 | 无论设置多大，窗口始终完整落在屏幕内（含任务栏避让） |
| 实时重算 | 系统缩放变化、插拔外接显示器时自动重新适配 |
| 对话框约束 | 昵称 / 引导 / 导入文章 / 选择使用者等窗口同样按工作区收口 |
| 虚拟键盘自适应 | 窄屏下整体等比缩小，不会横向溢出 |
| 界面缩放 | 0.8 ~ 1.6 倍手动调整，独立于系统缩放 |

### 按屏幕给的建议

| 你的情况 | 建议 |
| --- | --- |
| 2.8K / 3.2K / 4K，觉得字偏小 | 界面缩放调到 **1.15 ~ 1.30** |
| 1080p，系统缩放 150% 以上显得拥挤 | 界面缩放调到 **0.85 ~ 0.95** |
| 1080p，系统缩放 175% / 200% | 已自动收窄窗口；如仍拥挤可调小界面缩放 |
| 视力不佳 | 直接开**长辈模式**（字号、语速、提示一并调整） |

> 窗口尺寸滑块的可调范围会**按当前屏幕自动约束**，
> 因此不会设出"屏幕放不下"的值；也可以点「按当前屏幕重置尺寸」。

---

## 常见问题

<details>
<summary><b>点「换一篇」后文本变长了，打到一半还会自动加内容？</b></summary>

这是**设计如此**。只要对照文本长度有限，打到末尾就会停住，因此软件会在
剩余不足约 160 字符时自动接上一段**新的**随机内容，让练习可以一直进行。

想结束时点「提交成绩」即可——成绩按已输入部分结算。

课程关卡、指定文章、五笔模式下**不会**自动续接，以保证考核公平与尊重你的选择。
</details>

<details>
<summary><b>拼音朗读读的是汉字而不是字母？</b></summary>

这是**刻意的**。直接让 TTS 读 `b p m f` 会被读成英文字母名（"B P M F"），
所以拼音使用**呼读音汉字表**发音；五笔口诀中的括号提示（如 `戋（兼）`）会自动剥离，
生僻字用同音字替换表纠正。实测以人耳听感为准。
</details>

<details>
<summary><b>语音朗读没有声音？</b></summary>

语音朗读依赖系统已安装的**中文语音引擎**。可在「设置 → 时间和语言 → 语音」
中检查是否安装了中文语音包。若系统缺少中文引擎，朗读功能会自动跳过而不会报错。
</details>

<details>
<summary><b>游戏没有声音 / 声音很小？</b></summary>

在「系统设置」中检查：
- **音效**开关与音量（默认 80）
- **BGM** 开关与音量（默认 55）

BGM 与音效通过共享混音器播放，两者可同时生效，互不抢占。
</details>

<details>
<summary><b>换了电脑，成绩会丢吗？</b></summary>

成绩存在本地 `%LOCALAPPDATA%\TypeMaster`，**不会自动同步**。
换机器前请复制该目录；新机器上放回同一位置即可。
</details>

<details>
<summary><b>为什么发布包这么大（175MB）？</b></summary>

因为它是**自包含单文件**：内含 .NET 运行时、WPF 原生渲染库、SQLite 原生库。
好处是**目标机器无需安装任何运行时**，拷走即用。
</details>

<details>
<summary><b>支持 macOS / Linux 吗？</b></summary>

**不支持。** 项目使用 WPF（Windows 专有 UI 框架），且音频与语音依赖
Windows 专有 API（NAudio `WaveOut`、SAPI）。目标平台仅 Windows。
</details>

---

## 授权

本项目采用 [MIT License](LICENSE)。

### 第三方素材

| 素材 | 来源 | 授权 |
| --- | --- | --- |
| 游戏词库词表 | [google-10000-english](https://github.com/first20hours/google-10000-english)（Google Web Trillion Word Corpus） | 个人 / 教育 / 研究用途可用；**商业用途需另行向 [LDC](https://www.ldc.upenn.edu/) 取得授权** |
| UI 图标 | MaterialDesignInXamlToolkit | MIT |
| BGM / 卡通精灵 | 本项目原创合成 | 随本项目授权 |

> **关于游戏词库的商用限制**：词表来源的 LICENSE 明确说明「不建议在未向
> Linguistic Data Consortium 取得授权的情况下用于商业目的」。
> 本项目定位为个人 / 教育用途的打字练习工具。若你计划商业分发，
> 需自行处理词表的授权，或替换 `src/TypeMaster.Services/GameWords.cs` 中的词表。

---

## 贡献

欢迎提交 Issue 与 Pull Request。

- 提交前请确保 `dotnet build TypeMaster.sln -c Release` **0 警告 0 错误**
- 源码文件请保持 **UTF-8 无 BOM + LF** 换行（项目既有约定）
- 新增界面元素请遵循既有 UI 规范：图标用 MDIX `PackIcon`，不使用 emoji

---

<div align="center">

**TypeMaster** · 用 .NET 8 + WPF 打造的打字练习工具

</div>
