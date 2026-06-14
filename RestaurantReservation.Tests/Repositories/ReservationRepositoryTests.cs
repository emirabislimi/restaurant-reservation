using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Repositories.Implementations;
using Xunit;

namespace RestaurantReservation.Tests.Repositories;

public class ReservationRepositoryTests
{
    private static Reservation Booking(int tableId, DateTime start, int minutes, ReservationStatus status)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TableId = tableId,
            ReservationStartUtc = start,
            ReservationEndUtc = start.AddMinutes(minutes),
            PartySize = 2,
            Status = status
        };

    [Fact]
    public async Task HasOverlapping_ReturnsTrue_WhenWindowsOverlap()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        var start = new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc);
        ctx.Reservations.Add(Booking(1, start, 90, ReservationStatus.Confirmed));
        await ctx.SaveChangesAsync();
        var repo = new ReservationRepository(ctx);

        // New booking starts 30 min into the existing one -> overlap.
        var result = await repo.HasOverlappingReservationAsync(1, start.AddMinutes(30), start.AddMinutes(120));

        Assert.True(result);
    }

    [Fact]
    public async Task HasOverlapping_ReturnsFalse_WhenAdjacentButNotOverlapping()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        var start = new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc);
        ctx.Reservations.Add(Booking(1, start, 90, ReservationStatus.Confirmed));
        await ctx.SaveChangesAsync();
        var repo = new ReservationRepository(ctx);

        // New booking starts exactly when the previous ends -> no overlap.
        var result = await repo.HasOverlappingReservationAsync(1, start.AddMinutes(90), start.AddMinutes(180));

        Assert.False(result);
    }

    [Fact]
    public async Task HasOverlapping_IgnoresCancelledReservations()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        var start = new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc);
        ctx.Reservations.Add(Booking(1, start, 90, ReservationStatus.Cancelled));
        await ctx.SaveChangesAsync();
        var repo = new ReservationRepository(ctx);

        var result = await repo.HasOverlappingReservationAsync(1, start, start.AddMinutes(90));

        Assert.False(result);
    }

    [Fact]
    public async Task HasOverlapping_ExcludesGivenReservationId()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        var start = new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc);
        var existing = Booking(1, start, 90, ReservationStatus.Confirmed);
        ctx.Reservations.Add(existing);
        await ctx.SaveChangesAsync();
        var repo = new ReservationRepository(ctx);

        // Excluding the only overlapping booking -> reports no overlap.
        var result = await repo.HasOverlappingReservationAsync(1, start, start.AddMinutes(90), existing.Id);

        Assert.False(result);
    }
}
