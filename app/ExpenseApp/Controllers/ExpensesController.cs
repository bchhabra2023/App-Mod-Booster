using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>Get all expenses</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var (data, error) = await _expenseService.GetAllExpensesAsync();
        if (error?.HasError == true)
            return Ok(new { data, warning = error.DisplayMessage });
        return Ok(data);
    }

    /// <summary>Get a single expense by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var (data, error) = await _expenseService.GetExpenseByIdAsync(id);
        if (data is null && error is null)
            return NotFound(new { message = $"Expense {id} not found" });
        if (error?.HasError == true)
            return Ok(new { data, warning = error.DisplayMessage });
        return Ok(data);
    }

    /// <summary>Get expenses filtered by status name</summary>
    [HttpGet("status/{statusName}")]
    public async Task<IActionResult> GetByStatus(string statusName)
    {
        var (data, error) = await _expenseService.GetExpensesByStatusAsync(statusName);
        if (error?.HasError == true)
            return Ok(new { data, warning = error.DisplayMessage });
        return Ok(data);
    }

    /// <summary>Get all submitted expenses pending approval</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var (data, error) = await _expenseService.GetPendingExpensesAsync();
        if (error?.HasError == true)
            return Ok(new { data, warning = error.DisplayMessage });
        return Ok(data);
    }

    /// <summary>Create a new expense (Draft status)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request)
    {
        var (newId, error) = await _expenseService.CreateExpenseAsync(request);
        if (error?.HasError == true)
            return BadRequest(new { error = error.DisplayMessage });
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { expenseId = newId });
    }

    /// <summary>Submit an expense (Draft → Submitted)</summary>
    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var (success, error) = await _expenseService.SubmitExpenseAsync(id);
        if (error?.HasError == true)
            return BadRequest(new { error = error.DisplayMessage });
        if (!success)
            return BadRequest(new { message = "Expense could not be submitted. It may not be in Draft status." });
        return Ok(new { success = true });
    }

    /// <summary>Approve a submitted expense</summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] UpdateExpenseRequest request)
    {
        var (success, error) = await _expenseService.ApproveExpenseAsync(id, request.ReviewedBy);
        if (error?.HasError == true)
            return BadRequest(new { error = error.DisplayMessage });
        if (!success)
            return BadRequest(new { message = "Expense could not be approved. It may not be in Submitted status." });
        return Ok(new { success = true });
    }

    /// <summary>Reject a submitted expense</summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] UpdateExpenseRequest request)
    {
        var (success, error) = await _expenseService.RejectExpenseAsync(id, request.ReviewedBy);
        if (error?.HasError == true)
            return BadRequest(new { error = error.DisplayMessage });
        if (!success)
            return BadRequest(new { message = "Expense could not be rejected. It may not be in Submitted status." });
        return Ok(new { success = true });
    }
}
