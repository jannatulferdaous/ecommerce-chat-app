using EcommerceChat.Api.Data;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceChat.Api.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? category = null);
    Task<ProductDto?> GetByIdAsync(int id);

    /// <summary>
    /// Searches products by free-text query (matches name, brand, description, category)
    /// and/or category filter. Used by both the REST endpoint and the chatbot's
    /// "search_products" intent. Returns at most <paramref name="maxResults"/> items.
    /// </summary>
    Task<List<ProductDto>> SearchAsync(string? query, string? category, int maxResults = 5);

    /// <summary>
    /// Fuzzy-finds a single product by (partial) name and optional brand/category hints.
    /// Returns null if nothing reasonably matches - used by the chatbot when the user
    /// refers to a product by name (e.g. "Nike t-shirt").
    /// </summary>
    Task<Product?> FindProductByNameAsync(string nameHint);

    Task<List<string>> GetAllProductNamesAsync();
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductDto>> GetAllAsync(string? category = null)
    {
        var query = _db.Products.Include(p => p.Variants).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var products = await query.ToListAsync();
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _db.Products.Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product is null ? null : ToDto(product);
    }

    public async Task<List<ProductDto>> SearchAsync(string? query, string? category, int maxResults = 5)
    {
        var products = _db.Products.Include(p => p.Variants).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            products = products.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            products = products.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.Brand, $"%{term}%") ||
                EF.Functions.Like(p.Description, $"%{term}%") ||
                EF.Functions.Like(p.Category, $"%{term}%"));
        }

        var results = await products.Take(maxResults).ToListAsync();
        return results.Select(ToDto).ToList();
    }

    public async Task<Product?> FindProductByNameAsync(string nameHint)
    {
        if (string.IsNullOrWhiteSpace(nameHint)) return null;

        var term = nameHint.Trim();

        // 1) Try a direct "contains" match first (covers "Nike t-shirt" -> matches by name/brand)
        var candidates = await _db.Products
            .Include(p => p.Variants)
            .Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.Brand + " " + p.Category, $"%{term}%"))
            .ToListAsync();

        if (candidates.Count >= 1) return candidates[0];

        // 2) Fall back to matching individual significant words (e.g. "running shirt")
        var words = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2)
            .ToList();

        if (words.Count == 0) return null;

        var all = await _db.Products.Include(p => p.Variants).ToListAsync();

        var best = all
            .Select(p => new
            {
                Product = p,
                Score = words.Count(w =>
                    p.Name.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                    p.Brand.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(w, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return best?.Product;
    }

    public async Task<List<string>> GetAllProductNamesAsync()
    {
        return await _db.Products.Select(p => p.Name).ToListAsync();
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Brand = p.Brand,
        Description = p.Description,
        Price = p.Price,
        ImageUrl = p.ImageUrl,
        Category = p.Category,
        Variants = p.Variants.Select(v => new ProductVariantDto
        {
            VariantId = v.Id,
            Size = v.Size,
            Stock = v.Stock
        }).ToList()
    };
}
