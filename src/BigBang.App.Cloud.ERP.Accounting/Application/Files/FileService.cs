using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Common.Extensions;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FileConfigs;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Files;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services.File;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Files;

[Service(ServiceType = typeof(IFileService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
internal class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileConfigRepository _fileConfigRepository;
    private readonly IFileResultService _fileResultService;
    private readonly IAccountingIdentityService _accountingIdentityService;

    public FileService(IFileRepository fileRepository,
        IFileConfigRepository fileConfigRepository,
        IFileResultService fileResultService,
        IAccountingIdentityService accountingIdentityService)
    {
        _fileRepository = fileRepository;
        _fileConfigRepository = fileConfigRepository;
        _fileResultService = fileResultService;
        _accountingIdentityService = accountingIdentityService;
    }

    public async Task<Guid> UpdateFileOwnerAsync(Guid id, Guid entityOwnerId)
    {
        var file = await _fileRepository.GetAsync(id);

        file.EntityOwnerId = entityOwnerId;

        await _fileRepository.UpdateAsync(file);

        return file.Id;
    }

    public async Task<IFileResult> DownloadAsync(Guid id)
    {
        var file = await _fileRepository.GetAsync(id);
        if (file == null) throw ExceptionHelper.NotFound(Messages.Label_File);

        var fileResult = _fileResultService.CreateFileResult(file.FileName, file.FileConfig.ValidExtension);
        fileResult.Result.Write(file.Content);

        return fileResult;
    }

    public async Task<Guid> UploadAsync<TEntity>(Stream data, string fileName) where TEntity : class, new()
    {
        var bytes = await data.GetBytes();
        var validator = new VoucherFileUploadedRequestValidator(_fileConfigRepository);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var result = await validator.ValidateAsync(new VoucherFileUploadedRequest(bytes, fileNameWithoutExtension));

        if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

        var businessId = await _accountingIdentityService.GetBusinessIdAsync();

        var config = _fileConfigRepository.GetListByEntityName<ACC_Voucher>()
            .First(fileConfig => fileConfig.ValidExtension.Equals(bytes.GetFileExtensionFromStream().ToString(), StringComparison.InvariantCultureIgnoreCase));

        var file = new ACC_File
        {
            Id = Guid.NewGuid(),
            FileConfig = new ACC_FileConfig { Id = config.Id },
            Content = bytes,
            FileName = $"{fileNameWithoutExtension}.{config.ValidExtension}",
            Size = bytes.Length,
            CreatedDate = DateTime.Now,
            Business = new ACC_Business { Id = businessId }
        };

        await _fileRepository.AddAsync(file);

        return file.Id;
    }
}