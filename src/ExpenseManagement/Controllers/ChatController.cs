using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI assistant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _chatService.SendMessageAsync(request.Messages);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            return StatusCode(500, new ChatResponse
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Error = ex.Message
            });
        }
    }
}
