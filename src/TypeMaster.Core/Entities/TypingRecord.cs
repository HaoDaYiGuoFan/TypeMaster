using System;

namespace TypeMaster.Core.Entities;

/// <summary>
/// 打字成绩记录（TypingRecord 表）
/// </summary>
public class TypingRecord
{
    /// <summary>自增主键</summary>
    public int Id { get; set; }

    /// <summary>练习类型：见 <see cref="TypeMaster.Core.Enums.PracticeType"/></summary>
    public int PracticeType { get; set; }

    /// <summary>难度：0简单 / 1普通 / 2困难</summary>
    public int Difficulty { get; set; }

    /// <summary>对照文本总字符数</summary>
    public int TotalCharCount { get; set; }

    /// <summary>正确输入字符数</summary>
    public int RightCharCount { get; set; }

    /// <summary>错误字符数</summary>
    public int WrongCharCount { get; set; }

    /// <summary>速度值 WPM(英文) / KPM(中文) / 字每分钟(五笔)</summary>
    public double Speed { get; set; }

    /// <summary>正确率（百分比）</summary>
    public double Accuracy { get; set; }

    /// <summary>练习耗时（秒）</summary>
    public int UseSecond { get; set; }

    /// <summary>记录生成时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 难度细分等级（1~10）。旧记录无此列时按 1 处理。
    /// </summary>
    public int Level { get; set; } = DefaultLevel;

    /// <summary>
    /// 评级：0=D / 1=C / 2=B / 3=A / 4=S。旧记录默认 D（即「未达标」）。
    /// </summary>
    public int Grade { get; set; }

    /// <summary>
    /// 本次练习的逐键对错统计（JSON 序列化的 KeyStatEntry 列表）。
    /// 为空表示该类型不采集键位（如中文文章练习），或为旧记录。
    /// </summary>
    public string KeyStatsJson { get; set; } = string.Empty;

    /// <summary>细分等级的默认值：旧记录升级后落到 1 级。</summary>
    public const int DefaultLevel = 1;
}