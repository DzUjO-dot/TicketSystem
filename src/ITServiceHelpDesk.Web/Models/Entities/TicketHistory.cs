using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Historia zmian zgłoszenia - audit trail
/// </summary>
public class TicketHistory
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Zgłoszenie")]
    public int TicketId { get; set; }

    [Required]
    [Display(Name = "Użytkownik")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Akcja")]
    public string Action { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Poprzednia wartość")]
    public string? OldValue { get; set; }

    [StringLength(500)]
    [Display(Name = "Nowa wartość")]
    public string? NewValue { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Opis zmiany")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Data")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Ikona dla typu akcji (Bootstrap Icons)
    /// </summary>
    [NotMapped]
    public string ActionIcon => Action.ToLowerInvariant() switch
    {
        "created" or "utworzono" => "bi-plus-circle",
        "status changed" or "zmiana statusu" => "bi-arrow-repeat",
        "priority changed" or "zmiana priorytetu" => "bi-exclamation-triangle",
        "assigned" or "przypisano" => "bi-person-check",
        "unassigned" or "usunięto przypisanie" => "bi-person-dash",
        "comment added" or "dodano komentarz" => "bi-chat-left-text",
        "attachment added" or "dodano załącznik" => "bi-paperclip",
        "resolved" or "rozwiązano" => "bi-check-circle",
        "closed" or "zamknięto" => "bi-x-circle",
        "reopened" or "ponownie otwarto" => "bi-arrow-counterclockwise",
        "category changed" or "zmiana kategorii" => "bi-folder",
        "due date changed" or "zmiana terminu" => "bi-calendar-event",
        _ => "bi-pencil"
    };

    /// <summary>
    /// Kolor dla typu akcji
    /// </summary>
    [NotMapped]
    public string ActionColor => Action.ToLowerInvariant() switch
    {
        "created" or "utworzono" => "success",
        "resolved" or "rozwiązano" => "success",
        "closed" or "zamknięto" => "secondary",
        "priority changed" or "zmiana priorytetu" => "warning",
        "assigned" or "przypisano" => "info",
        _ => "primary"
    };

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Zgłoszenie, którego dotyczy zmiana
    /// </summary>
    [ForeignKey(nameof(TicketId))]
    public virtual Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Użytkownik, który dokonał zmiany
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;
}
