namespace ExpenseManagementApp.Models;

public sealed record ExpenseItem(
    int ExpenseId,
    string UserName,
    string UserEmail,
    string Category,
    string Status,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string? Description,
    string? ReceiptFile,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewedBy);

public sealed record CategoryItem(int CategoryId, string CategoryName);
public sealed record UserItem(int UserId, string UserName, string Email);

public sealed class CreateExpenseRequest
{
    public string UserEmail { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class UpdateExpenseStatusRequest
{
    public string ManagerEmail { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
}

public sealed class ChatRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public sealed class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public bool UsedDummy { get; set; }
}

public sealed class ApiResult<T>
{
    public T? Data { get; set; }
    public string? ErrorHeader { get; set; }
}
