namespace RestaurantReservation.Models.Enums;

/// <summary>
/// Lifecycle states of a reservation.
/// </summary>
public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}
