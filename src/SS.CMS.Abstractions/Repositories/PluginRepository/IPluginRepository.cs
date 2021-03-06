using System.Threading.Tasks;
using Datory;

namespace SS.CMS.Abstractions
{
    public interface IPluginRepository : IRepository
    {
        Task DeleteAsync(string pluginId);

        Task UpdateIsDisabledAsync(string pluginId, bool isDisabled);

        Task UpdateTaxisAsync(string pluginId, int taxis);

        Task<(bool IsDisabled, int Taxis)> SetIsDisabledAndTaxisAsync(string pluginId);
    }
}
