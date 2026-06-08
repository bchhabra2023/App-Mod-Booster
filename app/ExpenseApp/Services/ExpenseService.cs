using System.Runtime.CompilerServices;
using ExpenseApp.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseApp.Services;

public interface IExpenseService
{
    Task<(List<Expense> Data, ErrorBanner? Error)> GetAllExpensesAsync();
    Task<(List<Expense> Data, ErrorBanner? Error)> GetExpensesByStatusAsync(string statusName);
    Task<(List<Expense> Data, ErrorBanner? Error)> GetExpensesByUserAsync(int userId);
    Task<(List<Expense> Data, ErrorBanner? Error)> GetPendingExpensesAsync();
    Task<(Expense? Data, ErrorBanner? Error)> GetExpenseByIdAsync(int expenseId);
    Task<(int NewId, ErrorBanner? Error)> CreateExpenseAsync(CreateExpenseRequest request);
    Task<(bool Success, ErrorBanner? Error)> SubmitExpenseAsync(int expenseId);
    Task<(bool Success, ErrorBanner? Error)> ApproveExpenseAsync(int expenseId, int reviewedBy);
    Task<(bool Success, ErrorBanner? Error)> RejectExpenseAsync(int expenseId, int reviewedBy);
    Task<(List<User> Data, ErrorBanner? Error)> GetAllUsersAsync();
    Task<(List<Category> Data, ErrorBanner? Error)> GetAllCategoriesAsync();
    Task<(List<ExpenseStatus> Data, ErrorBanner? Error)> GetAllStatusesAsync();
    Task<(List<ExpenseSummary> Data, ErrorBanner? Error)> GetExpenseSummaryAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration config, ILogger<ExpenseService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Connection helper ─────────────────────────────────────────────────────
    private SqlConnection CreateConnection()
    {
        var cs = _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var useLocalDevAuth = string.IsNullOrWhiteSpace(_config["ManagedIdentityClientId"]) &&
                              !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) &&
                              string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

