using AutoMapper;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Tables;
using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Interfaces;

namespace RestaurantReservation.Services.Implementations;

public class TableService : ITableService
{
    private readonly ITableRepository _tables;
    private readonly IReservationRepository _reservations;
    private readonly IMapper _mapper;

    public TableService(ITableRepository tables, IReservationRepository reservations, IMapper mapper)
    {
        _tables = tables;
        _reservations = reservations;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TableDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tables = await _tables.GetAllAsync(ct);
        return tables.Select(_mapper.Map<TableDto>).ToList();
    }

    public async Task<TableDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var table = await _tables.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Table {id} was not found.");
        return _mapper.Map<TableDto>(table);
    }

    public async Task<TableDto> CreateAsync(CreateTableDto dto, CancellationToken ct = default)
    {
        // Business rule: table numbers must be unique.
        if (await _tables.ExistsAsync(t => t.TableNumber == dto.TableNumber, ct))
            throw new BusinessRuleException($"Table number {dto.TableNumber} already exists.");

        var table = _mapper.Map<RestaurantTable>(dto);
        await _tables.AddAsync(table, ct);
        await _tables.SaveChangesAsync(ct);
        return _mapper.Map<TableDto>(table);
    }

    public async Task<TableDto> UpdateAsync(int id, UpdateTableDto dto, CancellationToken ct = default)
    {
        var table = await _tables.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Table {id} was not found.");

        table.Capacity = dto.Capacity;
        table.Location = dto.Location;
        table.IsActive = dto.IsActive;

        _tables.Update(table);
        await _tables.SaveChangesAsync(ct);
        return _mapper.Map<TableDto>(table);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var table = await _tables.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Table {id} was not found.");

        // Business rule: don't delete a table that still has upcoming active reservations.
        var hasUpcoming = await _reservations.ExistsAsync(r =>
            r.TableId == id &&
            r.Status != ReservationStatus.Cancelled &&
            r.Status != ReservationStatus.Completed &&
            r.ReservationEndUtc > DateTime.UtcNow, ct);

        if (hasUpcoming)
            throw new BusinessRuleException(
                "This table has upcoming reservations and cannot be deleted. Deactivate it instead.");

        _tables.Remove(table);
        await _tables.SaveChangesAsync(ct);
    }
}
