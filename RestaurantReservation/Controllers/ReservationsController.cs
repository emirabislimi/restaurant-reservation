using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Reservations;
using RestaurantReservation.Services.Interfaces;

namespace RestaurantReservation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations) => _reservations = reservations;

    /// <summary>Lists every reservation. Admin only.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetAll(CancellationToken ct)
        => Ok(await _reservations.GetAllAsync(ct));

    /// <summary>Lists the current user's own reservations.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetMine(CancellationToken ct)
        => Ok(await _reservations.GetMineAsync(User.GetUserId(), ct));

    /// <summary>Gets a single reservation (owner or Admin).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _reservations.GetByIdAsync(id, User.GetUserId(), User.GetUserRole(), ct));

    /// <summary>Books a specific table (business logic).</summary>
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationDto dto, CancellationToken ct)
    {
        var created = await _reservations.CreateAsync(User.GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Finds and books the best available table automatically (complex business logic).</summary>
    [HttpPost("auto")]
    public async Task<ActionResult<ReservationDto>> AutoReserve(AutoReserveDto dto, CancellationToken ct)
    {
        var created = await _reservations.AutoReserveAsync(User.GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cancels a reservation (owner or Admin).</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(Guid id, CancellationToken ct)
        => Ok(await _reservations.CancelAsync(id, User.GetUserId(), User.GetUserRole(), ct));

    /// <summary>Changes a reservation status. Admin only.</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReservationDto>> UpdateStatus(
        Guid id, UpdateReservationStatusDto dto, CancellationToken ct)
        => Ok(await _reservations.UpdateStatusAsync(id, dto.Status, ct));
}
