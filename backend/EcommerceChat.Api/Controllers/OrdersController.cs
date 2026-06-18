using System.Security.Claims;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceChat.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/orders - order history for the logged-in user
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
    {
        return Ok(await _orders.GetOrdersForUserAsync(UserId));
    }

    // POST /api/orders/checkout - simulates placing an order from the current cart
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout()
    {
        var order = await _orders.CheckoutAsync(UserId);
        if (order is null) return BadRequest(new { message = "Your cart is empty." });
        return Ok(order);
    }
}
