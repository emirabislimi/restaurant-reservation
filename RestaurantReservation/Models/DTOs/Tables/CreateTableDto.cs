using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Models.DTOs.Tables;

public class CreateTableDto
{
    [Range(1, int.MaxValue)]
    public int TableNumber { get; set; }

    [Range(1, 50)]
    public int Capacity { get; set; }

    [Required, MaxLength(100)]
    public string Location { get; set; } = "Indoor";
}
