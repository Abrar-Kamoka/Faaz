using Faaz.SharedKernel.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Faaz.BuildingBlocks.FileStorage;

public static class FileStorageExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["FileStorage:Provider"] ?? "Local";

        if (provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
        else
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
