using System;
using System.IO;

namespace TypeMaster.Core;

/// <summary>
/// 集中管理应用数据文件存放位置。
/// 所有用户数据（数据库 / 配置 / 自定义文章）统一存放在当前用户的
/// AppData\Local\TypeMaster 目录下，而不是 exe 所在目录——
/// 这样在“免安装单文件”分发场景下，用户即使移动或删除 exe 也不会丢失数据，
/// 且不同 Windows 用户互不干扰。
/// </summary>
public static class AppDataPaths
{
    /// <summary>数据根目录：C:\Users\{用户名}\AppData\Local\TypeMaster</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TypeMaster");

    public static string DatabaseFile { get; } = Path.Combine(DataDirectory, "typemaster.db");
    public static string UserProfileFile { get; } = Path.Combine(DataDirectory, "user_profile.json");
    public static string CustomArticlesFile { get; } = Path.Combine(DataDirectory, "custom_articles.json");

    /// <summary>课程闯关进度文件：course_progress.json</summary>
    public static string CourseProgressFile { get; } = Path.Combine(DataDirectory, "course_progress.json");

    static AppDataPaths()
    {
        // 首次访问即确保目录存在，避免 SQLite / 文件写入时因目录缺失而抛异常
        Directory.CreateDirectory(DataDirectory);
    }
}
