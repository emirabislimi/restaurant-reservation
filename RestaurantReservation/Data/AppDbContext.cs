using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Models;

namespace RestaurantReservation.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<RestaurantTable>(e =>
        {
            e.HasIndex(t => t.TableNumber).IsUnique();
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            // Helps the overlap/availability queries.
            e.HasIndex(r => new { r.TableId, r.ReservationStartUtc, r.ReservationEndUtc });
        });
    }
}
