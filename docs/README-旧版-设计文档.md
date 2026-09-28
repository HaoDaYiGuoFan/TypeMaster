# TypeMaster — WPF 复刻金山打字通（完整落地开发文档）

> 技术栈：.NET 8 WPF + MVVM CommunityToolkit + SQLite(EF Core) + MDIX 美化
> 运行环境：Windows 10 / 11，.NET 8 桌面运行时
> 架构原则：严格分层（Core / Data / Services / ViewModels / App），依赖注入，MVVM 解耦，图标全部使用 MDIX 矢量 `PackIcon`（**不使用任何 emoji 图标**）

---

## 1. 整体方案与技术栈

| 模块 | 依赖组件 | 作用说明 |
| ---- | ---- | ---- |
| UI 框架 | WPF (.NET 8) | 桌面窗口、页面布局、控件渲染 |
| 架构 | CommunityToolkit.Mvvm | `[ObservableProperty]` / `[RelayCommand]`，界面与业务解耦 |
| 本地数据库 | Microsoft.EntityFrameworkCore.Sqlite | 持久化打字成绩、软件配置 |
| IOC 容器 | Microsoft.Extensions.DependencyInjection | 统一管理服务与 ViewModel 生命周期 |
| 界面美化 | MaterialDesignInXamlToolkit (MDIX) 4.9 | 卡片、按钮、弹窗、矢量图标、现代样式 |
| 系统交互 | User32 `WH_KEYBOARD_LL` | 全局键盘捕获，驱动虚拟键盘实时高亮 |
| 音效 | `Console.Beep`（kernel32） | 按键/错误/完成音效，无需外部音频文件 |

### 软件核心功能
- 英文打字练习、中文打字练习、限时速度测试
- 实时计算：正确率、WPM(英文速度)、KPM(中文速度)、耗时统计
- 输入字符逐字标色：正确=绿、错误=红、未输入=灰、当前字符=橙色高亮
- 虚拟键盘跟随物理按键实时高亮，基准键 ASDF JKL; 以浅绿底色提示指法
- 练习记录本地入库，历史成绩可查询 / 清空
- 重置、换一篇、提交保存、计时启停
- 亮色 / 护眼绿 / 暗黑三套主题，字体字号、虚拟键盘开关、音效开关可配置

---

## 2. 项目标准目录结构

```
TypeMaster（解决方案）
├─ TypeMaster.Core            # 核心模型、枚举、接口（不依赖任何外层）
│  ├─ Enums/                 # PracticeType / Difficulty / ThemeType
│  ├─ Entities/              # TypingRecord / AppConfig
│  ├─ AppState.cs            # 运行时全局配置单例（避免 VM 反向引用 App 层）
│  └─ Interfaces/            # IUnitOfWork / IRepository / ITypingService /
│                            #   IKeyboardHookService / ISoundService / INavigationService
├─ TypeMaster.Data           # 数据访问层（EF Core）
│  ├─ DbContext/             # TypeMasterDbContext
│  └─ Repositories/          # TypingRecordRepository / AppConfigRepository / UnitOfWork
├─ TypeMaster.Services       # 纯业务逻辑（无 UI 依赖）
│  ├─ TypingService.cs       # 核心打分算法
│  ├─ KeyboardHookService.cs # 全局键盘钩子
│  ├─ SoundService.cs        # 音效
│  ├─ KeyLabelHelper.cs      # VK 码 -> 按键标签
│  └─ TextLibrary.cs         # 内置中英文题库（按类型+难度分级）
├─ TypeMaster.ViewModels     # 所有页面 VM（MVVM）
│  ├─ MainViewModel / TypingViewModel / HistoryViewModel / SettingsViewModel
├─ TypeMaster.App            # 程序入口 + 资源字典 + 视图
│  ├─ App.xaml / App.xaml.cs # 入口、DI 容器、DB 初始化、主题应用
│  ├─ ThemeManager.cs        # 三套主题切换
│  ├─ NavigationService.cs   # 导航（解耦 VM 与 View）
│  ├─ Converters.cs          # 枚举 -> 中文文本
│  └─ Views/
│     ├─ Windows/MainWindow.xaml         # 导航栏 + 内容区
│     ├─ Pages/TypingPage.xaml           # 打字练习页
│     ├─ Pages/HistoryPage.xaml          # 成绩统计页
│     ├─ Pages/SettingsPage.xaml         # 系统设置页
│     └─ UserControls/VirtualKeyboard.xaml # 虚拟键盘控件
└─ TypeMaster.sln
```

