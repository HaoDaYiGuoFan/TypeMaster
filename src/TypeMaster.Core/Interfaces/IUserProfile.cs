using TypeMaster.Core.Entities;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 用户档案仓储（JSON 文件实现，见 <see cref="TypeMaster.Services.JsonUserProfile"/>）
/// </summary>
public interface IUserProfile
{
    /// <summary>读取档案；文件不存在时返回默认档案（昵称“用户1”，未初始化）。</summary>
    UserProfile Load();

    /// <summary>保存档案（写入失败静默忽略，不影响内存中的数据）。</summary>
    void Save(UserProfile profile);
}
