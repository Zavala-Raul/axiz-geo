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
        services.AddScoped<UnitService>();
        services.AddScoped<OperatorService>();
        return services;
    }
}
