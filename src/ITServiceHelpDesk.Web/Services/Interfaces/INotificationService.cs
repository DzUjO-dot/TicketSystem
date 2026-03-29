using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;

namespace ITServiceHelpDesk.Services.Interfaces;

/// <summary>
/// Interfejs serwisu do zarządzania powiadomieniami
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Tworzy powiadomienie
    /// </summary>
    Task<Notification> CreateAsync(string userId, string title, string message, NotificationType type, int? relatedTicketId = null);
    
    /// <summary>
    /// Pobiera nieprzeczytane powiadomienia użytkownika
    /// </summary>
    Task<IEnumerable<Notification>> GetUnreadAsync(string userId, int limit = 10);
    
    /// <summary>
    /// Pobiera wszystkie powiadomienia użytkownika
    /// </summary>
    Task<IEnumerable<Notification>> GetAllAsync(string userId, int limit = 50);
    
    /// <summary>
    /// Oznacza powiadomienie jako przeczytane
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    
    /// <summary>
    /// Oznacza wszystkie powiadomienia użytkownika jako przeczytane
    /// </summary>
    Task<bool> MarkAllAsReadAsync(string userId);
    
    /// <summary>
    /// Pobiera liczbę nieprzeczytanych powiadomień
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);
    
    // ============================================
    // TICKET NOTIFICATIONS
    // ============================================
    
    /// <summary>
    /// Wysyła powiadomienie o utworzeniu zgłoszenia
    /// </summary>
    Task NotifyTicketCreatedAsync(Ticket ticket);
    
    /// <summary>
    /// Wysyła powiadomienie o przypisaniu zgłoszenia
    /// </summary>
    Task NotifyTicketAssignedAsync(Ticket ticket, string agentId);
    
    /// <summary>
    /// Wysyła powiadomienie o zmianie statusu
    /// </summary>
    Task NotifyStatusChangedAsync(Ticket ticket, TicketStatus oldStatus, TicketStatus newStatus);
    
    /// <summary>
    /// Wysyła powiadomienie o nowym komentarzu
    /// </summary>
    Task NotifyCommentAddedAsync(Ticket ticket, TicketComment comment);
    
    /// <summary>
    /// Wysyła powiadomienie o rozwiązaniu zgłoszenia
    /// </summary>
    Task NotifyTicketResolvedAsync(Ticket ticket);
}
