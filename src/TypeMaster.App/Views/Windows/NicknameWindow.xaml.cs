using System.Windows;
using TypeMaster.Core.Entities;

namespace TypeMaster.App.Views.Windows;

/// <summary>
/// 首次启动的昵称设置对话框：玩家可输入昵称，留空则默认“用户1”。
/// 通过 <see cref="Profile"/> 回传结果。
/// </summary>
public partial class NicknameWindow : Window
{
    /// <summary>玩家确认后的档案（昵称已规范化，Initialized=true）。</summary>
    public UserProfile Profile { get; private set; } = new();

    /// <summary>可选的预填昵称（首页修改时带入当前昵称）。</summary>
    public string? InitialNickname { get; set; }

    /// <summary>true=从首页进入的修改模式（取消不保存）；false=首次启动（取消沿用“用户1”）。</summary>
    public bool IsEditMode { get; set; }

    public NicknameWindow()
    {
        InitializeComponent();
        var displayNick = (!string.IsNullOrEmpty(InitialNickname)) ? InitialNickname : "用户1";
        Title = "打字练习机 · 设置昵称";
        HintText.Text = $"不填的话，我就一直叫你 {displayNick} 啦～";
        if (!string.IsNullOrEmpty(InitialNickname))
            NameBox.Text = InitialNickname;
    }

    private void NameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // 实时把输入框内容写入结果（空则保持默认“用户1”），方便后续直接采用
        var raw = (NameBox.Text ?? string.Empty).Trim();
        Profile.Nickname = raw.Length == 0 ? "用户1" : raw;
    }

    private void Ok(object sender, RoutedEventArgs e)
    {
        var raw = (NameBox.Text ?? string.Empty).Trim();
        Profile.Nickname = raw.Length == 0 ? "用户1" : raw;
        Profile.Initialized = true;
        DialogResult = true;
    }

    private void Cancel(object sender, RoutedEventArgs e)
    {
        if (IsEditMode)
        {
            // 修改模式下取消：不改昵称，关闭且不保存
            DialogResult = false;
            return;
        }
        // 首次启动选择“以后再说”：沿用默认“用户1”，但记为已初始化以免重复打扰
        Profile.Nickname = "用户1";
        Profile.Initialized = true;
        DialogResult = true;
    }
}
