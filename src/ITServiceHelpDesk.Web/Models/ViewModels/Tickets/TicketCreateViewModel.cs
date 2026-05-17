using ITServiceHelpDesk.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Tickets;

/// <summary>
/// ViewModel do tworzenia nowego zgłoszenia
/// </summary>
public class TicketCreateViewModel
{
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
    public TicketPriority Priority { get; set; } = TicketPriority.Low;

    [Display(Name = "Termin realizacji")]
    [DataType(DataType.DateTime)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Załączniki")]
    public List<IFormFile>? Attachments { get; set; }

    // ============================================
    // SELECT LISTS (dla dropdownów)
    // ============================================
    
    public SelectList? Categories { get; set; }
    public SelectList? Priorities { get; set; }
}
