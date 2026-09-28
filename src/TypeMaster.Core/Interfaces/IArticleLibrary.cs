using System.Collections.Generic;
using TypeMaster.Core.Entities;

namespace TypeMaster.Core.Interfaces;

/// <summary>自定义文章库：提供用户导入文章的持久化与查询（实现可为 JSON 文件或数据库）。</summary>
public interface IArticleLibrary
{
    IReadOnlyList<CustomArticle> GetAll();
    CustomArticle Add(string title, string content);
    void Delete(int id);
    CustomArticle? GetById(int id);
}
