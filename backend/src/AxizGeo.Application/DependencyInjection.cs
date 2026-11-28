using AxizGeo.Application.Geofencing;
using AxizGeo.Application.Services;
using AxizGeo.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AxizGeo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        // Patrón Strategy: registro polimórfico de evaluadores espaciales
        services.AddSingleton<IGeofenceEvaluator, CircleGeofenceEvaluator>();
        services.AddSingleton<IGeofenceEvaluator, PolygonGeofenceEvaluator>();
        services.AddSingleton<GeofenceEvaluatorResolver>();
        services.AddSingleton<ICorridorEvaluator, CorridorEvaluator>();

        services.AddScoped<UnitService>();
        services.AddScoped<OperatorService>();
        services.AddScoped<RouteService>();
        services.AddScoped<GeofenceService>();
        services.AddScoped<TripService>();

        return services;
    }
}
