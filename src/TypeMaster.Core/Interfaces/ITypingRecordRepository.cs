using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TypeMaster.Core.Entities;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 打字成绩仓储
/// </summary>
public interface ITypingRecordRepository
{
    Task AddAsync(TypingRecord record);
    Task<IReadOnlyList<TypingRecord>> GetAllAsync();
    Task<IReadOnlyList<TypingRecord>> GetByTypeAsync(int practiceType);
    Task ClearAsync();

    /// <summary>
    /// 按时间范围取成绩（两端均可为 null 表示不限）。
    /// 数据量不大，统计交给上层内存计算，避免为每种筛选组合写一个查询。
    /// </summary>
    /// <param name="from">起始时间（含）</param>
    /// <param name="to">结束时间（含）</param>
    /// <returns>按时间升序排列的成绩列表</returns>
    Task<IReadOnlyList<TypingRecord>> GetRangeAsync(DateTime? from, DateTime? to);

    /// <summary>
    /// 取全部成绩的评级分布计数（键为评级整数值 0~4）。
    /// </summary>
    /// <returns>评级 -> 次数</returns>
    Task<IReadOnlyDictionary<int, int>> GetGradeDistributionAsync();
}