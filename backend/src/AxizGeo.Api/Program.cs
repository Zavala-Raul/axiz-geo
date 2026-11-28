using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AxizGeo.Api.BackgroundJobs;
using AxizGeo.Api.Hubs;
using AxizGeo.Api.Infrastructure;
using AxizGeo.Api.Realtime;
using AxizGeo.Application;
using AxizGeo.Application.Abstractions;
using AxizGeo.Infrastructure;
using AxizGeo.Infrastructure.Identity;
using AxizGeo.Infrastructure.Persistence;
using AxizGeo.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<IFleetNotifier, SignalRFleetNotifier>();
builder.Services.AddHostedService<NoSignalMonitorBackgroundService>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtIssuer = jwtSection["Issuer"] ?? "axiz-geo-api";
var jwtAudience = jwtSection["Audience"] ?? "axiz-geo-web";
var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Debe configurar la variable 'Jwt:Key' en entornos productivos.");

    jwtKey = "dev-only-fallback-key-replace-in-user-secrets-2026!";
    builder.Configuration["Jwt:Key"] = jwtKey;
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "name"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/fleet"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var allowedOrigins = (builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"])
    .Select(o => o.TrimEnd('/'))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

var vercelPreviewPrefix = builder.Configuration["Cors:VercelPreviewPrefix"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  var normalized = origin.TrimEnd('/');
                  if (allowedOrigins.Contains(normalized))
                      return true;

                  if (!string.IsNullOrWhiteSpace(vercelPreviewPrefix) &&
                      normalized.StartsWith(vercelPreviewPrefix, StringComparison.OrdinalIgnoreCase) &&
                      normalized.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                  {
                      return true;
                  }

                  return false;
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await DbSeeder.SeedAsync(app.Services, app.Configuration);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("FrontendPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db, IHostEnvironment env, CancellationToken ct) =>
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(2));

    var dbHealthy = false;
    try
    {
        dbHealthy = await db.Database.CanConnectAsync(cts.Token);
    }
    catch
    {
        dbHealthy = false;
    }

    if (!dbHealthy && !env.IsDevelopment())
    {
        return Results.Json(
            new { status = "Unhealthy", database = "Unreachable", service = "axiz-geo-api" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "Healthy",
        database = dbHealthy ? "Healthy" : "Disconnected",
        service = "axiz-geo-api"
    });
}).AllowAnonymous();

app.MapHub<FleetHub>("/hubs/fleet");
app.MapControllers();

app.Run();
