using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 自定义文章库：以 JSON 文件持久化（无需迁移数据库表结构），
/// 提供增删查，供打字练习页导入与使用用户自己的文章。
/// </summary>
public class JsonArticleLibrary : IArticleLibrary
{
    private static readonly string FilePath = TypeMaster.Core.AppDataPaths.CustomArticlesFile;
    private readonly List<CustomArticle> _cache;
    private int _nextId;

    public JsonArticleLibrary()
    {
        _cache = Load();
        _nextId = _cache.Count == 0 ? 1 : _cache.Max(a => a.Id) + 1;
    }

    public IReadOnlyList<CustomArticle> GetAll()
        => _cache.OrderBy(a => a.CreatedAt).ToList();

    public CustomArticle Add(string title, string content)
    {
        var article = new CustomArticle
        {
            Id = _nextId++,
            Title = title,
            Content = content,
            CreatedAt = DateTime.Now
        };
        _cache.Add(article);
        Save();
        return article;
    }

    public void Delete(int id)
    {
        _cache.RemoveAll(a => a.Id == id);
        Save();
    }

    public CustomArticle? GetById(int id) => _cache.FirstOrDefault(a => a.Id == id);

    private static List<CustomArticle> Load()
    {
        if (!File.Exists(FilePath)) return new List<CustomArticle>();
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<CustomArticle>>(json) ?? new List<CustomArticle>();
        }
        catch
        {
            return new List<CustomArticle>();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // 写入失败（如目录只读）时静默忽略，不影响内存中的本次操作
        }
    }
}
