using System;
using System.IO;

namespace TypeMaster.Core;

/// <summary>
/// 集中管理应用数据文件存放位置（支持多用户）。
///
/// 目录结构：
/// <code>
/// %LOCALAPPDATA%\TypeMaster\
/// ├─ users.json              用户清单（不属于任何用户）
/// └─ users\
///    ├─ u1\                  用户 1 的数据
///    │  ├─ typemaster.db
///    │  ├─ user_profile.json
///    │  ├─ custom_articles.json
///    │  └─ course_progress.json
///    └─ u2\                  用户 2 的数据（完全隔离）
/// </code>
///
/// **重要**：本类不再是"初始化即定型"的静态路径。当前用户必须在启动早期
/// 通过 <see cref="SetCurrentUser"/> 指定，之后所有路径才有效。
/// 这样做的原因：DI 容器在构建时就把数据库路径固化进 <c>DbContext</c> 单例，
/// 因此必须先确定用户、再构建容器。
///
/// 兼容性：升级自旧版（单用户、文件直接放在数据根目录）时，
/// 由迁移逻辑把文件移入第一个用户的子目录，见 <see cref="MigrateLegacyIfNeeded"/>。
/// </summary>
public static class AppDataPaths
{
    #region 常量与字段

    /// <summary>旧版单用户布局下的文件清单（迁移时按此判断）。</summary>
    private static readonly string[] LegacyFiles =
    {
        "typemaster.db", "user_profile.json", "custom_articles.json", "course_progress.json"
    };

    /// <summary>当前用户 Id；为空表示尚未指定（此时访问用户相关路径会抛异常，以便尽早暴露顺序错误）。</summary>
    private static string _currentUserId = string.Empty;

    #endregion 常量与字段

    #region 根目录

    /// <summary>数据根目录：C:\Users\{用户名}\AppData\Local\TypeMaster</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TypeMaster");

    /// <summary>用户清单文件（位于根目录，所有用户共用）。</summary>
    public static string UserRegistryFile { get; } = Path.Combine(DataDirectory, "users.json");

    /// <summary>存放各用户数据子目录的父目录。</summary>
    public static string UsersDirectory { get; } = Path.Combine(DataDirectory, "users");

    #endregion 根目录

    #region 当前用户

    /// <summary>当前用户 Id；尚未指定时为空串。</summary>
    public static string CurrentUserId => _currentUserId;

    /// <summary>是否已指定当前用户。</summary>
    public static bool HasCurrentUser => !string.IsNullOrEmpty(_currentUserId);

