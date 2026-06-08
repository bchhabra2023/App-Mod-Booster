using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages.Expenses;

public class ExpensesIndexModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<Expense> Expenses { get; private set; } = [];
    public ErrorBanner? Error { get; private set; }

    public ExpensesIndexModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        var (data, error) = await _expenseService.GetAllExpensesAsync();
        Expenses = data;
        Error = error;
    }
}
