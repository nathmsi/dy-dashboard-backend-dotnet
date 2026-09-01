using System.Threading.RateLimiting;
using DyDashboard.Api.Common.Middleware;
using DyDashboard.Api.Common.Validation;
using DyDashboard.Api.Configuration;
using DyDashboard.Api.Data;
using DyDashboard.Api.Features.Campaigns;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Hosts like Render inject the listening port via $PORT; honour it if the URLs
// were not otherwise configured (mirrors the Node server reading process.env.PORT).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) &&
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// --- Configuration (validated on start; fails fast on bad values) ------------
builder.Services
    .AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var apiOptions = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();
var isTest = builder.Environment.EnvironmentName == "Test";

// --- Structured logging (Serilog: JSON in prod, pretty in dev, quiet in test) -
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Is(isTest ? LogEventLevel.Fatal : LogEventLevel.Information)
       .Enrich.FromLogContext();
    if (ctx.HostingEnvironment.IsProduction())
        cfg.WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    else
        cfg.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// --- Persistence -------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={apiOptions.DatabasePath}"));

// --- Feature services --------------------------------------------------------
builder.Services.AddScoped<CampaignRepository>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCampaignRequestValidator>();

// --- Cross-cutting -----------------------------------------------------------
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

// Surface malformed request bodies as thrown exceptions so the central handler
// renders them as a 400 BAD_REQUEST envelope instead of an empty framework 400.
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(apiOptions.CorsOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .WithExposedHeaders("X-Total-Count", "X-Total-Pages", "X-Page", "X-Limit", "Link", "Location")));

// Rate limit only the API surface (health checks stay untouched).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = apiOptions.RateLimitMax,
                Window = TimeSpan.FromMilliseconds(apiOptions.RateLimitWindowMs),
                QueueLimit = 0,
            }));
});

// OpenAPI document + Swagger UI (spec at /api/openapi.json, UI at /api/docs).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("openapi", new() { Title = "DY Dashboard API", Version = "1.0.0" }));

// Behind Render's single reverse proxy: honour X-Forwarded-* for client IPs
// (rate limiting) and the scheme (absolute URLs in the Link header).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// --- Migrate + seed on boot (skipped in tests, which own their DB lifecycle) --
if (!isTest)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedIfEmptyAsync(db);
}

// --- Pipeline ----------------------------------------------------------------
app.UseForwardedHeaders();
app.UseExceptionHandler();
if (!isTest) app.UseSerilogRequestLogging();
app.UseCors();

// Health check — deliberately not rate-limited so uptime probes never trip it.
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    uptime = (DateTime.UtcNow - StartedAt).TotalSeconds,
}));

// API documentation: raw spec at /api/openapi.json, Swagger UI at /api/docs.
app.UseSwagger(c => c.RouteTemplate = "api/{documentName}.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api/openapi.json", "DY Dashboard API v1");
    c.RoutePrefix = "api/docs";
});

app.UseRateLimiter();

// Versioned API (canonical) + deprecated unversioned alias for the current client.
app.MapGroup("/api/v1/campaigns").MapCampaignEndpoints().RequireRateLimiting("api");
app.MapGroup("/api/campaigns").MapCampaignEndpoints().RequireRateLimiting("api");

app.Run();

public partial class Program
{
    internal static readonly DateTime StartedAt = DateTime.UtcNow;
}
