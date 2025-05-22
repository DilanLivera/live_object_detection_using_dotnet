using UI.Components;
using UI.Infrastructure;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Serilog;
using UI.Infrastructure.UploadedVideoProcessing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
                                                              .ReadFrom.Configuration(context.Configuration)
                                                              .ReadFrom.Services(services));

builder.Services.AddHttpClient();

builder.Services
       .AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();

builder.Services.AddObjectDetection(builder.Configuration);

builder.Services.AddUploadedVideoProcessing();

builder.Services.AddApplicationAuth(builder.Configuration);

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