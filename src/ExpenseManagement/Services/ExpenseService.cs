using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Services;

public class ExpenseService
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(DatabaseService dbService, ILogger<ExpenseService> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetAllExpenses", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all expenses");
            throw;
        }
        
        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetExpenseById @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapExpenseFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense {ExpenseId}", expenseId);
            throw;
        }
        
        return null;
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(string statusName)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetExpensesByStatus @StatusName", connection);
            command.Parameters.AddWithValue("@StatusName", statusName);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses by status {StatusName}", statusName);
            throw;
        }
        
        return expenses;
    }

    public async Task<List<Expense>> GetExpensesByUserAsync(int userId)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetExpensesByUser @UserId", connection);
            command.Parameters.AddWithValue("@UserId", userId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses for user {UserId}", userId);
            throw;
        }
        
        return expenses;
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            var amountMinor = (int)(request.Amount * 100);
            
            using var command = new SqlCommand(@"
                EXEC CreateExpense 
                    @UserId, @CategoryId, @AmountMinor, @Currency, 
                    @ExpenseDate, @Description, @ReceiptFile", connection);
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", amountMinor);
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            throw;
        }
    }

    public async Task<bool> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            var amountMinor = (int)(request.Amount * 100);
            
            using var command = new SqlCommand(@"
                EXEC UpdateExpense 
                    @ExpenseId, @CategoryId, @AmountMinor, 
                    @ExpenseDate, @Description, @ReceiptFile", connection);
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", amountMinor);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0) > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", request.ExpenseId);
            throw;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC SubmitExpense @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0) > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC ApproveExpense @ExpenseId, @ReviewerId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0) > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC RejectExpense @ExpenseId, @ReviewerId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0) > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC DeleteExpense @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0) > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    private Expense MapExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
