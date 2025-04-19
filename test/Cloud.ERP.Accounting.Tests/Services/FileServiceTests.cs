
using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Files;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FileConfigs;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Files;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using BigBang.WebServer.Common.Services.File;
using Cloud.ERP.Accounting.Tests.Fakes;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class FileServiceTests : BaseServiceTests
    {
        private readonly Mock<IFileConfigRepository> _fileConfigRepository;
        private readonly Mock<IFileRepository> _fileRepository;
        private readonly Mock<IFileResultService> _fileResultService;
        private readonly FileService _service;


        public FileServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _fileConfigRepository = new Mock<IFileConfigRepository>();
            _fileRepository = new Mock<IFileRepository>();
            _fileResultService = new Mock<IFileResultService>();

            _service = new FileService(
                _fileRepository.Object,
                _fileConfigRepository.Object,
                _fileResultService.Object,
                AccountingIdentityService.Object);
        }

        [Fact]
        public async Task UploadVoucherFile_ValidPdf_ShouldReturnFileId()
        {
            // Arrange
            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            using var stream = new MemoryStream(Constants.PdfHeader);
            const string fileName = "example.pdf";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 1)
               .With(config => config.MaxSizeInbyte, 10)
               .With(config => config.ValidExtension, FileType.Pdf.ToString().ToLower())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                                 .Returns(new List<ACC_FileConfig> { config });

            ACC_File? addedFile = null;

            _fileRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_File>()))
                           .Callback<ACC_File>(file => addedFile = file);

            //ACT
            var result = await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            result.Should().NotBeEmpty();
            _fileRepository.Verify(repo => repo.AddAsync(It.Is<ACC_File>(file => file.FileName == fileName &&
                file.Business.Id == businessId)), Times.Once);
            addedFile?.FileName.Should().Be(fileName);
        }

        [Fact]
        public async Task UploadVoucherFile_ValidJpg_ShouldReturnFileId()
        {
            // Arrange
            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            using var stream = new MemoryStream(Constants.JpgHeader);
            const string fileName = "example.jpg";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 1)
               .With(config => config.MaxSizeInbyte, 10)
               .With(config => config.ValidExtension, FileType.Jpg.ToString().ToLower())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                .Returns(new List<ACC_FileConfig> { config });

            ACC_File? addedFile = null;

            _fileRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_File>()))
                           .Callback<ACC_File>(file => addedFile = file);

            //ACT
            var result = await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            result.Should().NotBeEmpty();
            _fileRepository.Verify(repo => repo.AddAsync(It.Is<ACC_File>(file => file.FileName == fileName &&
                file.Business.Id == businessId)), Times.Once);
            addedFile?.FileName.Should().Be(fileName);
        }

        [Fact]
        public async Task UploadVoucherFile_ValidPng_ShouldReturnFileId()
        {
            // Arrange
            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            using var stream = new MemoryStream(Constants.PngHeader);
            const string fileName = "example.png";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 1)
               .With(config => config.MaxSizeInbyte, 10)
               .With(config => config.ValidExtension, FileType.Png.ToString().ToLower())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                .Returns(new List<ACC_FileConfig> { config });

            ACC_File? addedFile = null;

            _fileRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_File>()))
                           .Callback<ACC_File>(file => addedFile = file);

            //ACT
            var result = await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            result.Should().NotBeEmpty();
            _fileRepository.Verify(repo => repo.AddAsync(It.Is<ACC_File>(file => file.FileName == fileName &&
                file.Business.Id == businessId)), Times.Once);
            addedFile?.FileName.Should().Be(fileName);
        }

        [Fact]
        public async Task UploadVoucherFile_ValidGif_ShouldReturnFileId()
        {
            // Arrange
            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            using var stream = new MemoryStream(Constants.GifHeader);
            const string fileName = "example.gif";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 1)
               .With(config => config.MaxSizeInbyte, 10)
               .With(config => config.ValidExtension, FileType.Gif.ToString().ToLower())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                                 .Returns(new List<ACC_FileConfig> { config });

            ACC_File? addedFile = null;

            _fileRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_File>()))
                           .Callback<ACC_File>(file => addedFile = file);

            //ACT
            var result = await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            result.Should().NotBeEmpty();
            _fileRepository.Verify(repo => repo.AddAsync(It.Is<ACC_File>(file => file.FileName == fileName &&
                file.Business.Id == businessId)), Times.Once);
            addedFile?.FileName.Should().Be(fileName);
        }

        [Fact]
        public async Task UploadVoucherFile_InvalidLowerSizeFile_ShouldThrowException()
        {
            // Arrange
            using var stream = new MemoryStream(Constants.GifHeader);
            const string fileName = "example.gif";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 10)
               .With(config => config.MaxSizeInbyte, 100)
               .With(config => config.ValidExtension, FileType.Gif.ToString())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                                 .Returns(new List<ACC_FileConfig> { config });

            //ACT
            Func<Task> func = async () => await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            var message = exception.Which.Message;
            message.Should().Contain(string.Format(Messages.File_InvalidFileSizeRange, config.MinSizeInbyte, config.MaxSizeInbyte));
        }

        [Fact]
        public async Task UploadVoucherFile_InvalidGreaterSizeFile_ShouldThrowException()
        {
            // Arrange
            using var stream = new MemoryStream(Constants.PngHeader);
            const string fileName = "example.png";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 1)
               .With(config => config.MaxSizeInbyte, 5)
               .With(config => config.ValidExtension, FileType.Png.ToString())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                                 .Returns(new List<ACC_FileConfig> { config });

            //ACT
            Func<Task> func = async () => await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            var message = exception.Which.Message;
            message.Should().Contain(string.Format(Messages.File_InvalidFileSizeRange, config.MinSizeInbyte, config.MaxSizeInbyte));
        }

        [Fact]
        public async Task UploadVoucherFile_InvalidNameFile_ShouldThrowException()
        {
            // Arrange
            using var stream = new MemoryStream(Constants.PngHeader);
            const string fileName = "example1.png";

            var config = Fixture.Build<ACC_FileConfig>()
               .With(config => config.Id, 1)
               .With(config => config.MinSizeInbyte, 1)
               .With(config => config.MaxSizeInbyte, 5)
               .With(config => config.ValidExtension, FileType.Png.ToString())
               .With(config => config.NamingRule, Constants.AlphabetRegex)
               .Create();

            _fileConfigRepository.Setup(repo => repo.GetListByEntityName<ACC_Voucher>())
                                 .Returns(new List<ACC_FileConfig> { config });

            //ACT
            Func<Task> func = async () => await _service.UploadAsync<ACC_Voucher>(stream, fileName);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            var message = exception.Which.Message;
            message.Should().Contain(Messages.File_InvalidFileName);
        }

        [Fact]
        public async Task DownloadVoucherFile_ValidFile_ShouldReturnFile()
        {
            // Arrange

            var fileId = Guid.NewGuid();
            var file = Fixture.Build<ACC_File>()
                .With(file => file.Id, fileId)
                .Create();

            _fileRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                               .ReturnsAsync(file);

            _fileResultService.Setup(fileResult => fileResult.CreateFileResult(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new FakeFileResult());

            // Act
            var result = await _service.DownloadAsync(fileId);

            // Assert
            result.Should().NotBeNull();

            _fileRepository.Verify(repo => repo.GetAsync(fileId), Times.Once);
            _fileResultService.Verify(fileResult => fileResult.CreateFileResult(file.FileName, file.FileConfig.ValidExtension), Times.Once);
        }

        [Fact]
        public async Task DownloadVoucherFile_FileNotFound_ShouldThrowException()
        {
            // Arrange  
            var businessId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            _fileRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                           .ReturnsAsync(default(ACC_File));
            // Act
            Func<Task> func = async () => await _service.DownloadAsync(fileId);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            var message = exception.Which.Message;
            message.Should().Contain(string.Format(Messages.Error_EntityNotFound, Messages.Label_File));
            _fileRepository.Verify(repo => repo.GetAsync(fileId), Times.Once);
        }
        [Fact]

        public async Task UpdateUpdateBankAccount_ExistingFile_ShouldUpdateSuccessfully()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();
            var businessId = Guid.NewGuid();

            var File = Fixture.Build<ACC_File>()
                .With(file => file.EntityOwnerId, newOwnerId)
                .Create();

            _fileRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(File);

            // Act
            var result = await _service.UpdateFileOwnerAsync(fileId, newOwnerId);

            // Assert
            result.Should().NotBeEmpty();

            _fileRepository.Verify(repo => repo.GetAsync(fileId), Times.Once);
            _fileRepository.Verify(repo => repo.UpdateAsync(It.Is<ACC_File>(
                file =>
                file.EntityOwnerId == newOwnerId
                )), Times.Once);
        }


    }
}