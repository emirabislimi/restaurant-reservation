using RestaurantReservation.Models.DTOs.Tables;

namespace RestaurantReservation.Services.Interfaces;

public interface ITableService
{
    Task<IReadOnlyList<TableDto>> GetAllAsync(CancellationToken ct = default);
    Task<TableDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TableDto> CreateAsync(CreateTableDto dto, CancellationToken ct = default);
    Task<TableDto> UpdateAsync(int id, UpdateTableDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
