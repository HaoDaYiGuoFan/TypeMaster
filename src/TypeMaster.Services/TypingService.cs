using System;
using System.Collections.Generic;
using System.Linq;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;

namespace TypeMaster.Services;

/// <summary>
/// 打字核心算法实现
/// - 逐字符比对正确/错误
/// - 正确率 = 正确数 / (正确+错误) * 100
/// - 英文速度 WPM = (正确字符 / 5) / 分钟
/// - 中文速度 KPM = 正确字符 / 分钟
/// - 五笔速度 = 完整匹配的编码单元数（字数）/ 分钟
/// - 同时累计「逐键对错」，供薄弱键位分析与热力图使用
/// </summary>
public class TypingService : ITypingService
{
    public TypingResult Evaluate(string target, string typed, int elapsedSeconds, PracticeType type)
    {
        target ??= string.Empty;
        typed ??= string.Empty;

        int right = 0, wrong = 0;
        int len = Math.Min(target.Length, typed.Length);
        for (int i = 0; i < len; i++)
        {
            if (target[i] == typed[i]) right++;
            else wrong++;
        }

        // 超出对照文本长度的输入一律算错
        if (typed.Length > target.Length)
            wrong += typed.Length - target.Length;

        double accuracy = (right + wrong) == 0 ? 100d : Math.Round(right * 100d / (right + wrong), 1);

        double minutes = elapsedSeconds <= 0 ? (1d / 60d) : elapsedSeconds / 60d;
        double speed = IsChineseSpeed(type)
            ? Math.Round(right / minutes, 1)                 // KPM：字符/分钟
            : Math.Round((right / 5d) / minutes, 1);        // WPM：5字符=1词

        bool completed = target.Length > 0 && typed.Length >= target.Length;

        return new TypingResult
        {
            RightCount = right,
            WrongCount = wrong,
            Accuracy = accuracy,
            Speed = speed,
            IsCompleted = completed,
            KeyDeltas = CollectKeyDeltas(target, typed, type)
        };
    }

    public TypingResult EvaluateWubi(string expectedCodes, string typed, int elapsedSeconds)
    {
        expectedCodes ??= string.Empty;
        typed ??= string.Empty;

        #region 按编码单元统计对错（一个单元 = 一个字的五笔编码）

        // 五笔按顺序逐字输入，把"编码 空格 编码 空格…"按空格拆成单元逐一比对：
        // 完整且正确的单元记 1 个"正确字"，打错或未完成的单元记"错误字"
        string[] expectedUnits = SplitUnits(expectedCodes);
        string[] typedUnits = SplitUnits(typed);

        int right = 0, wrong = 0;
        for (int i = 0; i < typedUnits.Length; i++)
        {
            if (i < expectedUnits.Length && typedUnits[i] == expectedUnits[i])
            {
                right++;
            }
            else
            {
                wrong++;
            }
        }
        // 尚未输入完的单元不计入对错，只参与完成度判断

        #endregion 按编码单元统计对错（一个单元 = 一个字的五笔编码）

        double accuracy = (right + wrong) == 0 ? 100d : Math.Round(right * 100d / (right + wrong), 1);

        // 速度：字 / 分钟（每完整正确输入一个单元 = 完成 1 个字）
        double minutes = elapsedSeconds <= 0 ? (1d / 60d) : elapsedSeconds / 60d;
        double speed = Math.Round(right / minutes, 1);

        bool completed = expectedCodes.Length > 0 && typed.Length >= expectedCodes.Length;

        return new TypingResult
        {
            RightCount = right,
            WrongCount = wrong,
            Accuracy = accuracy,
            Speed = speed,
            IsCompleted = completed,
            KeyDeltas = CollectWubiKeyDeltas(expectedCodes, typed)
        };
    }

    #region 键位采集

    /// <summary>
    /// 中文类练习走输入法，物理按键与目标字符没有一一对应关系，
    /// 因此不做键位采集（否则会得出毫无意义的键位错误率）。
    /// </summary>
    private static bool IsChineseSpeed(PracticeType type)
        => type is PracticeType.Chinese or PracticeType.ChineseWord;

    /// <summary>该类型是否具备可靠的键位映射（用于决定是否采集键位）。</summary>
    private static bool SupportsKeyStats(PracticeType type)
        => type is PracticeType.English or PracticeType.EnglishWord or PracticeType.SpeedTest;

    /// <summary>
    /// 逐字符比对照文本，把「该按 A 结果按成 B」记成 A 的一次错误。
    /// 这样统计出来的才是「哪个键最容易按错」，而不是「哪个键被敲错」。
    /// </summary>
    private static Dictionary<string, (int Right, int Wrong)> CollectKeyDeltas(string target, string typed, PracticeType type)
    {
        var map = new Dictionary<string, (int Right, int Wrong)>(StringComparer.Ordinal);
        if (!SupportsKeyStats(type) || target.Length == 0 || typed.Length == 0)
        {
            return map;
        }

        int len = Math.Min(target.Length, typed.Length);
        for (int i = 0; i < len; i++)
        {
            string key = KeyLabel(target[i]);
            if (key.Length == 0) continue;

            bool ok = target[i] == typed[i];
            Add(map, key, ok);
        }

        // 超出目标长度的多余输入：无法归因到某个目标键，只记为空格键的错误
        for (int i = target.Length; i < typed.Length; i++)
        {
            Add(map, "Space", false);
        }

        return map;
    }

    /// <summary>
    /// 五笔键位采集：以「目标编码串」为基准逐字符比对，把该按的字母键记入统计。
    /// 空格（编码分隔符）同样计入。
    /// </summary>
    private static Dictionary<string, (int Right, int Wrong)> CollectWubiKeyDeltas(string expectedCodes, string typed)
    {
        var map = new Dictionary<string, (int Right, int Wrong)>(StringComparer.Ordinal);
        if (expectedCodes.Length == 0 || typed.Length == 0)
        {
            return map;
        }

        int len = Math.Min(expectedCodes.Length, typed.Length);
        for (int i = 0; i < len; i++)
        {
            string key = KeyLabel(expectedCodes[i]);
            if (key.Length == 0) continue;
            Add(map, key, char.ToLowerInvariant(expectedCodes[i]) == char.ToLowerInvariant(typed[i]));
        }

        return map;
    }

    /// <summary>字符 -> 与虚拟键盘一致的按键标签。</summary>
    private static string KeyLabel(char c) => c switch
    {
        ' ' => "Space",
        '\r' or '\n' => "Enter",
        '\t' => "Tab",
        _ => c.ToString().ToUpperInvariant()
    };

    /// <summary>累加一次击键结果。</summary>
    private static void Add(Dictionary<string, (int Right, int Wrong)> map, string key, bool ok)
    {
        map.TryGetValue(key, out var cur);
        map[key] = ok ? (cur.Right + 1, cur.Wrong) : (cur.Right, cur.Wrong + 1);
    }

    #endregion 键位采集

    #region 私有工具

    /// <summary>把"编码 空格 编码"串拆成编码单元数组（容忍多余空格与大小写差异）。</summary>
    /// <param name="text">编码串</param>
    /// <returns>编码单元数组</returns>
    private static string[] SplitUnits(string text)
        => text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Select(u => u.ToLowerInvariant())
               .ToArray();

    #endregion 私有工具
}