using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Services;

public class UserService
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<UserService> _logger;

    public UserService(DatabaseService dbService, ILogger<UserService> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetAllUsers", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(MapUserFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
        
        return users;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetUserById @UserId", connection);
            command.Parameters.AddWithValue("@UserId", userId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            throw;
        }
        
        return null;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetUserByEmail @Email", connection);
            command.Parameters.AddWithValue("@Email", email);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            throw;
        }
        
        return null;
    }

    public async Task<int> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand(@"
                EXEC CreateUser @UserName, @Email, @RoleId, @ManagerId", connection);
            
            command.Parameters.AddWithValue("@UserName", request.UserName);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@RoleId", request.RoleId);
            command.Parameters.AddWithValue("@ManagerId", (object?)request.ManagerId ?? DBNull.Value);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    private User MapUserFromReader(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
