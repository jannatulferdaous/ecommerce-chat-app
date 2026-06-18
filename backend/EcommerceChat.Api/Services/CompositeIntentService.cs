using EcommerceChat.Api.DTOs;
using Microsoft.Extensions.Logging;

namespace EcommerceChat.Api.Services;

/// <summary>
/// Tries the LLM-backed parser first (if an OpenAI API key is configured), and
/// falls back to the deterministic rule-based parser if the LLM call fails or
/// returns something unusable. This keeps the chatbot fully functional even with
/// zero external dependencies, while letting it understand more varied phrasing
/// when an API key is supplied.
/// </summary>
public class CompositeIntentService : IIntentService
{
    private readonly RuleBasedIntentService _ruleBased;
    private readonly OpenAiIntentService _openAi;
    private readonly IConfiguration _config;
    private readonly ILogger<CompositeIntentService> _logger;

    public CompositeIntentService(
        RuleBasedIntentService ruleBased,
        OpenAiIntentService openAi,
        IConfiguration config,
        ILogger<CompositeIntentService> logger)
    {
        _ruleBased = ruleBased;
        _openAi = openAi;
        _config = config;
        _logger = logger;
    }

    public async Task<ChatIntent> ParseIntentAsync(string message, List<ChatMessageDto> history)
    {
        // Always compute the rule-based result - it's cheap, deterministic, and acts
        // as our fallback / sanity baseline.
        var ruleResult = await _ruleBased.ParseIntentAsync(message, history);

        if (!OpenAiIntentService.IsConfigured(_config))
        {
            return ruleResult;
        }

        try
        {
            var llmResult = await _openAi.ParseIntentAsync(message, history);

            // If the LLM returned "unknown" but the rule-based parser found something
            // useful, prefer the rule-based result.
            if (llmResult.Action == DTOs.ChatAction.Unknown && ruleResult.Action != DTOs.ChatAction.Unknown)
            {
                return ruleResult;
            }

            return llmResult;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI intent parsing failed, falling back to rule-based parser.");
            return ruleResult;
        }
    }
}
