using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace UI;

public static class AuthEndpoints
{
    /// <summary>
    /// Configures authentication middleware in the application pipeline and sets up sign-in/sign-out endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet(pattern: "/signin",
                   async (HttpContext context) =>
                   {
                       AuthenticationProperties properties = new()
                                                             {
                                                                 RedirectUri = "/"
                                                             };
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