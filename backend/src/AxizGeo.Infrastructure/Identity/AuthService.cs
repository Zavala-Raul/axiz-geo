using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AxizGeo.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IOperatorRepository operatorRepository,
    IJwtTokenGenerator tokenGenerator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
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
        var (token, expiresAt) = tokenGenerator.GenerateToken(
            userDto.Id,
            userDto.Email,
            userDto.FullName,
            userDto.Role,
            userDto.OperatorId);

        return new LoginResponse(token, expiresAt, userDto);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException("Sesión no válida o usuario inactivo.");

        return await BuildUserDtoAsync(user, ct);
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
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IIdentityOperatorProvisioner
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
    }
}
