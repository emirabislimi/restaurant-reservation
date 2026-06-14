using NSubstitute;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Reservations;
using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Implementations;
using Xunit;

namespace RestaurantReservation.Tests.Services;

public class ReservationServiceTests
{
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly ITableRepository _tables = Substitute.For<ITableRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ReservationService _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private static DateTime FutureStart => DateTime.UtcNow.AddDays(1);

    public ReservationServiceTests()
    {
        _sut = new ReservationService(_reservations, _tables, _users, TestHelpers.NewMapper());
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(true);
    }

    private RestaurantTable ActiveTable(int id = 1, int capacity = 4) =>
        new() { Id = id, TableNumber = id, Capacity = capacity, Location = "Indoor", IsActive = true };

    private void StubReload() =>
        _reservations.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new Reservation
            {
                Id = ci.Arg<Guid>(),
                UserId = _userId,
                TableId = 1,
                ReservationStartUtc = FutureStart,
                ReservationEndUtc = FutureStart.AddMinutes(90),
                PartySize = 2,
                Status = ReservationStatus.Confirmed,
                Table = ActiveTable(),
                User = new User { Id = _userId, FullName = "Jane" }
            });

    [Fact]
    public async Task Create_Throws_WhenTimeInPast()
    {
        var dto = new CreateReservationDto
        {
            TableId = 1, PartySize = 2, DurationMinutes = 90,
            ReservationStartUtc = DateTime.UtcNow.AddHours(-1)
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(_userId, dto));
    }

    [Fact]
    public async Task Create_Throws_WhenPartyExceedsCapacity()
    {
        _tables.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveTable(capacity: 2));
        var dto = new CreateReservationDto
        {
            TableId = 1, PartySize = 6, DurationMinutes = 90, ReservationStartUtc = FutureStart
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(_userId, dto));
    }

    [Fact]
    public async Task Create_Throws_WhenTableDoubleBooked()
    {
        _tables.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveTable());
        _reservations.HasOverlappingReservationAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new CreateReservationDto
        {
            TableId = 1, PartySize = 2, DurationMinutes = 90, ReservationStartUtc = FutureStart
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(_userId, dto));
    }

    [Fact]
    public async Task Create_Succeeds_AndPersists_WhenValid()
    {
        _tables.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveTable());
        _reservations.HasOverlappingReservationAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        StubReload();

        var dto = new CreateReservationDto
        {
            TableId = 1, PartySize = 2, DurationMinutes = 90, ReservationStartUtc = FutureStart
        };

        var result = await _sut.CreateAsync(_userId, dto);

        Assert.Equal("Confirmed", result.Status);
        await _reservations.Received(1).AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>());
        await _reservations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoReserve_PicksFirstFreeTable_AndBooksIt()
    {
        var small = ActiveTable(id: 1, capacity: 2);
        var medium = ActiveTable(id: 2, capacity: 4);
        _tables.GetCandidateTablesAsync(2, null, Arg.Any<CancellationToken>())
            .Returns(new List<RestaurantTable> { small, medium });

        // Smallest table is taken, medium is free.
        _reservations.HasOverlappingReservationAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        _reservations.HasOverlappingReservationAsync(2, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        StubReload();

        var dto = new AutoReserveDto { PartySize = 2, DurationMinutes = 90, ReservationStartUtc = FutureStart };

        var result = await _sut.AutoReserveAsync(_userId, dto);

        Assert.NotNull(result);
        await _reservations.Received(1).AddAsync(
            Arg.Is<Reservation>(r => r.TableId == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoReserve_Throws_WhenNoCandidateTables()
    {
        _tables.GetCandidateTablesAsync(20, null, Arg.Any<CancellationToken>())
            .Returns(new List<RestaurantTable>());

        var dto = new AutoReserveDto { PartySize = 20, DurationMinutes = 90, ReservationStartUtc = FutureStart };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AutoReserveAsync(_userId, dto));
    }

    [Fact]
    public async Task AutoReserve_Throws_WhenAllSuitableTablesBooked()
    {
        _tables.GetCandidateTablesAsync(2, null, Arg.Any<CancellationToken>())
            .Returns(new List<RestaurantTable> { ActiveTable(1, 2), ActiveTable(2, 4) });
        _reservations.HasOverlappingReservationAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var dto = new AutoReserveDto { PartySize = 2, DurationMinutes = 90, ReservationStartUtc = FutureStart };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AutoReserveAsync(_userId, dto));
    }

    [Fact]
    public async Task Cancel_Throws_WhenCustomerCancelsSomeoneElsesReservation()
    {
        var otherUsersReservation = new Reservation
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TableId = 1,
            ReservationStartUtc = FutureStart, ReservationEndUtc = FutureStart.AddMinutes(90),
            Status = ReservationStatus.Confirmed
        };
        _reservations.GetByIdAsync(otherUsersReservation.Id, Arg.Any<CancellationToken>())
            .Returns(otherUsersReservation);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CancelAsync(otherUsersReservation.Id, _userId, UserRole.Customer));
    }

    [Fact]
    public async Task Cancel_Throws_WhenWithinLeadTime_ForCustomer()
    {
        var soon = new Reservation
        {
            Id = Guid.NewGuid(), UserId = _userId, TableId = 1,
            ReservationStartUtc = DateTime.UtcNow.AddMinutes(30), // < 60 min lead
            ReservationEndUtc = DateTime.UtcNow.AddMinutes(120),
            Status = ReservationStatus.Confirmed
        };
        _reservations.GetByIdAsync(soon.Id, Arg.Any<CancellationToken>()).Returns(soon);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CancelAsync(soon.Id, _userId, UserRole.Customer));
    }

    [Fact]
    public async Task Cancel_Succeeds_ForOwnerWithEnoughLeadTime()
    {
        var booking = new Reservation
        {
            Id = Guid.NewGuid(), UserId = _userId, TableId = 1,
            ReservationStartUtc = DateTime.UtcNow.AddDays(2),
            ReservationEndUtc = DateTime.UtcNow.AddDays(2).AddMinutes(90),
            Status = ReservationStatus.Confirmed
        };
        _reservations.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _reservations.GetByIdWithDetailsAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns(booking);

        var result = await _sut.CancelAsync(booking.Id, _userId, UserRole.Customer);

        Assert.Equal(ReservationStatus.Cancelled, booking.Status);
        Assert.Equal("Cancelled", result.Status);
    }
}
