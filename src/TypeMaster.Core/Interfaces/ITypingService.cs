using System.Collections.Generic;
using TypeMaster.Core.Enums;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 打字评测结果
/// </summary>
public class TypingResult
{
    /// <summary>正确字符数</summary>
    public int RightCount { get; set; }

    /// <summary>错误字符数</summary>
    public int WrongCount { get; set; }

    /// <summary>正确率（百分比）</summary>
    public double Accuracy { get; set; }

    /// <summary>速度：英文 WPM / 中文 KPM / 五笔 字每分钟</summary>
    public double Speed { get; set; }

    /// <summary>是否已打完整个对照文本</summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 本次练习新增的逐键对错计数（键标签 -> 对错次数）。
    /// 仅英文 / 五笔这类有明确键位映射的练习会填充；中文文章练习为空。
    /// </summary>
    public Dictionary<string, (int Right, int Wrong)> KeyDeltas { get; set; } = new();
}

/// <summary>
/// 打字核心算法：根据对照文本与已输入文本计算成绩
/// </summary>
public interface ITypingService
{
    TypingResult Evaluate(string target, string typed, int elapsedSeconds, PracticeType type);

    /// <summary>
    /// 五笔专用评测：对照文本为"编码 空格 编码…"串，按编码单元（一个字）统计对错，
    /// 速度单位为"字/分钟"。
    /// </summary>
    /// <param name="expectedCodes">期望的五笔编码串（空格分隔）</param>
    /// <param name="typed">已输入的编码串</param>
    /// <param name="elapsedSeconds">已用时长（秒）</param>
    /// <returns>评测结果（RightCount/WrongCount 为"字"数）</returns>
    TypingResult EvaluateWubi(string expectedCodes, string typed, int elapsedSeconds);
}