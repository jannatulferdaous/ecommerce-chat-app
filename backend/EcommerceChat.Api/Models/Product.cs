using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceChat.Api.Models;

// Allowed categories for the catalog
public static class ProductCategories
{
    public const string TShirt = "T-Shirt";
    public const string Pants = "Pants";

    public static readonly string[] All = { TShirt, Pants };
}

// Allowed sizes - matches the requirement S, M, L, XL, XXL
public static class SizeOptions
{
    public const string S = "S";
    public const string M = "M";
    public const string L = "L";
    public const string XL = "XL";
    public const string XXL = "XXL";

    public static readonly string[] All = { S, M, L, XL, XXL };

    public static bool IsValid(string size) =>
        All.Contains(size.Trim().ToUpperInvariant());

    public static string Normalize(string size) => size.Trim().ToUpperInvariant();
}

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = ProductCategories.TShirt; // "T-Shirt" or "Pants"

    public List<ProductVariant> Variants { get; set; } = new();
}

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(10)]
    public string Size { get; set; } = string.Empty; // S, M, L, XL, XXL

    public int Stock { get; set; }
}
