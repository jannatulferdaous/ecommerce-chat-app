using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceChat.Api.Models;

public static class OrderStatus
{
    public const string Placed = "Placed";
}

public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = OrderStatus.Placed;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductVariantId { get; set; }

    // Snapshot fields so order history stays correct even if catalog changes later
    public string ProductName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }
}
