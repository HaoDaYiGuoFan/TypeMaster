using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TypeMaster.Data.DbContext;

/// <summary>
/// SQLite 轻量结构升级器。
///
/// 背景：本项目用 <c>Database.EnsureCreated()</c> 建库——它只在数据库文件不存在时建表，
/// 对「已经存在但没有新列」的旧库不会做任何改动。因此新增列（Level / Grade / KeyStatsJson）
/// 之后，老用户升级程序会因缺列而查询失败。
///
/// 方案：启动时读取 <c>PRAGMA table_info</c>，对缺失的列执行 <c>ALTER TABLE ... ADD COLUMN</c>。
/// 该操作在 SQLite 中是纯增量的：不改动已有行、不重建表、不丢数据。
/// </summary>
public static class SqliteSchemaUpgrader
{
    /// <summary>
    /// 确保 TypeMaster 相关表具备当前版本所需的列。
    /// </summary>
    /// <param name="context">数据库上下文（应已 EnsureCreated）</param>
    public static void Upgrade(TypeMasterDbContext context)
    {
        // 表名 -> 需要的列（列名, 列定义）
        var required = new Dictionary<string, (string Column, string Definition)[]>
        {
            ["TypingRecord"] = new[]
            {
                ("Level", "INTEGER NOT NULL DEFAULT 1"),
                ("Grade", "INTEGER NOT NULL DEFAULT 0"),
                ("KeyStatsJson", "TEXT NOT NULL DEFAULT ''")
            },
            ["AppConfig"] = new[]
            {
                ("EnableSpeech", "INTEGER NOT NULL DEFAULT 1"),
                ("SpeechRate", "INTEGER NOT NULL DEFAULT 0"),
                ("EnableMusic", "INTEGER NOT NULL DEFAULT 1"),
                ("MusicVolume", "INTEGER NOT NULL DEFAULT 55"),
                ("SoundVolume", "INTEGER NOT NULL DEFAULT 80"),
                // 长辈模式（面向中老年学习者）：默认值与 AppConfig 保持一致，
                // 保证既有用户升级后行为不变——ElderMode 默认关闭。
                ("ElderMode", "INTEGER NOT NULL DEFAULT 0"),
                ("ShowWubiKeyHint", "INTEGER NOT NULL DEFAULT 1"),
                ("ShowWubiBigChar", "INTEGER NOT NULL DEFAULT 1"),
                ("SpeakWubiHint", "INTEGER NOT NULL DEFAULT 0"),
                ("FontSizeBeforeElderMode", "REAL NOT NULL DEFAULT 22"),
                ("SpeechRateBeforeElderMode", "INTEGER NOT NULL DEFAULT 0"),
                // 显示适配：默认值与 AppConfig 保持一致，
                // 保证既有用户升级后界面外观不变（UiScale 默认 1.0）。
                ("UiScale", "REAL NOT NULL DEFAULT 1.0"),
                ("AutoFitWindow", "INTEGER NOT NULL DEFAULT 1")
            }
        };

        foreach (var (table, columns) in required)
        {
            if (!TableExists(context, table))
            {
                // 表还没建（首次运行由 EnsureCreated 处理），无需升级
                continue;
            }

            var existing = GetColumns(context, table);
            foreach (var (column, definition) in columns)
            {
                if (existing.Contains(column))
                {
                    continue;
                }

                // 列名与表名均由本文件内的常量给出，不含外部输入，可直接拼接
                string sql = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};";
                context.Database.ExecuteSqlRaw(sql);
            }
        }
    }

    /// <summary>判断表是否存在。</summary>
    private static bool TableExists(TypeMasterDbContext context, string table)
    {
        using var cmd = context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=$name;";
        var p = cmd.CreateParameter();
        p.ParameterName = "$name";
        p.Value = table;
        cmd.Parameters.Add(p);

        context.Database.OpenConnection();
        try
        {
            object? result = cmd.ExecuteScalar();
            return Convert.ToInt64(result ?? 0L) > 0;
        }
        finally
        {
            context.Database.CloseConnection();
        }
    }

    /// <summary>读取某张表已有的列名集合（不区分大小写）。</summary>
    private static HashSet<string> GetColumns(TypeMasterDbContext context, string table)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table}\");";

        context.Database.OpenConnection();
        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // PRAGMA table_info 的第 2 列（下标 1）是列名
                set.Add(reader.GetString(1));
            }
        }
        finally
        {
            context.Database.CloseConnection();
        }
        return set;
    }
}