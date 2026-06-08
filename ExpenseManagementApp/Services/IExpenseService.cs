using ExpenseManagementApp.Models;

namespace ExpenseManagementApp.Services;

public interface IExpenseService
{
    Task<(IReadOnlyList<ExpenseItem> Expenses, string? ErrorHeader)> GetExpensesAsync(string? status, string? userEmail, CancellationToken ct);
    Task<(IReadOnlyList<CategoryItem> Categories, string? ErrorHeader)> GetCategoriesAsync(CancellationToken ct);
    Task<(IReadOnlyList<UserItem> Users, string? ErrorHeader)> GetUsersAsync(CancellationToken ct);
    Task<(bool Success, string? ErrorHeader)> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct);
    Task<(bool Success, string? ErrorHeader)> UpdateExpenseStatusAsync(int expenseId, UpdateExpenseStatusRequest request, CancellationToken ct);
}
