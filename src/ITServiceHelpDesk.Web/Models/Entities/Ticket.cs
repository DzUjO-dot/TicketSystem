using ITServiceHelpDesk.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Zgłoszenie (Ticket) w systemie HelpDesk
/// </summary>
public class Ticket
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Numer zgłoszenia")]
    public string TicketNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tytuł zgłoszenia jest wymagany")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tytuł musi mieć od 5 do 200 znaków")]
    [Display(Name = "Tytuł")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Opis zgłoszenia jest wymagany")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Opis musi mieć od 10 do 4000 znaków")]
    [Display(Name = "Opis")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Status")]
    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Required]
    [Display(Name = "Priorytet")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Required]
    [Display(Name = "Kategoria")]
    public int CategoryId { get; set; }

    [Required]
    [Display(Name = "Utworzone przez")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Display(Name = "Przypisane do")]
    public string? AssignedToUserId { get; set; }

    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Data aktualizacji")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Termin realizacji")]
    [DataType(DataType.DateTime)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Data rozwiązania")]
    public DateTime? ResolvedAt { get; set; }

    [Display(Name = "Data zamknięcia")]
    public DateTime? ClosedAt { get; set; }

    [StringLength(2000)]
    [Display(Name = "Podsumowanie rozwiązania")]
    public string? ResolutionSummary { get; set; }

    [Display(Name = "Usunięte")]
    public bool IsDeleted { get; set; } = false;

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Czy zgłoszenie jest przeterminowane
    /// </summary>
    [NotMapped]
    public bool IsOverdue => DueDate.HasValue &&
                             DueDate.Value < DateTime.Now &&
                             Status != TicketStatus.Resolved;

    /// <summary>
    /// Czy zgłoszenie jest otwarte (możliwe do edycji)
    /// </summary>
    [NotMapped]
    public bool IsOpen => Status != TicketStatus.Resolved;

    /// <summary>
    /// Czas od utworzenia zgłoszenia
    /// </summary>
    [NotMapped]
    public TimeSpan Age => DateTime.Now - CreatedAt;

    /// <summary>
    /// Czas do terminu realizacji (ujemny = po terminie)
    /// </summary>
    [NotMapped]
    public TimeSpan? TimeToDeadline => DueDate.HasValue ? DueDate.Value - DateTime.Now : null;

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Kategoria zgłoszenia
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;

    /// <summary>
    /// Użytkownik, który utworzył zgłoszenie
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    /// <summary>
    /// Agent/Admin przypisany do zgłoszenia
    /// </summary>
    [ForeignKey(nameof(AssignedToUserId))]
    public virtual ApplicationUser? AssignedTo { get; set; }

    /// <summary>
    /// Komentarze do zgłoszenia
    /// </summary>
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

    /// <summary>
    /// Załączniki do zgłoszenia
    /// </summary>
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();

    /// <summary>
    /// Historia zmian zgłoszenia
    /// </summary>
    public virtual ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}
