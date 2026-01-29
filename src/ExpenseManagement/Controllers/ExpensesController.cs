using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(ExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAll()
    {
        try
        {
            var expenses = await _expenseService.GetAllExpensesAsync();
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses");
            return StatusCode(500, new { error = "Failed to retrieve expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetById(int id)
    {
        try
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
                return NotFound(new { error = $"Expense with ID {id} not found" });
            
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to retrieve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expenses by status
    /// </summary>
    [HttpGet("status/{statusName}")]
    public async Task<ActionResult<List<Expense>>> GetByStatus(string statusName)
    {
        try
        {
            var expenses = await _expenseService.GetExpensesByStatusAsync(statusName);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses by status");
            return StatusCode(500, new { error = "Failed to retrieve expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expenses by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Expense>>> GetByUser(int userId)
    {
        try
        {
            var expenses = await _expenseService.GetExpensesByUserAsync(userId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses for user");
            return StatusCode(500, new { error = "Failed to retrieve expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _expenseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = expenseId }, new { expenseId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = "Failed to create expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing expense
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            request.ExpenseId = id;
            var success = await _expenseService.UpdateExpenseAsync(request);
            if (!success)
                return NotFound(new { error = $"Expense with ID {id} not found" });
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            return StatusCode(500, new { error = "Failed to update expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> Submit(int id)
    {
        try
        {
            var success = await _expenseService.SubmitExpenseAsync(id);
            if (!success)
                return NotFound(new { error = $"Expense with ID {id} not found" });
            
            return Ok(new { message = "Expense submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense");
            return StatusCode(500, new { error = "Failed to submit expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> Approve(int id, [FromBody] int reviewerId)
    {
        try
        {
            var success = await _expenseService.ApproveExpenseAsync(id, reviewerId);
            if (!success)
                return NotFound(new { error = $"Expense with ID {id} not found" });
            
            return Ok(new { message = "Expense approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            return StatusCode(500, new { error = "Failed to approve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> Reject(int id, [FromBody] int reviewerId)
    {
        try
        {
            var success = await _expenseService.RejectExpenseAsync(id, reviewerId);
            if (!success)
                return NotFound(new { error = $"Expense with ID {id} not found" });
            
            return Ok(new { message = "Expense rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            return StatusCode(500, new { error = "Failed to reject expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete an expense
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var success = await _expenseService.DeleteExpenseAsync(id);
            if (!success)
                return NotFound(new { error = $"Expense with ID {id} not found" });
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            return StatusCode(500, new { error = "Failed to delete expense", details = ex.Message });
        }
    }
}
