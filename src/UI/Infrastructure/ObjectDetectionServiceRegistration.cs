using Microsoft.Extensions.Options;
using UI.Infrastructure.Models;
using UI.Infrastructure.Models.TinyYoloV3;
using UI.Infrastructure.Models.YoloV4;

namespace UI.Infrastructure;

/// <summary>
/// Adds object detection services to the service collection.
/// </summary>
public static class ObjectDetectionServiceRegistration
{
    /// <summary>
    /// Adds object detection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObjectDetection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ObjectDetectionConfiguration>, ValidateObjectDetectionOptions>();

        services.AddOptions<ObjectDetectionConfiguration>()
                .Bind(configuration.GetSection(ObjectDetectionConfiguration.ConfigurationSectionName))
                .ValidateOnStart();

        services.AddSingleton<TinyYoloV3Model>();
        services.AddSingleton<YoloV4Model>();

        services.AddSingleton<IObjectDetectionModel>(sp =>
        {
            ObjectDetectionConfiguration config = sp.GetRequiredService<IOptions<ObjectDetectionConfiguration>>()
                                                    .Value;
            return config.YoloV4 is not null
                ? sp.GetRequiredService<YoloV4Model>()
                : sp.GetRequiredService<TinyYoloV3Model>();

        });

        services.AddSingleton<ObjectDetector>();

        return services;
    }
}