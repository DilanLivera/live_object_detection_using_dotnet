using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace UI.Infrastructure;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Adds authentication services to the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationAuth(this IServiceCollection services, IConfiguration configuration)
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

    /// <summary>
    /// Configures authentication middleware in the application pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseApplicationAuth(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(pattern: "/signin",
                   async (HttpContext context) =>
                   {
                       AuthenticationProperties properties = new() { RedirectUri = "/" };
                       await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);

                       return Results.Empty;
                   });

        app.MapGet(pattern: "/signout",
                   async (HttpContext context) =>
                   {
                       await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                       return Results.Redirect(url: "/");
                   });

        return app;
    }
}