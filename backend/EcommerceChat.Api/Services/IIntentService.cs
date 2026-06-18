using EcommerceChat.Api.DTOs;

namespace EcommerceChat.Api.Services;

/// <summary>
/// Converts free-text user messages into a structured ChatIntent.
/// Two implementations are provided:
///  - RuleBasedIntentService: deterministic regex/keyword parser (always available, no API key needed)
///  - OpenAiIntentService: uses an LLM with function-calling for more flexible language understanding
///
/// CompositeIntentService picks between them based on configuration.
/// </summary>
public interface IIntentService
{
    Task<ChatIntent> ParseIntentAsync(string message, List<ChatMessageDto> history);
}
