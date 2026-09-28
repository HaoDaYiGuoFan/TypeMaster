namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 按键事件参数
/// </summary>
public class KeyPressedEventArgs : EventArgs
{
    /// <summary>Windows 虚拟键码（VK）</summary>
    public int VirtualKeyCode { get; }

    /// <summary>按键标签（用于虚拟键盘高亮匹配，如 "A" / "Space"）</summary>
    public string? KeyLabel { get; }

    /// <summary>是否按下</summary>
    public bool IsDown { get; }

    public KeyPressedEventArgs(int virtualKeyCode, string? keyLabel, bool isDown)
    {
        VirtualKeyCode = virtualKeyCode;
        KeyLabel = keyLabel;
        IsDown = isDown;
    }
}

/// <summary>
/// 全局键盘钩子服务（基于 User32 WH_KEYBOARD_LL）
/// </summary>
public interface IKeyboardHookService
{
    event EventHandler<KeyPressedEventArgs>? KeyPressed;
    void Start();
    void Stop();
    bool IsRunning { get; }
}
