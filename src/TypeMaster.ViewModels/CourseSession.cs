using System;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Enums;
using TypeMaster.Core.Interfaces;
using TypeMaster.Services;

namespace TypeMaster.ViewModels;

/// <summary>
/// 一次关卡挑战的结果。
/// </summary>
public class LessonOutcome
{
    /// <summary>关卡定义</summary>
    public CourseLesson Lesson { get; init; } = null!;

    /// <summary>本次正确率（百分比）</summary>
    public double Accuracy { get; init; }

    /// <summary>本次评级</summary>
    public Grade Grade { get; init; }

    /// <summary>本次星级（1~5）</summary>
    public int Stars { get; init; }

    /// <summary>是否达到通关标准</summary>
    public bool Passed { get; init; }
}

/// <summary>
/// 课程会话（DI 单例）：在「课程中心页」与「打字练习页」之间传递当前关卡与挑战结果。
///
/// 之所以需要它：课程页与打字页是彼此独立的页面，二者不互相引用（遵循原有的
/// 「VM 不引用 View」约定）。把当前关卡与结果事件放在一个单例里，
/// 两个页面只依赖这个会话对象，即可完成「开始挑战 → 练习 → 回流结果」的闭环。
/// </summary>
public class CourseSession
{
    private readonly ICourseProgressStore _store;

    /// <summary>当前正在挑战的关卡；为 null 表示当前是自由练习。</summary>
    public CourseLesson? CurrentLesson { get; private set; }

    /// <summary>挑战结果已产生（课程页订阅后刷新关卡列表）。</summary>
    public event EventHandler<LessonOutcome>? LessonEvaluated;

    public CourseSession(ICourseProgressStore store) => _store = store;

    /// <summary>开始一关挑战，标记当前关卡。</summary>
    /// <param name="lesson">关卡</param>
    public void Begin(CourseLesson lesson) => CurrentLesson = lesson;

    /// <summary>退出关卡挑战，回到自由练习。</summary>
    public void End() => CurrentLesson = null;

    /// <summary>
    /// 上报一次关卡练习结果：写入进度（仅在达标时解锁下一关）并广播事件。
    /// </summary>
    /// <param name="accuracy">正确率（百分比）</param>
    /// <param name="grade">评级</param>
    /// <returns>本次挑战结果；无当前关卡时返回 null</returns>
    public LessonOutcome? Complete(double accuracy, Grade grade)
    {
        CourseLesson? lesson = CurrentLesson;
        if (lesson == null)
        {
            return null;
        }

        bool passed = CourseLibrary.IsPass(accuracy, grade);
        int stars = GradeScale.ToStars(grade);

        CourseProgress progress = _store.Load();
        progress.RecordAttempt(lesson.Id, stars, accuracy, passed);
        _store.Save(progress);

        var outcome = new LessonOutcome
        {
            Lesson = lesson,
            Accuracy = accuracy,
            Grade = grade,
            Stars = stars,
            Passed = passed
        };
        LessonEvaluated?.Invoke(this, outcome);
        return outcome;
    }
}