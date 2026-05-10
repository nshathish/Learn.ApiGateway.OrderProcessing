using System.ComponentModel.DataAnnotations;

namespace ProductService.Features.Products;

public class Product
{
    public int Id { get; set; }

    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}