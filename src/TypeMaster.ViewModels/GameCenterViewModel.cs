using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.ViewModels;

/// <summary>
/// 游戏乐园（首页）：列出全部打字小游戏，点击进入对应玩法。
/// </summary>
public partial class GameCenterViewModel : ObservableObject
{
    private readonly INavigationService _nav;

    public GameCenterViewModel(INavigationService nav) => _nav = nav;

    [RelayCommand]
    private void Play(GameMode mode) => _nav.NavigateGame(mode);
}
