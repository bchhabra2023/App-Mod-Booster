using ExpenseManagementApp.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagementApp.Services;

public sealed class ExpenseService(IConfiguration configuration) : IExpenseService
{
    private readonly string _connectionString = configuration.GetConnectionString("ExpenseDb") ?? string.Empty;

    public async Task<(IReadOnlyList<ExpenseItem> Expenses, string? ErrorHeader)> GetExpensesAsync(string? status, string? userEmail, CancellationToken ct)
    {
        try
        {
            var output = new List<ExpenseItem>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("usp_get_expenses", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@statusName", string.IsNullOrWhiteSpace(status) ? DBNull.Value : status);
            cmd.Parameters.AddWithValue("@userEmail", string.IsNullOrWhiteSpace(userEmail) ? DBNull.Value : userEmail);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                output.Add(new ExpenseItem(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetDecimal(5),
                    reader.GetString(6),
                    DateOnly.FromDateTime(reader.GetDateTime(7)),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : reader.GetString(9),
                    reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    reader.IsDBNull(12) ? null : reader.GetString(12)));
            }

            return (output, null);
        }
        catch (Exception ex)
        {
            return (DummyData.Expenses, ErrorDetailsFormatter.BuildHeader(ex));
        }
    }

    public async Task<(IReadOnlyList<CategoryItem> Categories, string? ErrorHeader)> GetCategoriesAsync(CancellationToken ct)
    {
        try
        {
            var output = new List<CategoryItem>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("usp_get_categories", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                output.Add(new CategoryItem(reader.GetInt32(0), reader.GetString(1)));
            }

            return (output, null);
        }
        catch (Exception ex)
        {
            return (DummyData.Categories, ErrorDetailsFormatter.BuildHeader(ex));
        }
    }

    public async Task<(IReadOnlyList<UserItem> Users, string? ErrorHeader)> GetUsersAsync(CancellationToken ct)
    {
        try
        {
            var output = new List<UserItem>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("usp_get_users", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                output.Add(new UserItem(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }

            return (output, null);
        }
        catch (Exception ex)
        {
            return (DummyData.Users, ErrorDetailsFormatter.BuildHeader(ex));
        }
    }

    public async Task<(bool Success, string? ErrorHeader)> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("usp_create_expense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@userEmail", request.UserEmail);
            cmd.Parameters.AddWithValue("@categoryName", request.CategoryName);
            cmd.Parameters.AddWithValue("@amount", request.Amount);
            cmd.Parameters.AddWithValue("@expenseDate", request.ExpenseDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@description", request.Description);
            await cmd.ExecuteNonQueryAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ErrorDetailsFormatter.BuildHeader(ex));
        }
    }

    public async Task<(bool Success, string? ErrorHeader)> UpdateExpenseStatusAsync(int expenseId, UpdateExpenseStatusRequest request, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("usp_update_expense_status", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@expenseId", expenseId);
            cmd.Parameters.AddWithValue("@managerEmail", request.ManagerEmail);
            cmd.Parameters.AddWithValue("@statusName", request.StatusName);
            await cmd.ExecuteNonQueryAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ErrorDetailsFormatter.BuildHeader(ex));
        }
    }
}

public static class DummyData
{
    public static readonly IReadOnlyList<ExpenseItem> Expenses =
    [
        new(1001, "Alice Example", "alice@example.co.uk", "Travel", "Submitted", 25.40m, "GBP", new DateOnly(2025, 10, 20), "Taxi from airport", "/receipts/alice/taxi_oct20.jpg", DateTime.UtcNow.AddDays(-2), null, null),
        new(1002, "Alice Example", "alice@example.co.uk", "Meals", "Approved", 14.25m, "GBP", new DateOnly(2025, 9, 15), "Client lunch", "/receipts/alice/lunch_sep15.jpg", DateTime.UtcNow.AddDays(-90), DateTime.UtcNow.AddDays(-89), "Bob Manager")
    ];

    public static readonly IReadOnlyList<CategoryItem> Categories =
    [
        new(1, "Travel"), new(2, "Meals"), new(3, "Supplies"), new(4, "Accommodation"), new(5, "Other")
    ];

    public static readonly IReadOnlyList<UserItem> Users =
    [
        new(1, "Alice Example", "alice@example.co.uk"),
        new(2, "Bob Manager", "bob.manager@example.co.uk")
    ];
}
