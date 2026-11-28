using AxizGeo.Application.DTOs;
using FluentValidation;

namespace AxizGeo.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es obligatorio.");
    }
}

public sealed class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.EconomicNumber)
            .NotEmpty().WithMessage("El número económico es obligatorio.")
            .MaximumLength(32);

        RuleFor(x => x.Plates)
            .NotEmpty().WithMessage("Las placas son obligatorias.")
            .MaximumLength(20);

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca es obligatoria.")
            .MaximumLength(64);

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo es obligatorio.")
            .MaximumLength(64);

        RuleFor(x => x.Year)
            .InclusiveBetween(1990, 2035).WithMessage("El año del vehículo debe estar entre 1990 y 2035.");

        RuleFor(x => x.HeightMeters)
            .InclusiveBetween(2.0, 6.0)
            .When(x => x.HeightMeters.HasValue)
            .WithMessage("La altura debe estar entre 2.0 y 6.0 metros.");

        RuleFor(x => x.WeightTons)
            .InclusiveBetween(1.0, 80.0)
            .When(x => x.WeightTons.HasValue)
            .WithMessage("El peso debe estar entre 1.0 y 80.0 toneladas.");
    }
}

public sealed class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.EconomicNumber).NotEmpty().WithMessage("El número económico es obligatorio.").MaximumLength(32);
        RuleFor(x => x.Plates).NotEmpty().WithMessage("Las placas son obligatorias.").MaximumLength(20);
        RuleFor(x => x.Brand).NotEmpty().WithMessage("La marca es obligatoria.").MaximumLength(64);
        RuleFor(x => x.Model).NotEmpty().WithMessage("El modelo es obligatorio.").MaximumLength(64);
        RuleFor(x => x.Year).InclusiveBetween(1990, 2035);
        RuleFor(x => x.HeightMeters).InclusiveBetween(2.0, 6.0).When(x => x.HeightMeters.HasValue);
        RuleFor(x => x.WeightTons).InclusiveBetween(1.0, 80.0).When(x => x.WeightTons.HasValue);
    }
}

public sealed class CreateOperatorRequestValidator : AbstractValidator<CreateOperatorRequest>
{
    public CreateOperatorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(150);

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("El número de licencia es obligatorio.")
            .MaximumLength(40);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .MaximumLength(30);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña inicial es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe incluir al menos una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe incluir al menos una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe incluir al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe incluir al menos un carácter especial.");
    }
}

public sealed class UpdateOperatorRequestValidator : AbstractValidator<UpdateOperatorRequest>
{
    public UpdateOperatorRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("El nombre completo es obligatorio.").MaximumLength(150);
        RuleFor(x => x.LicenseNumber).NotEmpty().WithMessage("El número de licencia es obligatorio.").MaximumLength(40);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("El teléfono es obligatorio.").MaximumLength(30);
    }
}
