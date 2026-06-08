using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public CategoriesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>Get all active expense categories</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (data, error) = await _expenseService.GetAllCategoriesAsync();
        if (error?.HasError == true)
            return Ok(new { data, warning = error.DisplayMessage });
        return Ok(data);
    }
}
