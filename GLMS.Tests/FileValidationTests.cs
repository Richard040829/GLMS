using GLMS.Tests.Helpers;
using GLMS.Web.Exceptions;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace GLMS.Tests;

public class FileValidationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "glms-tests-" + Guid.NewGuid());
    private readonly FileStorageService _service;

    public FileValidationTests()
    {
        Directory.CreateDirectory(_root);
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(_root);
        _service = new FileStorageService(env.Object);
    }

    [Theory]
    [InlineData("agreement.pdf")]
    [InlineData("AGREEMENT.PDF")]
    [InlineData("Signed.Pdf")]
    public void ValidatePdf_ValidPdf_DoesNotThrow(string name)
    {
        var ex = Record.Exception(() => _service.ValidatePdf(TestHelpers.CreateFile(name)));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.bat")]
    [InlineData("doc.docx")]
    [InlineData("agreement.pdf.exe")]
    [InlineData("noextension")]
    public void ValidatePdf_RestrictedExtension_ThrowsFileValidationException(string name)
    {
        Assert.Throws<FileValidationException>(() => _service.ValidatePdf(TestHelpers.CreateFile(name)));
    }

    [Fact]
    public void ValidatePdf_ExeRenamedToPdf_ThrowsBecauseContentIsNotPdf()
    {
        var fake = TestHelpers.CreateFile("evil.pdf", "MZ-this-is-an-executable");
        Assert.Throws<FileValidationException>(() => _service.ValidatePdf(fake));
    }

    [Fact]
    public void ValidatePdf_EmptyFile_Throws()
    {
        Assert.Throws<FileValidationException>(() => _service.ValidatePdf(TestHelpers.CreateFile("a.pdf", "")));
    }

    [Fact]
    public async Task SaveSignedAgreement_ValidPdf_SavesFileAndReturnsDownloadPath()
    {
        var path = await _service.SaveSignedAgreementAsync(TestHelpers.CreateFile("contract.pdf"));

        Assert.StartsWith("/uploads/contracts/", path);
        Assert.EndsWith(".pdf", path);
        var (physical, _) = _service.ResolveFile(path);
        Assert.True(File.Exists(physical));
    }

    [Fact]
    public async Task SaveSignedAgreement_Exe_ThrowsAndWritesNothing()
    {
        await Assert.ThrowsAsync<FileValidationException>(() =>
            _service.SaveSignedAgreementAsync(TestHelpers.CreateFile("virus.exe")));

        var folder = Path.Combine(_root, "uploads", "contracts");
        Assert.False(Directory.Exists(folder) && Directory.GetFiles(folder).Any());
    }

    [Fact]
    public void ResolveFile_PathTraversal_OnlyUsesFileName()
    {
        Assert.Throws<EntityNotFoundException>(() => _service.ResolveFile("/uploads/contracts/../../appsettings.json"));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
