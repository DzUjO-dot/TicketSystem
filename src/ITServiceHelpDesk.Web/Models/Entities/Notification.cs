using ITServiceHelpDesk.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Powiadomienie systemowe dla użytkownika
/// </summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Użytkownik")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Tytuł")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Wiadomość")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Typ powiadomienia")]
    public NotificationType Type { get; set; } = NotificationType.System;

    [Display(Name = "Powiązane zgłoszenie")]
    public int? RelatedTicketId { get; set; }

    [Display(Name = "Przeczytane")]
    public bool IsRead { get; set; } = false;

    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Data odczytania")]
    public DateTime? ReadAt { get; set; }

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Ikona dla typu powiadomienia (Bootstrap Icons)
    /// </summary>
    [NotMapped]
    public string TypeIcon => Type switch
    {
        NotificationType.TicketCreated => "bi-ticket-perforated",
        NotificationType.TicketAssigned => "bi-person-check",
        NotificationType.TicketStatusChanged => "bi-arrow-repeat",
        NotificationType.TicketCommented => "bi-chat-left-text",
        NotificationType.TicketResolved => "bi-check-circle",
        NotificationType.TicketClosed => "bi-x-circle",
        NotificationType.System => "bi-bell",
        _ => "bi-info-circle"
    };

    /// <summary>
    /// Kolor dla typu powiadomienia
    /// </summary>
    [NotMapped]
    public string TypeColor => Type switch
    {
        NotificationType.TicketCreated => "primary",
        NotificationType.TicketAssigned => "info",
        NotificationType.TicketStatusChanged => "warning",
        NotificationType.TicketCommented => "secondary",
        NotificationType.TicketResolved => "success",
        NotificationType.TicketClosed => "dark",
        NotificationType.System => "light",
        _ => "secondary"
    };

    /// <summary>
    /// Czas od utworzenia powiadomienia w formacie względnym
    /// </summary>
    [NotMapped]
    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.Now - CreatedAt;
            
            if (timeSpan.TotalMinutes < 1)
                return "przed chwilą";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min temu";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} godz. temu";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} dni temu";
            
            return CreatedAt.ToString("dd.MM.yyyy");
        }
    }

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Użytkownik, do którego należy powiadomienie
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Powiązane zgłoszenie (jeśli dotyczy)
    /// </summary>
    [ForeignKey(nameof(RelatedTicketId))]
    public virtual Ticket? RelatedTicket { get; set; }
}
