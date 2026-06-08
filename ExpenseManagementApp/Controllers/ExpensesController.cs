using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<ExpenseItem>>>> Get([FromQuery] string? status, [FromQuery] string? userEmail, CancellationToken ct)
    {
        var (expenses, errorHeader) = await expenseService.GetExpensesAsync(status, userEmail, ct);
        return Ok(new ApiResult<IReadOnlyList<ExpenseItem>> { Data = expenses, ErrorHeader = errorHeader });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<string>>> Create([FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var (success, errorHeader) = await expenseService.CreateExpenseAsync(request, ct);
        return success
            ? Ok(new ApiResult<string> { Data = "Created" })
            : StatusCode(500, new ApiResult<string> { Data = "Failed", ErrorHeader = errorHeader });
    }

    [HttpPut("{expenseId:int}/status")]
    public async Task<ActionResult<ApiResult<string>>> UpdateStatus(int expenseId, [FromBody] UpdateExpenseStatusRequest request, CancellationToken ct)
    {
        var (success, errorHeader) = await expenseService.UpdateExpenseStatusAsync(expenseId, request, ct);
        return success
            ? Ok(new ApiResult<string> { Data = "Updated" })
            : StatusCode(500, new ApiResult<string> { Data = "Failed", ErrorHeader = errorHeader });
    }
}
