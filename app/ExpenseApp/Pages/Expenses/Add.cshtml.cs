using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages.Expenses;

public class AddModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<User> Users { get; private set; } = [];
    public List<Category> Categories { get; private set; } = [];
    public int DefaultUserId { get; private set; } = 1;
    public string DefaultUserName { get; private set; } = "Alice Example";
    public ErrorBanner? Error { get; private set; }

    public AddModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        var (users, usersError) = await _expenseService.GetAllUsersAsync();
        var (cats, categoriesError) = await _expenseService.GetAllCategoriesAsync();
        Users = users;
        Categories = cats;
        DefaultUserId = Users.FirstOrDefault()?.UserId ?? 1;
        DefaultUserName = Users.FirstOrDefault()?.UserName ?? "Alice Example";
        Error = usersError ?? categoriesError;
    }

    public async Task<IActionResult> OnPostAsync(
        int UserId, int CategoryId, decimal AmountGbp, DateTime ExpenseDate, string? Description)
    {
        var (users, usersError) = await _expenseService.GetAllUsersAsync();
        var (cats, categoriesError) = await _expenseService.GetAllCategoriesAsync();
        Users = users;
        Categories = cats;
        DefaultUserId = Users.FirstOrDefault()?.UserId ?? UserId;
        DefaultUserName = Users.FirstOrDefault(u => u.UserId == DefaultUserId)?.UserName ?? "Alice Example";
        Error = usersError ?? categoriesError;

        var request = new CreateExpenseRequest
        {
            UserId      = UserId,
            CategoryId  = CategoryId,
            AmountMinor = (int)Math.Round(AmountGbp * 100),
            ExpenseDate = ExpenseDate,
            Description = Description,
            ReceiptFile = null,
        };

        var (newId, error) = await _expenseService.CreateExpenseAsync(request);

        if (error?.HasError == true)
        {
            Error = error;
            return Page();
        }

        TempData["Success"] = $"Expense #{newId} created successfully.";
        return RedirectToPage("/Expenses/Add");
    }
}
