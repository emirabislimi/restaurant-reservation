using System.ComponentModel.DataAnnotations;
using RestaurantReservation.Models.Enums;

namespace RestaurantReservation.Models;

/// <summary>
/// Represents an application user (Admin or Customer).
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Hashed password. Plain text is never stored.</summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Customer;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
