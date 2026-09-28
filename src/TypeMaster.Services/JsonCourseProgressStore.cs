using System;
using System.IO;
using System.Text.Json;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 课程闯关进度的 JSON 文件实现（与 <see cref="JsonUserProfile"/> 同款思路）。
/// 选择 JSON 而不是新增 EF 表，是为了避免给已有数据库引入迁移负担。
/// </summary>
public class JsonCourseProgressStore : ICourseProgressStore
{
    private static readonly string FilePath = TypeMaster.Core.AppDataPaths.CourseProgressFile;

    /// <summary>写入时加锁，避免多个页面同时保存造成文件写坏。</summary>
    private static readonly object SyncRoot = new();

    public CourseProgress Load()
    {
        if (!File.Exists(FilePath)) return new CourseProgress();
        try
        {
            var json = File.ReadAllText(FilePath);
            var p = JsonSerializer.Deserialize<CourseProgress>(json);
            return p ?? new CourseProgress();
        }
        catch
        {
            // 文件损坏时退化为全新进度，不让闯关功能整体不可用
            return new CourseProgress();
        }
    }

    public void Save(CourseProgress progress)
    {
        try
        {
            lock (SyncRoot)
            {
                var json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
        }
        catch
        {
            // 目录只读等情况下静默忽略，不影响内存中的本次操作
        }
    }

    public void Reset()
    {
        try
        {
            lock (SyncRoot)
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
        }
        catch
        {
            // 删除失败时静默忽略
        }
    }
}