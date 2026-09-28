using System.Threading.Tasks;
using TypeMaster.Core.Entities;

namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 软件配置仓储
/// </summary>
public interface IAppConfigRepository
{
    Task<AppConfig> GetAsync();
    Task SaveAsync(AppConfig config);
}
