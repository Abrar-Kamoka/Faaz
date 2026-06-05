namespace Faaz.SharedKernel.Abstractions;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, FileCategory category, CancellationToken ct = default);
    Task DeleteAsync(string storedPath, CancellationToken ct = default);
    string GetUrl(string storedPath);
}

public enum FileCategory
{
    Profiles     = 1,
    Credentials  = 2,
    Invoices     = 3,
    Applications = 4
}
