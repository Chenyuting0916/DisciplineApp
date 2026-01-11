using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DisciplineApp.Services;

/// <summary>
/// Service for managing user categories
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetCategoriesAsync(string userId)
    {
        return await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.UserId == null ? 0 : 1) // System categories first
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(string userId, string name, string colorCode)
    {
        // Validate color code format
        if (!IsValidHexColor(colorCode))
        {
            colorCode = "#808080"; // Default to grey if invalid
        }

        var category = new Category
        {
            Name = name.Trim(),
            ColorCode = colorCode,
            UserId = userId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        return category;
    }

    public async Task<Category?> UpdateCategoryAsync(int categoryId, string userId, string name, string colorCode)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        
        // Validate: category exists, user owns it (not a system category)
        if (category == null || category.UserId != userId)
        {
            return null;
        }

        // Validate color code format
        if (!IsValidHexColor(colorCode))
        {
            colorCode = category.ColorCode; // Keep existing color if invalid
        }

        category.Name = name.Trim();
        category.ColorCode = colorCode;

        await _context.SaveChangesAsync();
        
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, string userId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        
        // Validate: category exists
        if (category == null)
        {
            return false;
        }

        // Check if category is in use
        if (await IsCategoryInUseAsync(categoryId))
        {
            return false;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> IsCategoryInUseAsync(int categoryId)
    {
        return await _context.UserTasks
            .AnyAsync(t => t.CategoryId == categoryId);
    }

    /// <summary>
    /// Validates if a string is a valid hex color code
    /// </summary>
    private bool IsValidHexColor(string colorCode)
    {
        if (string.IsNullOrWhiteSpace(colorCode))
            return false;

        // Match #RGB or #RRGGBB format
        var hexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
        return Regex.IsMatch(colorCode, hexColorPattern);
    }
}
