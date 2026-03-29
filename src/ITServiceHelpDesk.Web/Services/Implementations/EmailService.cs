using ITServiceHelpDesk.Services.Interfaces;

namespace ITServiceHelpDesk.Services.Implementations;

/// <summary>
/// Implementacja serwisu do wysyłania emaili
/// W wersji studyjnej - mockuje wysyłanie (loguje zamiast wysyłać)
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _isEnabled;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _isEnabled = _configuration.GetValue<bool>("EmailSettings:EnableEmailSending", false);
    }

    public bool IsEmailEnabled()
    {
        return _isEnabled;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new[] { to }, subject, body, isHtml);
    }

    public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation(
                "[EMAIL MOCK] Wysyłka wyłączona. Email: To={To}, Subject={Subject}", 
                string.Join(", ", to), 
                subject);
            return true; // Return success even when mocking
        }

        try
        {
            // W prawdziwej aplikacji tutaj byłby kod wysyłający email przez SMTP
            // Na potrzeby projektu studenckiego - tylko logujemy
            
            var smtpServer = _configuration.GetValue<string>("EmailSettings:SmtpServer");
            var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort");
            var senderEmail = _configuration.GetValue<string>("EmailSettings:SenderEmail");

            _logger.LogInformation(
                "[EMAIL] Wysyłanie: From={From}, To={To}, Subject={Subject}, Server={Server}:{Port}",
                senderEmail,
                string.Join(", ", to),
                subject,
                smtpServer,
                smtpPort);

            // Symulacja opóźnienia wysyłki
            await Task.Delay(100);

            /* 
            // Prawdziwa implementacja SMTP:
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(username, password);
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            
            foreach (var recipient in to)
            {
                mailMessage.To.Add(recipient);
            }
            
            await client.SendMailAsync(mailMessage);
            */

            _logger.LogInformation("[EMAIL] Wysłano pomyślnie do {Recipients}", string.Join(", ", to));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Błąd wysyłki do {Recipients}: {Message}", string.Join(", ", to), ex.Message);
            return false;
        }
    }

    public async Task SendTicketCreatedEmailAsync(string userEmail, string ticketNumber, string ticketTitle)
    {
        var subject = $"[HelpDesk] Zgłoszenie {ticketNumber} - Utworzone";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #7c3aed;'>IT Service HelpDesk</h2>
                    <p>Twoje zgłoszenie zostało utworzone i oczekuje na obsługę.</p>
                    <div style='background: #f4f4f5; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Numer zgłoszenia:</strong> {ticketNumber}</p>
                        <p><strong>Tytuł:</strong> {ticketTitle}</p>
                    </div>
                    <p>Możesz śledzić status zgłoszenia logując się do systemu HelpDesk.</p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #6b7280;'>
                        Ta wiadomość została wygenerowana automatycznie. Prosimy nie odpowiadać na nią.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(userEmail, subject, body);
    }

    public async Task SendTicketStatusChangedEmailAsync(string userEmail, string ticketNumber, string oldStatus, string newStatus)
    {
        var subject = $"[HelpDesk] Zgłoszenie {ticketNumber} - Zmiana statusu";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #7c3aed;'>IT Service HelpDesk</h2>
                    <p>Status Twojego zgłoszenia został zmieniony.</p>
                    <div style='background: #f4f4f5; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Numer zgłoszenia:</strong> {ticketNumber}</p>
                        <p><strong>Poprzedni status:</strong> {oldStatus}</p>
                        <p><strong>Nowy status:</strong> <span style='color: #7c3aed; font-weight: bold;'>{newStatus}</span></p>
                    </div>
                    <p>Zaloguj się do systemu HelpDesk aby zobaczyć szczegóły.</p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #6b7280;'>
                        Ta wiadomość została wygenerowana automatycznie. Prosimy nie odpowiadać na nią.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(userEmail, subject, body);
    }

    public async Task SendTicketCommentEmailAsync(string userEmail, string ticketNumber, string commentAuthor)
    {
        var subject = $"[HelpDesk] Zgłoszenie {ticketNumber} - Nowy komentarz";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #7c3aed;'>IT Service HelpDesk</h2>
                    <p>Do Twojego zgłoszenia został dodany nowy komentarz.</p>
                    <div style='background: #f4f4f5; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Numer zgłoszenia:</strong> {ticketNumber}</p>
                        <p><strong>Autor komentarza:</strong> {commentAuthor}</p>
                    </div>
                    <p>Zaloguj się do systemu HelpDesk aby przeczytać komentarz i odpowiedzieć.</p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #6b7280;'>
                        Ta wiadomość została wygenerowana automatycznie. Prosimy nie odpowiadać na nią.
                    </p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(userEmail, subject, body);
    }
}
