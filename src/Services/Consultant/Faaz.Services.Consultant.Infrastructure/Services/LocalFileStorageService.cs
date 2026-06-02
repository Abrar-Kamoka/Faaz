using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Faaz.Services.Consultant.Infrastructure.Services;

internal sealed class LocalFileStorageService : IFileStorageService
{
    // Physical root : {ContentRootPath}/wwwroot/uploads/
    // URL path      : /uploads/{subFolder}/{fileName}   (served via UseStaticFiles)
    private readonly string _basePath;
    private readonly string _wwwRoot;

    public LocalFileStorageService(IHostEnvironment env)
    {
        _wwwRoot  = Path.Combine(env.ContentRootPath, "wwwroot");
        _basePath = Path.Combine(_wwwRoot, "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, string subFolder, CancellationToken ct)
    {
        // Folder: wwwroot/uploads/{subFolder}/
        // e.g.    wwwroot/uploads/applications/{applicationId}/
        var folder = Path.Combine(_basePath, subFolder);
        Directory.CreateDirectory(folder);

        var safeName = Sanitize(fileName);
        var fullPath = Path.Combine(folder, safeName);

        // If a file with the same name already exists in this folder, append a short unique suffix
        if (File.Exists(fullPath))
        {
            var ext      = Path.GetExtension(safeName);
            var nameOnly = Path.GetFileNameWithoutExtension(safeName);
            var suffix   = Guid.NewGuid().ToString("N")[..8];
            safeName = $"{nameOnly}_{suffix}{ext}";
            fullPath = Path.Combine(folder, safeName);
        }

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        // Return path relative to wwwroot — used as the stored FilePath and buildable as a URL
        return $"uploads/{subFolder}/{safeName}".Replace('\\', '/');
    }

    public void Delete(string relativePath)
    {
        var fullPath = Path.Combine(_wwwRoot, relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static string Sanitize(string fileName)
    {
        // Strip directory traversal, then remove characters the OS won't allow in filenames
        var name = Path.GetFileName(fileName);
        name = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrWhiteSpace(name) ? $"{Guid.NewGuid()}.bin" : name;
    }
}
