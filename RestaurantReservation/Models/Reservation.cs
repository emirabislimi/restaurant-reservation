using RestaurantReservation.Models.Enums;

namespace RestaurantReservation.Models;

/// <summary>
/// A reservation linking a user to a table for a specific time window.
/// </summary>
public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int TableId { get; set; }
    public RestaurantTable? Table { get; set; }

    public DateTime ReservationStartUtc { get; set; }
    public DateTime ReservationEndUtc { get; set; }

    public int PartySize { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
