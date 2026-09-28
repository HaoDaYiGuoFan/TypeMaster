using System;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App.Views.Pages;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.App;

public class ViewNavigatedEventArgs : EventArgs
{
    public object View { get; }
    public ViewNavigatedEventArgs(object view) => View = view;
}

/// <summary>
/// 导航服务：将导航目标解析为对应页面，并通过事件通知主窗口切换内容。
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _sp;

    public event EventHandler<ViewNavigatedEventArgs>? ViewChanged;

    public NavigationService(IServiceProvider sp) => _sp = sp;

    public void Navigate(NavigationTarget target)
    {
        object view = target switch
        {
            NavigationTarget.Home => _sp.GetRequiredService<HomePage>(),
            NavigationTarget.Typing => _sp.GetRequiredService<TypingPage>(),
            NavigationTarget.History => _sp.GetRequiredService<HistoryPage>(),
            NavigationTarget.Settings => _sp.GetRequiredService<SettingsPage>(),
            NavigationTarget.GameCenter => _sp.GetRequiredService<GameCenterPage>(),
            NavigationTarget.Learn => _sp.GetRequiredService<LearnCenterPage>(),
            NavigationTarget.Course => _sp.GetRequiredService<CoursePage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
        ViewChanged?.Invoke(this, new ViewNavigatedEventArgs(view));
    }

    public void NavigateGame(GameMode mode)
    {
        var page = _sp.GetRequiredService<GamePlayPage>();
        page.Configure(mode);
        ViewChanged?.Invoke(this, new ViewNavigatedEventArgs(page));
    }
}
