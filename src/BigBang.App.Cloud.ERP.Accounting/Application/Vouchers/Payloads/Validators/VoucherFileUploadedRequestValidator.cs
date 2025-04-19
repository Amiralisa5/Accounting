using System;
using System.Linq;
using System.Text.RegularExpressions;
using BigBang.App.Cloud.ERP.Accounting.Common.Extensions;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FileConfigs;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads.Validators
{
    internal class VoucherFileUploadedRequestValidator : BaseValidator<VoucherFileUploadedRequest>
    {
        public VoucherFileUploadedRequestValidator(IFileConfigRepository fileConfigRepository)
        {
            RuleFor(voucherFile => voucherFile)
                .Custom((voucherFile, context) =>
                {
                    var configs = fileConfigRepository.GetListByEntityName<ACC_Voucher>();
                    if (!configs.Any())
                        context.AddFailure(Messages.Label_File);

                    var config = configs.FirstOrDefault(fileConfig => fileConfig.ValidExtension.Equals(voucherFile.Data.GetFileExtensionFromStream().ToString(), StringComparison.CurrentCultureIgnoreCase));

                    if (config is null)
                        context.AddFailure(Messages.File_InvalidFileFormat);
                    else
                    {
                        var regex = new Regex(config.NamingRule);
                        if (!regex.IsMatch(voucherFile.FileName))
                            context.AddFailure(Messages.File_InvalidFileName);

                        if (voucherFile.Data.Length > config.MaxSizeInbyte ||
                            voucherFile.Data.Length < config.MinSizeInbyte)
                            context.AddFailure(string.Format(Messages.File_InvalidFileSizeRange, config.MinSizeInbyte,
                                config.MaxSizeInbyte));
                    }
                });
        }
    }
}