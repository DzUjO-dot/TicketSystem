namespace ITServiceHelpDesk.Services.Interfaces;

/// <summary>
/// Interfejs serwisu do wysyłania emaili
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Wysyła email
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    
    /// <summary>
    /// Wysyła email do wielu odbiorców
    /// </summary>
    Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true);
    
    /// <summary>
    /// Wysyła powiadomienie o nowym zgłoszeniu
    /// </summary>
    Task SendTicketCreatedEmailAsync(string userEmail, string ticketNumber, string ticketTitle);
    
    /// <summary>
    /// Wysyła powiadomienie o zmianie statusu zgłoszenia
    /// </summary>
    Task SendTicketStatusChangedEmailAsync(string userEmail, string ticketNumber, string oldStatus, string newStatus);
    
    /// <summary>
    /// Wysyła powiadomienie o nowym komentarzu
    /// </summary>
    Task SendTicketCommentEmailAsync(string userEmail, string ticketNumber, string commentAuthor);
    
    /// <summary>
    /// Sprawdza czy wysyłanie emaili jest włączone
    /// </summary>
    bool IsEmailEnabled();
}
