using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Enums;

namespace AxizGeo.Application.Abstractions;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(
        Guid userId,
        string email,
        string fullName,
        Role role,
        Guid? operatorId = null);
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default);
    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IIdentityOperatorProvisioner
{
    Task<Guid> CreateOperatorUserAsync(string email, string fullName, string password, CancellationToken ct = default);
    Task SyncOperatorUserAsync(Guid userId, string fullName, bool isActive, CancellationToken ct = default);
}
