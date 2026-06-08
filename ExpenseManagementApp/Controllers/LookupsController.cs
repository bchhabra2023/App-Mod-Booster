using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/lookups")]
public sealed class LookupsController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<CategoryItem>>>> Categories(CancellationToken ct)
    {
        var (categories, error) = await expenseService.GetCategoriesAsync(ct);
        return Ok(new ApiResult<IReadOnlyList<CategoryItem>> { Data = categories, ErrorHeader = error });
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<UserItem>>>> Users(CancellationToken ct)
    {
        var (users, error) = await expenseService.GetUsersAsync(ct);
        return Ok(new ApiResult<IReadOnlyList<UserItem>> { Data = users, ErrorHeader = error });
    }
}
