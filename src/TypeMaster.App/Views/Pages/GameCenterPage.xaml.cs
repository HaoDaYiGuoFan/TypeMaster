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

        // 进入游戏厅即播放游戏厅 BGM；离开时只在"当前放的还是大厅曲"时才停止。
        //
        // 为什么 Play 要 Dispatcher 推迟一拍：导航是"先创建新页再换内容"，
        // 新页 Loaded 常先于旧页 Unloaded 触发。若在 Loaded 里同步 Play，
        // 随后旧页（往往也是游戏厅——用户连点导航按钮）的 Unloaded 会看到
        // CurrentTrackId 仍是 lobby 而把它停掉——表现为"首页点几下后
        // 游戏乐园 BGM 又消失"。推迟一拍后 Play 必然落在本次切换的
        // 所有 Unloaded 之后，旧的归属校验不再误伤新页的曲子。
        //
        // 为什么 Stop 要校验归属：进入游戏对战页时，游戏 BGM 已在
        // Configure 里接棒（CurrentTrackId 已切换），此刻不能再 Stop，
        // 否则会把刚响起的游戏曲杀掉（"进游戏没音乐"的根因）。
        Loaded += (_, _) => Dispatcher.BeginInvoke(new System.Action(() => _music.Play(MusicLibrary.Lobby)));
        Unloaded += (_, _) =>
        {
            if (_music.CurrentTrackId == MusicLibrary.Lobby)
            {
                _music.Stop();
            }
        };
    }
}
