using System.Text;
using EcommerceChat.Api.Data;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceChat.Api.Services;

/// <summary>
/// Central chatbot brain. Flow for every message:
///   1. Persist the user's message (so chat history is linked to their account)
///   2. Parse intent (IIntentService -> rule-based and/or LLM)
///   3. VALIDATE the intent against the real database (ProductService/CartService/OrderService)
///   4. Build a natural-language reply from the *actual result*, never from the LLM's guess
///   5. Persist the assistant's reply
///
/// This validate-then-respond pattern is the core error-handling strategy: the LLM
/// (or regex parser) only decides WHAT the user wants to do; the database decides
/// whether it's actually possible.
/// </summary>
public interface IChatOrchestrator
{
    Task<ChatResponseDto> HandleMessageAsync(string userId, string message);
    Task<List<ChatMessageDto>> GetHistoryAsync(string userId);
}

public class ChatOrchestrator : IChatOrchestrator
{
    private readonly AppDbContext _db;
    private readonly IIntentService _intentService;
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public ChatOrchestrator(
        AppDbContext db,
        IIntentService intentService,
        IProductService productService,
        ICartService cartService,
        IOrderService orderService)
    {
        _db = db;
        _intentService = intentService;
        _productService = productService;
        _cartService = cartService;
        _orderService = orderService;
    }

