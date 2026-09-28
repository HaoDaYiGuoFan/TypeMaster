using System;
using System.IO;
using System.Text.Json;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 用户档案：以 JSON 文件持久化（与 JsonArticleLibrary 同款思路，避开 EF 表结构迁移坑），
/// 当前仅保存玩家昵称，供趣味提示语统一称呼。
/// </summary>
public class JsonUserProfile : IUserProfile
{
    private static readonly string FilePath = TypeMaster.Core.AppDataPaths.UserProfileFile;

    public UserProfile Load()
    {
        if (!File.Exists(FilePath)) return new UserProfile();
        try
        {
            var json = File.ReadAllText(FilePath);
            var p = JsonSerializer.Deserialize<UserProfile>(json);
            return p ?? new UserProfile();
        }
        catch
        {
            return new UserProfile();
        }
    }

    public void Save(UserProfile profile)
    {
        try
        {
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // 目录只读等情况下静默忽略
        }
    }
}
