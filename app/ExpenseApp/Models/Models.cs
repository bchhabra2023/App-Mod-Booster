namespace ExpenseApp.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AmountMinor { get; set; }  // in pence
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Amount as a decimal pounds value (AmountMinor / 100)</summary>
    public decimal AmountGbp => AmountMinor / 100m;
}

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class ExpenseSummary
{
    public string StatusName { get; set; } = string.Empty;
    public int ExpenseCount { get; set; }
    public int TotalAmountMinor { get; set; }
    public decimal TotalAmountGbp => TotalAmountMinor / 100m;
}

public class CreateExpenseRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
}

public class UpdateExpenseRequest
{
    public int ReviewedBy { get; set; }
}

public class ErrorBanner
{
    public bool HasError => !string.IsNullOrEmpty(Message);
    public string Message { get; set; } = string.Empty;
    public string? SourceFile { get; set; }
    public int? SourceLine { get; set; }
    public bool IsManagedIdentityError { get; set; }

    public string DisplayMessage
    {
        get
        {
            var location = SourceFile is not null
                ? $" [{System.IO.Path.GetFileName(SourceFile)}:{SourceLine}]"
                : string.Empty;

            var miHint = IsManagedIdentityError
                ? " — Managed Identity Fix: Ensure the App Service has a User-Assigned Managed Identity assigned and AZURE_CLIENT_ID app setting is set to the identity's Client ID. For local dev, use 'Authentication=Active Directory Default' in the connection string and run 'az login'."
                : string.Empty;

            return $"{Message}{location}{miHint}";
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
}
