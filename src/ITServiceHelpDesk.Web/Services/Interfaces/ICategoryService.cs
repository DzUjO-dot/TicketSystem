using ITServiceHelpDesk.Models.Entities;

namespace ITServiceHelpDesk.Services.Interfaces;

/// <summary>
/// Interfejs serwisu do zarządzania kategoriami
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Pobiera wszystkie aktywne kategorie
    /// </summary>
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    
    /// <summary>
    /// Pobiera wszystkie kategorie (włącznie z nieaktywnymi)
    /// </summary>
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    
    /// <summary>
    /// Pobiera kategorię po ID
    /// </summary>
    Task<Category?> GetByIdAsync(int id);
    
    /// <summary>
    /// Tworzy nową kategorię
    /// </summary>
    Task<Category> CreateAsync(Category category);
    
    /// <summary>
    /// Aktualizuje kategorię
    /// </summary>
    Task<bool> UpdateAsync(Category category);
    
    /// <summary>
    /// Dezaktywuje kategorię (soft delete)
    /// </summary>
    Task<bool> DeactivateAsync(int id);
    
    /// <summary>
    /// Aktywuje kategorię
    /// </summary>
    Task<bool> ActivateAsync(int id);
    
    /// <summary>
    /// Sprawdza czy nazwa kategorii jest unikalna
    /// </summary>
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null);
    
    /// <summary>
    /// Pobiera liczbę zgłoszeń w kategorii
    /// </summary>
    Task<int> GetTicketCountAsync(int categoryId);
}
