using Microsoft.EntityFrameworkCore;
using TypeMaster.Core.Entities;

namespace TypeMaster.Data.DbContext;

/// <summary>
/// 数据库上下文（SQLite）
/// </summary>
public class TypeMasterDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<TypingRecord> TypingRecords { get; set; } = null!;
    public DbSet<AppConfig> AppConfigs { get; set; } = null!;

    public TypeMasterDbContext(DbContextOptions<TypeMasterDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TypingRecord>(e =>
        {
            e.ToTable("TypingRecord");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 新增列：给出数据库级默认值，保证旧库升级后存量数据可读
            e.Property(x => x.Level).HasDefaultValue(TypingRecord.DefaultLevel);
            e.Property(x => x.Grade).HasDefaultValue(0);
            e.Property(x => x.KeyStatsJson).HasDefaultValue(string.Empty);
        });

        modelBuilder.Entity<AppConfig>(e =>
        {
            e.ToTable("AppConfig");
            e.HasKey(x => x.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}