using TypeMaster.Core.Entities;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 课程闯关进度仓储（JSON 文件实现，见 <see cref="TypeMaster.Services.JsonCourseProgressStore"/>）。
/// </summary>
public interface ICourseProgressStore
{
    /// <summary>读取进度；文件不存在时返回空进度。</summary>
    CourseProgress Load();

    /// <summary>保存进度（写入失败静默忽略，不影响内存中的数据）。</summary>
    void Save(CourseProgress progress);

    /// <summary>清空进度（重新开始闯关）。</summary>
    void Reset();
}