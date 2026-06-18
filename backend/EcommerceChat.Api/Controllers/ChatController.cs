using System.Security.Claims;
using EcommerceChat.Api.DTOs;
using EcommerceChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceChat.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatOrchestrator _orchestrator;

    public ChatController(IChatOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/chat/history - returns this user's persisted chat history
    [HttpGet("history")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetHistory()
    {
        return Ok(await _orchestrator.GetHistoryAsync(UserId));
    }

    // POST /api/chat/message - send a natural-language message to the bot
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponseDto>> SendMessage(ChatRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "Message cannot be empty." });

        var response = await _orchestrator.HandleMessageAsync(UserId, dto.Message);
        return Ok(response);
    }
}
