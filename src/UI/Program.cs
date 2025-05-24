using UI.Components;
using Serilog;
using UI.Components.Pages.Upload;
using UI.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
                                                              .ReadFrom.Configuration(context.Configuration)
                                                              .ReadFrom.Services(services));

builder.Services
       .AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddSingleton<UploadedVideoProcessor>();

builder.Services.AddHttpContextAccessor();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorHandlingPath: "/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.UseApplicationAuth();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();