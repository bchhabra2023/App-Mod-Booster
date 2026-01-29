using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(CategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetAll()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new { error = "Failed to retrieve categories", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet("statuses")]
    public async Task<ActionResult<List<ExpenseStatus>>> GetStatuses()
    {
        try
        {
            var statuses = await _categoryService.GetAllStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statuses");
            return StatusCode(500, new { error = "Failed to retrieve statuses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<List<Role>>> GetRoles()
    {
        try
        {
            var roles = await _categoryService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new { error = "Failed to retrieve roles", details = ex.Message });
        }
    }
}
