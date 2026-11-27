using System.IdentityModel.Tokens.Jwt;
using AxizGeo.Application.DTOs;
using AxizGeo.Application.Validators;
using AxizGeo.Domain.Enums;
using AxizGeo.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace AxizGeo.Tests;

public class AuthAndCatalogTests
{
    [Fact]
    public void JwtTokenGenerator_EmbedsUserRoleAndOperatorClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "axiz-geo-api",
            Audience = "axiz-geo-web",
            Key = "unit-test-secret-key-with-at-least-32-bytes!!",
            ExpirationMinutes = 60
        });

        var generator = new JwtTokenGenerator(options, TimeProvider.System);
        var userId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var (token, expiresAt) = generator.GenerateToken(
            userId,
            "operador1@axiz.demo",
            "Operador Prueba",
            Role.Operator,
            operatorId);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.ToString(), parsed.Subject);
        Assert.Contains(parsed.Claims, c => c.Type == "role" && c.Value == "Operator");
        Assert.Contains(parsed.Claims, c => c.Type == "operatorId" && c.Value == operatorId.ToString());
    }

    [Fact]
    public void CreateUnitValidator_RejectsOutOfRangeDimensions()
    {
        var validator = new CreateUnitRequestValidator();
        var request = new CreateUnitRequest(
            EconomicNumber: "T-900",
            Plates: "AA-111-B",
            Brand: "Kenworth",
            Model: "T680",
            Year: 2024,
            HeightMeters: 9.5,
            WeightTons: 120);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUnitRequest.HeightMeters));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUnitRequest.WeightTons));
    }

    [Fact]
    public void CreateOperatorValidator_RequiresStrongPassword()
    {
        var validator = new CreateOperatorRequestValidator();
        var request = new CreateOperatorRequest(
            FullName: "Operador Demo",
            LicenseNumber: "FED-12345",
            Phone: "+52 867 000 0000",
            Email: "op@axiz.demo",
            Password: "weakpassword");

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOperatorRequest.Password));
    }
}
