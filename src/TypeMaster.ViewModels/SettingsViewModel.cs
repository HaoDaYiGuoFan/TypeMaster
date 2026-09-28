using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IUnitOfWork _uow;
    private readonly IUserProfile _profile;
    private readonly FunTipService _fun;
    private readonly IMusicService _music;

    [ObservableProperty]
    private AppConfig _config = new();

    [ObservableProperty]
    private string _nickname = "用户1";

    [ObservableProperty]
    private string _statusMessage = "正在加载配置...";

    /// <summary>当前程序版本号，每次构建自动递增。</summary>
    public string AppVersion => TypeMaster.Core.AppVersion.Display;

    /// <summary>带说明的版本文案，供"关于"区域直接绑定展示。</summary>
    public string AppVersionText => TypeMaster.Core.AppVersion.DisplayWithLabel;

    public SettingsViewModel(IUnitOfWork uow, IUserProfile profile, FunTipService fun, IMusicService music)
    {
        _uow = uow;
        _profile = profile;
        _fun = fun;
        _music = music;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Config = await _uow.AppConfig.GetAsync();
        Nickname = _profile.Load().Nickname;
        StatusMessage = "配置已加载";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _uow.AppConfig.SaveAsync(Config);
        AppState.Update(Config);

        // 音量 / 音乐开关改动立即生效，无需重启曲目
        _music.ApplySettings();

        // 昵称存独立 JSON 档案，并立即同步给提示语服务
        var p = _profile.Load();
        var raw = (Nickname ?? string.Empty).Trim();
        p.Nickname = raw.Length == 0 ? "用户1" : raw;
        p.Initialized = true;
        _profile.Save(p);
        _fun.Nickname = p.Nickname;

        StatusMessage = "设置已保存并即时生效";
    }
}
