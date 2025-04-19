using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Owners
{
    public interface IOwnerService
    {
        public Task CreateOwnerAsync(CreateOwnerRequest request);
    }
}