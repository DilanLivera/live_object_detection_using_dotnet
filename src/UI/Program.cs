using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

using UI;
using UI.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ObjectDetector>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        builder.Configuration.Bind(key: "Authentication:Google", options);

        if (string.IsNullOrEmpty(options.ClientId))
        {
            throw new InvalidOperationException("Google ClientId not found.");
        }

        if (string.IsNullOrEmpty(options.ClientSecret))
        {
            throw new InvalidOperationException("Google ClientSecret not found.");
        }
    });

builder.Services.AddAuthorizationCore();

WebApplication? app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    pattern: "/signin",
    async (HttpContext context) =>
    {
        AuthenticationProperties properties = new() { RedirectUri = "/" };
        await context.ChallengeAsync(
            GoogleDefaults.AuthenticationScheme,
            properties);

        return Results.Empty;
    });

app.MapGet(
    pattern: "/signout",
    async (HttpContext context) =>
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Redirect(url: "/");
    });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();