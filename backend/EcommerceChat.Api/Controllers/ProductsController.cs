using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceChat.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    // GET /api/products?category=T-Shirt
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] string? category)
    {
        return Ok(await _products.GetAllAsync(category));
    }

    // GET /api/products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _products.GetByIdAsync(id);
        return product is null ? NotFound(new { message = "Product not found." }) : Ok(product);
    }

    // GET /api/products/search?q=running&category=T-Shirt
    [HttpGet("search")]
    public async Task<ActionResult<List<ProductDto>>> Search([FromQuery] string? q, [FromQuery] string? category)
    {
        return Ok(await _products.SearchAsync(q, category, maxResults: 20));
    }
}
