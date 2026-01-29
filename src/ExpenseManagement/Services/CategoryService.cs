using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Services;

public class CategoryService
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(DatabaseService dbService, ILogger<CategoryService> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        var categories = new List<Category>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetAllCategories", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            throw;
        }
        
        return categories;
    }

    public async Task<List<ExpenseStatus>> GetAllStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetAllStatuses", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statuses");
            throw;
        }
        
        return statuses;
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        var roles = new List<Role>();
        
        try
        {
            using var connection = _dbService.GetConnection();
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC GetAllRoles", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    RoleId = reader.GetInt32(0),
                    RoleName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            throw;
        }
        
        return roles;
    }
}
