using System.Security.Cryptography;
using System.Text;
using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Enums;
using AxizGeo.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AxizGeo.Infrastructure.Identity;

public sealed class AuthService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IOperatorRepository operatorRepository,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider,
    IOptions<JwtOptions> jwtOptions,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshValidator) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException("Credenciales inválidas.");

        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAppException("La cuenta está bloqueada temporalmente por intentos fallidos.");

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedAppException("Credenciales inválidas.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var userDto = await BuildUserDtoAsync(user, ct);
        var (accessToken, expiresAt) = tokenGenerator.GenerateToken(
            userDto.Id,
            userDto.Email,
            userDto.FullName,
            userDto.Role,
            userDto.OperatorId);

        var (rawRefreshToken, refreshEntity) = CreateRefreshTokenEntity(user.Id);
        db.RefreshTokens.Add(refreshEntity);
        await db.SaveChangesAsync(ct);

        return new LoginResponse(accessToken, expiresAt, rawRefreshToken, userDto);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, ct);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var incomingHash = HashToken(request.RefreshToken.Trim());

        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash, ct);

        if (storedToken is null || !storedToken.IsActive(nowUtc))
            throw new UnauthorizedAppException("El token de renovación es inválido o ya expiró.");

        var user = storedToken.User;
        if (!user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            storedToken.RevokedAt = nowUtc;
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAppException("La cuenta de usuario se encuentra inactiva o bloqueada.");
        }

        var (newRawRefresh, newRefreshEntity) = CreateRefreshTokenEntity(user.Id);
        storedToken.RevokedAt = nowUtc;
        storedToken.ReplacedByTokenHash = newRefreshEntity.TokenHash;

        db.RefreshTokens.Add(newRefreshEntity);
        await db.SaveChangesAsync(ct);

        var userDto = await BuildUserDtoAsync(user, ct);
        var (accessToken, expiresAt) = tokenGenerator.GenerateToken(
            userDto.Id,
            userDto.Email,
            userDto.FullName,
            userDto.Role,
            userDto.OperatorId);

        return new LoginResponse(accessToken, expiresAt, newRawRefresh, userDto);
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = HashToken(refreshToken.Trim());
            var token = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == hash && rt.RevokedAt == null, ct);

            if (token is not null)
            {
                token.RevokedAt = nowUtc;
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > nowUtc)
            .ToListAsync(ct);

        if (activeTokens.Count > 0)
        {
            foreach (var token in activeTokens)
            {
                token.RevokedAt = nowUtc;
            }
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException("Sesión no válida o usuario inactivo.");

        return await BuildUserDtoAsync(user, ct);
    }

    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshTokenEntity(Guid userId)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(rawToken),
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.AddDays(Math.Max(1, _jwtOptions.RefreshExpirationDays))
        };

        return (rawToken, entity);
    }

    private async Task<CurrentUserDto> BuildUserDtoAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var roleString = roles.FirstOrDefault() ?? nameof(Role.Operator);
        if (!Enum.TryParse<Role>(roleString, ignoreCase: true, out var role))
            role = Role.Operator;

        Guid? operatorId = null;
        if (role == Role.Operator)
        {
            var op = await operatorRepository.GetByUserIdAsync(user.Id, ct);
            operatorId = op?.Id;
        }

        return new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            role,
            operatorId);
    }
}

public sealed class IdentityOperatorProvisioner(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    TimeProvider timeProvider) : IIdentityOperatorProvisioner
{
    public async Task<Guid> CreateOperatorUserAsync(string email, string fullName, string password, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new ConflictException($"Ya existe un usuario registrado con el correo '{email}'.");

        if (!await roleManager.RoleExistsAsync(nameof(Role.Operator)))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Role.Operator)));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var details = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new ConflictException($"No se pudo crear la cuenta de operador: {details}");
        }

        await userManager.AddToRoleAsync(user, nameof(Role.Operator));
        return user.Id;
    }

    public async Task SyncOperatorUserAsync(Guid userId, string fullName, bool isActive, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        user.FullName = fullName;
        user.IsActive = isActive;
        await userManager.UpdateAsync(user);

        if (!isActive)
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var activeTokens = await db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = nowUtc;
            }

            if (activeTokens.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
