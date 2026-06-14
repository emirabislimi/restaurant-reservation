using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Data;
using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Interfaces;

namespace RestaurantReservation.Repositories.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(u => u.Email == email, ct);
}