    public async Task<List<ChatMessageDto>> GetHistoryAsync(string userId)
    {
        var messages = await _db.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => new ChatMessageDto
        {
            Role = m.Role,
            Content = m.Content,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<ChatResponseDto> HandleMessageAsync(string userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new ChatResponseDto { Reply = "I didn't catch that — could you type your message again?" };
        }

        await SaveMessageAsync(userId, ChatRoles.User, message);

        var history = await GetHistoryAsync(userId);
        var intent = await _intentService.ParseIntentAsync(message, history);

        var response = intent.Action switch
        {
            ChatAction.Greeting => Greeting(),
            ChatAction.Help => Help(),
            ChatAction.SearchProducts => await HandleSearchAsync(intent),
            ChatAction.AddToCart => await HandleAddToCartAsync(userId, intent),
            ChatAction.RemoveFromCart => await HandleRemoveFromCartAsync(userId, intent),
            ChatAction.ViewCart => await HandleViewCartAsync(userId),
            ChatAction.Checkout => await HandleCheckoutAsync(userId),
            ChatAction.RequestSize => await HandleAddToCartAsync(userId, intent), // same validation path
            _ => Unknown()
        };

        await SaveMessageAsync(userId, ChatRoles.Assistant, response.Reply);
        return response;
    }

    // ----------------------------------------------------------------------------------
    // Intent handlers
    // ----------------------------------------------------------------------------------

    private static ChatResponseDto Greeting() => new()
    {
        Reply = "Hi there! 👋 I can help you browse t-shirts and pants, add items to your cart, " +
                "remove items, or place your order. Try: \"Show me running t-shirts\" or " +
                "\"Add Nike t-shirt size L to my cart\"."
    };

    private static ChatResponseDto Help() => new()
    {
        Reply = "Here's what I can do:\n" +
                "• Browse: \"Show me running products\" or \"Do you have pants?\"\n" +
                "• Add to cart: \"Add Nike t-shirt size L to my cart\"\n" +
                "• Remove from cart: \"Remove Nike t-shirt size M from my cart\"\n" +
                "• View cart: \"What's in my cart?\"\n" +
                "• Checkout: \"I'm ready to place my order\"\n\n" +
                "If a size isn't in stock, I'll log a request for it on your behalf."
    };

    private static ChatResponseDto Unknown() => new()
    {
        Reply = "Sorry, I'm not sure what you mean. You can ask me to browse products " +
                "(e.g. \"show me t-shirts\"), add/remove items from your cart, view your cart, " +
                "or say \"place my order\" to check out."
    };

    private async Task<ChatResponseDto> HandleSearchAsync(ChatIntent intent)
    {
        var results = await _productService.SearchAsync(intent.Query, intent.Category, maxResults: 5);

        if (results.Count == 0)
        {
            // Try again with no category filter, just the keyword (or vice versa) before giving up
            if (intent.Category != null && intent.Query != null)
            {
                results = await _productService.SearchAsync(intent.Query, null, maxResults: 5);
            }
        }

        if (results.Count == 0)
        {
            return new ChatResponseDto
            {
                Reply = "I couldn't find any products matching that. We currently stock t-shirts and pants " +
                        "from brands like Nike, Adidas, Puma, Levi's, Under Armour, New Balance and H&M. " +
                        "Try \"show me t-shirts\" or \"show me pants\"."
            };
        }

        var sb = new StringBuilder();
        var label = intent.Query ?? intent.Category ?? "matching";
        sb.AppendLine($"Here are {results.Count} {label} product(s):");
        foreach (var p in results)
        {
            var sizes = string.Join(", ", p.Variants.Where(v => v.Stock > 0).Select(v => v.Size));
            if (string.IsNullOrEmpty(sizes)) sizes = "currently out of stock in all sizes";
            sb.AppendLine($"• {p.Name} ({p.Brand}) — ${p.Price:0.00} — available sizes: {sizes}");
        }

        return new ChatResponseDto { Reply = sb.ToString().TrimEnd(), Products = results };
    }

    private async Task<ChatResponseDto> HandleAddToCartAsync(string userId, ChatIntent intent)
    {
        if (string.IsNullOrWhiteSpace(intent.ProductName))
        {
            return new ChatResponseDto
            {
                Reply = "Sure — which product would you like to add? You can name the product and/or brand, " +
                        "e.g. \"Add Nike t-shirt size L to my cart\"."
            };
        }

        var product = await _productService.FindProductByNameAsync(intent.ProductName);
        if (product is null)
        {
            return new ChatResponseDto
            {
                Reply = $"I couldn't find a product matching \"{intent.ProductName}\". " +
                        "Try \"show me t-shirts\" or \"show me pants\" to see what's available."
            };
        }

        if (string.IsNullOrWhiteSpace(intent.Size))
        {
            return new ChatResponseDto
            {
                Reply = $"Got it — \"{product.Name}\". What size would you like? (S, M, L, XL, XXL)"
            };
        }

        var result = await _cartService.AddToCartAsync(userId, product.Id, intent.Size, intent.Quantity);

        switch (result.Status)
        {
            case CartOperationStatus.Success:
                return new ChatResponseDto
                {
                    Reply = $"Added {intent.Quantity} x \"{product.Name}\" (size {result.RequestedSize}) to your cart. " +
                            $"Your cart total is now ${result.Cart!.Total:0.00}.",
                    Cart = result.Cart
                };

            case CartOperationStatus.InvalidSize:
                return new ChatResponseDto
                {
                    Reply = $"\"{intent.Size}\" isn't a valid size. Available sizes are S, M, L, XL, and XXL — " +
                            "which one would you like?"
                };

            case CartOperationStatus.OutOfStock:
                await CreateSizeRequestAsync(userId, product.Id, product.Name, result.RequestedSize!);
                return new ChatResponseDto
                {
                    Reply = $"\"{product.Name}\" in size {result.RequestedSize} is currently out of stock. " +
                            "I've submitted a restock request on your behalf — we'll notify you when it's available."
                };

            default:
                return new ChatResponseDto
                {
                    Reply = $"I couldn't add \"{product.Name}\" to your cart right now. Please try again."
                };
        }
    }

    private async Task<ChatResponseDto> HandleRemoveFromCartAsync(string userId, ChatIntent intent)
    {
        if (string.IsNullOrWhiteSpace(intent.ProductName))
        {
            return new ChatResponseDto
            {
                Reply = "Which item would you like to remove? You can say something like " +
                        "\"Remove Nike t-shirt size M from my cart\"."
            };
        }

        if (string.IsNullOrWhiteSpace(intent.Size))
        {
            return new ChatResponseDto
            {
                Reply = $"What size of \"{intent.ProductName}\" would you like to remove from your cart? (S, M, L, XL, XXL)"
            };
        }

        var result = await _cartService.RemoveFromCartAsync(userId, intent.ProductName, intent.Size);

        if (result.Status == CartOperationStatus.ItemNotInCart)
        {
            return new ChatResponseDto
            {
                Reply = $"I couldn't find \"{intent.ProductName}\" (size {SizeOptions.Normalize(intent.Size)}) in your cart. " +
                        "Say \"what's in my cart?\" to see your current items."
            };
        }

        return new ChatResponseDto
        {
            Reply = $"Removed \"{result.Product!.Name}\" (size {result.RequestedSize}) from your cart. " +
                    $"Your cart total is now ${result.Cart!.Total:0.00}.",
            Cart = result.Cart
        };
    }

    private async Task<ChatResponseDto> HandleViewCartAsync(string userId)
    {
        var cart = await _cartService.GetCartAsync(userId);

        if (cart.Items.Count == 0)
        {
            return new ChatResponseDto { Reply = "Your cart is currently empty.", Cart = cart };
        }

        var sb = new StringBuilder();
        sb.AppendLine("Here's what's in your cart:");
        foreach (var item in cart.Items)
        {
            sb.AppendLine($"• {item.ProductName} ({item.Brand}) — size {item.Size} — qty {item.Quantity} — ${item.LineTotal:0.00}");
        }
        sb.AppendLine($"Total: ${cart.Total:0.00}");
        sb.AppendLine("Say \"I'm ready to place my order\" to check out.");

        return new ChatResponseDto { Reply = sb.ToString().TrimEnd(), Cart = cart };
    }

    private async Task<ChatResponseDto> HandleCheckoutAsync(string userId)
    {
        var order = await _orderService.CheckoutAsync(userId);

        if (order is null)
        {
            return new ChatResponseDto
            {
                Reply = "Your cart is empty, so there's nothing to check out yet. " +
                        "Try \"show me t-shirts\" to start shopping."
            };
        }

        var sb = new StringBuilder();
        sb.AppendLine($"🎉 Order #{order.Id} placed successfully! (This is a simulated order — no real payment was processed.)");
        foreach (var item in order.Items)
        {
            sb.AppendLine($"• {item.ProductName} — size {item.Size} — qty {item.Quantity} — ${item.LineTotal:0.00}");
        }
        sb.AppendLine($"Total charged: ${order.TotalAmount:0.00}");

        return new ChatResponseDto { Reply = sb.ToString().TrimEnd(), Order = order };
    }

    // ----------------------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------------------

    private async Task CreateSizeRequestAsync(string userId, int productId, string productName, string size)
    {
        var alreadyRequested = await _db.SizeRequests.AnyAsync(r =>
            r.UserId == userId && r.ProductId == productId && r.RequestedSize == size && r.Status == SizeRequestStatus.Pending);

        if (alreadyRequested) return;

        _db.SizeRequests.Add(new SizeRequest
        {
            UserId = userId,
            ProductId = productId,
            ProductName = productName,
            RequestedSize = size,
            Status = SizeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private async Task SaveMessageAsync(string userId, string role, string content)
    {
        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
