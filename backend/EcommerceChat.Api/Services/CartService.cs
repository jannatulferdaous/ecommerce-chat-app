using EcommerceChat.Api.Data;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceChat.Api.Services;

public enum CartOperationStatus
{
    Success,
    ProductNotFound,
    InvalidSize,
    OutOfStock,
    ItemNotInCart
}

public class CartOperationResult
{
    public CartOperationStatus Status { get; set; }
    public CartDto? Cart { get; set; }
    public Product? Product { get; set; }
    public string? RequestedSize { get; set; }
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<CartOperationResult> AddToCartAsync(string userId, int productId, string size, int quantity);
    Task<CartOperationResult> RemoveFromCartAsync(string userId, string productNameHint, string size);
    Task ClearCartAsync(string userId);
}

public class CartService : ICartService
{
    private readonly AppDbContext _db;
    private readonly IProductService _products;

    public CartService(AppDbContext db, IProductService products)
    {
        _db = db;
        _products = products;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var items = await _db.CartItems
            .Include(c => c.ProductVariant).ThenInclude(v => v!.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return ToCartDto(items);
    }

    public async Task<CartOperationResult> AddToCartAsync(string userId, int productId, string size, int quantity)
    {
        var normalizedSize = SizeOptions.Normalize(size);

        if (!SizeOptions.IsValid(normalizedSize))
        {
            return new CartOperationResult { Status = CartOperationStatus.InvalidSize, RequestedSize = size };
        }

        var product = await _db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return new CartOperationResult { Status = CartOperationStatus.ProductNotFound };
        }

        var variant = product.Variants.FirstOrDefault(v => v.Size == normalizedSize);
        if (variant is null || variant.Stock <= 0)
        {
            return new CartOperationResult
            {
                Status = CartOperationStatus.OutOfStock,
                Product = product,
                RequestedSize = normalizedSize
            };
        }

        // Cap quantity to available stock
        var qty = Math.Min(quantity, variant.Stock);

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductVariantId == variant.Id);

        if (existing is null)
        {
            _db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductVariantId = variant.Id,
                Quantity = qty
            });
        }
        else
        {
            existing.Quantity = Math.Min(existing.Quantity + qty, variant.Stock);
        }

        await _db.SaveChangesAsync();

        var cart = await GetCartAsync(userId);
        return new CartOperationResult { Status = CartOperationStatus.Success, Cart = cart, Product = product, RequestedSize = normalizedSize };
    }

    public async Task<CartOperationResult> RemoveFromCartAsync(string userId, string productNameHint, string size)
    {
        var normalizedSize = SizeOptions.Normalize(size);

        var items = await _db.CartItems
            .Include(c => c.ProductVariant).ThenInclude(v => v!.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var match = items.FirstOrDefault(c =>
            c.ProductVariant!.Size == normalizedSize &&
            c.ProductVariant.Product!.Name.Contains(productNameHint, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return new CartOperationResult { Status = CartOperationStatus.ItemNotInCart, RequestedSize = normalizedSize };
        }

        _db.CartItems.Remove(match);
        await _db.SaveChangesAsync();

        var cart = await GetCartAsync(userId);
        return new CartOperationResult
        {
            Status = CartOperationStatus.Success,
            Cart = cart,
            Product = match.ProductVariant!.Product,
            RequestedSize = normalizedSize
        };
    }

    public async Task ClearCartAsync(string userId)
    {
        var items = _db.CartItems.Where(c => c.UserId == userId);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    private static CartDto ToCartDto(List<CartItem> items) => new()
    {
        Items = items.Select(c => new CartItemDto
        {
            CartItemId = c.Id,
            ProductId = c.ProductVariant!.ProductId,
            ProductName = c.ProductVariant.Product!.Name,
            Brand = c.ProductVariant.Product.Brand,
            ImageUrl = c.ProductVariant.Product.ImageUrl,
            Size = c.ProductVariant.Size,
            Quantity = c.Quantity,
            UnitPrice = c.ProductVariant.Product.Price
        }).ToList()
    };
}
