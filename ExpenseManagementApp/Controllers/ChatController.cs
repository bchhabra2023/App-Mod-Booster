using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request, CancellationToken ct)
    {
        var (message, usedDummy) = await chatService.AskAsync(request.Prompt, ct);
        return Ok(new ChatResponse { Response = message, UsedDummy = usedDummy });
    }
}
