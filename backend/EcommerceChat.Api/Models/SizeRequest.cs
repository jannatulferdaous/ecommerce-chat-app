namespace EcommerceChat.Api.Models;

public static class ChatRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

public class ChatMessage
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Role { get; set; } = ChatRoles.User; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class SizeRequestStatus
{
    public const string Pending = "Pending";
}

// Created automatically when a user tries to add an out-of-stock / unavailable size
public class SizeRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string RequestedSize { get; set; } = string.Empty;

    public string Status { get; set; } = SizeRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
