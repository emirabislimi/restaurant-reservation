namespace RestaurantReservation.Models.DTOs.Reservations;

public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public DateTime ReservationStartUtc { get; set; }
    public DateTime ReservationEndUtc { get; set; }
    public int PartySize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
