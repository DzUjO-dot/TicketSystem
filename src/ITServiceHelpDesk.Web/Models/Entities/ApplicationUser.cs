using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Użytkownik aplikacji - rozszerzenie IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Dział")]
    public string? Department { get; set; }

    [StringLength(20)]
    [Display(Name = "Numer wewnętrzny")]
    public string? PhoneExtension { get; set; }

    [StringLength(255)]
    [Display(Name = "Avatar")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "Aktywny")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Ostatnie logowanie")]
    public DateTime? LastLoginAt { get; set; }

    [Display(Name = "Usunięty")]
    public bool IsDeleted { get; set; } = false;

    [Display(Name = "Data usunięcia")]
    public DateTime? DeletedAt { get; set; }

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Pełne imię i nazwisko użytkownika
    /// </summary>
    [Display(Name = "Imię i nazwisko")]
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Inicjały użytkownika (do awatara)
    /// </summary>
    public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpper();

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Zgłoszenia utworzone przez użytkownika
    /// </summary>
    public virtual ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

    /// <summary>
    /// Zgłoszenia przypisane do użytkownika (agent/admin)
    /// </summary>
    public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

    /// <summary>
    /// Komentarze dodane przez użytkownika
    /// </summary>
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

    /// <summary>
    /// Powiadomienia użytkownika
    /// </summary>
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    /// <summary>
    /// Załączniki przesłane przez użytkownika
    /// </summary>
    public virtual ICollection<TicketAttachment> UploadedAttachments { get; set; } = new List<TicketAttachment>();

    /// <summary>
    /// Historia zmian dokonanych przez użytkownika
    /// </summary>
    public virtual ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
}
