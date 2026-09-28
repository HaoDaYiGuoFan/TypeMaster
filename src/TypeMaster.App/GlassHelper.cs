using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TypeMaster.Core.Enums;

namespace TypeMaster.App;

/// <summary>
/// 窗体毛玻璃（亚克力）质感辅助类。
/// 通过 Win32 <c>SetWindowCompositionAttribute</c> 为窗体启用系统级背景模糊，
/// 在不支持的系统上静默降级为纯半透明渐变（由 GlassStyles.xaml 提供）。
/// </summary>
public static class GlassHelper
{
    #region 局部变量属性

    /// <summary>关闭背景合成效果。</summary>
    private const int AccentDisabled = 0;

    /// <summary>传统 Aero 背景模糊（Win7 / Win10 通用）：静态模糊一次成型，
    /// 窗口移动 / 缩放时无需重新采样背景，系统开销最低。</summary>
    private const int AccentEnableBlurBehind = 3;

    /// <summary>亚克力背景模糊（Win10 1703+）：观感最接近 macOS，
    /// 但需要实时重新采样背景，拖动窗口时 GPU / CPU 开销明显更高，仅作降级备选。</summary>
    private const int AccentEnableAcrylicBlurBehind = 4;

    /// <summary>设置窗口合成属性的消息标识。</summary>
    private const int WindowCompositionAttributeAccentPolicy = 19;

    /// <summary>默认的玻璃色调（含透明度），格式为 0xAABBGGRR。</summary>
    private const uint DefaultTint = 0x99F2F7FF;

    #endregion 局部变量属性

    #region Win32 声明

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowCompositionAttribute", SetLastError = true)]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    #endregion Win32 声明

    #region 对外方法

    /// <summary>
    /// 为指定窗体启用毛玻璃背景。窗体尚未创建句柄时，会自动挂到 <c>SourceInitialized</c> 上延迟执行。
    /// </summary>
    /// <param name="window">目标窗体</param>
    /// <param name="theme">当前主题，用于选择玻璃色调</param>
    public static void Enable(Window window, ThemeType theme)
    {
        if (window == null)
        {
            return;
        }
        if (window.IsLoaded)
        {
            Apply(window, ResolveTint(theme));
            return;
        }
        // 句柄尚未创建：等 SourceInitialized 后再调用，避免拿到空句柄
        window.SourceInitialized += (_, _) => Apply(window, ResolveTint(theme));
    }

    /// <summary>
    /// 关闭指定窗体的毛玻璃背景（恢复为普通半透明背景）。
    /// </summary>
    /// <param name="window">目标窗体</param>
    public static void Disable(Window window)
    {
        if (window == null || !window.IsLoaded)
        {
            return;
        }
        try
        {
            SetAccentPolicy(new WindowInteropHelper(window).Handle, AccentDisabled, DefaultTint, 0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GlassHelper] 关闭毛玻璃背景失败：{ex.Message}");
        }
    }

    #endregion 对外方法

    #region 私有方法

    /// <summary>
    /// 应用毛玻璃效果：为节省系统性能，优先使用一次成型的 Aero 背景模糊；
    /// 失败再降级为亚克力模糊，最后保持纯半透明玻璃背景。
    /// </summary>
    /// <param name="window">目标窗体</param>
    /// <param name="tint">玻璃色调（0xAABBGGRR）</param>
    private static void Apply(Window window, uint tint)
    {
        try
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                Debug.WriteLine("[GlassHelper] 窗体句柄为空，跳过毛玻璃设置。");
                return;
            }

            // Aero 模糊为静态背景模糊，窗口拖动 / 缩放时不重复采样，系统开销远低于亚克力，
            // 在低端机与核显设备上能明显降低占用，因此作为默认首选
            bool aero = SetAccentPolicy(hwnd, AccentEnableBlurBehind, tint, 0);
            if (!aero)
            {
                bool acrylic = SetAccentPolicy(hwnd, AccentEnableAcrylicBlurBehind, tint, 2);
                Debug.WriteLine(acrylic
                    ? "[GlassHelper] Aero 模糊不可用，已降级为亚克力背景模糊。"
                    : "[GlassHelper] 系统不支持背景模糊，已降级为纯半透明玻璃背景。");
            }
            else
            {
                Debug.WriteLine("[GlassHelper] 已启用 Aero 背景模糊（低开销模式）。");
            }
        }
        catch (Exception ex)
        {
            // 毛玻璃只是观感增强，失败时不能影响主流程
            Debug.WriteLine($"[GlassHelper] 启用毛玻璃失败，已降级为纯半透明背景：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用 Win32 接口设置窗体的背景合成策略。
    /// </summary>
    /// <param name="hwnd">窗体句柄</param>
    /// <param name="accentState">合成状态（亚克力 / Aero / 关闭）</param>
    /// <param name="tint">玻璃色调（0xAABBGGRR）</param>
    /// <param name="accentFlags">附加标志，亚克力需要传 2</param>
    /// <returns>设置成功返回 true，否则返回 false</returns>
    private static bool SetAccentPolicy(IntPtr hwnd, int accentState, uint tint, int accentFlags)
    {
        var policy = new AccentPolicy
        {
            AccentState = accentState,
            AccentFlags = accentFlags,
            GradientColor = tint,
            AnimationId = 0
        };

        int size = Marshal.SizeOf(policy);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(policy, ptr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttributeAccentPolicy,
                Data = ptr,
                SizeOfData = size
            };
            return SetWindowCompositionAttribute(hwnd, ref data) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// 按主题返回玻璃色调（0xAABBGGRR 格式）。
    /// </summary>
    /// <param name="theme">当前主题</param>
    /// <returns>与主题匹配的玻璃色调</returns>
    private static uint ResolveTint(ThemeType theme)
    {
        return theme switch
        {
            ThemeType.Dark => 0x99252525,
            ThemeType.EyeCare => 0x99E7F3E4,
            _ => DefaultTint
        };
    }

    #endregion 私有方法
}
