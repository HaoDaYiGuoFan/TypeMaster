using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;
using TypeMaster.ViewModels;

namespace TypeMaster.App.Views.Pages;

/// <summary>
/// 游戏厅页：挑选打字小游戏。进入本页时播放游戏厅背景音乐。
/// </summary>
public partial class GameCenterPage : UserControl
{
    private readonly IMusicService _music;

    public GameCenterPage()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<GameCenterViewModel>();
        _music = App.ServiceProvider.GetRequiredService<IMusicService>();

        // 进入游戏厅即播放游戏厅 BGM，离开时停止（避免回到其它页面还在响）
        Loaded += (_, _) => _music.Play(MusicLibrary.Lobby);
        Unloaded += (_, _) => _music.Stop();
    }
}
