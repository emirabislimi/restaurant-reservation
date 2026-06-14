using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Data;
using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Repositories.Interfaces;

namespace RestaurantReservation.Repositories.Implementations;

public class ReservationRepository : GenericRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(AppDbContext context) : base(context) { }

    public async Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Reservation>> GetAllWithDetailsAsync(CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Table)
            .OrderByDescending(r => r.ReservationStartUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Reservation>> GetForUserAsync(Guid userId, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Include(r => r.Table)
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReservationStartUtc)
            .ToListAsync(ct);

    public async Task<bool> HasOverlappingReservationAsync(int tableId, DateTime startUtc, DateTime endUtc,
        Guid? excludeReservationId = null, CancellationToken ct = default)
    {
        // Two windows overlap when start < otherEnd AND end > otherStart.
        return await Set.AnyAsync(r =>
            r.TableId == tableId &&
            r.Status != ReservationStatus.Cancelled &&
            (excludeReservationId == null || r.Id != excludeReservationId) &&
            startUtc < r.ReservationEndUtc &&
            endUtc > r.ReservationStartUtc,
            ct);
    }
}
