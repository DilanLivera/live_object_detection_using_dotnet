namespace UI.Infrastructure.UploadedVideoProcessing;

/// <summary>
/// Adds object detection services to the service collection.
/// </summary>
public static class UploadedVideoProcessingServiceRegistration
{
    /// <summary>
    /// Adds the services required for detecting objects in uploaded videos
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddUploadedVideoProcessing(this IServiceCollection services)
    {
        services.AddSingleton<FFmpegFrameExtractor>();

        services.AddSingleton<UploadedVideoProcessor>();

        return services;
    }
}