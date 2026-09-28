using TypeMaster.Core.Interfaces;
using TypeMaster.Data.DbContext;

namespace TypeMaster.Data.Repositories;

/// <summary>
/// 工作单元实现，聚合仓储并持有 DbContext
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TypeMasterDbContext _ctx;

    public ITypingRecordRepository TypingRecords { get; }
    public IAppConfigRepository AppConfig { get; }

    public UnitOfWork(TypeMasterDbContext ctx)
    {
        _ctx = ctx;
        TypingRecords = new TypingRecordRepository(ctx);
        AppConfig = new AppConfigRepository(ctx);
    }

    public void SaveChanges() => _ctx.SaveChanges();

    public Task SaveChangesAsync() => _ctx.SaveChangesAsync();

    public void Dispose() => _ctx.Dispose();
}
