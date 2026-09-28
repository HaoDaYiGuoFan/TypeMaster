using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TypeMaster.App;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.App.Views.UserControls;

/// <summary>
/// 虚拟键盘：根据物理按键实时高亮对应键帽。
/// 基准键（ASDF JKL;）以浅绿底色提示指法位置。
/// </summary>
public partial class VirtualKeyboard : UserControl
{
    private readonly IKeyboardHookService _hook;
    private readonly Dictionary<string, Border> _keyMap = new();
    private readonly DispatcherTimer _flashTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private Border? _flashing;

    private static readonly HashSet<string> HomeRow = new() { "A", "S", "D", "F", "J", "K", "L", ";" };
    private static readonly HashSet<string> WideKeys = new() { "Space", "Bksp", "Tab", "Caps", "Enter", "Shift" };

    private static readonly SolidColorBrush RestBrush = new(Color.FromRgb(0xF2, 0xF2, 0xF2));
    private static readonly SolidColorBrush HomeBrush = new(Color.FromRgb(0xE3, 0xF2, 0xE3));

    public VirtualKeyboard()
    {
        InitializeComponent();
        _hook = App.ServiceProvider.GetRequiredService<IKeyboardHookService>();
        _flashTimer.Tick += (_, _) => ClearFlash();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _hook.KeyPressed -= OnKey;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildKeys();
        _hook.KeyPressed += OnKey;
    }

    private void OnKey(object? sender, KeyPressedEventArgs args)
    {
        if (args.KeyLabel is null) return;
        if (_keyMap.TryGetValue(args.KeyLabel, out var border))
            Flash(border);
    }

    private void Flash(Border border)
    {
        border.Background = (Brush)Application.Current.FindResource("PrimaryHueMidBrush")!;
        _flashing = border;
        _flashTimer.Stop();
        _flashTimer.Start();
    }

    private void ClearFlash()
    {
        if (_flashing is not null)
            _flashing.Background = (Brush)_flashing.Tag;
        _flashing = null;
        _flashTimer.Stop();
    }

    private void BuildKeys()
    {
        string[] row0 = { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Bksp" };
        string[] row1 = { "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" };
        string[] row2 = { "Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter" };
        string[] row3 = { "Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift" };
        string[] row4 = { "Space" };

        AddRow(row0);
        AddRow(row1);
        AddRow(row2);
        AddRow(row3);
        AddRow(row4);
    }

    private void AddRow(string[] labels)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var label in labels)
        {
            bool isHome = HomeRow.Contains(label);
            var rest = isHome ? HomeBrush : RestBrush;
            var border = new Border
            {
                Style = (Style)Resources["KeyCap"]!,
                Background = rest,
                Tag = rest,
                Child = new TextBlock
                {
                    Text = label,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = label.Length > 1 ? 11 : 15,
                    FontWeight = FontWeights.SemiBold
                }
            };
            if (label == "Space") border.Width = 240;
            else if (WideKeys.Contains(label)) border.Width = 64;

            _keyMap[label] = border;
            row.Children.Add(border);
        }
        RowsPanel.Children.Add(row);
    }
}
