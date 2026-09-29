using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMaster.Core.Entities;

/// <summary>
/// 一个本地用户（使用同一台电脑的一个人）。
///
/// 用途：让多个家庭成员共用一台电脑时，各自的成绩、闯关进度、自定义文章、
/// 昵称与设置完全独立，互不干扰。
/// 典型场景：一位学拼音、一位学五笔。
/// </summary>
public class UserAccount
{
    /// <summary>
    /// 用户标识：仅含小写字母与数字，用作数据目录名。
    /// 一旦创建不再变更（改名只改 <see cref="Nickname"/>，不动此 Id），
    /// 这样重命名用户不会导致其既有成绩"搬家"。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>界面显示用的昵称（可随时修改，允许中文）。</summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后一次使用时间，用于在选人界面按最近使用排序。</summary>
    public DateTime LastUsedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 本地用户清单（存于 <c>users.json</c>，位于数据根目录，不属于任何用户）。
///
/// 设计说明：数据按"每个用户一个子目录"物理隔离，而不是在数据表里加 UserId 字段。
/// 后者需要给每张表加列并让所有查询带上过滤条件，改动面大且漏一处就会串数据；
/// 前者天然不会串。
/// </summary>
public class UserRegistry
{
    /// <summary>全部用户。</summary>
    public List<UserAccount> Users { get; set; } = new();

    /// <summary>
    /// 当前选中的用户 Id。为空表示尚未选择（首次启动或用户被删除），
    /// 此时启动流程会弹出选择界面。
    /// </summary>
    public string CurrentUserId { get; set; } = string.Empty;

    /// <summary>
    /// 是否已完成过"单用户 → 多用户"的数据迁移。
    /// 迁移只做一次：把旧版散落在数据根目录的数据库与配置文件，
    /// 收进第一个用户的子目录。
    /// </summary>
    public bool MigratedFromLegacy { get; set; }

    /// <summary>按 Id 找用户；找不到返回 null。</summary>
    /// <param name="id">用户 Id</param>
    /// <returns>用户或 null</returns>
    public UserAccount? Find(string id)
        => string.IsNullOrEmpty(id) ? null : Users.FirstOrDefault(u => u.Id == id);

    /// <summary>
    /// 取当前用户；未选择或已被删除时返回 null。
    ///
    /// 标注 <see cref="JsonIgnoreAttribute"/>：这是由 <see cref="CurrentUserId"/>
    /// 推导出的计算属性，不是需要持久化的数据。
    /// 不忽略会让 users.json 里多出一份冗余副本，看起来像"清单里存了两个当前用户"。
    /// </summary>
    /// <returns>当前用户或 null</returns>
    [System.Text.Json.Serialization.JsonIgnore]
    public UserAccount? Current => Find(CurrentUserId);

    /// <summary>
    /// 生成一个未被占用的用户 Id。
    /// 用「u + 序号」而不是随机串，便于用户自己在文件管理器里辨认备份目录。
    /// </summary>
    /// <returns>形如 u1 / u2 的 Id</returns>
    public string NextId()
    {
        for (int i = 1; i < 1000; i++)
        {
            string id = "u" + i;
            if (Users.All(u => u.Id != id))
            {
                return id;
            }
        }
        // 极端情况下退回时间戳，保证唯一
        return "u" + DateTime.Now.ToString("yyyyMMddHHmmss");
    }
}
