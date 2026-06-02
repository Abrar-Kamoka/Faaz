namespace Faaz.Services.Consultant.Infrastructure.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, string subFolder, CancellationToken ct = default);
    void Delete(string relativePath);
}
