using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Models.DTOs.Tables;

public class UpdateTableDto
{
    [Range(1, 50)]
    public int Capacity { get; set; }

    [Required, MaxLength(100)]
    public string Location { get; set; } = "Indoor";

    public bool IsActive { get; set; } = true;
}
