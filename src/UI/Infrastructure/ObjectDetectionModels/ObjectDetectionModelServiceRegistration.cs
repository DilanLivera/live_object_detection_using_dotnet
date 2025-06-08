using Microsoft.Extensions.Options;
using UI.Infrastructure.ObjectDetectionModels.TinyYoloV3;

namespace UI.Infrastructure.ObjectDetectionModels;

/// <summary>
/// Adds object detection model services to the service collection.
/// </summary>
public static class ObjectDetectionModelServicesRegistration
{
    /// <summary>
    /// Adds object detection model services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObjectDetectionModels(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ObjectDetectionModelConfiguration>, ValidateObjectDetectionModelOptions>();

        services.AddOptions<ObjectDetectionModelConfiguration>()
                .Bind(configuration.GetSection(key: ObjectDetectionModelConfiguration.ConfigurationSectionName))
                .ValidateOnStart();

        services.AddSingleton<IObjectDetectionModel, TinyYoloV3Model>();

        return services;
    }
}