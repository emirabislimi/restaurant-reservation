using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Models;

/// <summary>
/// A physical table in the restaurant that can be reserved.
/// </summary>
public class RestaurantTable
{
    public int Id { get; set; }

    /// <summary>Human-friendly unique table number.</summary>
    public int TableNumber { get; set; }

    [Range(1, 50)]
    public int Capacity { get; set; }

    [Required, MaxLength(100)]
    public string Location { get; set; } = "Indoor";

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
