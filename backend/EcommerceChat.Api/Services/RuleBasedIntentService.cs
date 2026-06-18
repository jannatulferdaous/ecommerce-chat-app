using System.Text.RegularExpressions;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Models;

namespace EcommerceChat.Api.Services;

/// <summary>
/// Deterministic, regex/keyword based intent parser. This is the primary NLU engine
/// when no LLM API key is configured, and acts as the safety net / validator even
/// when an LLM is used, because it never "hallucinates" - it only returns what it
/// can actually find in the text.
/// </summary>
public class RuleBasedIntentService : IIntentService
{
    // Word boundaries are used so "S"/"M"/"L" don't accidentally match inside other words.
    private static readonly Dictionary<string, string> SizeWordMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "small", "S" },
        { "medium", "M" },
        { "large", "L" },
        { "extra large", "XL" },
        { "extra-large", "XL" },
        { "xtra large", "XL" },
        { "2xl", "XXL" },
        { "double extra large", "XXL" },
        { "double-extra-large", "XXL" },
        { "xx large", "XXL" },
        { "xxl", "XXL" },
        { "xl", "XL" },
        { "s", "S" },
        { "m", "M" },
        { "l", "L" }
    };

    private static readonly (string keyword, string category)[] CategoryKeywords =
    {
        ("t-shirt", ProductCategories.TShirt),
        ("tshirt", ProductCategories.TShirt),
        ("t shirt", ProductCategories.TShirt),
        ("shirt", ProductCategories.TShirt),
        ("tee", ProductCategories.TShirt),
        ("pants", ProductCategories.Pants),
        ("trouser", ProductCategories.Pants),
        ("jeans", ProductCategories.Pants),
        ("sweatpants", ProductCategories.Pants),
        ("chino", ProductCategories.Pants),
        ("track pants", ProductCategories.Pants)
    };

    private static readonly string[] KeywordHints =
    {
        "running", "run", "training", "gym", "casual", "sports", "sport",
        "nike", "adidas", "puma", "levi", "levi's", "under armour", "new balance", "h&m"
    };

    public Task<ChatIntent> ParseIntentAsync(string message, List<ChatMessageDto> history)
    {
        var text = (message ?? string.Empty).Trim();
        var lower = text.ToLowerInvariant();

        var intent = new ChatIntent { Confidence = 1.0 };

        // 1) Greeting / help — checked first, short-circuits everything else
        if (Regex.IsMatch(lower, @"^\s*(hi|hello|hey|good (morning|afternoon|evening))\b"))
        {
            intent.Action = ChatAction.Greeting;
            return Task.FromResult(intent);
        }

        if (Regex.IsMatch(lower, @"\b(help|what can you do|how does this work|commands?)\b"))
        {
            intent.Action = ChatAction.Help;
            return Task.FromResult(intent);
        }

        // 2) Checkout — specific phrases only, to avoid colliding with "add to cart"
        if (Regex.IsMatch(lower, @"\b(check\s?out|place (my |the )?order|complete (my |the )?(order|purchase)|" +
                                  @"ready to (pay|order|checkout|check out)|proceed to (payment|checkout)|confirm (my |the )?order)\b"))
        {
            intent.Action = ChatAction.Checkout;
            return Task.FromResult(intent);
        }

        // 3) View cart
        if (Regex.IsMatch(lower, @"\b(view|show|what'?s in|see) (my )?(shopping )?cart\b") ||
            Regex.IsMatch(lower, @"^\s*(my )?cart\s*\??\s*$"))
        {
            intent.Action = ChatAction.ViewCart;
            return Task.FromResult(intent);
        }

        // 4) Remove from cart
        if (Regex.IsMatch(lower, @"\b(remove|delete|take out|cancel|don'?t want)\b"))
        {
            intent.Action = ChatAction.RemoveFromCart;
            intent.Size = ExtractSize(lower);
            intent.ProductName = ExtractProductName(text, lower, removeAction: true);
            return Task.FromResult(intent);
        }

        // 5) Add to cart
        if (Regex.IsMatch(lower, @"\b(add|put .* (in|into) .*cart|i want to (add|buy|order|get)|i'?d like|buy)\b"))
        {
            intent.Action = ChatAction.AddToCart;
            intent.Size = ExtractSize(lower);
            intent.Quantity = ExtractQuantity(lower);
            intent.ProductName = ExtractProductName(text, lower, removeAction: false);

            // If we couldn't find any product name hint at all, this is probably
            // a generic "I want to buy something" -> fall through to search instead.
            if (string.IsNullOrWhiteSpace(intent.ProductName))
            {
                intent.Action = ChatAction.SearchProducts;
                intent.Category = ExtractCategory(lower);
                intent.Query = ExtractKeyword(lower) ?? intent.Category;
                intent.Confidence = 0.4;
            }

            return Task.FromResult(intent);
        }

        // 6) Browse / search products
        if (Regex.IsMatch(lower, @"\b(show|browse|search|find|looking for|do you have|what.*available|see (some|all)?)\b") ||
            CategoryKeywords.Any(c => lower.Contains(c.keyword)) ||
            KeywordHints.Any(k => lower.Contains(k)))
        {
            intent.Action = ChatAction.SearchProducts;
            intent.Category = ExtractCategory(lower);
            intent.Query = ExtractKeyword(lower);
            return Task.FromResult(intent);
        }

        // 7) Nothing matched
        intent.Action = ChatAction.Unknown;
        intent.Confidence = 0.0;
        return Task.FromResult(intent);
    }

    private static string? ExtractSize(string lower)
    {
        // Look for "size <X>" or "in <X>" patterns first (most reliable)
        var m = Regex.Match(lower, @"\b(?:size|in size|in)\s+(xxl|xx large|2xl|extra[\s-]?large|xl|small|medium|large|s|m|l)\b");
        if (m.Success) return MapSize(m.Groups[1].Value);

        // Then look for a standalone size token anywhere in the message
        m = Regex.Match(lower, @"\b(xxl|2xl|xl|small|medium|large|s|m|l)\b");
        if (m.Success) return MapSize(m.Groups[1].Value);

        return null;
    }

    private static string MapSize(string raw)
    {
        var key = raw.Trim().ToLowerInvariant();
        return SizeWordMap.TryGetValue(key, out var size) ? size : raw.ToUpperInvariant();
    }

    private static int ExtractQuantity(string lower)
    {
        var m = Regex.Match(lower, @"\b(\d{1,2})\s*(x|pcs|pieces|units)?\b");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var qty) && qty > 0 && qty < 100)
            return qty;
        return 1;
    }

    private static string? ExtractCategory(string lower)
    {
        foreach (var (keyword, category) in CategoryKeywords)
        {
            if (lower.Contains(keyword)) return category;
        }
        return null;
    }

    private static string? ExtractKeyword(string lower)
    {
        foreach (var k in KeywordHints)
        {
            if (lower.Contains(k)) return k;
        }
        return null;
    }

    /// <summary>
    /// Strips known filler words/phrases (action verbs, "to/from cart", size, quantity)
    /// from the original message to leave behind a product-name hint, e.g.
    /// "Add Nike t-shirt in size L to my cart" -> "Nike t-shirt".
    /// </summary>
    private static string ExtractProductName(string original, string lower, bool removeAction)
    {
        var working = " " + lower + " ";

        // Remove cart phrases
        working = Regex.Replace(working, @"\b(to|from)\s+(my|the)?\s*(shopping )?cart\b", " ");
        working = Regex.Replace(working, @"\bcart\b", " ");

        // Remove action / politeness phrases
        var fillers = new[]
        {
            "i want to remove", "i want to add", "i want to buy", "i'd like to add", "i would like to add",
            "please remove", "please add", "can you remove", "can you add", "could you remove", "could you add",
            "i want", "i'd like", "i would like", "remove", "delete", "take out", "cancel", "don't want", "add",
            "put", "buy", "order", "get me", "get"
        };
        foreach (var f in fillers)
        {
            working = Regex.Replace(working, $@"\b{Regex.Escape(f)}\b", " ");
        }

        // Remove size phrases ("in size l", "size: xl", "in xl", standalone size words)
        working = Regex.Replace(working, @"\b(in\s+)?size\s*:?\s*(xxl|xx large|2xl|extra[\s-]?large|xl|small|medium|large|s|m|l)\b", " ");
        working = Regex.Replace(working, @"\bin\s+(xxl|2xl|extra[\s-]?large|xl|small|medium|large)\b", " ");
        working = Regex.Replace(working, @"\b(xxl|2xl|xl)\b", " ");
        working = Regex.Replace(working, @"\b(small|medium|large)\b", " ");

        // Remove standalone single-letter size tokens (S, M, L) - careful, word-boundary only
        working = Regex.Replace(working, @"\b(s|m|l)\b", " ");

        // Remove quantity numbers
        working = Regex.Replace(working, @"\b\d{1,2}\b", " ");
        working = Regex.Replace(working, @"\b(piece|pieces|unit|units|x)\b", " ");

        // Remove leftover connector words
        working = Regex.Replace(working, @"\b(in|to|of|a|an|the|my|for)\b", " ");

        // Collapse whitespace
        working = Regex.Replace(working, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(working)) return string.Empty;

        // Title-case the result for nicer matching/display, but matching itself is
        // done case-insensitively in ProductService.
        return working;
    }
}
