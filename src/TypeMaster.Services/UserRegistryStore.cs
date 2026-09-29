using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TypeMaster.Core;
using TypeMaster.Core.Entities;

namespace TypeMaster.Services;

/// <summary>
/// 用户清单的读写（<c>users.json</c>）。
///
/// 放在 Services 层（而非 Data/EF），因为：
/// 1) 它必须在 DI 容器构建【之前】就能读写——容器还没建，不能用仓储；
/// 2) 数据量极小（几个用户），JSON 足够，无需数据库；
/// 3) 与既有的 JsonUserProfile / JsonCourseProgressStore 同款思路，风格一致。
/// </summary>
public class UserRegistryStore
{
    private static readonly object SyncRoot = new();

    /// <summary>读取用户清单；文件不存在或损坏时返回一份可用的空清单。</summary>
    /// <returns>用户清单</returns>
    public UserRegistry Load()
    {
        lock (SyncRoot)
        {
            try
            {
                string path = AppDataPaths.UserRegistryFile;
                if (!File.Exists(path))
                {
                    return new UserRegistry();
                }
                string json = File.ReadAllText(path);
                var reg = JsonSerializer.Deserialize<UserRegistry>(json);
                if (reg is null)
                {
                    return new UserRegistry();
                }
                reg.Users ??= new List<UserAccount>();
                // 清掉指向已不存在用户的 CurrentUserId，避免启动时拿到 null
                if (!string.IsNullOrEmpty(reg.CurrentUserId) && reg.Find(reg.CurrentUserId) is null)
                {
                    reg.CurrentUserId = string.Empty;
                }
                return reg;
            }
            catch
            {
                // 文件损坏时退化为空清单，不让应用起不来
                return new UserRegistry();
            }
        }
    }

    /// <summary>保存用户清单（写入失败静默忽略，不影响内存中的状态）。</summary>
    /// <param name="registry">用户清单</param>
    public void Save(UserRegistry registry)
    {
        lock (SyncRoot)
        {
            try
            {
                string json = JsonSerializer.Serialize(registry, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppDataPaths.UserRegistryFile, json);
            }
            catch
            {
                // 目录只读等情况：忽略，内存中的状态仍然有效
            }
        }
    }

    /// <summary>
    /// 新建一个用户并返回它（**不写入**清单，由调用方决定何时保存）。
    /// 昵称为空时自动取"用户 N"。
    /// </summary>
    /// <param name="registry">用户清单</param>
    /// <param name="nickname">昵称（可为空）</param>
    /// <returns>新建的用户</returns>
    public UserAccount CreateUser(UserRegistry registry, string nickname)
    {
        var user = new UserAccount
        {
            Id = registry.NextId(),
            Nickname = string.IsNullOrWhiteSpace(nickname)
                ? $"用户{registry.Users.Count + 1}"
                : nickname.Trim(),
            CreatedAt = DateTime.Now,
            LastUsedAt = DateTime.Now
        };
        registry.Users.Add(user);
        return user;
    }

    /// <summary>
    /// 确保至少有一个用户，并且存在当前用户；同时完成旧数据迁移。
    ///
    /// 首次启动（或无任何用户）时会创建一个默认用户，
    /// 若检测到旧版单用户数据则一并迁移到该用户目录下。
    /// </summary>
    /// <param name="registry">用户清单</param>
    /// <returns>迁移失败的文件名列表（空表示无问题）</returns>
    public List<string> EnsureUsable(UserRegistry registry)
    {
        var failed = new List<string>();

        if (registry.Users.Count == 0)
        {
            var first = CreateUser(registry, "用户1");

            // 旧版数据收进第一个用户目录，避免升级后"成绩消失"
            if (!registry.MigratedFromLegacy)
            {
                failed = AppDataPaths.MigrateLegacyIfNeeded(first.Id);
                registry.MigratedFromLegacy = true;
            }

            registry.CurrentUserId = first.Id;
            Save(registry);
        }
        else if (registry.Current is null)
        {
            // 有用户但没指定当前用户（例如上次选的人被删了）
            registry.CurrentUserId = registry.Users
                .OrderByDescending(u => u.LastUsedAt)
                .First().Id;
            Save(registry);
        }

        return failed;
    }

    /// <summary>
    /// 切换到指定用户：更新当前用户标记与最后使用时间并落盘。
    /// **注意**：切换后必须重启应用才能生效（数据库连接已绑定到旧目录）。
    /// </summary>
    /// <param name="registry">用户清单</param>
    /// <param name="userId">目标用户 Id</param>
    /// <returns>成功返回 true；用户不存在返回 false</returns>
    public bool SwitchTo(UserRegistry registry, string userId)
    {
        var user = registry.Find(userId);
        if (user is null)
        {
            return false;
        }
        registry.CurrentUserId = user.Id;
        user.LastUsedAt = DateTime.Now;
        Save(registry);
        return true;
    }

    /// <summary>
    /// 重命名用户（只改昵称，不动 Id，因此既有成绩不会"搬家"）。
    /// </summary>
    /// <param name="registry">用户清单</param>
    /// <param name="userId">用户 Id</param>
    /// <param name="nickname">新昵称</param>
    /// <returns>成功返回 true</returns>
    public bool Rename(UserRegistry registry, string userId, string nickname)
    {
        var user = registry.Find(userId);
        if (user is null || string.IsNullOrWhiteSpace(nickname))
        {
            return false;
        }
        user.Nickname = nickname.Trim();
        Save(registry);
        return true;
    }

    /// <summary>
    /// 删除用户及其全部数据。当前用户与最后一个用户不允许删除
    /// （否则会出现"没有用户"的状态，用户无法自行恢复）。
    /// </summary>
    /// <param name="registry">用户清单</param>
    /// <param name="userId">要删除的用户 Id</param>
    /// <param name="dataDeleted">数据文件是否已成功删除</param>
    /// <returns>可删除并已删除返回 true</returns>
    public bool Delete(UserRegistry registry, string userId, out bool dataDeleted)
    {
        dataDeleted = true;
        var user = registry.Find(userId);
        if (user is null)
        {
            return false;
        }
        // 保护：不能删当前用户（正在用），也不能删到只剩零个
        if (user.Id == registry.CurrentUserId || registry.Users.Count <= 1)
        {
            return false;
        }

        registry.Users.Remove(user);
        dataDeleted = AppDataPaths.DeleteUserData(user.Id);
        Save(registry);
        return true;
    }
}
