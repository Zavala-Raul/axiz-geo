using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Enums;
using FluentValidation;

namespace AxizGeo.Application.Validators;

public sealed class PreviewRouteRequestValidator : AbstractValidator<PreviewRouteRequest>
{
    public PreviewRouteRequestValidator()
    {
        RuleFor(x => x.Waypoints)
            .NotNull().WithMessage("Los waypoints son obligatorios.")
            .Must(w => w is { Count: >= 2 and <= 50 })
            .WithMessage("Debe especificar entre 2 y 50 waypoints.");
    }
}

public sealed class UpsertRouteRequestValidator : AbstractValidator<UpsertRouteRequest>
{
    public UpsertRouteRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la ruta es obligatorio.")
            .MaximumLength(160);

        RuleFor(x => x.CorridorMeters)
            .InclusiveBetween(25, 2000)
            .WithMessage("El margen del corredor debe estar entre 25 y 2000 metros.");

        RuleFor(x => x.Waypoints)
            .NotNull()
            .Must(w => w is { Count: >= 2 and <= 50 })
            .WithMessage("La ruta debe incluir entre 2 y 50 waypoints.");

        RuleFor(x => x.Path)
            .NotNull().WithMessage("El trazo ('path') de la ruta es obligatorio.")
            .Must(p => p?.Coordinates is { Length: >= 2 })
            .WithMessage("El trazo ('path') debe tener al menos 2 coordenadas.");
    }
}

public sealed class UpsertGeofenceRequestValidator : AbstractValidator<UpsertGeofenceRequest>
{
    public UpsertGeofenceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la geocerca es obligatorio.")
            .MaximumLength(160);

        RuleFor(x => x.RadiusMeters)
            .NotNull().WithMessage("El radio en metros es obligatorio para geocercas circulares.")
            .InclusiveBetween(10, 20000).WithMessage("El radio debe estar entre 10 y 20000 metros.")
            .When(x => x.Type == GeofenceType.Circle);

        RuleFor(x => x.Color)
            .MaximumLength(16)
            .When(x => !string.IsNullOrWhiteSpace(x.Color));
    }
}

public sealed class CreateTripRequestValidator : AbstractValidator<CreateTripRequest>
{
    public CreateTripRequestValidator()
    {
        RuleFor(x => x.UnitId).NotEmpty().WithMessage("La unidad es obligatoria.");
        RuleFor(x => x.OperatorId).NotEmpty().WithMessage("El operador es obligatorio.");
        RuleFor(x => x.RouteId).NotEmpty().WithMessage("La ruta autorizada es obligatoria.");
        RuleFor(x => x.ScheduledStart).NotEmpty().WithMessage("La fecha programada de inicio es obligatoria.");
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
