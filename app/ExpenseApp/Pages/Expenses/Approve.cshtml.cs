using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages.Expenses;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<Expense> PendingExpenses { get; private set; } = [];
    public ErrorBanner? Error { get; private set; }

    /// <summary>
    /// The user ID of the reviewing manager. Can be passed via ?reviewerId=N query parameter.
    /// Defaults to 2 (Bob Manager from seed data) for demo purposes.
    /// In a production app this would come from authenticated user claims.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int ReviewerUserId { get; set; } = 2;

    public ApproveModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        var (data, error) = await _expenseService.GetPendingExpensesAsync();
        PendingExpenses = data;
        Error = error;
    }
}
