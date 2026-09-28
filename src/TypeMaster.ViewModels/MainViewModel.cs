using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.ViewModels;

/// <summary>
/// 主窗口视图模型：维护顶部导航的选中状态、当前用户昵称与应用标题。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    #region 局部变量属性

    private readonly INavigationService _nav;
    private readonly IUserProfile _profile;

    #endregion 局部变量属性

    #region 绑定属性

    [ObservableProperty]
    private NavigationTarget _currentTarget = NavigationTarget.Home;

    /// <summary>应用标题：固定为"打字练习机"，不再拼接用户昵称。</summary>
    [ObservableProperty]
    private string _title = "打字练习机";

    [ObservableProperty]
    private bool _isTypingSelected = true;

    [ObservableProperty]
    private bool _isHistorySelected;

    [ObservableProperty]
    private bool _isSettingsSelected;

    [ObservableProperty]
    private bool _isGameCenterSelected;

    [ObservableProperty]
    private bool _isLearnSelected;

    [ObservableProperty]
    private bool _isCourseSelected;

    [ObservableProperty]
    private bool _isHomeSelected = true;

    [ObservableProperty]
    private string _nickname = "用户1";

    #endregion 绑定属性

    #region 构造函数

    public MainViewModel(INavigationService nav, IUserProfile profile)
    {
        _nav = nav;
        _profile = profile;
        Nickname = _profile.Load().Nickname;
    }

    #endregion 构造函数

    #region 导航命令

    [RelayCommand]
    private void GoHome() => SetTarget(NavigationTarget.Home);

    [RelayCommand]
    private void GoTyping() => SetTarget(NavigationTarget.Typing);

    [RelayCommand]
    private void GoHistory() => SetTarget(NavigationTarget.History);

    [RelayCommand]
    private void GoSettings() => SetTarget(NavigationTarget.Settings);

    [RelayCommand]
    private void GoGameCenter() => SetTarget(NavigationTarget.GameCenter);

    [RelayCommand]
    private void GoLearn() => SetTarget(NavigationTarget.Learn);

    [RelayCommand]
    private void GoCourse() => SetTarget(NavigationTarget.Course);

    /// <summary>
    /// 切换当前页面并同步顶部导航的选中状态。
    /// </summary>
    /// <param name="target">目标页面</param>
    private void SetTarget(NavigationTarget target)
    {
        CurrentTarget = target;
        IsHomeSelected = target == NavigationTarget.Home;
        IsTypingSelected = target == NavigationTarget.Typing;
        IsHistorySelected = target == NavigationTarget.History;
        IsSettingsSelected = target == NavigationTarget.Settings;
        IsGameCenterSelected = target == NavigationTarget.GameCenter;
        IsLearnSelected = target == NavigationTarget.Learn;
        IsCourseSelected = target == NavigationTarget.Course;
        _nav.Navigate(target);
    }

    #endregion 导航命令
}
