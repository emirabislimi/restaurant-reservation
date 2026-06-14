using System.Linq.Expressions;

namespace RestaurantReservation.Repositories.Interfaces;

/// <summary>
/// Generic data-access abstraction. Concrete implementations are injected via DI,
/// keeping services free of any persistence concerns (separation of concerns).
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
