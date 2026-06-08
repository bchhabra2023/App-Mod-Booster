using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<ExpenseSummary> Summary { get; private set; } = [];
    public List<Expense> RecentExpenses { get; private set; } = [];
    public ErrorBanner? Error { get; private set; }

    public IndexModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        var (summary, summaryError) = await _expenseService.GetExpenseSummaryAsync();
        var (all, allError) = await _expenseService.GetAllExpensesAsync();

        Summary = summary;
        RecentExpenses = all.Take(5).ToList();
        Error = summaryError ?? allError;
    }
}
