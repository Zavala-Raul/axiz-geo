namespace AxizGeo.Application.DTOs;

public sealed record UnitDto(
    Guid Id,
    string EconomicNumber,
    string Plates,
    string Brand,
    string Model,
    int Year,
    double? HeightMeters,
    double? WeightTons,
    bool IsActive);

public sealed record CreateUnitRequest(
    string EconomicNumber,
    string Plates,
    string Brand,
    string Model,
    int Year,
    double? HeightMeters,
    double? WeightTons);

public sealed record UpdateUnitRequest(
    string EconomicNumber,
    string Plates,
    string Brand,
    string Model,
    int Year,
    double? HeightMeters,
    double? WeightTons,
    bool IsActive);

public sealed record OperatorDto(
    Guid Id,
    string FullName,
    string LicenseNumber,
    string Phone,
    string Email,
    Guid UserId,
    bool IsActive);

public sealed record CreateOperatorRequest(
    string FullName,
    string LicenseNumber,
    string Phone,
    string Email,
    string Password);

public sealed record UpdateOperatorRequest(
    string FullName,
    string LicenseNumber,
    string Phone,
    bool IsActive);
