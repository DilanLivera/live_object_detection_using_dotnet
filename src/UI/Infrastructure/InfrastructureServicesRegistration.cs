using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Server.Circuits;
using UI.Infrastructure.ObjectDetection;
using UI.Infrastructure.VideoProcessing;

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

        services.AddAuth(configuration);

        services.AddScoped<CircuitHandler, CircuitHandlerService>();

        services.AddObjectDetection(configuration);

        services.AddSingleton<FFmpegFrameExtractor>();

        services.AddSingleton<FileService>();

        services.AddHostedService<FileCleanupService>();


        return services;
    }

    /// <summary>
    /// Adds authentication services to the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogle(options =>
                {
                    configuration.Bind(key: "Authentication:Google", options);

                    if (string.IsNullOrEmpty(options.ClientId))
                    {
                        throw new InvalidOperationException("Google ClientId not found.");
                    }

                    if (string.IsNullOrEmpty(options.ClientSecret))
                    {
                        throw new InvalidOperationException("Google ClientSecret not found.");
                    }
                });

        services.AddAuthorizationCore();

        return services;
    }
}