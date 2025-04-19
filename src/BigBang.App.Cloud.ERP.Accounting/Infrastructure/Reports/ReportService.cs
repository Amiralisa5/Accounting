using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.Metadata.Models;
using BigBang.Metadata.Models.Datagram;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services.File;
using BigBang.WebServer.Common.Services.Metadata;
using BigBang.WebServer.Common.Services.Print;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Reports
{
    [Service(ServiceType = typeof(IReportService), InstanceMode = InstanceMode.PerRequest, Requestable = false)]
    public class ReportService : IReportService
    {
        private readonly IMetadataService _metadataService;
        private readonly IPrintService _printService;

        public ReportService(IMetadataService metadataService, IPrintService printService)
        {
            _metadataService = metadataService;
            _printService = printService;
        }

        public ITemporaryFile Print<TEntity>(string printTemplate, Dictionary<string, object> parameters) where TEntity : class, IEntity, new()
        {
            var name = typeof(TEntity).Name;
            var entityModelInfo = _metadataService.GetModelByName(name);
            var printTemplateInfo = entityModelInfo.PrintTemplates
                                                   .FirstOrDefault(t => t.Name.Equals(printTemplate, StringComparison.InvariantCultureIgnoreCase));
            var temporaryFile = _printService.Print(printTemplateInfo.Model.ModuleInfo.ApplicationInfo.Name, printTemplateInfo.Id, new Datagram(), parameters);
            temporaryFile.IsDownloadable = true;
            return temporaryFile;
        }
    }
}