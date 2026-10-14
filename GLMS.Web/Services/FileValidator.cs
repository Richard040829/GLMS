using GLMS.Web.Exceptions;

namespace GLMS.Web.Services;

public interface IFileValidator
{
    void ValidatePdf(IFormFile file);
}

/// <summary>Validates extension, size and PDF signature before a file is stored.</summary>
public sealed class FileValidator : IFileValidator
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46 }; // %PDF

    public void ValidatePdf(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new FileValidationException("No file was provided or the file is empty.");
        if (file.Length > MaxBytes)
            throw new FileValidationException("File exceeds the 10 MB limit.");

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new FileValidationException($"Invalid file type '{extension}'. Only .pdf files are allowed.");

        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[PdfMagic.Length];
        var read = stream.Read(header);
        if (read != PdfMagic.Length || !header.SequenceEqual(PdfMagic))
            throw new FileValidationException("File content is not a valid PDF.");
    }
}
