using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using MQTTManager.DB;
using MudBlazor.Services;
using Serilog.Events;
using Serilog;
using MudBlazor;
using MQTTManager.Services;
using System.Collections.Concurrent;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;

    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});


// Configure Serilog
var logLevel = LogEventLevel.Information;

if (builder.Environment.IsDevelopment())
{
    logLevel = LogEventLevel.Debug;
}
var logEvents = new ConcurrentQueue<LogEvent>();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Sink(new InMemorySink(logEvents))
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .MinimumLevel.Is(logLevel)
    .CreateLogger();

// Add the same logEvents instance to the DI container
builder.Services.AddSingleton(logEvents);

builder.Services.AddScoped<IBrokerConfigurationService, BrokerConfigurationService>();
builder.Services.AddScoped<IAuthorizationConfService, AuthorizationConfService>();
builder.Services.AddSingleton<IMqttBrokerService, MqttBrokerService>();
builder.Services.AddSingleton<AppState>();

builder.Host.UseSerilog();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();