---

## 3. 数据库设计（SQLite，文件 `typemaster.db`）

EF Core Code First，启动时 `EnsureCreated()` 自动建库建表（无需迁移脚本）。

### 3.1 TypingRecord 打字成绩表
| 字段 | 类型 | 主键 | 说明 |
| ---- | ---- | ---- | ---- |
| Id | int | 自增 | 记录唯一编号 |
| PracticeType | int | - | 0英文 / 1中文 / 2测速 |
| Difficulty | int | - | 0简单 / 1普通 / 2困难 |
| TotalCharCount | int | - | 对照文本总字符 |
| RightCharCount | int | - | 正确输入字符数 |
| WrongCharCount | int | - | 错误字符数 |
| Speed | double | - | 速度值 WPM/KPM |
| Accuracy | double | - | 百分比正确率 |
| UseSecond | int | - | 练习耗时（秒） |
| CreateTime | DateTime | - | 记录生成时间 |

### 3.2 AppConfig 软件配置表（单行，Id 固定为 1）
存储：Theme(主题)、ShowVirtualKeyboard、EnableSound、FontFamily、FontSize、WindowWidth、WindowHeight。

---

## 4. 公共基础层代码（Core）
- 枚举：`PracticeType` / `Difficulty` / `ThemeType`
- 实体：`TypingRecord` / `AppConfig`（纯 POCO，无特性污染，模型映射在 `DbContext.OnModelCreating` 用 Fluent API 完成）
- 接口：`IUnitOfWork`、`ITypingRecordRepository`、`IAppConfigRepository`、`ITypingService`、`IKeyboardHookService`、`ISoundService`、`INavigationService`
- `AppState`：静态单例，保存当前运行配置并广播 `SettingsChanged`，页面据此即时应用主题/字体/键盘可见性

---

## 5. 数据访问层（Data）
- `TypeMasterDbContext`：`DbSet<TypingRecord>`、`DbSet<AppConfig>`，Fluent 配置表名/主键/自增
- `UnitOfWork`：聚合两个仓储，统一 `SaveChanges`
- 仓储实现：`AddAsync` / `GetAllAsync` / `GetByTypeAsync` / `ClearAsync`（成绩）、`GetAsync` / `SaveAsync`（配置，单行 Id=1 自动初始化）

---

## 6. 核心业务服务（Services）
### 6.1 打字算法 `TypingService.Evaluate`
```
逐字符比较 target[i] == typed[i] -> right++，否则 wrong++
超出 target 长度的输入一律计入 wrong

正确率 Accuracy = right / (right + wrong) * 100
分钟数 minutes = max(elapsedSeconds, 1) / 60
英文速度 WPM  = (right / 5) / minutes        // 5 字符 ≈ 1 词
中文速度 KPM  = right / minutes             // 字符 / 分钟
完成态 IsCompleted = typed.Length >= target.Length
```
### 6.2 全局键盘钩子 `KeyboardHookService`
- `SetWindowsHookEx(WH_KEYBOARD_LL, …)` 安装底层钩子，不拦截只广播
- 回调读取 `KBDLLHOOKSTRUCT.vkCode`，经 `KeyLabelHelper` 转为标签（A / Space / Bksp …）
- 委托保存为字段防止被 GC 回收；`Start/Stop` 幂等
### 6.3 音效 / 题库
- `SoundService`：`Console.Beep` 三种频率对应 按键/错误/完成
- `TextLibrary`：英文 / 中文 / 测速 分级题库，`GetRandomText(type, difficulty)` 随机抽取

---

## 7. MVVM ViewModel 业务代码
- `MainViewModel`：导航状态 + `GoTyping/GoHistory/GoSettings` 命令，依赖 `INavigationService`（不直接引用 View）
- `TypingViewModel`：
  - `SelectType` / `SelectDifficulty` 切换并重新抽取文本
  - `SetTyped(string)`：由页面输入事件驱动，计算成绩、触发音效、判断完成态
  - `DispatcherTimer` 每 250ms 刷新计时；`SubmitAsync` 落库并重新抽取
  - 统计属性 `RightCount/WrongCount/AccuracyText/SpeedText/TimeText/Progress/IsRunning/IsCompleted`
- `HistoryViewModel`：`LoadAsync` / `ClearAsync` + `ObservableCollection<TypingRecord>`
- `SettingsViewModel`：`LoadAsync` / `SaveAsync`，保存后调用 `AppState.Update` 广播

---

