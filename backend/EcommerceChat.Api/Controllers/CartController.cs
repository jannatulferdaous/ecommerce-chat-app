using System.Security.Claims;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceChat.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart)
    {
        _cart = cart;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        return Ok(await _cart.GetCartAsync(UserId));
    }

    [HttpPost("items")]
    public async Task<ActionResult> AddItem(AddToCartDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _cart.AddToCartAsync(UserId, dto.ProductId, dto.Size, dto.Quantity);

        return result.Status switch
        {
            CartOperationStatus.Success => Ok(result.Cart),
            CartOperationStatus.ProductNotFound => NotFound(new { message = "Product not found." }),
            CartOperationStatus.InvalidSize => BadRequest(new { message = $"'{dto.Size}' is not a valid size. Valid sizes: S, M, L, XL, XXL." }),
            CartOperationStatus.OutOfStock => Conflict(new
            {
                message = $"Size {result.RequestedSize} is currently out of stock for '{result.Product!.Name}'. " +
                          "A restock request has not been created via this endpoint — use the chat assistant to request it."
            }),
            _ => BadRequest(new { message = "Could not add item to cart." })
        };
    }

    [HttpDelete("items/{cartItemId:int}")]
    public async Task<ActionResult> RemoveItem(int cartItemId)
    {
        var cart = await _cart.GetCartAsync(UserId);
        var item = cart.Items.FirstOrDefault(i => i.CartItemId == cartItemId);
        if (item is null) return NotFound(new { message = "Cart item not found." });

        var result = await _cart.RemoveFromCartAsync(UserId, item.ProductName, item.Size);
        return Ok(result.Cart);
    }

    [HttpDelete]
    public async Task<ActionResult> ClearCart()
    {
        await _cart.ClearCartAsync(UserId);
        return NoContent();
    }
}
