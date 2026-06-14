using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Models.DTOs.Reservations;

/// <summary>
/// Request to auto-assign the best available table for a party.
/// Used by the complex business-logic endpoint.
/// </summary>
public class AutoReserveDto
{
    [Required]
    public DateTime ReservationStartUtc { get; set; }

    [Range(15, 600)]
    public int DurationMinutes { get; set; } = 90;

    [Range(1, 50)]
    public int PartySize { get; set; }

    /// <summary>Optional preferred location (e.g. "Patio"). Null = any.</summary>
    public string? PreferredLocation { get; set; }
}
