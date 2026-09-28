using System.Reflection;

namespace TypeMaster.Core;

/// <summary>
/// 应用程序版本信息：版本号由 MSBuild 在每次构建时按"主版本.次版本.月日.时分"自动生成，
/// 因此每次修改代码重新编译后小版本号都会变化。
/// </summary>
public static class AppVersion
{
    #region 公开属性

    /// <summary>完整版本号，形如 1.0.828.2155。</summary>
    public static string Current => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";

    /// <summary>界面展示用版本号，形如 v1.0.828.2155。</summary>
    public static string Display => "v" + Current;

    /// <summary>带说明的版本文案，用于"关于"区域展示。</summary>
    public static string DisplayWithLabel => "版本 " + Display;

    #endregion 公开属性
}