## 8. 全部界面（XAML）
- **MainWindow**：左侧导航栏（`PackIcon` 矢量图标）+ 右侧 `ContentControl` 内容区
- **TypingPage**：类型/难度选择 → 彩色对照 `RichTextBox`（只读渲染）+ 输入 `TextBox`（支持中文 IME）→ 实时统计卡片 → 虚拟键盘 + 操作按钮
- **VirtualKeyboard**：代码生成 QWERTY 布局，`KeyPressed` 事件驱动键帽高亮（基准键浅绿底）
- **HistoryPage / SettingsPage**：`DataGrid` 成绩表 / 主题·字体·键盘·音效 设置

> P0 红线已遵守：**全程不使用 emoji 作图标**，功能图标统一使用 MDIX `PackIcon`（Keyboard / Typewriter / ChartLine / CogOutline 等矢量图标）。

---

## 9. 程序入口与 DI 容器（`App.xaml.cs`）
1. `ConfigureServices()` 构建 `ServiceCollection`：
   - `TypeMasterDbContext`（Singleton，连接 `typemaster.db`）
   - `IUnitOfWork`、`ITypingService`、`IKeyboardHookService`、`ISoundService`、`INavigationService`
   - 四个 ViewModel（Transient）、三个 Page（Transient）、MainWindow（Singleton）
2. `OnStartup`：`EnsureCreated()` 建库 → 读取/应用配置与主题 → 显示 MainWindow
3. 页面构造函数通过 `App.ServiceProvider.GetRequiredService<T>()` 解析自身 ViewModel，彻底解耦

---

## 10. NuGet 依赖包清单
| 包 | 版本 | 用于 |
| ---- | ---- | ---- |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.8 | SQLite 数据访问 |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 源生成器 |
| MaterialDesignThemes | 4.9.0 | MDIX 主题与控件 |
| MaterialDesignColors | 2.1.4 | MDIX 调色板 |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.122 | XAML 行为（预留扩展） |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | IOC 容器 |

> 各 `csproj` 已锁定版本；恢复命令：`dotnet restore`。

---

## 11. 功能扩展方案
- **闯关 / 分段考核评级**：在 `TypingService` 增加评级阈值（如 speed/accuracy 映射 S/A/B/C），VM 展示评级
- **指法基准键提示**：VirtualKeyboard 已对 ASDF JKL; 着色，可进一步加手指分区动画
- **海量题库**：扩充 `TextLibrary` 或改为从 SQLite/JSON 读取外部文本库
- **成绩可视化**：HistoryPage 接入图表（LiveCharts2 等）绘制速度/正确率趋势
- **自定义键位 / 小键盘高亮**：扩展 `KeyLabelHelper` 与 VirtualKeyboard 布局
- **云端同步**：将 `IAppConfigRepository` / 成绩仓储替换为 Web API 实现即可，VM 无需改动

---

