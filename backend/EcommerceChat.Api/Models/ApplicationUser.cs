using Microsoft.AspNetCore.Identity;

namespace EcommerceChat.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public List<CartItem> CartItems { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public List<SizeRequest> SizeRequests { get; set; } = new();
}
