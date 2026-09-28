using System;
using TypeMaster.Core.Entities;

namespace TypeMaster.Core;

/// <summary>
/// 运行时全局配置（单例），避免 ViewModel 反向引用 App 层。
/// 配置保存后触发 SettingsChanged，由 App / 页面据此应用主题与字体等。
/// </summary>
public static class AppState
{
    public static AppConfig Current { get; private set; } = new();

    public static event EventHandler? SettingsChanged;

    public static void Update(AppConfig config)
    {
        Current = config;
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }
}
