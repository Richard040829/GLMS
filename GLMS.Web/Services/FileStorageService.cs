using GLMS.Web.Exceptions;

namespace GLMS.Web.Services;

public interface IFileStorageService
{
    void ValidatePdf(IFormFile file);
    Task<string> SaveSignedAgreementAsync(IFormFile file, CancellationToken ct = default);
    (string PhysicalPath, string DownloadName) ResolveFile(string relativePath);
}

public class FileStorageService : IFileStorageService
{
    private const string UploadFolder = "uploads/contracts";

    private readonly IWebHostEnvironment _env;
    private readonly IFileValidator _validator;
    public FileStorageService(IWebHostEnvironment env, IFileValidator? validator = null)
    {
        _env = env;
        _validator = validator ?? new FileValidator();
    }

    public void ValidatePdf(IFormFile file) => _validator.ValidatePdf(file);

    public async Task<string> SaveSignedAgreementAsync(IFormFile file, CancellationToken ct = default)
    {
        ValidatePdf(file);

        var folder = Path.Combine(_env.WebRootPath, UploadFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(folder);

        // Server-generated name prevents overwrites and path traversal
        var storedName = $"{Guid.NewGuid():N}.pdf";
        var physical = Path.Combine(folder, storedName);

        await using var fs = new FileStream(physical, FileMode.CreateNew);
        await file.CopyToAsync(fs, ct);

        return $"/{UploadFolder}/{storedName}";
    }

    public (string PhysicalPath, string DownloadName) ResolveFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath); // strips any traversal
        var physical = Path.Combine(_env.WebRootPath, UploadFolder.Replace('/', Path.DirectorySeparatorChar), fileName);
        if (!File.Exists(physical))
            throw new EntityNotFoundException("Signed agreement file was not found on the server.");
        return (physical, $"SignedAgreement_{fileName}");
    }
}
