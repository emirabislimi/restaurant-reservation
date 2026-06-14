using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;

namespace RestaurantReservation.Data;

/// <summary>Applies migrations and seeds a default admin and sample tables.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IConfiguration config)
    {
        // If EF Core migrations have been generated (recommended for Azure/CI),
        // apply them. Otherwise create the schema directly so the app still runs
        // out of the box during local development.
        if (context.Database.GetMigrations().Any())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        if (!await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var hasher = new PasswordHasher<User>();
            var adminEmail = config["Seed:AdminEmail"] ?? "admin@restaurant.local";
            var adminPassword = config["Seed:AdminPassword"] ?? "Admin123!";

            var admin = new User
            {
                FullName = "System Administrator",
                Email = adminEmail.ToLowerInvariant(),
                Role = UserRole.Admin
            };
            admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
            context.Users.Add(admin);
        }

        if (!await context.Tables.AnyAsync())
        {
            context.Tables.AddRange(
                new RestaurantTable { TableNumber = 1, Capacity = 2, Location = "Indoor" },
                new RestaurantTable { TableNumber = 2, Capacity = 2, Location = "Patio" },
                new RestaurantTable { TableNumber = 3, Capacity = 4, Location = "Indoor" },
                new RestaurantTable { TableNumber = 4, Capacity = 4, Location = "Patio" },
                new RestaurantTable { TableNumber = 5, Capacity = 6, Location = "Indoor" },
                new RestaurantTable { TableNumber = 6, Capacity = 8, Location = "Private Room" }
            );
        }

        await context.SaveChangesAsync();
    }
}
