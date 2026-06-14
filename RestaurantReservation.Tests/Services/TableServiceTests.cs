using NSubstitute;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Tables;
using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Implementations;
using Xunit;

namespace RestaurantReservation.Tests.Services;

public class TableServiceTests
{
    private readonly ITableRepository _tables = Substitute.For<ITableRepository>();
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly TableService _sut;

    public TableServiceTests()
        => _sut = new TableService(_tables, _reservations, TestHelpers.NewMapper());

    [Fact]
    public async Task Create_Throws_WhenTableNumberExists()
    {
        _tables.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<RestaurantTable, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var dto = new CreateTableDto { TableNumber = 1, Capacity = 4, Location = "Indoor" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_Succeeds_WhenTableNumberIsUnique()
    {
        _tables.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<RestaurantTable, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(false);

        var dto = new CreateTableDto { TableNumber = 9, Capacity = 4, Location = "Patio" };

        var result = await _sut.CreateAsync(dto);

        Assert.Equal(9, result.TableNumber);
        await _tables.Received(1).AddAsync(Arg.Any<RestaurantTable>(), Arg.Any<CancellationToken>());
        await _tables.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_Throws_WhenMissing()
    {
        _tables.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((RestaurantTable?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task Delete_Throws_WhenTableHasUpcomingReservations()
    {
        _tables.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new RestaurantTable { Id = 1, TableNumber = 1, Capacity = 4 });
        _reservations.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Reservation, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(1));
    }
}
