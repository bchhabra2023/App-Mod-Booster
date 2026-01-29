using Microsoft.Data.SqlClient;
using Azure.Identity;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string? _managedIdentityClientId;
    private readonly ILogger<DatabaseService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _managedIdentityClientId = configuration["ManagedIdentityClientId"];
        
        // Get base connection string
        var connString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=tcp:localhost,1433;Database=Northwind;Authentication=Active Directory Default;";
        
        // If running in Azure with Managed Identity, update the connection string
        if (!string.IsNullOrEmpty(_managedIdentityClientId))
        {
            // Extract server and database from connection string
            var builder = new SqlConnectionStringBuilder(connString);
            _connectionString = $"Server={builder.DataSource};Database={builder.InitialCatalog};Authentication=Active Directory Managed Identity;User Id={_managedIdentityClientId};";
            _logger.LogInformation("Using Managed Identity authentication with Client ID: {ClientId}", _managedIdentityClientId);
        }
        else
        {
            _connectionString = connString;
            _logger.LogInformation("Using default authentication");
        }
    }

    public SqlConnection GetConnection()
    {
        try
        {
            var connection = new SqlConnection(_connectionString);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    public string GetConnectionInfo()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            return $"Server: {builder.DataSource}, Database: {builder.InitialCatalog}, Auth: {builder.Authentication}";
        }
        catch
        {
            return "Connection string parsing failed";
        }
    }
}
