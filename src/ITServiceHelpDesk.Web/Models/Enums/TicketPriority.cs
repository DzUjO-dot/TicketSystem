using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.Enums;

/// <summary>
/// Priorytet zgłoszenia
/// </summary>
public enum TicketPriority
{
    [Display(Name = "Niski")]
    Low = 0,

    [Display(Name = "Średni")]
    Medium = 1,

    [Display(Name = "Wysoki")]
    High = 2,

    [Display(Name = "Krytyczny")]
    Critical = 3
}
