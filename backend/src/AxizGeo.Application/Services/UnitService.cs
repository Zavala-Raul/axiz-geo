using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using AxizGeo.Domain.Entities;
using FluentValidation;

namespace AxizGeo.Application.Services;

public sealed class UnitService(
    IUnitOfWork uow,
    IValidator<CreateUnitRequest> createValidator,
    IValidator<UpdateUnitRequest> updateValidator)
{
    public async Task<PagedResult<UnitDto>> ListAsync(
        string? search,
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await uow.Units.ListPagedAsync(search, includeInactive, page, pageSize, ct);
        return new PagedResult<UnitDto>(items.Select(MapToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<UnitDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var unit = await uow.Units.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la unidad con ID '{id}'.");

        return MapToDto(unit);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var normalizedNumber = request.EconomicNumber.Trim().ToUpperInvariant();
        if (await uow.Units.ExistsByEconomicNumberAsync(normalizedNumber, null, ct))
            throw new ConflictException($"Ya existe una unidad con el número económico '{normalizedNumber}'.");

        var unit = new Unit
        {
            EconomicNumber = normalizedNumber,
            Plates = request.Plates.Trim().ToUpperInvariant(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            HeightMeters = request.HeightMeters,
            WeightTons = request.WeightTons,
            IsActive = true
        };

        await uow.Units.AddAsync(unit, ct);
        await uow.SaveChangesAsync(ct);

        return MapToDto(unit);
    }

    public async Task<UnitDto> UpdateAsync(Guid id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var unit = await uow.Units.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la unidad con ID '{id}'.");

        var normalizedNumber = request.EconomicNumber.Trim().ToUpperInvariant();
        if (await uow.Units.ExistsByEconomicNumberAsync(normalizedNumber, id, ct))
            throw new ConflictException($"Ya existe otra unidad con el número económico '{normalizedNumber}'.");

        if (!request.IsActive && unit.IsActive && await uow.Units.HasActiveTripAsync(id, ct))
            throw new ConflictException("No se puede desactivar una unidad que tiene un viaje en curso.");

        unit.EconomicNumber = normalizedNumber;
        unit.Plates = request.Plates.Trim().ToUpperInvariant();
        unit.Brand = request.Brand.Trim();
        unit.Model = request.Model.Trim();
        unit.Year = request.Year;
        unit.HeightMeters = request.HeightMeters;
        unit.WeightTons = request.WeightTons;
        unit.IsActive = request.IsActive;

        uow.Units.Update(unit);
        await uow.SaveChangesAsync(ct);

        return MapToDto(unit);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var unit = await uow.Units.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"No se encontró la unidad con ID '{id}'.");

        if (await uow.Units.HasActiveTripAsync(id, ct))
            throw new ConflictException("No se puede dar de baja una unidad que tiene un viaje en curso.");

        unit.IsActive = false;
        uow.Units.Update(unit);
        await uow.SaveChangesAsync(ct);
    }

    private static UnitDto MapToDto(Unit u) => new(
        u.Id,
        u.EconomicNumber,
        u.Plates,
        u.Brand,
        u.Model,
        u.Year,
        u.HeightMeters,
        u.WeightTons,
        u.IsActive);
}
