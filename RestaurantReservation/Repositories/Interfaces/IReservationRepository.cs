using RestaurantReservation.Models;

namespace RestaurantReservation.Repositories.Interfaces;

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetAllWithDetailsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the given table already has a non-cancelled reservation
    /// overlapping the requested window. Core double-booking guard.
    /// </summary>
    Task<bool> HasOverlappingReservationAsync(int tableId, DateTime startUtc, DateTime endUtc,
        Guid? excludeReservationId = null, CancellationToken ct = default);
}
