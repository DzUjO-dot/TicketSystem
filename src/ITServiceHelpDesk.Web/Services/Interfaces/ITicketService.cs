using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;
using ITServiceHelpDesk.Models.ViewModels.Shared;
using ITServiceHelpDesk.Models.ViewModels.Tickets;

namespace ITServiceHelpDesk.Services.Interfaces;

/// <summary>
/// Interfejs serwisu do zarządzania zgłoszeniami
/// </summary>
public interface ITicketService
{
    // ============================================
    // TICKET CRUD
    // ============================================
    
    /// <summary>
    /// Tworzy nowe zgłoszenie
    /// </summary>
    Task<Ticket> CreateTicketAsync(TicketCreateViewModel model, string userId);
    
    /// <summary>
    /// Pobiera zgłoszenie po ID
    /// </summary>
    Task<Ticket?> GetTicketByIdAsync(int id);
    
    /// <summary>
    /// Pobiera zgłoszenie po numerze
    /// </summary>
    Task<Ticket?> GetTicketByNumberAsync(string ticketNumber);
    
    /// <summary>
    /// Aktualizuje zgłoszenie
    /// </summary>
    Task<bool> UpdateTicketAsync(TicketEditViewModel model, string userId);
    
    /// <summary>
    /// Soft delete zgłoszenia
    /// </summary>
    Task<bool> DeleteTicketAsync(int id, string userId);

    // ============================================
    // TICKET QUERIES
    // ============================================
    
    /// <summary>
    /// Pobiera zgłoszenia z filtrowaniem i paginacją
    /// </summary>
    Task<PaginatedList<Ticket>> GetTicketsAsync(TicketFilterViewModel filter, int pageIndex, int pageSize);
    
    /// <summary>
    /// Pobiera zgłoszenia użytkownika
    /// </summary>
    Task<PaginatedList<Ticket>> GetUserTicketsAsync(string userId, TicketFilterViewModel? filter, int pageIndex, int pageSize);
    
    /// <summary>
    /// Pobiera zgłoszenia przypisane do agenta
    /// </summary>
    Task<PaginatedList<Ticket>> GetAgentTicketsAsync(string agentId, TicketFilterViewModel? filter, int pageIndex, int pageSize);
    
    /// <summary>
    /// Pobiera nieprzypisane zgłoszenia (dla agentów do przejęcia)
    /// </summary>
    Task<PaginatedList<Ticket>> GetUnassignedTicketsAsync(TicketFilterViewModel? filter, int pageIndex, int pageSize);

    // ============================================
    // TICKET STATUS & ASSIGNMENT
    // ============================================
    
    /// <summary>
    /// Zmienia status zgłoszenia
    /// </summary>
    Task<bool> ChangeStatusAsync(int ticketId, TicketStatus newStatus, string userId, string? resolutionSummary = null);
    
    /// <summary>
    /// Przypisuje zgłoszenie do agenta
    /// </summary>
    Task<bool> AssignTicketAsync(int ticketId, string? agentId, string userId);
    
    /// <summary>
    /// Agent przejmuje zgłoszenie
    /// </summary>
    Task<bool> TakeTicketAsync(int ticketId, string agentId);
    
    /// <summary>
    /// Wznawia zgłoszenie (reopen przez właściciela w ciągu 14 dni od rozwiązania)
    /// </summary>
    Task<bool> ReopenTicketAsync(int ticketId, string userId);

    Task<bool> UpdateContentAsync(int ticketId, string title, string description, string userId);

    Task<bool> UpdateInfoAsync(int ticketId, int categoryId, TicketPriority priority, DateTime? dueDate, string? assignedToUserId, string userId);

    /// <summary>
    /// Zmienia priorytet zgłoszenia
    /// </summary>
    Task<bool> ChangePriorityAsync(int ticketId, TicketPriority newPriority, string userId);
    
    /// <summary>
    /// Zmienia kategorię zgłoszenia
    /// </summary>
    Task<bool> ChangeCategoryAsync(int ticketId, int newCategoryId, string userId);

    // ============================================
    // COMMENTS
    // ============================================
    
    /// <summary>
    /// Dodaje komentarz do zgłoszenia
    /// </summary>
    Task<TicketComment> AddCommentAsync(int ticketId, string content, CommentType type, string userId);
    
    /// <summary>
    /// Pobiera komentarze zgłoszenia
    /// </summary>
    Task<IEnumerable<TicketComment>> GetTicketCommentsAsync(int ticketId, bool includeInternal = false);

    // ============================================
    // ATTACHMENTS
    // ============================================
    
    /// <summary>
    /// Dodaje załącznik do zgłoszenia
    /// </summary>
    Task<TicketAttachment> AddAttachmentAsync(int ticketId, IFormFile file, string userId);
    
    /// <summary>
    /// Pobiera załącznik
    /// </summary>
    Task<TicketAttachment?> GetAttachmentAsync(int attachmentId);
    
    /// <summary>
    /// Usuwa załącznik
    /// </summary>
    Task<bool> DeleteAttachmentAsync(int attachmentId, string userId);

    // ============================================
    // HISTORY
    // ============================================
    
    /// <summary>
    /// Pobiera historię zmian zgłoszenia
    /// </summary>
    Task<IEnumerable<TicketHistory>> GetTicketHistoryAsync(int ticketId);

    // ============================================
    // STATISTICS
    // ============================================
    
    /// <summary>
    /// Pobiera statystyki dla dashboardu użytkownika
    /// </summary>
    Task<UserTicketStatsViewModel> GetUserStatsAsync(string userId);
    
    /// <summary>
    /// Pobiera statystyki dla dashboardu agenta
    /// </summary>
    Task<AgentTicketStatsViewModel> GetAgentStatsAsync(string agentId);
    
    /// <summary>
    /// Pobiera statystyki dla dashboardu admina
    /// </summary>
    Task<AdminTicketStatsViewModel> GetAdminStatsAsync();
    
    /// <summary>
    /// Generuje następny numer zgłoszenia
    /// </summary>
    Task<string> GenerateTicketNumberAsync();

    // ============================================
    // VALIDATION
    // ============================================
    
    /// <summary>
    /// Sprawdza czy użytkownik ma dostęp do zgłoszenia
    /// </summary>
    Task<bool> CanUserAccessTicketAsync(int ticketId, string userId, bool isAgent, bool isAdmin);
}

// ============================================
// STATS VIEW MODELS
// ============================================

public class UserTicketStatsViewModel
{
    public int TotalTickets { get; set; }
    public int NewTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int WaitingForUserTickets { get; set; }
    public int ResolvedTickets { get; set; }
}

public class AgentTicketStatsViewModel
{
    public int AssignedToMe { get; set; }
    public int UnassignedTickets { get; set; }
    public int NewTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int WaitingForUserTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ResolvedToday { get; set; }
    public int OverdueTickets { get; set; }
}

public class AdminTicketStatsViewModel
{
    public int TotalTickets { get; set; }
    public int NewTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int WaitingForUserTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int TotalUsers { get; set; }
    public int TotalAgents { get; set; }
    public int TicketsCreatedToday { get; set; }
    public int TicketsResolvedToday { get; set; }
    public Dictionary<string, int> TicketsByCategory { get; set; } = new();
    public Dictionary<string, int> TicketsByPriority { get; set; } = new();
}
