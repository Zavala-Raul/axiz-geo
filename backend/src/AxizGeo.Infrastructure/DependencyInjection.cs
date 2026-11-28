using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Rules;
using AxizGeo.Infrastructure.Identity;
using AxizGeo.Infrastructure.Notifications;
using AxizGeo.Infrastructure.Persistence;
using AxizGeo.Infrastructure.Repositories;
using AxizGeo.Infrastructure.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AxizGeo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=axiz_geo;Username=axiz_user;Password=axiz_dev_password_change_me";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OrsOptions>(configuration.GetSection(OrsOptions.SectionName));
        services.Configure<TelemetryRulesOptions>(configuration.GetSection(TelemetryRulesOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIdentityOperatorProvisioner, IdentityOperatorProvisioner>();
        services.AddScoped<IFleetNotifier, LoggingFleetNotifier>();

        var orsBaseUrl = configuration[$"{OrsOptions.SectionName}:BaseUrl"]
                         ?? "https://api.heigit.org/openrouteservice/";
        if (!orsBaseUrl.EndsWith('/'))
            orsBaseUrl += "/";

        services.AddHttpClient<IRoutingProvider, OpenRouteServiceAdapter>(client =>
            {
                client.BaseAddress = new Uri(orsBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .AddStandardResilienceHandler();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IOperatorRepository, OperatorRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IGeofenceRepository, GeofenceRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IFleetReadRepository, FleetReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
