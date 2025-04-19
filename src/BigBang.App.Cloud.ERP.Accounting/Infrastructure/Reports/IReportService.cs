using System.Collections.Generic;
using BigBang.Metadata.Models;
using BigBang.WebServer.Common.Services.File;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Reports
{
    public interface IReportService
    {
        public ITemporaryFile Print<TEntity>(string printTemplate,
                                                  Dictionary<string, object> parameters) where TEntity : class,
                                                                                                           IEntity,
                                                                                                            new();

    }
}