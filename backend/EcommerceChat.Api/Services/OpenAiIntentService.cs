using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Models;
using Microsoft.Extensions.Logging;

namespace EcommerceChat.Api.Services;

/// <summary>
/// LLM-backed NLU using OpenAI's chat completions API with function/tool calling.
/// The model is FORCED to call the "set_intent" function, whose JSON-schema
/// parameters mirror <see cref="ChatIntent"/> exactly - this is the
/// "prompt engineering" layer that maps free text onto our product schema.
///
/// IMPORTANT: this service only ever *proposes* an intent. The orchestrator
/// (ChatOrchestrator) always re-validates ProductName/Size/Category against the
/// real database before taking any action, so a hallucinated product name simply
/// results in a "couldn't find that product" reply rather than a fake confirmation.
/// </summary>
public class OpenAiIntentService : IIntentService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAiIntentService> _logger;

    public OpenAiIntentService(HttpClient http, IConfiguration config, ILogger<OpenAiIntentService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public static bool IsConfigured(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["OpenAI:ApiKey"]);

    public async Task<ChatIntent> ParseIntentAsync(string message, List<ChatMessageDto> history)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        var model = _config["OpenAI:Model"] ?? "gpt-4o-mini";
        var baseUrl = _config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/chat/completions";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");

        var systemPrompt = $$"""
            You are the NLU layer for a t-shirts & pants e-commerce chatbot.
            Map the user's latest message to EXACTLY ONE function call to `set_intent`.

            Valid categories: "{{ProductCategories.TShirt}}", "{{ProductCategories.Pants}}".
            Valid sizes: S, M, L, XL, XXL (normalize words like "small"/"medium"/"large"/"extra large").

            Action guide:
            - search_products: user wants to browse/find items (e.g. "show me running products", "do you have jeans?")
            - add_to_cart: user wants to add a specific item, optionally with a size, to their cart
            - remove_from_cart: user wants to remove a specific item (with size) from their cart
            - view_cart: user wants to see what's currently in their cart
            - checkout: user wants to place/complete their order
            - request_size: user explicitly asks to be notified/request a size that's unavailable
            - greeting: a simple hello/hi with no other request
            - help: user asks what the bot can do
            - unknown: anything that doesn't clearly fit the above

            Only fill in fields you are confident about based on the message; leave others null.
            Use conversation history only for context (e.g. resolving "it" / "that one").
            """;

        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var h in history.TakeLast(6))
        {
            messages.Add(new { role = h.Role == ChatRoles.Assistant ? "assistant" : "user", content = h.Content });
        }
        messages.Add(new { role = "user", content = message });

        var requestBody = new
        {
            model,
            messages,
            tools = new object[]
            {
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "set_intent",
                        description = "Record the structured intent extracted from the user's message.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                action = new
                                {
                                    type = "string",
                                    @enum = new[]
                                    {
                                        "search_products", "add_to_cart", "remove_from_cart",
                                        "view_cart", "checkout", "request_size", "greeting", "help", "unknown"
                                    }
                                },
                                category = new { type = "string", description = "T-Shirt or Pants, if mentioned" },
                                query = new { type = "string", description = "free-text keyword, e.g. 'running' or a brand name" },
                                product_name = new { type = "string", description = "the product name or partial name the user referred to" },
                                size = new { type = "string", description = "S, M, L, XL, or XXL if mentioned" },
                                quantity = new { type = "integer", description = "quantity, default 1" }
                            },
                            required = new[] { "action" }
                        }
                    }
                }
            },
            tool_choice = new { type = "function", function = new { name = "set_intent" } },
            temperature = 0
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var toolCall = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function")
            .GetProperty("arguments")
            .GetString();

        if (string.IsNullOrWhiteSpace(toolCall))
            throw new InvalidOperationException("LLM did not return tool call arguments.");

        var args = JsonSerializer.Deserialize<JsonElement>(toolCall);

        var intent = new ChatIntent
        {
            Action = ParseAction(GetString(args, "action") ?? "unknown"),
            Category = NormalizeCategory(GetString(args, "category")),
            Query = GetString(args, "query"),
            ProductName = GetString(args, "product_name"),
            Size = NormalizeSize(GetString(args, "size")),
            Quantity = GetInt(args, "quantity") ?? 1,
            Confidence = 0.9
        };

        return intent;
    }

    private static ChatAction ParseAction(string action) => action switch
    {
        "search_products" => ChatAction.SearchProducts,
        "add_to_cart" => ChatAction.AddToCart,
        "remove_from_cart" => ChatAction.RemoveFromCart,
        "view_cart" => ChatAction.ViewCart,
        "checkout" => ChatAction.Checkout,
        "request_size" => ChatAction.RequestSize,
        "greeting" => ChatAction.Greeting,
        "help" => ChatAction.Help,
        _ => ChatAction.Unknown
    };

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return null;
        var c = category.Trim().ToLowerInvariant();
        if (c.Contains("shirt") || c.Contains("tee")) return ProductCategories.TShirt;
        if (c.Contains("pant") || c.Contains("trouser") || c.Contains("jean")) return ProductCategories.Pants;
        return null;
    }

    private static string? NormalizeSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return null;
        var s = size.Trim().ToUpperInvariant();
        return SizeOptions.IsValid(s) ? s : null;
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? GetInt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
}
