using RestaurantReservation.Models.DTOs.Reservations;
using RestaurantReservation.Models.Enums;

namespace RestaurantReservation.Services.Interfaces;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReservationDto>> GetMineAsync(Guid userId, CancellationToken ct = default);
    Task<ReservationDto> GetByIdAsync(Guid id, Guid requesterId, UserRole requesterRole, CancellationToken ct = default);

    /// <summary>Business logic: reserve a specific table with full validation.</summary>
    Task<ReservationDto> CreateAsync(Guid userId, CreateReservationDto dto, CancellationToken ct = default);

    /// <summary>Complex business logic: find the best free table and reserve it.</summary>
    Task<ReservationDto> AutoReserveAsync(Guid userId, AutoReserveDto dto, CancellationToken ct = default);

    /// <summary>Business logic: cancel respecting ownership and timing rules.</summary>
    Task<ReservationDto> CancelAsync(Guid id, Guid requesterId, UserRole requesterRole, CancellationToken ct = default);

    /// <summary>Admin-only status transition with validation.</summary>
    Task<ReservationDto> UpdateStatusAsync(Guid id, ReservationStatus status, CancellationToken ct = default);
}
