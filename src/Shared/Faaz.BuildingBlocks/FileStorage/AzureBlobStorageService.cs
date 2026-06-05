using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Faaz.SharedKernel.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Faaz.BuildingBlocks.FileStorage;

internal sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IConfiguration config)
    {
        var client   = new BlobServiceClient(config["FileStorage:Azure:ConnectionString"]);
        _container   = client.GetBlobContainerClient(config["FileStorage:Azure:ContainerName"] ?? "faaz-files");
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, FileCategory category, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
        var ext          = Path.GetExtension(fileName);
        var blobName     = $"{category.ToString().ToLower()}/{Guid.NewGuid():N}_{safeFileName}{ext}";
        await _container.GetBlobClient(blobName).UploadAsync(stream, overwrite: true, ct);
        return blobName;
    }

    public async Task DeleteAsync(string storedPath, CancellationToken ct = default)
    {
        await _container.GetBlobClient(storedPath).DeleteIfExistsAsync(cancellationToken: ct);
    }

    public string GetUrl(string storedPath) =>
        _container.GetBlobClient(storedPath).Uri.ToString();
}
