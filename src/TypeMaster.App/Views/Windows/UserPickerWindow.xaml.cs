using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TypeMaster.Core;
using TypeMaster.Core.Entities;
using TypeMaster.Services;

namespace TypeMaster.App.Views.Windows;

/// <summary>
/// 使用者选择窗口。
///
/// 出现时机：数据目录中已有多个用户时，启动后立即弹出，
/// 让每个人选自己的名字——否则容易出现"我打的分怎么成了别人的"。
///
/// 只有一个用户时不弹（直接进入），避免每次启动都被打扰。
/// </summary>
public partial class UserPickerWindow : Window
{
    private readonly UserRegistry _registry;
    private readonly UserRegistryStore _store = new();
    private readonly bool _allowCreate;

    /// <summary>最终选定的用户 Id。</summary>
    public string SelectedUserId { get; private set; } = string.Empty;

    /// <summary>
    /// 构造使用者选择窗口。
    /// </summary>
    /// <param name="registry">用户清单</param>
    /// <param name="allowCreate">是否允许在此新建使用者</param>
    public UserPickerWindow(UserRegistry registry, bool allowCreate = true)
    {
        InitializeComponent();
        _registry = registry;
        _allowCreate = allowCreate;

        NewNameBox.Visibility = allowCreate ? Visibility.Visible : Visibility.Collapsed;
        Title = "打字练习机 · 选择使用者";

        RefreshList();
    }

    /// <summary>列表项（仅用于展示，不直接绑定实体，便于算"上次使用"文案）。</summary>
    private sealed class Item
    {
        public string Id { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public bool IsCurrent { get; init; }
    }

    /// <summary>重建列表，按最近使用时间倒序（最近用的人排最前，方便一眼选中）。</summary>
    private void RefreshList()
    {
        var items = _registry.Users
            .OrderByDescending(u => u.LastUsedAt)
            .Select(u => new Item
            {
                Id = u.Id,
                Nickname = u.Nickname,
                Detail = $"上次使用：{FormatLastUsed(u.LastUsedAt)}",
                IsCurrent = u.Id == _registry.CurrentUserId
            })
            .ToList();

        UserList.ItemsSource = items;

        // 默认选中当前用户（或第一个），让用户直接回车即可
        var prefer = items.FirstOrDefault(i => i.IsCurrent) ?? items.FirstOrDefault();
        if (prefer is not null)
        {
            UserList.SelectedItem = prefer;
        }
        HintText.Text = $"共 {items.Count} 位使用者";
    }

    /// <summary>把"上次使用时间"转成人能一眼看懂的说法。</summary>
    /// <param name="t">上次使用时间</param>
    /// <returns>相对时间描述</returns>
    private static string FormatLastUsed(DateTime t)
    {
        var span = DateTime.Now - t;
        if (span.TotalMinutes < 2) return "刚刚";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} 分钟前";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} 小时前";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} 天前";
        return t.ToString("yyyy-MM-dd");
    }

    private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserList.SelectedItem is Item item)
        {
            SelectedUserId = item.Id;
        }
    }

    /// <summary>双击直接进入，省一次点击。</summary>
    private void UserList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => Ok_Click(sender, e);

    /// <summary>新建使用者：创建目录并选中它。</summary>
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowCreate)
        {
            return;
        }

        string raw = (NewNameBox.Text ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            HintText.Text = "请先输入新使用者的名字";
            return;
        }

        // 昵称去过首尾空格即可；重名不算错误（靠 Id 区分），但提示一下避免混淆
        if (_registry.Users.Any(u => string.Equals(u.Nickname, raw, StringComparison.OrdinalIgnoreCase)))
        {
            HintText.Text = "已存在同名使用者，换一个名字更好区分";
            return;
        }

        var user = _store.CreateUser(_registry, raw);
        _store.Save(_registry);

        // 立刻建好数据目录，避免后续访问时才创建
        try
        {
            System.IO.Directory.CreateDirectory(AppDataPaths.GetUserDirectory(user.Id));
        }
        catch
        {
            // 创建失败不阻断流程，后续写入时会再尝试
        }

        NewNameBox.Text = string.Empty;
        RefreshList();
        UserList.SelectedItem = ((IEnumerable<Item>)UserList.ItemsSource!)
            .FirstOrDefault(i => i.Id == user.Id);
        HintText.Text = $"已新建使用者「{user.Nickname}」";
    }

    /// <summary>确认选择：写入清单，由启动流程据此设定数据目录。</summary>
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (UserList.SelectedItem is not Item item)
        {
            HintText.Text = "请先选择一位使用者";
            return;
        }

        _store.SwitchTo(_registry, item.Id);
        SelectedUserId = item.Id;
        DialogResult = true;
    }
}
