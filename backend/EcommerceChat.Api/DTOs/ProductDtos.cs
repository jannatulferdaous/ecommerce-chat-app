namespace EcommerceChat.Api.DTOs;

public class ProductVariantDto
{
    public int VariantId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Stock { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<ProductVariantDto> Variants { get; set; } = new();
}
