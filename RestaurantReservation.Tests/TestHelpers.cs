using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Data;
using RestaurantReservation.Services;

namespace RestaurantReservation.Tests;

/// <summary>Shared helpers for building an in-memory DbContext and a real AutoMapper instance.</summary>
public static class TestHelpers
{
    public static AppDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options);
    }

    public static IMapper NewMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }
}
