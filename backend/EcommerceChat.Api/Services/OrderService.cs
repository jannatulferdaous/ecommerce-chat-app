using EcommerceChat.Api.Data;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceChat.Api.Services;

public interface IOrderService
{
    Task<OrderDto?> CheckoutAsync(string userId);
    Task<List<OrderDto>> GetOrdersForUserAsync(string userId);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Simulates placing an order: snapshots the current cart into an Order + OrderItems,
    /// decrements stock, clears the cart. Returns null if the cart is empty.
    /// No real payment processing is performed.
    /// </summary>
    public async Task<OrderDto?> CheckoutAsync(string userId)
    {
        var cartItems = await _db.CartItems
            .Include(c => c.ProductVariant).ThenInclude(v => v!.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (cartItems.Count == 0) return null;

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Placed
        };

        decimal total = 0;

        foreach (var item in cartItems)
        {
            var variant = item.ProductVariant!;
            var product = variant.Product!;

            // Clamp quantity to whatever stock is currently available (defensive check)
            var qty = Math.Min(item.Quantity, Math.Max(variant.Stock, 0));
            if (qty <= 0) qty = item.Quantity; // allow simulated checkout even if stock ran out

            var lineTotal = product.Price * qty;
            total += lineTotal;

            order.Items.Add(new OrderItem
            {
                ProductVariantId = variant.Id,
                ProductName = product.Name,
                Size = variant.Size,
                Quantity = qty,
                UnitPrice = product.Price
            });

            // Decrease stock (simulation only)
            variant.Stock = Math.Max(0, variant.Stock - qty);
        }

        order.TotalAmount = total;

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cartItems);

        await _db.SaveChangesAsync();

        return ToDto(order);
    }

    public async Task<List<OrderDto>> GetOrdersForUserAsync(string userId)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(ToDto).ToList();
    }

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        OrderDate = o.OrderDate,
        Status = o.Status,
        TotalAmount = o.TotalAmount,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductName = i.ProductName,
            Size = i.Size,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
