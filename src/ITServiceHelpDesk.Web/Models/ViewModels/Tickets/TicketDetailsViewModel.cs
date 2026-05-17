using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Tickets;

/// <summary>
/// ViewModel do wyświetlania szczegółów zgłoszenia
/// </summary>
public class TicketDetailsViewModel
{
    public int Id { get; set; }
    
    [Display(Name = "Numer zgłoszenia")]
    public string TicketNumber { get; set; } = string.Empty;

    [Display(Name = "Tytuł")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Opis")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Kategoria")]
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }

    [Display(Name = "Status")]
    public TicketStatus Status { get; set; }

    [Display(Name = "Priorytet")]
    public TicketPriority Priority { get; set; }

    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Ostatnia aktualizacja")]
    public DateTime UpdatedAt { get; set; }

    [Display(Name = "Termin realizacji")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Data rozwiązania")]
    public DateTime? ResolvedAt { get; set; }

    [Display(Name = "Data zamknięcia")]
    public DateTime? ClosedAt { get; set; }

    [Display(Name = "Podsumowanie rozwiązania")]
    public string? ResolutionSummary { get; set; }

    // ============================================
    // USER INFO
    // ============================================
    
    [Display(Name = "Utworzone przez")]
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public string CreatedByInitials { get; set; } = string.Empty;
    public string? CreatedByDepartment { get; set; }

    [Display(Name = "Przypisane do")]
    public string? AssignedToName { get; set; }
    public string? AssignedToEmail { get; set; }
    public string? AssignedToInitials { get; set; }
    public string? AssignedToUserId { get; set; }
    
    // Aliasy dla kompatybilności z widokami
    public string RequesterId => CreatedByUserId;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? AssignedToId => AssignedToUserId;
    public string RequesterName => CreatedByName;
    public string StatusDisplay => StatusDisplayName;
    public string PriorityDisplay => PriorityDisplayName;

    // ============================================
    // COMPUTED
    // ============================================
    
    public bool IsOverdue { get; set; }
    public bool IsOpen { get; set; }
    public TimeSpan Age { get; set; }
    public TimeSpan? TimeToDeadline { get; set; }

    // ============================================
    // RELATED DATA
    // ============================================
    
    public List<TicketCommentViewModel> Comments { get; set; } = new();
    public List<TicketAttachmentViewModel> Attachments { get; set; } = new();
    public List<TicketHistoryViewModel> History { get; set; } = new();

    // ============================================
    // ACTIONS (co użytkownik może zrobić)
    // ============================================
    
    public bool CanEdit { get; set; }
    public bool CanComment { get; set; }
    public bool CanChangeStatus { get; set; }
    public bool CanAssign { get; set; }
    public bool CanClose { get; set; }
    public bool CanReopen { get; set; }
    public bool CanAddInternalComment { get; set; }

    // ============================================
    // FOR FORMS
    // ============================================
    
    [Display(Name = "Nowy komentarz")]
    public string? NewComment { get; set; }

    public bool IsInternalComment { get; set; }

    public SelectList? StatusOptions { get; set; }
    public SelectList? AgentOptions { get; set; }
    public SelectList? CategoryOptions { get; set; }
    public SelectList? PriorityOptions { get; set; }

    // ============================================
    // HELPERS
    // ============================================
    
    public string StatusBadgeClass => Status switch
    {
        TicketStatus.New => "badge-status-new",
        TicketStatus.InProgress => "badge-status-inprogress",
        TicketStatus.WaitingForUser => "badge-status-waitingforuser",
        TicketStatus.Resolved => "bg-success",
        _ => "bg-secondary"
    };

    public string PriorityBadgeClass => Priority switch
    {
        TicketPriority.Low => "bg-success",
        TicketPriority.Medium => "bg-warning text-dark",
        TicketPriority.High => "bg-orange",
        TicketPriority.Critical => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusDisplayName => Status switch
    {
        TicketStatus.New => "Nowy",
        TicketStatus.InProgress => "W realizacji",
        TicketStatus.WaitingForUser => "Oczekuje na użytkownika",
        TicketStatus.Resolved => "Rozwiązany",
        _ => Status.ToString()
    };

    public string PriorityDisplayName => Priority switch
    {
        TicketPriority.Low => "Niski",
        TicketPriority.Medium => "Średni",
        TicketPriority.High => "Wysoki",
        TicketPriority.Critical => "Krytyczny",
        _ => Priority.ToString()
    };

    public string DueDateDisplayClass => IsOverdue ? "text-danger fw-bold" : 
        (TimeToDeadline.HasValue && TimeToDeadline.Value.TotalHours < 24 ? "text-warning" : "");

    // ============================================
    // FACTORY METHOD
    // ============================================
    
    public static TicketDetailsViewModel FromTicket(Ticket ticket, bool isAgent, bool isAdmin, string currentUserId)
    {
        var vm = new TicketDetailsViewModel
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryName = ticket.Category.Name,
            CategoryIcon = ticket.Category.Icon,
            CategoryColor = ticket.Category.Color,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            DueDate = ticket.DueDate,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            ResolutionSummary = ticket.ResolutionSummary,
            CreatedByName = ticket.CreatedBy.FullName,
            CreatedByEmail = ticket.CreatedBy.Email ?? "",
            CreatedByInitials = ticket.CreatedBy.Initials,
            CreatedByDepartment = ticket.CreatedBy.Department,
            AssignedToName = ticket.AssignedTo?.FullName,
            AssignedToEmail = ticket.AssignedTo?.Email,
            AssignedToInitials = ticket.AssignedTo?.Initials,
            AssignedToUserId = ticket.AssignedToUserId,
            CreatedByUserId = ticket.CreatedByUserId,
            IsOverdue = ticket.IsOverdue,
            IsOpen = ticket.IsOpen,
            Age = ticket.Age,
            TimeToDeadline = ticket.TimeToDeadline
        };

        // Set permissions
        var isOwner = ticket.CreatedByUserId == currentUserId;
        var isAssignedAgent = ticket.AssignedToUserId == currentUserId;

        vm.CanEdit = (isOwner && ticket.IsOpen) || isAdmin;
        vm.CanComment = ticket.IsOpen || isAgent || isAdmin;
        vm.CanChangeStatus = isAgent || isAdmin;
        vm.CanAssign = isAgent || isAdmin;
        vm.CanClose = (isOwner && ticket.IsOpen) || isAgent || isAdmin;
        vm.CanReopen = isOwner &&
                       ticket.Status == TicketStatus.Resolved &&
                       ticket.ResolvedAt.HasValue &&
                       (DateTime.Now - ticket.ResolvedAt.Value).TotalDays <= 14;
        vm.CanAddInternalComment = isAgent || isAdmin;

        // Map comments
        vm.Comments = ticket.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => TicketCommentViewModel.FromComment(c, isAgent || isAdmin))
            .ToList();

        // Map attachments
        vm.Attachments = ticket.Attachments
            .Select(a => TicketAttachmentViewModel.FromAttachment(a))
            .ToList();

        // Map history
        vm.History = ticket.History
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => TicketHistoryViewModel.FromHistory(h))
            .ToList();

        return vm;
    }
}

