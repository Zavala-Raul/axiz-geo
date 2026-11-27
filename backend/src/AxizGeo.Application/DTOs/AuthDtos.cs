using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    Role Role,
    Guid? OperatorId);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    CurrentUserDto User);
