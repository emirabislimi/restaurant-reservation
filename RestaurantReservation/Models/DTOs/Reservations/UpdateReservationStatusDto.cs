using System.ComponentModel.DataAnnotations;
using RestaurantReservation.Models.Enums;

namespace RestaurantReservation.Models.DTOs.Reservations;

public class UpdateReservationStatusDto
{
    [Required]
    public ReservationStatus Status { get; set; }
}