    /// <summary>
    /// 指定当前用户，并确保其数据目录存在。
    /// 必须在构建 DI 容器（注册 DbContext）之前调用。
    /// </summary>
    /// <param name="userId">用户 Id（由 <c>UserRegistry.NextId()</c> 生成或取自清单）</param>
    public static void SetCurrentUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("用户 Id 不能为空", nameof(userId));
        }

        // 目录名只允许字母数字与下划线，避免路径穿越或非法字符
        foreach (char c in userId)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                throw new ArgumentException($"用户 Id 含非法字符：{userId}", nameof(userId));
            }
        }

        _currentUserId = userId;
        Directory.CreateDirectory(CurrentUserDirectory);
    }

    /// <summary>取指定用户的数据目录（不改变当前用户）。</summary>
    /// <param name="userId">用户 Id</param>
    /// <returns>该用户的目录路径</returns>
    public static string GetUserDirectory(string userId)
        => Path.Combine(UsersDirectory, userId);

    /// <summary>当前用户的数据目录。未指定用户时抛异常（尽早暴露启动顺序错误）。</summary>
    private static string CurrentUserDirectory
    {
        get
        {
            if (!HasCurrentUser)
            {
                throw new InvalidOperationException(
                    "尚未指定当前用户；须在构建 DI 容器前调用 AppDataPaths.SetCurrentUser()。");
            }
            return GetUserDirectory(_currentUserId);
        }
    }

    #endregion 当前用户

    #region 当前用户的文件路径

    /// <summary>当前用户的 SQLite 数据库。</summary>
    public static string DatabaseFile => Path.Combine(CurrentUserDirectory, "typemaster.db");

    /// <summary>当前用户的档案（昵称等）。</summary>
    public static string UserProfileFile => Path.Combine(CurrentUserDirectory, "user_profile.json");

    /// <summary>当前用户的自定义文章。</summary>
    public static string CustomArticlesFile => Path.Combine(CurrentUserDirectory, "custom_articles.json");

    /// <summary>当前用户的课程闯关进度。</summary>
    public static string CourseProgressFile => Path.Combine(CurrentUserDirectory, "course_progress.json");

    /// <summary>当前用户的导出目录（成绩 CSV 等）。</summary>
    public static string ExportDirectory => Path.Combine(CurrentUserDirectory, "exports");

    #endregion 当前用户的文件路径

    #region 初始化与迁移

    static AppDataPaths()
    {
        // 只保证根目录存在；用户子目录在 SetCurrentUser 时创建
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(UsersDirectory);
    }

    /// <summary>
    /// 判断是否存在"旧版单用户布局"的数据（文件直接放在数据根目录）。
    /// </summary>
    /// <returns>存在旧数据返回 true</returns>
    public static bool HasLegacyData()
    {
        foreach (string name in LegacyFiles)
        {
            if (File.Exists(Path.Combine(DataDirectory, name)))
            {
                return true;
            }
        }
        return Directory.Exists(Path.Combine(DataDirectory, "exports"));
    }

    /// <summary>
    /// 把旧版单用户数据迁移到指定用户的目录下。
    ///
    /// 行为：**移动**（而非复制）文件，避免留下两份导致用户困惑；
    /// 同名文件已存在时保留目标文件、跳过该文件（不覆盖已有数据）。
    /// 迁移失败不抛出，而是返回失败项，由调用方决定如何提示——
    /// 宁可让用户在旧目录继续用，也不能因为迁移异常导致应用起不来。
    /// </summary>
    /// <param name="userId">迁移到哪个用户</param>
    /// <returns>未能迁移的文件名列表（空表示全部成功）</returns>
    public static System.Collections.Generic.List<string> MigrateLegacyIfNeeded(string userId)
    {
        var failed = new System.Collections.Generic.List<string>();
        if (!HasLegacyData())
        {
            return failed;
        }

        string target = GetUserDirectory(userId);
        Directory.CreateDirectory(target);

        foreach (string name in LegacyFiles)
        {
            string src = Path.Combine(DataDirectory, name);
            string dst = Path.Combine(target, name);
            if (!File.Exists(src))
            {
                continue;
            }
            try
            {
                if (File.Exists(dst))
                {
                    // 目标已有数据：保留目标，把旧文件改名留档，避免直接丢弃
                    string bak = dst + ".legacy";
                    if (!File.Exists(bak))
                    {
                        File.Move(src, bak);
                    }
                    else
                    {
                        File.Delete(src);
                    }
                }
                else
                {
                    File.Move(src, dst);
                }
            }
            catch
            {
                failed.Add(name);
            }
        }

        // 导出目录整体迁移
        string srcExports = Path.Combine(DataDirectory, "exports");
        if (Directory.Exists(srcExports))
        {
            try
            {
                string dstExports = Path.Combine(target, "exports");
                Directory.CreateDirectory(dstExports);
                foreach (string f in Directory.GetFiles(srcExports))
                {
                    string fn = Path.GetFileName(f);
                    string d = Path.Combine(dstExports, fn);
                    if (!File.Exists(d))
                    {
                        File.Move(f, d);
                    }
                }
            }
            catch
            {
                failed.Add("exports");
            }
        }

        return failed;
    }

    /// <summary>
    /// 删除某个用户的整个数据目录（用于"删除用户"功能）。
    /// 目录不存在时视为已删除成功。
    /// </summary>
    /// <param name="userId">用户 Id</param>
    /// <returns>成功返回 true；被占用或权限不足返回 false</returns>
    public static bool DeleteUserData(string userId)
    {
        try
        {
            string dir = GetUserDirectory(userId);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion 初始化与迁移
}
