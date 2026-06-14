using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RestaurantReservation.Controllers;
using RestaurantReservation.Models.DTOs.Auth;
using RestaurantReservation.Models.DTOs.Reservations;
using RestaurantReservation.Models.DTOs.Tables;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Services.Interfaces;
using Xunit;

namespace RestaurantReservation.Tests.Controllers;

public class ControllerTests
{
    private static ControllerContext ContextFor(Guid userId, UserRole role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        }, "TestAuth");

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // ---------- AuthController ----------
    [Fact]
    public async Task AuthController_Register_ReturnsOkWithToken()
    {
        var auth = Substitute.For<IAuthService>();
        var response = new AuthResponseDto { Token = "jwt", Email = "a@x.com" };
        auth.RegisterAsync(Arg.Any<RegisterDto>(), Arg.Any<CancellationToken>()).Returns(response);
        var controller = new AuthController(auth);

        var result = await controller.Register(new RegisterDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task AuthController_Login_DelegatesToService()
    {
        var auth = Substitute.For<IAuthService>();
        auth.LoginAsync(Arg.Any<LoginDto>(), Arg.Any<CancellationToken>())
            .Returns(new AuthResponseDto { Token = "jwt" });
        var controller = new AuthController(auth);

        var dto = new LoginDto { Email = "a@x.com", Password = "p" };
        await controller.Login(dto, CancellationToken.None);

        await auth.Received(1).LoginAsync(dto, Arg.Any<CancellationToken>());
    }

    // ---------- TablesController ----------
    [Fact]
    public async Task TablesController_GetAll_ReturnsOkWithList()
    {
        var service = Substitute.For<ITableService>();
        service.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TableDto> { new() { Id = 1 } });
        var controller = new TablesController(service);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<TableDto>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task TablesController_Create_ReturnsCreatedAtAction()
    {
        var service = Substitute.For<ITableService>();
        service.CreateAsync(Arg.Any<CreateTableDto>(), Arg.Any<CancellationToken>())
            .Returns(new TableDto { Id = 5, TableNumber = 5 });
        var controller = new TablesController(service);

        var result = await controller.Create(new CreateTableDto(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<TableDto>(created.Value);
        Assert.Equal(5, dto.Id);
    }

    // ---------- ReservationsController ----------
    [Fact]
    public async Task ReservationsController_GetMine_PassesAuthenticatedUserId()
    {
        var service = Substitute.For<IReservationService>();
        var userId = Guid.NewGuid();
        service.GetMineAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<ReservationDto>());
        var controller = new ReservationsController(service)
        {
            ControllerContext = ContextFor(userId, UserRole.Customer)
        };

        await controller.GetMine(CancellationToken.None);

        await service.Received(1).GetMineAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationsController_Create_ReturnsCreatedAtAction()
    {
        var service = Substitute.For<IReservationService>();
        var userId = Guid.NewGuid();
        var created = new ReservationDto { Id = Guid.NewGuid(), Status = "Confirmed" };
        service.CreateAsync(userId, Arg.Any<CreateReservationDto>(), Arg.Any<CancellationToken>())
            .Returns(created);
        var controller = new ReservationsController(service)
        {
            ControllerContext = ContextFor(userId, UserRole.Customer)
        };

        var result = await controller.Create(new CreateReservationDto(), CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task ReservationsController_AutoReserve_ForwardsRoleAwareUser()
    {
        var service = Substitute.For<IReservationService>();
        var userId = Guid.NewGuid();
        service.AutoReserveAsync(userId, Arg.Any<AutoReserveDto>(), Arg.Any<CancellationToken>())
            .Returns(new ReservationDto { Id = Guid.NewGuid() });
        var controller = new ReservationsController(service)
        {
            ControllerContext = ContextFor(userId, UserRole.Customer)
        };

        await controller.AutoReserve(new AutoReserveDto(), CancellationToken.None);

        await service.Received(1).AutoReserveAsync(userId, Arg.Any<AutoReserveDto>(), Arg.Any<CancellationToken>());
    }
}
