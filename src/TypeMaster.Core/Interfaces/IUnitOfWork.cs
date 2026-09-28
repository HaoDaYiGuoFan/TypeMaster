using TypeMaster.Core.Entities;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 工作单元：聚合仓储并统一提交
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ITypingRecordRepository TypingRecords { get; }
    IAppConfigRepository AppConfig { get; }
    void SaveChanges();
    Task SaveChangesAsync();
}
