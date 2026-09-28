using System;

namespace TypeMaster.Core.Entities;

/// <summary>用户自定义导入的打字练习文章（以 JSON 文件持久化，无需迁移数据库表结构）。</summary>
public class CustomArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
