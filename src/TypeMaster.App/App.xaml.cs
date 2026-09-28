using System;
using System.IO;
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
