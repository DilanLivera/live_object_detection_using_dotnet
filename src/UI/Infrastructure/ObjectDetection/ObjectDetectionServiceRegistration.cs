using Microsoft.Extensions.Options;
using UI.Infrastructure.ObjectDetection.Models;
using UI.Infrastructure.ObjectDetection.Models.TinyYoloV3;

namespace UI.Infrastructure.ObjectDetection;

/// <summary>
/// Adds object detection services to the service collection.
/// </summary>
public static class ObjectDetectionServicesRegistration
{
    /// <summary>
    /// Adds object detection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObjectDetection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ObjectDetectionConfiguration>, ValidateObjectDetectionOptions>();

        services.AddOptions<ObjectDetectionConfiguration>()
                .Bind(configuration.GetSection(key: ObjectDetectionConfiguration.ConfigurationSectionName))
                .ValidateOnStart();

        services.AddSingleton<IObjectDetectionModel, TinyYoloV3Model>();
        services.AddSingleton<ObjectDetector>();

        return services;
    }
}