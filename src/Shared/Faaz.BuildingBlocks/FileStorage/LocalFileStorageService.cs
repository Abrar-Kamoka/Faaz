using Faaz.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Faaz.BuildingBlocks.FileStorage;

internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        var webRoot = string.IsNullOrEmpty(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;
        _basePath = Path.Combine(webRoot, config["FileStorage:Local:BasePath"] ?? "uploads");
        _baseUrl  = config["FileStorage:Local:BaseUrl"] ?? string.Empty;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, FileCategory category, CancellationToken ct = default)
    {
        var folder = Path.Combine(_basePath, category.ToString().ToLower());
        Directory.CreateDirectory(folder);

        var safeFileName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
        var ext          = Path.GetExtension(fileName);
        var stored       = $"{Guid.NewGuid():N}_{safeFileName}{ext}";
        var fullPath     = Path.Combine(folder, stored);

        await using var file = File.Create(fullPath);
        await stream.CopyToAsync(file, ct);

        return $"uploads/{category.ToString().ToLower()}/{stored}";
    }

    public Task DeleteAsync(string storedPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, "..", storedPath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetUrl(string storedPath) => $"{_baseUrl}/{storedPath}";
}
