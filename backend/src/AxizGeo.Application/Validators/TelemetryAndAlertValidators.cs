using AxizGeo.Application.DTOs;
using FluentValidation;

namespace AxizGeo.Application.Validators;

public sealed class TelemetryBatchRequestValidator : AbstractValidator<TelemetryBatchRequest>
{
    public TelemetryBatchRequestValidator()
    {
        RuleFor(x => x.Positions)
            .NotNull().WithMessage("El lote de posiciones es obligatorio.")
            .Must(p => p is { Count: >= 1 and <= 500 })
            .WithMessage("Cada lote debe incluir entre 1 y 500 posiciones.");
    }
}

public sealed class ResolveAlertRequestValidator : AbstractValidator<ResolveAlertRequest>
{
    public ResolveAlertRequestValidator()
    {
        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage("El comentario de resolución es obligatorio.")
            .MinimumLength(5).WithMessage("El comentario debe tener al menos 5 caracteres.")
            .MaximumLength(500);
    }
}
