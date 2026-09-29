using System;
using System.Linq;
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
    private readonly UserRegistryStore _users;
    private readonly UserRegistry _registry;

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

    public SettingsViewModel(IUnitOfWork uow, IUserProfile profile, FunTipService fun, IMusicService music,
                             UserRegistryStore users, UserRegistry registry)
    {
        _uow = uow;
        _profile = profile;
        _fun = fun;
        _music = music;
        _users = users;
        _registry = registry;
        RefreshUsers();
        _ = LoadAsync();
    }

    #region 使用者管理

    /// <summary>全部使用者（供设置页列表展示）。</summary>
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<UserRow> _userRows = new();

    /// <summary>当前使用者昵称，用于标题栏与提示。</summary>
    [ObservableProperty]
    private string _currentUserName = string.Empty;

    /// <summary>新建使用者时输入的昵称。</summary>
    [ObservableProperty]
    private string _newUserName = string.Empty;

    /// <summary>使用者管理区的状态提示。</summary>
    [ObservableProperty]
    private string _userStatusMessage = string.Empty;

    /// <summary>列表中的一行使用者（供界面绑定显示）。</summary>
    public sealed class UserRow
    {
        /// <summary>用户 Id。</summary>
        public string Id { get; init; } = string.Empty;
        /// <summary>昵称。</summary>
        public string Nickname { get; init; } = string.Empty;
        /// <summary>是否当前使用者。</summary>
        public bool IsCurrent { get; init; }
        /// <summary>附加说明（上次使用时间）。</summary>
        public string Detail { get; init; } = string.Empty;
        /// <summary>显示用文本（昵称 + 当前标记）。</summary>
        public string Display => IsCurrent ? Nickname + "（当前）" : Nickname;
    }

    /// <summary>重建使用者列表。</summary>
    private void RefreshUsers()
    {
        var rows = _registry.Users
            .OrderByDescending(u => u.LastUsedAt)
            .Select(u => new UserRow
            {
                Id = u.Id,
                Nickname = u.Nickname,
                IsCurrent = u.Id == _registry.CurrentUserId,
                Detail = u.Id == _registry.CurrentUserId
                    ? "正在使用"
                    : $"上次使用 {FormatWhen(u.LastUsedAt)}"
            })
            .ToList();

        UserRows = new System.Collections.ObjectModel.ObservableCollection<UserRow>(rows);
        CurrentUserName = _registry.Current?.Nickname ?? "未选择";
    }

    /// <summary>把时间转成"多久以前"的说法。</summary>
    private static string FormatWhen(System.DateTime t)
    {
        var span = System.DateTime.Now - t;
        if (span.TotalMinutes < 2) return "刚刚";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} 分钟前";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} 小时前";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} 天前";
        return t.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 切换到指定使用者。切换需要重启应用才能生效——
    /// 数据库连接与各服务单例都已绑定到旧的数据目录，热切换会串数据。
    /// </summary>
    /// <param name="userId">目标使用者 Id</param>
    /// <returns>返回 true 表示已标记切换、需要重启</returns>
    public bool SwitchUser(string userId)
    {
        if (userId == _registry.CurrentUserId)
        {
            UserStatusMessage = "已经是在使用这位了";
            return false;
        }
        if (!_users.SwitchTo(_registry, userId))
        {
            UserStatusMessage = "切换失败：找不到该使用者";
            return false;
        }
        RefreshUsers();
        UserStatusMessage = $"已切换到「{_registry.Current?.Nickname}」，需重启应用后生效";
        return true;
    }

    /// <summary>新建使用者（以输入的昵称为准）。</summary>
    /// <returns>新建成功返回新用户 Id，失败返回空串</returns>
    public string CreateUser()
    {
        string raw = (NewUserName ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            UserStatusMessage = "请先输入新使用者的昵称";
            return string.Empty;
        }
        if (_registry.Users.Any(u => string.Equals(u.Nickname, raw, System.StringComparison.OrdinalIgnoreCase)))
        {
            UserStatusMessage = "已存在同名使用者，换一个名字更好区分";
            return string.Empty;
        }

        var user = _users.CreateUser(_registry, raw);
        _users.Save(_registry);
        try
        {
            System.IO.Directory.CreateDirectory(TypeMaster.Core.AppDataPaths.GetUserDirectory(user.Id));
        }
        catch
        {
            // 目录创建失败不阻断；后续写入会再尝试
        }

        NewUserName = string.Empty;
        RefreshUsers();
        UserStatusMessage = $"已新建使用者「{user.Nickname}」，可点击「切换到此使用者」开始使用";
        return user.Id;
    }

    /// <summary>重命名使用者（只改昵称，数据目录不动，成绩不受影响）。</summary>
    /// <param name="userId">使用者 Id</param>
    /// <param name="nickname">新昵称</param>
    /// <returns>成功返回 true</returns>
    public bool RenameUser(string userId, string nickname)
    {
        if (!_users.Rename(_registry, userId, nickname))
        {
            UserStatusMessage = "重命名失败：昵称不能为空";
            return false;
        }
        RefreshUsers();
        UserStatusMessage = "已重命名；该使用者的成绩与进度不受影响";
        return true;
    }

    /// <summary>
    /// 删除使用者及其全部数据。
    /// 当前使用者与最后一位使用者不允许删除（否则会陷入无人可用又无法自救的状态）。
    /// </summary>
    /// <param name="userId">使用者 Id</param>
    /// <returns>成功返回 true</returns>
    public bool DeleteUser(string userId)
    {
        var target = _registry.Find(userId);
        if (target is null)
        {
            UserStatusMessage = "删除失败：找不到该使用者";
            return false;
        }
        if (userId == _registry.CurrentUserId)
        {
            UserStatusMessage = "不能删除正在使用的使用者；请先切换到别的人";
            return false;
        }
        if (_registry.Users.Count <= 1)
        {
            UserStatusMessage = "至少要保留一位使用者";
            return false;
        }

        string name = target.Nickname;
        bool ok = _users.Delete(_registry, userId, out bool dataDeleted);
        if (!ok)
        {
            UserStatusMessage = "删除失败";
            return false;
        }

        RefreshUsers();
        UserStatusMessage = dataDeleted
            ? $"已删除使用者「{name}」及其全部数据"
            : $"已删除使用者「{name}」，但其数据目录被占用未能删除（可手动清理）";
        return true;
    }

    #endregion 使用者管理

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