## 12. 运行与部署步骤
1. **环境**：Windows 10/11，安装 [.NET 8 桌面运行时 / SDK](https://dot.net)。
2. **获取代码**：本目录即完整解决方案 `TypeMaster.sln`。
3. **恢复依赖**：`dotnet restore`
4. **生成**：`dotnet build -c Release`
5. **运行**：`dotnet run --project src/TypeMaster.App` 或直接启动 `src/TypeMaster.App/bin/Release/net8.0-windows/TypeMaster.exe`
6. **首次启动**：自动在程序目录生成 `typemaster.db` 并写入默认配置；进入“打字练习”页即可开始（物理键盘输入时虚拟键盘会实时高亮）。
7. **部署**：将 `bin/Release/net8.0-windows/` 整个发布目录拷贝到目标机，目标机需安装 .NET 8 桌面运行时；可用 `dotnet publish -c Release -r win-x64 --self-contained false` 生成独立部署包。

### 已验证
- 全 5 个项目 `dotnet build` **0 错误 0 警告**（含 WPF/BAML 编译）。
- 分层、DI、EF Core 映射、MDIX 主题类引用、键盘钩子 P/Invoke 签名均通过编译检查。

### 说明
- 本环境为无显示终端，无法实机启动 GUI；请在 Windows 桌面环境执行上述运行步骤以体验完整交互。
- 图标严格使用 MDIX 矢量 `PackIcon`，未使用任何 emoji；未使用紫粉渐变等模板化样式。

---

## 13. 打字小游戏（新增）
参考金山打字通经典玩法，内置 4 款打字小游戏，入口为左侧导航「游戏乐园」。

### 13.1 游戏列表
| 模式 (GameMode) | 名称 | 玩法 | 逃逸惩罚 |
| ---- | ---- | ---- | ---- |
| SpaceWar | 太空大战 | 敌机携带单词从顶部下落，输入单词将其击落 | 触底扣 1 生命 |
| WhackMole | 打地鼠 | 地鼠顶着单词从洞口冒出，抢在缩回前敲掉 | 超时仅消失，不扣生命 |
| CatchThief | 抓小偷 | 小偷带单词横向逃窜，输入单词擒获 | 逃出屏幕扣 1 生命 |
| FrogBug | 青蛙吃虫 | 虫子爬向青蛙，输入单词吃掉，守住中线 | 到达中线扣 1 生命 |

### 13.2 核心机制（统一）
- **锁定首字母**：输入某个目标单词的首字母即锁定该目标（多个候选时自动锁定「最紧急」者），继续输入逐字匹配，完整输入即消灭并计分。
- **计分**：`单词长度 × 10 × (1 + 连击×0.1)`，连击越高分越多；逃逸/被打中扣生命并清空连击。
- **生命归零**触发 `GameOver`，弹出结算遮罩（得分 / 最高连击），可「再来一局」或「返回游戏厅」。
- **难度**：简单 / 普通 / 困难 影响 速度、刷怪间隔、同屏上限、初始生命。

### 13.3 关键文件
- `TypeMaster.Core/Enums/GameMode.cs` — 游戏模式与图形枚举
- `TypeMaster.Services/GameTarget.cs` — 活动目标模型（位置/匹配进度，INPC 驱动 Canvas）
- `TypeMaster.Services/TypingGameEngine.cs` — 纯逻辑引擎（刷怪/移动/危险判定/计分/锁字），**无 WPF 依赖**，由页面 `DispatcherTimer` 约 30fps 驱动
- `TypeMaster.Services/TextLibrary.cs` — `GetGameWords(Difficulty)` 英文单词池
- `TypeMaster.ViewModels/GameCenterViewModel.cs` / `GameViewModel.cs` — 游戏厅与对战 VM
- `TypeMaster.App/Views/Pages/GameCenterPage.xaml` / `GamePlayPage.xaml` — 游戏厅与对战界面（Canvas 渲染目标 + 输入框 + 虚拟键盘）
- `TypeMaster.App/Converters.cs` — `GameGlyphToPackIconKindConverter`（图形枚举 → MDIX 矢量图标）

### 13.4 设计约束
- 目标图形统一使用 MDIX `PackIcon`（Ship→Rocket / Mole→Rodent / Thief→Run / Bug→Bug），**未使用任何 emoji 图标**。
- 各游戏强调色为纯色实心（`#29B6F6`/`#66BB6A`/`#FFA726`/`#26A69A`），非紫粉渐变，符合 UI 规范。
- 全局键盘钩子为应用级单例，页面只负责 `Start`（幂等），避免导航切换时钩子被意外关闭导致虚拟键盘失效。

### 13.5 视觉贴图与流畅度优化（新增）
- **品牌更名**：主窗口标题与左侧品牌统一为「小胡的打字通」，游戏厅大标题同为「小胡的打字通 · 游戏乐园」。
- **矢量贴图（无外部图片资源，纯 XAML 绘制）**：
  - 游戏厅头部横幅加入装饰键盘贴图（圆角键帽阵列）。
  - 每张游戏卡片图标改为「实心圆角图标瓦片」（MDIX `PackIcon` + 主色底），更醒目。
  - 对战页按模式渲染背景贴图：太空大战（星点 + 星球）、打地鼠（草地 + 洞口）、抓小偷（楼宇剪影）、青蛙吃虫（水池 + 荷叶），全部 `IsHitTestVisible=False` 不拦截输入。
- **锁定高亮**：被输入锁定的目标显示一圈强调色高亮环（`IsLocked` 由引擎在锁定/解锁时维护）。
- **流畅度优化**：
  - 引擎驱动改为**真实帧间隔 dt**（`Stopwatch` 测量并钳制 ≤0.05s），运动与帧率解耦，切回前台不再突跳。
  - 计时器从 33ms（~30fps）提升到 25ms（~40fps）。
  - 每个活动目标启用 `BitmapCache` + `UseLayoutRounding`/`SnapsToDevicePixels`，减少逐帧重绘开销。
- 全部沿用既有 UI 规范：图标仅用 MDIX `PackIcon` 矢量（无 emoji）、强调色为纯色实心（无紫粉渐变）、硬编码色仅 `#000`/`#fff`。
