using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App.Views.Pages;
using TypeMaster.App.Views.Windows;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Data.DbContext;
using TypeMaster.Data.Repositories;
using TypeMaster.Services;
using TypeMaster.ViewModels;

namespace TypeMaster.App;

/// <summary>
/// 应用程序入口：负责构建 DI 容器、初始化数据库与主题，并显示主窗口。
/// </summary>
public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 用户清单（启动早期即加载完成）。
    /// 供设置页的「切换/新建/删除用户」使用，也用于在标题栏显示当前用户名。
    /// </summary>
    public static UserRegistry Registry { get; private set; } = new();

    /// <summary>用户清单读写器。启动早期就要用，因此不放在 DI 容器里。</summary>
    public static UserRegistryStore RegistryStore { get; } = new();

    // 代码级 DPI 感知（manifest 的保底兜底）
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    protected override void OnStartup(StartupEventArgs e)
    {
        // 尝试设置 Per-Monitor V2 DPI 感知（若 manifest 已生效则此调用无害）
        try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { }

        base.OnStartup(e);

        // ====================================================================
        //  第一步：确定"本轮使用哪个用户的数据"
        //
        //  必须放在 ConfigureServices 之前——DI 容器构建时会把数据库路径
        //  固化进 DbContext 单例，之后再换用户就不会生效。
        //  这也正是"切换用户需要重启应用"的原因。
        // ====================================================================
        ResolveCurrentUser();

        ConfigureServices();

        // 确保 SQLite 数据库与表结构存在
        var ctx = ServiceProvider.GetRequiredService<TypeMasterDbContext>();
        ctx.Database.EnsureCreated();

        // EnsureCreated 不会给「已存在的旧库」补新列，这里做一次非破坏性升级
        // （PRAGMA table_info 检查 + ALTER TABLE ADD COLUMN，不动存量数据）
        SqliteSchemaUpgrader.Upgrade(ctx);

        // 加载配置并应用主题
        var uow = ServiceProvider.GetRequiredService<IUnitOfWork>();
        AppConfig cfg = uow.AppConfig.GetAsync().GetAwaiter().GetResult();
        AppState.Update(cfg);
        ThemeManager.ApplyTheme((ThemeType)cfg.Theme);
        AppState.SettingsChanged += (_, _) => ThemeManager.ApplyTheme((ThemeType)AppState.Current.Theme);

        var main = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = main;
        main.Show();

        // 首次启动：让玩家设置昵称，留空则默认“用户1”；所有趣味提示统一用此称呼
        var profile = ServiceProvider.GetRequiredService<IUserProfile>().Load();
        var funTip = ServiceProvider.GetRequiredService<FunTipService>();
        if (!profile.Initialized)
        {
            var dlg = new NicknameWindow { Owner = main };
            if (dlg.ShowDialog() == true)
            {
                profile = dlg.Profile;
                ServiceProvider.GetRequiredService<IUserProfile>().Save(profile);
            }
            else
            {
                // 对话框被强制关闭时仍沿用默认昵称
                profile.Initialized = true;
                ServiceProvider.GetRequiredService<IUserProfile>().Save(profile);
            }
        }
        funTip.Nickname = profile.Nickname;
        if (main.DataContext is MainViewModel mv)
            mv.Nickname = profile.Nickname;

        // 首次启动：新手引导（看完或跳过后标记，避免重复打扰）
        if (!profile.HasSeenGuide)
        {
            var guide = new OnboardingWindow(profile.Nickname) { Owner = main };
            guide.ShowDialog();
            profile.HasSeenGuide = true;
            ServiceProvider.GetRequiredService<IUserProfile>().Save(profile);
        }
    }

    /// <summary>
    /// 确定本轮使用哪个用户：加载清单、必要时让用户选择，最后写进 <see cref="AppDataPaths"/>。
    ///
    /// 流程：
    ///   1) 读 users.json
    ///   2) 一个用户都没有 → 自动建"用户1"，并把旧版单用户数据迁移进来
    ///   3) 多于一个用户   → 弹选择窗口（免得用户以为看的是自己的成绩）
    ///   4) 把选中用户写进 AppDataPaths
    /// </summary>
    private static void ResolveCurrentUser()
    {
        Registry = RegistryStore.Load();

        bool wasEmpty = Registry.Users.Count == 0;
        List<string> migrateFailed = RegistryStore.EnsureUsable(Registry);

        // 首次创建用户时可能发生了迁移，失败要告知（否则用户以为成绩丢了）
        if (wasEmpty && migrateFailed.Count > 0)
        {
            MessageBox.Show(
                "升级时以下文件未能迁移到新的用户目录，已保留在原位置：\n\n" +
                string.Join("\n", migrateFailed) +
                "\n\n原位置：" + AppDataPaths.DataDirectory,
                "数据迁移提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // 多于一个用户才需要选择；只有一个就直接用
        if (Registry.Users.Count > 1)
        {
            var picker = new UserPickerWindow(Registry, allowCreate: true);
            picker.ShowDialog();
        }

        // 兜底：选人窗口被强制关闭、或选中的用户已不存在
        if (Registry.Current is null)
        {
            var fallback = Registry.Users.OrderByDescending(u => u.LastUsedAt).FirstOrDefault()
                           ?? RegistryStore.CreateUser(Registry, "用户1");
            if (!Registry.Users.Contains(fallback))
            {
                Registry.Users.Add(fallback);
            }
            Registry.CurrentUserId = fallback.Id;
            RegistryStore.Save(Registry);
        }

        AppDataPaths.SetCurrentUser(Registry.Current!.Id);
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // 数据库上下文（单例，桌面单用户场景足够）
        string dbPath = TypeMaster.Core.AppDataPaths.DatabaseFile;
        services.AddSingleton<TypeMasterDbContext>(_ =>
            new TypeMasterDbContext(
                new DbContextOptionsBuilder<TypeMasterDbContext>()
                    .UseSqlite($"Data Source={dbPath}").Options));

        // 数据层
        services.AddSingleton<IUnitOfWork, UnitOfWork>();

        // 业务服务
        services.AddSingleton<ITypingService, TypingService>();
        services.AddSingleton<IKeyboardHookService, KeyboardHookService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<ITextToSpeechService, SpeechService>();
        services.AddSingleton<IMusicService, MusicService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<FunTipService>();
        services.AddSingleton<IArticleLibrary, JsonArticleLibrary>();
        services.AddSingleton<IUserProfile, JsonUserProfile>();
        services.AddSingleton<ICourseProgressStore, JsonCourseProgressStore>();

        // 用户清单读写器：复用启动时那个实例，避免两份状态不一致
        services.AddSingleton(RegistryStore);
        services.AddSingleton(Registry);

        // 课程会话：在课程中心页与打字练习页之间传递当前关卡与挑战结果
        services.AddSingleton<CourseSession>();

        // 打字小游戏引擎（每个对战页独立实例）
        services.AddTransient<TypingGameEngine>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<TypingViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<GameCenterViewModel>();
        services.AddTransient<GameViewModel>();
        services.AddTransient<CourseViewModel>();

        // Views（页面按需解析，主窗口单例）
        services.AddTransient<HomePage>();
        services.AddTransient<TypingPage>();
        services.AddTransient<HistoryPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<GameCenterPage>();
        services.AddTransient<GamePlayPage>();
        services.AddTransient<LearnCenterPage>();
        services.AddTransient<CoursePage>();
        services.AddSingleton<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();
    }
}
