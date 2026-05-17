using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Services.Implementations;

/// <summary>
/// Implementacja serwisu do zarządzania powiadomieniami
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public NotificationService(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Notification> CreateAsync(string userId, string title, string message, NotificationType type, int? relatedTicketId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedTicketId = relatedTicketId,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task<IEnumerable<Notification>> GetUnreadAsync(string userId, int limit = 10)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetAllAsync(string userId, int limit = 50)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        
        if (notification == null) return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    // ============================================
    // TICKET NOTIFICATIONS
    // ============================================

    public async Task NotifyTicketCreatedAsync(Ticket ticket)
    {
        // Load ticket with related data if not already loaded
        if (ticket.CreatedBy == null)
        {
            await _context.Entry(ticket).Reference(t => t.CreatedBy).LoadAsync();
        }

        // Notify the creator
        await CreateAsync(
            ticket.CreatedByUserId,
            "Zgłoszenie utworzone",
            $"Twoje zgłoszenie {ticket.TicketNumber} zostało utworzone i oczekuje na obsługę.",
            NotificationType.TicketCreated,
            ticket.Id);

        // Send email
        if (ticket.CreatedBy?.Email != null)
        {
            await _emailService.SendTicketCreatedEmailAsync(
                ticket.CreatedBy.Email,
                ticket.TicketNumber,
                ticket.Title);
        }
    }

    public async Task NotifyTicketAssignedAsync(Ticket ticket, string agentId)
    {
        // Notify the assigned agent
        await CreateAsync(
            agentId,
            "Nowe przypisane zgłoszenie",
            $"Zgłoszenie {ticket.TicketNumber}: {ticket.Title} zostało do Ciebie przypisane.",
            NotificationType.TicketAssigned,
            ticket.Id);

        // Notify the ticket creator
        if (ticket.CreatedByUserId != agentId)
        {
            await CreateAsync(
                ticket.CreatedByUserId,
                "Zgłoszenie przypisane",
                $"Twoje zgłoszenie {ticket.TicketNumber} zostało przypisane do agenta i wkrótce zostanie obsłużone.",
                NotificationType.TicketAssigned,
                ticket.Id);
        }
    }

    public async Task NotifyStatusChangedAsync(Ticket ticket, TicketStatus oldStatus, TicketStatus newStatus)
    {
        var statusName = GetStatusDisplayName(newStatus);

        // Notify the ticket creator
        await CreateAsync(
            ticket.CreatedByUserId,
            "Zmiana statusu zgłoszenia",
            $"Status zgłoszenia {ticket.TicketNumber} zmieniony na: {statusName}",
            NotificationType.TicketStatusChanged,
            ticket.Id);

        // Load creator email
        if (ticket.CreatedBy == null)
        {
            await _context.Entry(ticket).Reference(t => t.CreatedBy).LoadAsync();
        }

        // Send email notification
        if (ticket.CreatedBy?.Email != null)
        {
            await _emailService.SendTicketStatusChangedEmailAsync(
                ticket.CreatedBy.Email,
                ticket.TicketNumber,
                GetStatusDisplayName(oldStatus),
                statusName);
        }
    }

    public async Task NotifyCommentAddedAsync(Ticket ticket, TicketComment comment)
    {
        // Load comment author
        if (comment.Author == null)
        {
            await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
        }

        var authorName = comment.Author?.FullName ?? "Użytkownik";

        // Notify ticket creator if comment is not from them
        if (ticket.CreatedByUserId != comment.AuthorId)
        {
            await CreateAsync(
                ticket.CreatedByUserId,
                "Nowy komentarz",
                $"{authorName} dodał komentarz do zgłoszenia {ticket.TicketNumber}",
                NotificationType.TicketCommented,
                ticket.Id);
        }

        // Notify assigned agent if comment is not from them
        if (ticket.AssignedToUserId != null && ticket.AssignedToUserId != comment.AuthorId)
        {
            await CreateAsync(
                ticket.AssignedToUserId,
                "Nowy komentarz",
                $"{authorName} dodał komentarz do zgłoszenia {ticket.TicketNumber}",
                NotificationType.TicketCommented,
                ticket.Id);
        }
    }

    public async Task NotifyTicketResolvedAsync(Ticket ticket)
    {
        // Load creator
        if (ticket.CreatedBy == null)
        {
            await _context.Entry(ticket).Reference(t => t.CreatedBy).LoadAsync();
        }

        await CreateAsync(
            ticket.CreatedByUserId,
            "Zgłoszenie rozwiązane",
            $"Twoje zgłoszenie {ticket.TicketNumber} zostało rozwiązane. Sprawdź rozwiązanie i zamknij zgłoszenie jeśli problem został usunięty.",
            NotificationType.TicketResolved,
            ticket.Id);

        // Send email
        if (ticket.CreatedBy?.Email != null)
        {
            await _emailService.SendTicketStatusChangedEmailAsync(
                ticket.CreatedBy.Email,
                ticket.TicketNumber,
                "W realizacji",
                "Rozwiązany");
        }
    }

    private static string GetStatusDisplayName(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.New => "Nowy",
            TicketStatus.InProgress => "W realizacji",
            TicketStatus.WaitingForUser => "Oczekuje na użytkownika",
            TicketStatus.Resolved => "Rozwiązany",
            _ => status.ToString()
        };
    }
}
