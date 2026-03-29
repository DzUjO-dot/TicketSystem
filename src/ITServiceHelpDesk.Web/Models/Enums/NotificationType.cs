using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.Enums;

/// <summary>
/// Typ powiadomienia systemowego
/// </summary>
public enum NotificationType
{
    [Display(Name = "Zgłoszenie utworzone")]
    TicketCreated = 0,

    [Display(Name = "Zgłoszenie przypisane")]
    TicketAssigned = 1,

    [Display(Name = "Status zmieniony")]
    TicketStatusChanged = 2,

    [Display(Name = "Nowy komentarz")]
    TicketCommented = 3,

    [Display(Name = "Zgłoszenie rozwiązane")]
    TicketResolved = 4,

    [Display(Name = "Zgłoszenie zamknięte")]
    TicketClosed = 5,

    [Display(Name = "Systemowe")]
    System = 6
}
