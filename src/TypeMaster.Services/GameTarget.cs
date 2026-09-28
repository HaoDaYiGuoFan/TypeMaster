using System.ComponentModel;
using TypeMaster.Core.Enums;

namespace TypeMaster.Services;

/// <summary>
/// 游戏中的一个活动目标（敌机 / 地鼠 / 小偷 / 虫子），携带一个待输入的单词。
/// 位置 (X,Y) 与匹配进度 (MatchedLength) 通过 INotifyPropertyChanged 实时驱动 Canvas 渲染。
/// </summary>
public class GameTarget : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private int _matchedLength;
    private bool _isLocked;
    private string _word = string.Empty;

    public int Id { get; init; }
    public GameGlyph Glyph { get; init; }

    /// <summary>水平速度（像素/秒），向左为负、向右为正</summary>
    public double Vx { get; set; }
    /// <summary>垂直速度（像素/秒），向下为正</summary>
    public double Vy { get; set; }

    /// <summary>已存在时长（毫秒），仅打地鼠用于判断冒头超时</summary>
    public double AgeMs { get; set; }

    /// <summary>存活时长上限（毫秒），仅打地鼠使用；其它模式为无穷大</summary>
    public double LifetimeMs { get; set; } = double.MaxValue;

    public string Word
    {
        get => _word;
        init
        {
            _word = value;
            OnChanged(nameof(Word));
            OnChanged(nameof(MatchedSubstring));
            OnChanged(nameof(RemainingSubstring));
        }
    }

    public double X
    {
        get => _x;
        set { if (_x != value) { _x = value; OnChanged(nameof(X)); } }
    }

    public double Y
    {
        get => _y;
        set { if (_y != value) { _y = value; OnChanged(nameof(Y)); } }
    }

    /// <summary>已正确输入的前缀长度</summary>
    public int MatchedLength
    {
        get => _matchedLength;
        set
        {
            if (_matchedLength != value)
            {
                _matchedLength = value;
                OnChanged(nameof(MatchedLength));
                OnChanged(nameof(MatchedSubstring));
                OnChanged(nameof(RemainingSubstring));
            }
        }
    }

    /// <summary>是否被当前输入锁定（首字母已匹配），用于界面高亮</summary>
    public bool IsLocked
    {
        get => _isLocked;
        set { if (_isLocked != value) { _isLocked = value; OnChanged(nameof(IsLocked)); } }
    }

    /// <summary>已匹配的前缀（高亮显示）</summary>
    public string MatchedSubstring =>
        _word.Length == 0 ? string.Empty : _word[..Math.Min(_matchedLength, _word.Length)];

    /// <summary>尚未匹配的后缀</summary>
    public string RemainingSubstring =>
        _word.Length == 0 ? string.Empty : _word[Math.Min(_matchedLength, _word.Length)..];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
