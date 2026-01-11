using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

/// <summary>
/// Service interface for managing user categories
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Get all categories available to a user (system categories + user's custom categories)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of categories</returns>
    Task<List<Category>> GetCategoriesAsync(string userId);

    /// <summary>
    /// Create a new custom category for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="name">Category name</param>
    /// <param name="colorCode">Hex color code (e.g., #FF5733)</param>
    /// <returns>Created category</returns>
    Task<Category> CreateCategoryAsync(string userId, string name, string colorCode);

    /// <summary>
    /// Update an existing user category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="userId">User ID (for ownership validation)</param>
    /// <param name="name">New category name</param>
    /// <param name="colorCode">New hex color code</param>
    /// <returns>Updated category, or null if not found or user doesn't own it</returns>
    Task<Category?> UpdateCategoryAsync(int categoryId, string userId, string name, string colorCode);

    /// <summary>
    /// Delete a user category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="userId">User ID (for ownership validation)</param>
    /// <returns>True if deleted successfully, false if not found, not owned by user, or in use</returns>
    Task<bool> DeleteCategoryAsync(int categoryId, string userId);

    /// <summary>
    /// Check if a category is being used by any tasks
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <returns>True if category is in use</returns>
    Task<bool> IsCategoryInUseAsync(int categoryId);
}
