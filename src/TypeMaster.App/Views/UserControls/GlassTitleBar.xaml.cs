using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace TypeMaster.App.Views.UserControls;

/// <summary>
/// 玻璃风格标题栏：提供 macOS 交通灯关闭按钮、标题文本与空白区拖拽。
/// 供启用了 <see cref="GlassWindowBehavior"/> 的无边框窗体复用。
/// </summary>
public partial class GlassTitleBar : UserControl
{
    #region 依赖属性

    /// <summary>标题文本的依赖属性。</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(GlassTitleBar), new PropertyMetadata(string.Empty));

    /// <summary>右侧装饰图标的依赖属性。</summary>
    public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
        nameof(IconKind), typeof(PackIconKind), typeof(GlassTitleBar), new PropertyMetadata(PackIconKind.KeyboardVariant));

    #endregion 依赖属性

    #region 构造函数

    public GlassTitleBar()
    {
        InitializeComponent();
    }

    #endregion 构造函数

    #region 公开属性

    /// <summary>标题栏显示的标题文本。</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>标题栏右侧的装饰图标。</summary>
    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    #endregion 公开属性

    #region 事件处理

    /// <summary>
    /// 标题栏空白处按下左键时拖动所属窗体。
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 命中按钮时交给按钮自己处理，避免拖拽吞掉点击
        if (e.ClickCount > 1 || IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }
        try
        {
            Window.GetWindow(this)?.DragMove();
        }
        catch (System.InvalidOperationException)
        {
            // 鼠标已释放或窗口正在动画中，忽略本次拖拽
        }
    }

    /// <summary>
    /// 点击红色交通灯按钮关闭所属窗体。
    /// </summary>
    private void CloseWindow(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();

    /// <summary>
    /// 判断命中元素是否位于按钮内部。
    /// </summary>
    /// <param name="source">鼠标事件的原始命中元素</param>
    /// <returns>位于按钮内返回 true，否则返回 false</returns>
    private static bool IsInsideButton(DependencyObject? source)
    {
        DependencyObject? current = source;
        while (current != null)
        {
            if (current is Button)
            {
                return true;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    #endregion 事件处理
}
