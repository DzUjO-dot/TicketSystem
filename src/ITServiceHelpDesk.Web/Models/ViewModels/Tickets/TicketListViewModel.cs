using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.ViewModels.Shared;

namespace ITServiceHelpDesk.Models.ViewModels.Tickets;

/// <summary>
/// ViewModel dla widoku listy zgłoszeń
/// </summary>
public class TicketListViewModel
{
    public PaginatedList<TicketListItemViewModel> Tickets { get; set; } = null!;
    public TicketFilterViewModel Filter { get; set; } = new();
    public string ViewTitle { get; set; } = "Zgłoszenia";
    public string ViewDescription { get; set; } = "";
    public bool ShowAssignedTo { get; set; } = true;
    public bool ShowCreatedBy { get; set; } = true;
    public bool CanCreateNew { get; set; } = true;
    public List<Category> Categories { get; set; } = new();
}

/// <summary>
/// ViewModel dla pojedynczego elementu na liście zgłoszeń
/// </summary>
public class TicketListItemViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public Models.Enums.TicketStatus StatusEnum { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string PriorityBadgeClass { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedByInitials { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public string? AssignedToInitials { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int CommentCount { get; set; }
    public int AttachmentCount { get; set; }
    
    // Aliasy dla kompatybilności z widokami
    public string RequesterName => CreatedByName;
    public int CommentsCount => CommentCount;
    public string? Description { get; set; }
    public string StatusDisplay => Status;
    public string PriorityDisplay => Priority;

    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.Now - UpdatedAt;
            
            if (timeSpan.TotalMinutes < 1)
                return "przed chwilą";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min temu";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} godz. temu";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} dni temu";
            
            return UpdatedAt.ToString("dd.MM.yyyy");
        }
    }

    public static TicketListItemViewModel FromTicket(Ticket ticket)
    {
        return new TicketListItemViewModel
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Status = GetStatusDisplayName(ticket.Status),
            StatusBadgeClass = GetStatusBadgeClass(ticket.Status),
            StatusEnum = ticket.Status,
            Priority = GetPriorityDisplayName(ticket.Priority),
            PriorityBadgeClass = GetPriorityBadgeClass(ticket.Priority),
            CategoryName = ticket.Category.Name,
            CategoryIcon = ticket.Category.Icon,
            CategoryColor = ticket.Category.Color,
            CreatedByName = ticket.CreatedBy.FullName,
            CreatedByInitials = ticket.CreatedBy.Initials,
            AssignedToName = ticket.AssignedTo?.FullName,
            AssignedToInitials = ticket.AssignedTo?.Initials,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            DueDate = ticket.DueDate,
            IsOverdue = ticket.IsOverdue,
            CommentCount = ticket.Comments.Count,
            AttachmentCount = ticket.Attachments.Count
        };
    }

    private static string GetStatusDisplayName(Models.Enums.TicketStatus status) => status switch
    {
        Models.Enums.TicketStatus.New => "Nowy",
        Models.Enums.TicketStatus.InProgress => "W realizacji",
        Models.Enums.TicketStatus.WaitingForUser => "Oczekuje",
        Models.Enums.TicketStatus.Resolved => "Rozwiązany",
        _ => status.ToString()
    };

    private static string GetStatusBadgeClass(Models.Enums.TicketStatus status) => status switch
    {
        Models.Enums.TicketStatus.New => "badge-status-new",
        Models.Enums.TicketStatus.InProgress => "badge-status-inprogress",
        Models.Enums.TicketStatus.WaitingForUser => "badge-status-waitingforuser",
        Models.Enums.TicketStatus.Resolved => "badge-status-resolved",
        _ => "badge-status-new"
    };

    private static string GetPriorityDisplayName(Models.Enums.TicketPriority priority) => priority switch
    {
        Models.Enums.TicketPriority.Low => "Niski",
        Models.Enums.TicketPriority.Medium => "Średni",
        Models.Enums.TicketPriority.High => "Wysoki",
        Models.Enums.TicketPriority.Critical => "Krytyczny",
        _ => priority.ToString()
    };

    private static string GetPriorityBadgeClass(Models.Enums.TicketPriority priority) => priority switch
    {
        Models.Enums.TicketPriority.Low => "badge-priority-low",
        Models.Enums.TicketPriority.Medium => "badge-priority-medium",
        Models.Enums.TicketPriority.High => "badge-priority-high",
        Models.Enums.TicketPriority.Critical => "badge-priority-critical",
        _ => "badge-priority-low"
    };
}
