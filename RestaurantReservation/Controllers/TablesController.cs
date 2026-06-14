using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantReservation.Models.DTOs.Tables;
using RestaurantReservation.Services.Interfaces;

namespace RestaurantReservation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // any authenticated user
public class TablesController : ControllerBase
{
    private readonly ITableService _tables;

    public TablesController(ITableService tables) => _tables = tables;

    /// <summary>Lists all tables. Available to any authenticated user.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TableDto>>> GetAll(CancellationToken ct)
        => Ok(await _tables.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableDto>> GetById(int id, CancellationToken ct)
        => Ok(await _tables.GetByIdAsync(id, ct));

    /// <summary>Creates a table. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TableDto>> Create(CreateTableDto dto, CancellationToken ct)
    {
        var created = await _tables.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a table. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TableDto>> Update(int id, UpdateTableDto dto, CancellationToken ct)
        => Ok(await _tables.UpdateAsync(id, dto, ct));

    /// <summary>Deletes a table. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _tables.DeleteAsync(id, ct);
        return NoContent();
    }
}
