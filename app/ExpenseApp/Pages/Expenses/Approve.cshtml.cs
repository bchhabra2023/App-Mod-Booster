using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages.Expenses;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<Expense> PendingExpenses { get; private set; } = [];
    public ErrorBanner? Error { get; private set; }

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
