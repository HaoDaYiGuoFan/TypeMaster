using System;
using System.Collections.Generic;

namespace TypeMaster.Core.Entities;

/// <summary>
/// 课程闯关进度（以 JSON 文件持久化，与 user_profile.json 同款思路，避开 EF 表结构迁移）。
/// </summary>
public class CourseProgress
{
    /// <summary>
    /// 已通关的关卡 Id 集合。第一关无需前置、天然可玩；
    /// 某关达到通关标准后把它的 Id 记进来，即可解锁后续关卡。
    /// </summary>
    public List<string> ClearedLessonIds { get; set; } = new();

    /// <summary>各关的最佳星级（1~5），关。</summary>
    public Dictionary<string, int> BestStars { get; set; } = new();

    /// <summary>各关的最佳正确率（百分比），用于展示与留痕。</summary>
    public Dictionary<string, double> BestAccuracy { get; set; } = new();

    /// <summary>最近一次练习的关卡 Id（用于「继续上次进度」）。</summary>
    public string LastLessonId { get; set; } = string.Empty;

    /// <summary>进度最后更新时间。</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>判断某关是否已通关。</summary>
    /// <param name="lessonId">关卡 Id</param>
    /// <returns>是否已通关</returns>
    public bool IsCleared(string lessonId) => ClearedLessonIds.Contains(lessonId);

    /// <summary>读取某关最佳星级（未打过返回 0）。</summary>
    /// <param name="lessonId">关卡 Id</param>
    /// <returns>0~5 星</returns>
    public int GetStars(string lessonId)
        => BestStars.TryGetValue(lessonId, out int s) ? s : 0;

    /// <summary>
    /// 记录一次挑战结果：星级与最好正确率只升不降；
    /// 仅在达到通关标准时才把关卡标记为已通关（这是解锁下一关的唯一依据）。
    /// </summary>
    /// <param name="lessonId">关卡 Id</param>
    /// <param name="stars">本次星级（1~5）</param>
    /// <param name="accuracy">本次正确率（百分比）</param>
    /// <param name="passed">本次是否达到通关标准</param>
    public void RecordAttempt(string lessonId, int stars, double accuracy, bool passed)
    {
        if (passed && !ClearedLessonIds.Contains(lessonId))
        {
            ClearedLessonIds.Add(lessonId);
        }
        if (stars > GetStars(lessonId))
        {
            BestStars[lessonId] = stars;
        }
        if (!BestAccuracy.TryGetValue(lessonId, out double prev) || accuracy > prev)
        {
            BestAccuracy[lessonId] = accuracy;
        }
        LastLessonId = lessonId;
        UpdatedAt = DateTime.Now;
    }
}