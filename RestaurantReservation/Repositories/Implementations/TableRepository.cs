using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Data;
using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Interfaces;

namespace RestaurantReservation.Repositories.Implementations;

public class TableRepository : GenericRepository<RestaurantTable>, ITableRepository
{
    public TableRepository(AppDbContext context) : base(context) { }

    public async Task<RestaurantTable?> GetByTableNumberAsync(int tableNumber, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(t => t.TableNumber == tableNumber, ct);

    public async Task<IReadOnlyList<RestaurantTable>> GetCandidateTablesAsync(
        int partySize, string? location, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking()
            .Where(t => t.IsActive && t.Capacity >= partySize);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(t => t.Location == location);

        // Smallest suitable table first => best capacity utilisation.
        return await query.OrderBy(t => t.Capacity).ThenBy(t => t.TableNumber).ToListAsync(ct);
    }
}
