namespace EcommerceChat.Api.DTOs;

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
}

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public List<ProductDto>? Products { get; set; }
    public CartDto? Cart { get; set; }
    public OrderDto? Order { get; set; }
}

// --- Internal NLU representation -------------------------------------------------

/// <summary>
/// The set of supported chatbot intents. This is the "function schema" that both
/// the rule-based parser and the LLM-backed parser must map free text into.
/// </summary>
public enum ChatAction
{
    SearchProducts,
    AddToCart,
    RemoveFromCart,
    ViewCart,
    Checkout,
    RequestSize,
    Greeting,
    Help,
    Unknown
}

/// <summary>
/// Structured result of NLU parsing. Both RuleBasedIntentService and
/// OpenAiIntentService produce this same shape so the orchestrator can treat
/// them interchangeably.
/// </summary>
public class ChatIntent
{
    public ChatAction Action { get; set; } = ChatAction.Unknown;

    /// <summary>Category filter, e.g. "T-Shirt" or "Pants" (for SearchProducts).</summary>
    public string? Category { get; set; }

    /// <summary>Free-text keyword used for product search/matching, e.g. "running", "Nike".</summary>
    public string? Query { get; set; }

    /// <summary>Product name (or partial name) mentioned by the user.</summary>
    public string? ProductName { get; set; }

    /// <summary>Size mentioned by the user (S, M, L, XL, XXL) if any.</summary>
    public string? Size { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>Confidence score 0-1, used for logging / error-handling decisions.</summary>
    public double Confidence { get; set; } = 1.0;
}
