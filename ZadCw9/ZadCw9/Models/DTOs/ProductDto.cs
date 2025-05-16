using System.ComponentModel.DataAnnotations;

namespace ZadCw9.Models.DTOs;


public class AddProductDto
{
    [Required]
    public int IdProduct { get; set; }

    [Required]
    public int IdWarehouse { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
}