namespace TypeMaster.Core.Entities;

/// <summary>
/// 单个按键的累计对错统计：用于薄弱键位分析与键位热力图。
/// </summary>
public class KeyStatEntry
{
    /// <summary>按键标签（与虚拟键盘一致，如 A / ; / Space）</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>该键被正确击中的次数</summary>
    public int Right { get; set; }

    /// <summary>该键被击错的次数（该按此键时敲错）</summary>
    public int Wrong { get; set; }

    /// <summary>总击键次数</summary>
    public int Total => Right + Wrong;

    /// <summary>该键的错误率（百分比，0~100；总次数为 0 时返回 0）</summary>
    public double ErrorRate => Total == 0 ? 0d : Wrong * 100d / Total;
}