using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Enums
{
    public interface IEnumService
    {
        Task<EnumDataModel> GetAsync(string enumName);
        Task<List<EnumDataModel>> GetListAsync();
    }
}
