using System.ComponentModel.DataAnnotations;

namespace EcommerceChat.Api.DTOs;

public class CartItemDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.LineTotal);
}

public class AddToCartDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public string Size { get; set; } = string.Empty;

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    [Range(1, 99)]
    public int Quantity { get; set; }
}
