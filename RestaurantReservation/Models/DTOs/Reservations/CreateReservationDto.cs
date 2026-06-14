using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Models.DTOs.Reservations;

/// <summary>
/// Request to reserve a specific table.
/// </summary>
public class CreateReservationDto
{
    [Required]
    public int TableId { get; set; }

    [Required]
    public DateTime ReservationStartUtc { get; set; }

    [Range(15, 600)]
    public int DurationMinutes { get; set; } = 90;

    [Range(1, 50)]
    public int PartySize { get; set; }
}
