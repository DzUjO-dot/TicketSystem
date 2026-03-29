using ITServiceHelpDesk.Models.Entities;

namespace ITServiceHelpDesk.Services.Interfaces;

/// <summary>
/// Interfejs serwisu do logowania audytowego
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Loguje akcję (dla zalogowanego użytkownika)
    /// </summary>
    Task LogAsync(string action, string entityType, string entityId, string? oldValues = null, string? newValues = null);
    
    /// <summary>
    /// Loguje akcję z określonym użytkownikiem
    /// </summary>
    Task LogWithUserAsync(string? userId, string action, string entityType, string entityId, string? oldValues = null, string? newValues = null);
    
    /// <summary>
    /// Pobiera logi audytowe z filtrowaniem
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? userId = null,
        string? entityType = null,
        string? action = null,
        int limit = 100);
    
    /// <summary>
    /// Pobiera logi dla konkretnej encji
    /// </summary>
    Task<IEnumerable<AuditLog>> GetEntityLogsAsync(string entityType, string entityId);
}
