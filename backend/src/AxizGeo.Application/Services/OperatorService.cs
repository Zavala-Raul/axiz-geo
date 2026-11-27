using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Entities;
using FluentValidation;

namespace AxizGeo.Application.Services;

public sealed class OperatorService(
    IUnitOfWork uow,
    IIdentityOperatorProvisioner identityProvisioner,
    IValidator<CreateOperatorRequest> createValidator,
    IValidator<UpdateOperatorRequest> updateValidator)
{
    public async Task<PagedResult<OperatorDto>> ListAsync(
        string? search,
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await uow.Operators.ListPagedAsync(search, includeInactive, page, pageSize, ct);
        return new PagedResult<OperatorDto>(items.Select(MapToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<OperatorDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var op = await uow.Operators.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el operador con ID '{id}'.");

        return MapToDto(op);
    }

    public async Task<OperatorDto> CreateAsync(CreateOperatorRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var normalizedLicense = request.LicenseNumber.Trim().ToUpperInvariant();
        if (await uow.Operators.ExistsByLicenseAsync(normalizedLicense, null, ct))
            throw new ConflictException($"Ya existe un operador con la licencia '{normalizedLicense}'.");

        var email = request.Email.Trim().ToLowerInvariant();
        var userId = await identityProvisioner.CreateOperatorUserAsync(email, request.FullName.Trim(), request.Password, ct);

        var op = new Operator
        {
            UserId = userId,
            FullName = request.FullName.Trim(),
            LicenseNumber = normalizedLicense,
            Phone = request.Phone.Trim(),
            Email = email,
            IsActive = true
        };

        await uow.Operators.AddAsync(op, ct);
        await uow.SaveChangesAsync(ct);

        return MapToDto(op);
    }

    public async Task<OperatorDto> UpdateAsync(Guid id, UpdateOperatorRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var op = await uow.Operators.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el operador con ID '{id}'.");

        var normalizedLicense = request.LicenseNumber.Trim().ToUpperInvariant();
        if (await uow.Operators.ExistsByLicenseAsync(normalizedLicense, id, ct))
            throw new ConflictException($"Ya existe otro operador con la licencia '{normalizedLicense}'.");

        if (!request.IsActive && op.IsActive && await uow.Operators.HasActiveTripAsync(id, ct))
            throw new ConflictException("No se puede desactivar un operador que tiene un viaje en curso.");

        op.FullName = request.FullName.Trim();
        op.LicenseNumber = normalizedLicense;
        op.Phone = request.Phone.Trim();
        op.IsActive = request.IsActive;

        await identityProvisioner.SyncOperatorUserAsync(op.UserId, op.FullName, op.IsActive, ct);

        uow.Operators.Update(op);
        await uow.SaveChangesAsync(ct);

        return MapToDto(op);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var op = await uow.Operators.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró el operador con ID '{id}'.");

        if (await uow.Operators.HasActiveTripAsync(id, ct))
            throw new ConflictException("No se puede dar de baja un operador que tiene un viaje en curso.");

        op.IsActive = false;
        await identityProvisioner.SyncOperatorUserAsync(op.UserId, op.FullName, false, ct);

        uow.Operators.Update(op);
        await uow.SaveChangesAsync(ct);
    }

    private static OperatorDto MapToDto(Operator o) => new(
        o.Id,
        o.FullName,
        o.LicenseNumber,
        o.Phone,
        o.Email,
        o.UserId,
        o.IsActive);
}
