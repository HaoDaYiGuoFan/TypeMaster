namespace TypeMaster.Services;

/// <summary>
/// Windows 虚拟键码 -> 按键标签（与虚拟键盘显示标签保持一致）
/// </summary>
public static class KeyLabelHelper
{
    public static string? VkToLabel(int vk)
    {
        if (vk >= 65 && vk <= 90) return ((char)vk).ToString();   // A-Z
        if (vk >= 48 && vk <= 57) return ((char)vk).ToString();  // 0-9（主键盘数字行）

        return vk switch
        {
            8 => "Bksp",
            9 => "Tab",
            13 => "Enter",
            16 => "Shift",
            17 => "Ctrl",
            18 => "Alt",
            20 => "Caps",
            32 => "Space",
            186 => ";",
            187 => "=",
            188 => ",",
            189 => "-",
            190 => ".",
            191 => "/",
            192 => "`",
            219 => "[",
            220 => "\\",
            221 => "]",
            222 => "'",
            _ => null
        };
    }
}
