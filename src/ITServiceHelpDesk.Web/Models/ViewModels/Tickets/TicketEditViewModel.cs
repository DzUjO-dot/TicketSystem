using ITServiceHelpDesk.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Tickets;

/// <summary>
/// ViewModel do edycji zgłoszenia
/// </summary>
public class TicketEditViewModel
{
    public int Id { get; set; }
    
    [Display(Name = "Numer zgłoszenia")]
    public string TicketNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tytuł jest wymagany")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tytuł musi mieć od 5 do 200 znaków")]
    [Display(Name = "Tytuł")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Opis jest wymagany")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Opis musi mieć od 10 do 4000 znaków")]
    [Display(Name = "Opis")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategoria jest wymagana")]
    [Display(Name = "Kategoria")]
    public int CategoryId { get; set; }

    [Required]
    [Display(Name = "Priorytet")]
    public TicketPriority Priority { get; set; }

    [Display(Name = "Status")]
    public TicketStatus Status { get; set; }

    [Display(Name = "Termin realizacji")]
    [DataType(DataType.DateTime)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Przypisany do")]
    public string? AssignedToUserId { get; set; }

    [Display(Name = "Nowe załączniki")]
    public List<IFormFile>? NewAttachments { get; set; }

    // ============================================
    // SELECT LISTS
    // ============================================
    
    public SelectList? Categories { get; set; }
    public SelectList? Priorities { get; set; }
    public SelectList? Statuses { get; set; }
    public SelectList? Agents { get; set; }

    // ============================================
    // READONLY INFO
    // ============================================
    
    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Ostatnia aktualizacja")]
    public DateTime? UpdatedAt { get; set; }

    [Display(Name = "Utworzone przez")]
    public string CreatedByName { get; set; } = string.Empty;
    
    // Aliasy dla kompatybilności z widokami
    public string? AssignedToId => AssignedToUserId;
    public string RequesterName => CreatedByName;
    public List<ExistingAttachmentViewModel>? ExistingAttachments { get; set; }
    
    public string StatusDisplay => Status switch
    {
        TicketStatus.New => "Nowy",
        TicketStatus.InProgress => "W realizacji",
        TicketStatus.WaitingForUser => "Oczekuje na użytkownika",
        TicketStatus.Resolved => "Rozwiązany",
        _ => Status.ToString()
    };
}

public class ExistingAttachmentViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
}
