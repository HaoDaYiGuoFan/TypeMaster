using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Interfaces;
using TypeMaster.Data.DbContext;

namespace TypeMaster.Data.Repositories;

public class TypingRecordRepository : ITypingRecordRepository
{
    private readonly TypeMasterDbContext _ctx;

    public TypingRecordRepository(TypeMasterDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(TypingRecord record)
    {
        _ctx.TypingRecords.Add(record);
        await _ctx.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TypingRecord>> GetAllAsync()
        => await _ctx.TypingRecords.OrderByDescending(x => x.CreateTime).ToListAsync();

    public async Task<IReadOnlyList<TypingRecord>> GetByTypeAsync(int practiceType)
        => await _ctx.TypingRecords
            .Where(x => x.PracticeType == practiceType)
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();

    public async Task<IReadOnlyList<TypingRecord>> GetRangeAsync(DateTime? from, DateTime? to)
    {
        IQueryable<TypingRecord> q = _ctx.TypingRecords;
        if (from.HasValue)
        {
            q = q.Where(x => x.CreateTime >= from.Value);
        }
        if (to.HasValue)
        {
            q = q.Where(x => x.CreateTime <= to.Value);
        }
        return await q.OrderBy(x => x.CreateTime).ToListAsync();
    }

    public async Task<IReadOnlyDictionary<int, int>> GetGradeDistributionAsync()
    {
        var grouped = await _ctx.TypingRecords
            .GroupBy(x => x.Grade)
            .Select(g => new { Grade = g.Key, Count = g.Count() })
            .ToListAsync();

        return grouped.ToDictionary(g => g.Grade, g => g.Count);
    }

    public async Task ClearAsync()
    {
        var all = await _ctx.TypingRecords.ToListAsync();
        _ctx.TypingRecords.RemoveRange(all);
        await _ctx.SaveChangesAsync();
    }
}