// ============================================
// SUB VIEW MODELS
// ============================================

public class TicketCommentViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsInternal { get; set; }
    public bool IsEdited { get; set; }
    public bool IsVisible { get; set; } = true;

    public static TicketCommentViewModel FromComment(TicketComment comment, bool canSeeInternal)
    {
        var vm = new TicketCommentViewModel
        {
            Id = comment.Id,
            Content = comment.Content,
            AuthorName = comment.Author.FullName,
            AuthorInitials = comment.Author.Initials,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsInternal = comment.IsInternal,
            IsEdited = comment.IsEdited,
            IsVisible = !comment.IsInternal || canSeeInternal
        };
        return vm;
    }
}

public class TicketAttachmentViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string FileIcon { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    
    // Aliasy dla kompatybilności z widokami
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileSize => FileSizeFormatted;

    public static TicketAttachmentViewModel FromAttachment(TicketAttachment attachment)
    {
        return new TicketAttachmentViewModel
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            OriginalFileName = attachment.OriginalFileName,
            FileSizeFormatted = attachment.FileSizeFormatted,
            FileIcon = attachment.FileIcon,
            IsImage = attachment.IsImage,
            UploadedByName = attachment.UploadedBy.FullName,
            UploadedAt = attachment.UploadedAt,
            ContentType = attachment.ContentType,
            FilePath = attachment.FilePath
        };
    }
}

public class TicketHistoryViewModel
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ActionIcon { get; set; } = string.Empty;
    public string ActionColor { get; set; } = string.Empty;
    
    // Aliasy dla kompatybilności z widokami
    public string FieldName => Action;
    public DateTime ChangedAt => CreatedAt;
    public string ChangedByName => UserName;

    public static TicketHistoryViewModel FromHistory(TicketHistory history)
    {
        return new TicketHistoryViewModel
        {
            Id = history.Id,
            Action = history.Action,
            OldValue = history.OldValue,
            NewValue = history.NewValue,
            Description = history.Description,
            UserName = history.User.FullName,
            UserInitials = history.User.Initials,
            CreatedAt = history.CreatedAt,
            ActionIcon = history.ActionIcon,
            ActionColor = history.ActionColor
        };
    }
}
