using Microsoft.AspNetCore.Components.Server.Circuits;
using UI.Infrastructure.ObjectDetection;
using UI.Infrastructure.VideoProcessing;
using UI.Infrastructure.FileStorage;

namespace UI.Infrastructure;

/// <summary>
/// Provides extension methods for registering infrastructure services in the dependency injection container.
/// </summary>
public static class InfrastructureServicesRegistration
{
    /// <summary>
    /// Adds infrastructure services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> used to configure services.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient();

        services.AddApplicationAuth(configuration);

        services.AddScoped<CircuitHandler, CircuitHandlerService>();

        services.AddObjectDetection(configuration);

        services.AddSingleton<FFmpegFrameExtractor>();

        services.AddSingleton<FileStorageService>();

        return services;
    }
}