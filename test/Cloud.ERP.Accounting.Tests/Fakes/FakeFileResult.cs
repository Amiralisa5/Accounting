using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.WebServer.Common.Services.File;

namespace Cloud.ERP.Accounting.Tests.Fakes
{
    public class FakeFileResult : IFileResult
    {
        public Stream Result => new MemoryStream(Constants.PngHeader);
        public string ContentType => "application/json";
        public string FileName => "sample.json";
        public string Extension => ".json";
    }
}