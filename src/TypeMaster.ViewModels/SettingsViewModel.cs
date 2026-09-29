using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;
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

    /// <summary>
    /// 长辈模式开关变化：一键应用 / 撤销"适合中老年学习者"的一组设置。
    ///
    /// 开启时设定：
    ///   · 字号放大到至少 24（正文更大更清晰）
    ///   · 语速放慢 2 档（听编码更清楚）
    ///   · 打开五笔逐码按键提示 + 大字显示
    ///   · 打开语音朗读
    ///   · 显示虚拟键盘（按键位置有人指）
    ///   · 切到护眼绿主题（长时间练习眼睛更舒服）
    /// 关闭时只恢复"字号 + 语速"，其它项保持用户现状，避免误清设置。
    /// </summary>
    partial void OnConfigChanged(AppConfig value)
    {
        if (value is null)
        {
            return;
        }
        // 记录进入长辈模式前的值，供退出时恢复
        _lastElderMode = value.ElderMode;
    }

    /// <summary>上一次观察到的长辈模式状态，用于判断是"刚开启"还是"刚关闭"。</summary>
    private bool _lastElderMode;

    /// <summary>
    /// 切换长辈模式（由界面复选框绑定调用）。
    /// </summary>
    /// <param name="enabled">是否开启</param>
    public void ApplyElderMode(bool enabled)
    {
        if (enabled == _lastElderMode)
        {
            return;
        }

        var cfg = Config;
        if (enabled)
        {
            // 记录当前值，退出时恢复
            cfg.FontSizeBeforeElderMode = cfg.FontSize;
            cfg.SpeechRateBeforeElderMode = cfg.SpeechRate;

            if (cfg.FontSize < ElderFontSize) cfg.FontSize = ElderFontSize;
            cfg.SpeechRate = Math.Max(-5, cfg.SpeechRate - 2);
            cfg.EnableSpeech = true;
            cfg.ShowWubiKeyHint = true;
            cfg.ShowWubiBigChar = true;
            cfg.ShowVirtualKeyboard = true;
            // 护眼绿（ThemeType.EyeCare = 1），长时间练习更舒适
            cfg.Theme = (int)ThemeType.EyeCare;

            _lastElderMode = true;
            StatusMessage = "已开启长辈模式：字号更大、语速更慢，五笔提示与语音朗读已打开";
        }
        else
        {
            // 只恢复字号与语速，其余保持用户现状
            if (cfg.FontSizeBeforeElderMode > 0)
            {
                cfg.FontSize = cfg.FontSizeBeforeElderMode;
            }
            cfg.SpeechRate = cfg.SpeechRateBeforeElderMode;

            _lastElderMode = false;
            StatusMessage = "已关闭长辈模式，字号与语速已恢复";
        }
    }

    /// <summary>长辈模式下的最小字号（再小就看不清楚了）。</summary>
    private const double ElderFontSize = 26;

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