        if (useLocalDevAuth)
        {
            cs = cs
                .Replace("Authentication=Active Directory Managed Identity", "Authentication=Active Directory Default", StringComparison.OrdinalIgnoreCase)
                .Replace("User Id=placeholder-client-id;", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return new SqlConnection(cs);
    }

    // ── Error builder ─────────────────────────────────────────────────────────
    private static ErrorBanner BuildError(Exception ex, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var msg = ex.Message;
        var isMi = msg.Contains("Managed Identity", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("AZURE_CLIENT_ID", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("DefaultAzureCredential", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("Active Directory", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("ManagedIdentityCredential", StringComparison.OrdinalIgnoreCase);
        return new ErrorBanner
        {
            Message = msg,
            SourceFile = file,
            SourceLine = line,
            IsManagedIdentityError = isMi
        };
    }

    // ── Dummy data ────────────────────────────────────────────────────────────
    private static List<Expense> DummyExpenses() =>
    [
        new() { ExpenseId = 1, UserId = 1, UserName = "Alice Example", CategoryId = 1, CategoryName = "Travel",
                StatusId = 2, StatusName = "Submitted", AmountMinor = 2540, Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-5), Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.UtcNow.AddDays(-4), CreatedAt = DateTime.UtcNow.AddDays(-5) },
        new() { ExpenseId = 2, UserId = 1, UserName = "Alice Example", CategoryId = 2, CategoryName = "Meals",
                StatusId = 3, StatusName = "Approved", AmountMinor = 1425, Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-20), Description = "Client lunch meeting",
                SubmittedAt = DateTime.UtcNow.AddDays(-19), ReviewedBy = 2, ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-18), CreatedAt = DateTime.UtcNow.AddDays(-20) },
        new() { ExpenseId = 3, UserId = 1, UserName = "Alice Example", CategoryId = 3, CategoryName = "Supplies",
                StatusId = 1, StatusName = "Draft", AmountMinor = 799, Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-2), Description = "Office stationery",
                CreatedAt = DateTime.UtcNow.AddDays(-2) },
        new() { ExpenseId = 4, UserId = 1, UserName = "Alice Example", CategoryId = 4, CategoryName = "Accommodation",
                StatusId = 3, StatusName = "Approved", AmountMinor = 12300, Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-55), Description = "Hotel during client visit",
                SubmittedAt = DateTime.UtcNow.AddDays(-54), ReviewedBy = 2, ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-53), CreatedAt = DateTime.UtcNow.AddDays(-55) },
    ];

    private static List<User> DummyUsers() =>
    [
        new() { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", ManagerId = 2, ManagerName = "Bob Manager", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-100) },
        new() { UserId = 2, UserName = "Bob Manager",  Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-100) },
    ];

    private static List<Category> DummyCategories() =>
    [
        new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
        new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
        new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
        new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
        new() { CategoryId = 5, CategoryName = "Other", IsActive = true },
    ];

    private static List<ExpenseStatus> DummyStatuses() =>
    [
        new() { StatusId = 1, StatusName = "Draft" },
        new() { StatusId = 2, StatusName = "Submitted" },
        new() { StatusId = 3, StatusName = "Approved" },
        new() { StatusId = 4, StatusName = "Rejected" },
    ];

    private static List<ExpenseSummary> DummySummary() =>
    [
        new() { StatusName = "Draft",     ExpenseCount = 1, TotalAmountMinor =  799 },
        new() { StatusName = "Submitted", ExpenseCount = 1, TotalAmountMinor = 2540 },
        new() { StatusName = "Approved",  ExpenseCount = 2, TotalAmountMinor = 13725 },
        new() { StatusName = "Rejected",  ExpenseCount = 0, TotalAmountMinor = 0 },
    ];

    // ── Map helpers ───────────────────────────────────────────────────────────
    private static Expense MapExpense(SqlDataReader r) => new()
    {
        ExpenseId     = r.GetInt32(r.GetOrdinal("ExpenseId")),
        UserId        = r.GetInt32(r.GetOrdinal("UserId")),
        UserName      = r.GetString(r.GetOrdinal("UserName")),
        CategoryId    = r.GetInt32(r.GetOrdinal("CategoryId")),
        CategoryName  = r.GetString(r.GetOrdinal("CategoryName")),
        StatusId      = r.GetInt32(r.GetOrdinal("StatusId")),
        StatusName    = r.GetString(r.GetOrdinal("StatusName")),
        AmountMinor   = r.GetInt32(r.GetOrdinal("AmountMinor")),
        Currency      = r.GetString(r.GetOrdinal("Currency")),
        ExpenseDate   = r.GetDateTime(r.GetOrdinal("ExpenseDate")),
        Description   = r.IsDBNull(r.GetOrdinal("Description"))   ? null : r.GetString(r.GetOrdinal("Description")),
        ReceiptFile   = r.IsDBNull(r.GetOrdinal("ReceiptFile"))    ? null : r.GetString(r.GetOrdinal("ReceiptFile")),
        SubmittedAt   = r.IsDBNull(r.GetOrdinal("SubmittedAt"))    ? null : r.GetDateTime(r.GetOrdinal("SubmittedAt")),
        ReviewedBy    = r.IsDBNull(r.GetOrdinal("ReviewedBy"))     ? null : r.GetInt32(r.GetOrdinal("ReviewedBy")),
        ReviewedByName= r.IsDBNull(r.GetOrdinal("ReviewedByName")) ? null : r.GetString(r.GetOrdinal("ReviewedByName")),
        ReviewedAt    = r.IsDBNull(r.GetOrdinal("ReviewedAt"))     ? null : r.GetDateTime(r.GetOrdinal("ReviewedAt")),
        CreatedAt     = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };

    // ── Public methods ────────────────────────────────────────────────────────

    public async Task<(List<Expense> Data, ErrorBanner? Error)> GetAllExpensesAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetAllExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Expense>();
            while (await reader.ReadAsync()) list.Add(MapExpense(reader));
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllExpensesAsync");
            return (DummyExpenses(), BuildError(ex));
        }
    }

    public async Task<(List<Expense> Data, ErrorBanner? Error)> GetExpensesByStatusAsync(string statusName)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpensesByStatus", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@StatusName", statusName);
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Expense>();
            while (await reader.ReadAsync()) list.Add(MapExpense(reader));
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpensesByStatusAsync");
            return (DummyExpenses().Where(e => e.StatusName == statusName).ToList(), BuildError(ex));
        }
    }

    public async Task<(List<Expense> Data, ErrorBanner? Error)> GetExpensesByUserAsync(int userId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpensesByUser", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Expense>();
            while (await reader.ReadAsync()) list.Add(MapExpense(reader));
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpensesByUserAsync");
            return (DummyExpenses().Where(e => e.UserId == userId).ToList(), BuildError(ex));
        }
    }

    public async Task<(List<Expense> Data, ErrorBanner? Error)> GetPendingExpensesAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetPendingExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Expense>();
            while (await reader.ReadAsync()) list.Add(MapExpense(reader));
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPendingExpensesAsync");
            return (DummyExpenses().Where(e => e.StatusName == "Submitted").ToList(), BuildError(ex));
        }
    }

    public async Task<(Expense? Data, ErrorBanner? Error)> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetAllExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var e = MapExpense(reader);
                if (e.ExpenseId == expenseId) return (e, null);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpenseByIdAsync");
            var dummy = DummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
            return (dummy, BuildError(ex));
        }
    }

    public async Task<(int NewId, ErrorBanner? Error)> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.CreateExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId",      request.UserId);
            cmd.Parameters.AddWithValue("@CategoryId",  request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency",    request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return (Convert.ToInt32(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateExpenseAsync");
            return (0, BuildError(ex));
        }
    }

    public async Task<(bool Success, ErrorBanner? Error)> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.SubmitExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            var rows = await cmd.ExecuteScalarAsync();
            return (Convert.ToInt32(rows) > 0, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitExpenseAsync");
            return (false, BuildError(ex));
        }
    }

    public async Task<(bool Success, ErrorBanner? Error)> ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.ApproveExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId",  expenseId);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            var rows = await cmd.ExecuteScalarAsync();
            return (Convert.ToInt32(rows) > 0, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveExpenseAsync");
            return (false, BuildError(ex));
        }
    }

    public async Task<(bool Success, ErrorBanner? Error)> RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.RejectExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId",  expenseId);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            var rows = await cmd.ExecuteScalarAsync();
            return (Convert.ToInt32(rows) > 0, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectExpenseAsync");
            return (false, BuildError(ex));
        }
    }

    public async Task<(List<User> Data, ErrorBanner? Error)> GetAllUsersAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetAllUsers", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<User>();
            while (await reader.ReadAsync())
            {
                list.Add(new User
                {
                    UserId      = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName    = reader.GetString(reader.GetOrdinal("UserName")),
                    Email       = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId      = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName    = reader.GetString(reader.GetOrdinal("RoleName")),
                    ManagerId   = reader.IsDBNull(reader.GetOrdinal("ManagerId"))   ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                    ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
                    IsActive    = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedAt   = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                });
            }
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllUsersAsync");
            return (DummyUsers(), BuildError(ex));
        }
    }

    public async Task<(List<Category> Data, ErrorBanner? Error)> GetAllCategoriesAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetAllCategories", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Category>();
            while (await reader.ReadAsync())
            {
                list.Add(new Category
                {
                    CategoryId   = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive     = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                });
            }
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllCategoriesAsync");
            return (DummyCategories(), BuildError(ex));
        }
    }

    public async Task<(List<ExpenseStatus> Data, ErrorBanner? Error)> GetAllStatusesAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetAllStatuses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<ExpenseStatus>();
            while (await reader.ReadAsync())
            {
                list.Add(new ExpenseStatus
                {
                    StatusId   = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                });
            }
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllStatusesAsync");
            return (DummyStatuses(), BuildError(ex));
        }
    }

    public async Task<(List<ExpenseSummary> Data, ErrorBanner? Error)> GetExpenseSummaryAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpenseSummary", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<ExpenseSummary>();
            while (await reader.ReadAsync())
            {
                list.Add(new ExpenseSummary
                {
                    StatusName       = reader.GetString(reader.GetOrdinal("StatusName")),
                    ExpenseCount     = reader.GetInt32(reader.GetOrdinal("ExpenseCount")),
                    TotalAmountMinor = reader.GetInt32(reader.GetOrdinal("TotalAmountMinor")),
                });
            }
            return (list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpenseSummaryAsync");
            return (DummySummary(), BuildError(ex));
        }
    }
}
