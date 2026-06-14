using RestaurantReservation.Models;

namespace RestaurantReservation.Repositories.Interfaces;

public interface ITableRepository : IGenericRepository<RestaurantTable>
{
    Task<RestaurantTable?> GetByTableNumberAsync(int tableNumber, CancellationToken ct = default);

    /// <summary>Active tables that can seat at least <paramref name="partySize"/>, smallest first.</summary>
    Task<IReadOnlyList<RestaurantTable>> GetCandidateTablesAsync(int partySize, string? location, CancellationToken ct = default);
}
