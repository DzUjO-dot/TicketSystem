using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.Enums;

/// <summary>
/// Status zgłoszenia w systemie HelpDesk
/// </summary>
public enum TicketStatus
{
    [Display(Name = "Nowy")]
    New = 0,

    [Display(Name = "W realizacji")]
    InProgress = 2,

    [Display(Name = "Oczekuje na użytkownika")]
    WaitingForUser = 3,

    [Display(Name = "Rozwiązany")]
    Resolved = 4,
}
