using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ExpenseService _expenseService;
    private readonly UserService _userService;
    private readonly CategoryService _categoryService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense>? Expenses { get; set; }
    public List<Expense>? PendingExpenses { get; set; }
    public List<User>? Users { get; set; }
    public List<Category>? Categories { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public IndexModel(
        ExpenseService expenseService,
        UserService userService,
        CategoryService categoryService,
        DatabaseService databaseService,
        ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _userService = userService;
        _categoryService = categoryService;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Try to load data from database
            Expenses = await _expenseService.GetAllExpensesAsync();
            PendingExpenses = await _expenseService.GetExpensesByStatusAsync("Submitted");
            Users = await _userService.GetAllUsersAsync();
            Categories = await _categoryService.GetAllCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading page data");
            
            // Return dummy data for demonstration when database is not available
            ErrorMessage = "Unable to connect to database. Displaying demo data.";
            ErrorDetails = BuildErrorDetails(ex);
            
            LoadDummyData();
        }
    }

    private void LoadDummyData()
    {
        // Sample data for demonstration
        var dummyExpenses = new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                Amount = 25.40m,
                AmountMinor = 2540,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.Now.AddDays(-5),
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                Amount = 14.25m,
                AmountMinor = 1425,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-15),
                Description = "Client lunch meeting",
                SubmittedAt = DateTime.Now.AddDays(-14),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-13),
                CreatedAt = DateTime.Now.AddDays(-15)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 1,
                StatusName = "Draft",
                Amount = 7.99m,
                AmountMinor = 799,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-1),
                Description = "Office stationery",
                CreatedAt = DateTime.Now.AddDays(-1)
            }
        };

        Expenses = dummyExpenses;
        PendingExpenses = dummyExpenses.Where(e => e.StatusName == "Submitted").ToList();
        
        Users = new List<User>
        {
            new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-6) },
            new User { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-12) }
        };
        
        Categories = new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new Category { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private string BuildErrorDetails(Exception ex)
    {
        var connInfo = _databaseService.GetConnectionInfo();
        var errorLocation = $"{ex.Source ?? "Unknown"} - {ex.TargetSite?.Name ?? "Unknown method"}";
        
        var details = $"Connection: {connInfo}\n";
        details += $"Location: {errorLocation}\n";
        details += $"Error Type: {ex.GetType().Name}\n";
        details += $"Message: {ex.Message}\n\n";
        
        // Check if it's a managed identity issue
        if (ex.Message.Contains("managed identity", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            details += "MANAGED IDENTITY FIX:\n";
            details += "1. Ensure the Managed Identity is created and assigned to the App Service\n";
            details += "2. Run the database role script: python3 run-sql-dbrole.py\n";
            details += "3. Set the ManagedIdentityClientId in App Service configuration:\n";
            details += "   az webapp config appsettings set --name <app-name> --resource-group <rg-name> --settings \"ManagedIdentityClientId=<client-id>\"\n";
            details += "4. Restart the App Service after configuration changes\n";
        }
        
        if (ex.Message.Contains("firewall", StringComparison.OrdinalIgnoreCase))
        {
            details += "\nFIREWALL FIX:\n";
            details += "Add your IP to the SQL Server firewall rules in Azure Portal or via CLI\n";
        }
        
        return details;
    }
}
