using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Services.Implementations;

/// <summary>
/// Implementacja serwisu do zarządzania kategoriami
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        category.CreatedAt = DateTime.Now;
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        var existing = await _context.Categories.FindAsync(category.Id);
        if (existing == null) return false;

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.Icon = category.Icon;
        existing.Color = category.Color;
        existing.DisplayOrder = category.DisplayOrder;
        existing.IsActive = category.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        category.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        category.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
    {
        var query = _context.Categories.Where(c => c.Name.ToLower() == name.ToLower());
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<int> GetTicketCountAsync(int categoryId)
    {
        return await _context.Tickets
            .Where(t => t.CategoryId == categoryId)
            .CountAsync();
    }
}
