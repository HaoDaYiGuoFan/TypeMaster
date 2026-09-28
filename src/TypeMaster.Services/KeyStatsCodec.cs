using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TypeMaster.Core.Entities;

namespace TypeMaster.Services;

/// <summary>
/// 逐键对错的序列化 / 反序列化与跨记录聚合。
/// 成绩表以 JSON 字符串保存每键统计，避免为键位分析单独建表。
/// </summary>
public static class KeyStatsCodec
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    /// <summary>
    /// 把逐键计数序列化为 JSON；无数据时返回空串（便于「无键位数据」的判断）。
    /// </summary>
    /// <param name="deltas">键标签 -> (对, 错)</param>
    /// <returns>JSON 字符串或空串</returns>
    public static string Serialize(IReadOnlyDictionary<string, (int Right, int Wrong)>? deltas)
    {
        if (deltas == null || deltas.Count == 0)
        {
            return string.Empty;
        }

        var list = deltas
            .Where(kv => kv.Value.Right + kv.Value.Wrong > 0)
            .Select(kv => new KeyStatEntry { Key = kv.Key, Right = kv.Value.Right, Wrong = kv.Value.Wrong })
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToList();

        return list.Count == 0 ? string.Empty : JsonSerializer.Serialize(list, Options);
    }

    /// <summary>
    /// 反序列化逐键计数；空串或损坏内容返回空列表。
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>键位统计列表</returns>
    public static List<KeyStatEntry> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<KeyStatEntry>();
        }
        try
        {
            return JsonSerializer.Deserialize<List<KeyStatEntry>>(json) ?? new List<KeyStatEntry>();
        }
        catch
        {
            return new List<KeyStatEntry>();
        }
    }

    /// <summary>
    /// 把多条成绩的键位统计合并汇总（同一按键跨记录累加）。
    /// </summary>
    /// <param name="records">成绩记录序列</param>
    /// <returns>按按键聚合后的统计列表（按错误次数从多到少排序）</returns>
    public static List<KeyStatEntry> Aggregate(IEnumerable<TypingRecord> records)
    {
        var map = new Dictionary<string, KeyStatEntry>(StringComparer.Ordinal);
        foreach (var r in records ?? Enumerable.Empty<TypingRecord>())
        {
            foreach (var e in Deserialize(r.KeyStatsJson))
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                if (!map.TryGetValue(e.Key, out var acc))
                {
                    acc = new KeyStatEntry { Key = e.Key };
                    map[e.Key] = acc;
                }
                acc.Right += e.Right;
                acc.Wrong += e.Wrong;
            }
        }

        return map.Values
            .OrderByDescending(e => e.Wrong)
            .ThenByDescending(e => e.ErrorRate)
            .ToList();
    }
}