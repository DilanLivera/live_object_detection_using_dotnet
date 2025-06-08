using Serilog;
using UI;
using UI.Features.FileCleanup;
using UI.Features.ObjectDetection;
using UI.Features.VideoUpload;
using UI.Infrastructure;
using UI.Pages;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
                                                              .ReadFrom.Configuration(context.Configuration)
                                                              .ReadFrom.Services(services));

builder.Services
       .AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddSingleton<ObjectDetector>();
builder.Services.AddSingleton<VideoFileValidationService>();
builder.Services.AddSingleton<VideoUploadOrchestrator>();
builder.Services.AddHostedService<FileCleanupService>();


builder.Services.AddHttpContextAccessor();

builder.Services.AddProblemDetails();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.MapVideoEndpoints();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();