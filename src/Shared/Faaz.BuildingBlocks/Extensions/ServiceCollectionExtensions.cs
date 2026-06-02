using Faaz.BuildingBlocks.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Faaz.BuildingBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR with the logging + validation pipeline behaviors.
    /// Validators are scanned from the calling assembly.
    /// </summary>
    public static IServiceCollection AddMediatrWithBehaviors(
        this IServiceCollection services,
        Assembly assembly)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        return services;
    }
}
