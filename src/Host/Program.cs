using BigBang.WebServer.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddBigBangServer(options =>
{
    options.WithCustomServerCatalog("CloudERP")
        .DisplayName("CloudERP");
});
builder.AddBigBangCoreWebApplication();
var app = builder.Build();
app.UseBigBangCoreWebApplication();
app.Run();