using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;
using ITServiceHelpDesk.Models.ViewModels.Shared;
using ITServiceHelpDesk.Models.ViewModels.Tickets;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Services.Implementations;

/// <summary>
/// Implementacja serwisu do zarządzania zgłoszeniami
/// </summary>
public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketService(
        ApplicationDbContext context,
        IFileService fileService,
        INotificationService notificationService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileService = fileService;
        _notificationService = notificationService;
        _auditService = auditService;
        _userManager = userManager;
    }

    // ============================================
    // TICKET CRUD
    // ============================================

    public async Task<Ticket> CreateTicketAsync(TicketCreateViewModel model, string userId)
    {
        var ticketNumber = await GenerateTicketNumberAsync();
        
        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            Title = model.Title,
            Description = model.Description,
            CategoryId = model.CategoryId,
            Priority = model.Priority,
            Status = TicketStatus.New,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DueDate = model.DueDate ?? DateTime.UtcNow.AddHours(48)
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Add history entry
        await AddHistoryAsync(ticket.Id, userId, "Utworzono", null, ticketNumber, 
            $"Utworzono zgłoszenie: {ticket.Title}");

        // Handle attachments
        if (model.Attachments != null && model.Attachments.Any())
        {
            foreach (var file in model.Attachments)
            {
                await AddAttachmentAsync(ticket.Id, file, userId);
            }
        }

        // Send notification
        await _notificationService.NotifyTicketCreatedAsync(ticket);

        // Audit log
        await _auditService.LogWithUserAsync(userId, "Create", "Ticket", ticket.Id.ToString(), 
            null, System.Text.Json.JsonSerializer.Serialize(new { ticket.TicketNumber, ticket.Title }));

        return ticket;
    }

    public async Task<Ticket?> GetTicketByIdAsync(int id)
    {
        return await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Author)
            .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedBy)
            .Include(t => t.History.OrderByDescending(h => h.CreatedAt))
                .ThenInclude(h => h.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Ticket?> GetTicketByNumberAsync(string ticketNumber)
    {
        return await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);
    }

    public async Task<bool> UpdateTicketAsync(TicketEditViewModel model, string userId)
    {
        var ticket = await _context.Tickets.FindAsync(model.Id);
        if (ticket == null) return false;

        var changes = new List<string>();

        if (ticket.Title != model.Title)
        {
            await AddHistoryAsync(ticket.Id, userId, "Zmiana tytułu", ticket.Title, model.Title,
                $"Zmieniono tytuł z '{ticket.Title}' na '{model.Title}'");
            ticket.Title = model.Title;
            changes.Add("title");
        }

        if (ticket.Description != model.Description)
        {
            await AddHistoryAsync(ticket.Id, userId, "Zmiana opisu", null, null, "Zmieniono opis zgłoszenia");
            ticket.Description = model.Description;
            changes.Add("description");
        }

        if (ticket.CategoryId != model.CategoryId)
        {
            var oldCategory = await _context.Categories.FindAsync(ticket.CategoryId);
            var newCategory = await _context.Categories.FindAsync(model.CategoryId);
            await AddHistoryAsync(ticket.Id, userId, "Zmiana kategorii", 
                oldCategory?.Name, newCategory?.Name,
                $"Zmieniono kategorię z '{oldCategory?.Name}' na '{newCategory?.Name}'");
            ticket.CategoryId = model.CategoryId;
            changes.Add("category");
        }

        if (ticket.Priority != model.Priority)
        {
            await AddHistoryAsync(ticket.Id, userId, "Zmiana priorytetu", 
                ticket.Priority.ToString(), model.Priority.ToString(),
                $"Zmieniono priorytet z '{ticket.Priority}' na '{model.Priority}'");
            ticket.Priority = model.Priority;
            changes.Add("priority");
        }

        if (ticket.DueDate != model.DueDate)
        {
            await AddHistoryAsync(ticket.Id, userId, "Zmiana terminu",
                ticket.DueDate?.ToString("dd.MM.yyyy HH:mm"),
                model.DueDate?.ToString("dd.MM.yyyy HH:mm"),
                "Zmieniono termin realizacji");
            ticket.DueDate = model.DueDate;
            changes.Add("dueDate");
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Handle new attachments
        if (model.NewAttachments != null && model.NewAttachments.Any())
        {
            foreach (var file in model.NewAttachments)
            {
                await AddAttachmentAsync(ticket.Id, file, userId);
            }
        }

        if (changes.Any())
        {
            await _auditService.LogWithUserAsync(userId, "Update", "Ticket", ticket.Id.ToString(),
                null, System.Text.Json.JsonSerializer.Serialize(changes));
        }

        return true;
    }

    public async Task<bool> DeleteTicketAsync(int id, string userId)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return false;

        ticket.IsDeleted = true;
        ticket.UpdatedAt = DateTime.UtcNow;

        await AddHistoryAsync(id, userId, "Usunięto", null, null, "Zgłoszenie zostało usunięte");
        await _context.SaveChangesAsync();

        await _auditService.LogWithUserAsync(userId, "Delete", "Ticket", id.ToString());

        return true;
    }

    // ============================================
    // TICKET QUERIES
    // ============================================

    public async Task<PaginatedList<Ticket>> GetTicketsAsync(TicketFilterViewModel filter, int pageIndex, int pageSize)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .AsQueryable();

        query = ApplyFilters(query, filter);
        query = ApplySorting(query, filter.SortBy, filter.SortDescending);

        return await PaginatedList<Ticket>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);
    }

    public async Task<PaginatedList<Ticket>> GetUserTicketsAsync(string userId, TicketFilterViewModel? filter, int pageIndex, int pageSize)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Where(t => t.CreatedByUserId == userId)
            .AsQueryable();

        if (filter != null)
        {
            query = ApplyFilters(query, filter);
            query = ApplySorting(query, filter.SortBy, filter.SortDescending);
        }
        else
        {
            query = query.OrderByDescending(t => t.CreatedAt);
        }

        return await PaginatedList<Ticket>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);
    }

    public async Task<PaginatedList<Ticket>> GetAgentTicketsAsync(string agentId, TicketFilterViewModel? filter, int pageIndex, int pageSize)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Where(t => t.AssignedToUserId == agentId)
            .AsQueryable();

        if (filter != null)
        {
            query = ApplyFilters(query, filter);
            query = ApplySorting(query, filter.SortBy, filter.SortDescending);
        }
        else
        {
            query = query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt);
        }

        return await PaginatedList<Ticket>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);
    }

    public async Task<PaginatedList<Ticket>> GetUnassignedTicketsAsync(TicketFilterViewModel? filter, int pageIndex, int pageSize)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Where(t => t.AssignedToUserId == null && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Rejected)
            .AsQueryable();

        if (filter != null)
        {
            query = ApplyFilters(query, filter);
            query = ApplySorting(query, filter.SortBy, filter.SortDescending);
        }
        else
        {
            query = query.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt);
        }

        return await PaginatedList<Ticket>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);
    }

    // ============================================
    // TICKET STATUS & ASSIGNMENT
    // ============================================

    public async Task<bool> ChangeStatusAsync(int ticketId, TicketStatus newStatus, string userId, string? resolutionSummary = null)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;

        var oldStatus = ticket.Status;
        ticket.Status = newStatus;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (newStatus == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = DateTime.UtcNow;
            ticket.ResolutionSummary = resolutionSummary;
        }
        else if (newStatus == TicketStatus.Closed)
        {
            ticket.ClosedAt = DateTime.UtcNow;
            if (!ticket.ResolvedAt.HasValue)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }
        }

        await AddHistoryAsync(ticketId, userId, "Zmiana statusu", 
            oldStatus.ToString(), newStatus.ToString(),
            $"Status zmieniony z '{GetStatusDisplayName(oldStatus)}' na '{GetStatusDisplayName(newStatus)}'");

        await _context.SaveChangesAsync();

        // Notifications
        await _notificationService.NotifyStatusChangedAsync(ticket, oldStatus, newStatus);

        if (newStatus == TicketStatus.Resolved)
        {
            await _notificationService.NotifyTicketResolvedAsync(ticket);
        }

        return true;
    }

    public async Task<bool> AssignTicketAsync(int ticketId, string? agentId, string userId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null) return false;

        var oldAgentName = ticket.AssignedTo?.FullName ?? "Brak";
        string? newAgentName = null;

        if (agentId != null)
        {
            var newAgent = await _userManager.FindByIdAsync(agentId);
            newAgentName = newAgent?.FullName ?? "Nieznany";
        }

        ticket.AssignedToUserId = agentId;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (ticket.Status == TicketStatus.New && agentId != null)
        {
            ticket.Status = TicketStatus.Open;
        }

        var action = agentId == null ? "Usunięto przypisanie" : "Przypisano";
        await AddHistoryAsync(ticketId, userId, action, oldAgentName, newAgentName ?? "Brak",
            agentId == null 
                ? "Usunięto przypisanie agenta" 
                : $"Przypisano do agenta: {newAgentName}");

        await _context.SaveChangesAsync();

        if (agentId != null)
        {
            await _notificationService.NotifyTicketAssignedAsync(ticket, agentId);
        }

        return true;
    }

    public async Task<bool> TakeTicketAsync(int ticketId, string agentId)
    {
        return await AssignTicketAsync(ticketId, agentId, agentId);
    }

    public async Task<bool> ChangePriorityAsync(int ticketId, TicketPriority newPriority, string userId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;

        var oldPriority = ticket.Priority;
        ticket.Priority = newPriority;
        ticket.UpdatedAt = DateTime.UtcNow;

        await AddHistoryAsync(ticketId, userId, "Zmiana priorytetu",
            oldPriority.ToString(), newPriority.ToString(),
            $"Priorytet zmieniony z '{oldPriority}' na '{newPriority}'");

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeCategoryAsync(int ticketId, int newCategoryId, string userId)
    {
        var ticket = await _context.Tickets.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null) return false;

        var oldCategory = ticket.Category;
        var newCategory = await _context.Categories.FindAsync(newCategoryId);
        if (newCategory == null) return false;

        ticket.CategoryId = newCategoryId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await AddHistoryAsync(ticketId, userId, "Zmiana kategorii",
            oldCategory.Name, newCategory.Name,
            $"Kategoria zmieniona z '{oldCategory.Name}' na '{newCategory.Name}'");

        await _context.SaveChangesAsync();
        return true;
    }

    // ============================================
    // COMMENTS
    // ============================================

    public async Task<TicketComment> AddCommentAsync(int ticketId, string content, CommentType type, string userId)
    {
        var comment = new TicketComment
        {
            TicketId = ticketId,
            AuthorId = userId,
            Content = content,
            CommentType = type,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketComments.Add(comment);

        // Update ticket's UpdatedAt
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket != null)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await AddHistoryAsync(ticketId, userId, "Dodano komentarz", null, null,
            type == CommentType.Internal ? "Dodano komentarz wewnętrzny" : "Dodano komentarz");

        await _context.SaveChangesAsync();

        // Load author for the response
        await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

        // Notification
        if (type == CommentType.Public && ticket != null)
        {
            await _notificationService.NotifyCommentAddedAsync(ticket, comment);
        }

        return comment;
    }

    public async Task<IEnumerable<TicketComment>> GetTicketCommentsAsync(int ticketId, bool includeInternal = false)
    {
        var query = _context.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId && !c.IsDeleted);

        if (!includeInternal)
        {
            query = query.Where(c => c.CommentType == CommentType.Public);
        }

        return await query.OrderBy(c => c.CreatedAt).ToListAsync();
    }

    // ============================================
    // ATTACHMENTS
    // ============================================

    public async Task<TicketAttachment> AddAttachmentAsync(int ticketId, IFormFile file, string userId)
    {
        var (fileName, filePath) = await _fileService.SaveFileAsync(file, $"tickets/{ticketId}");

        var attachment = new TicketAttachment
        {
            TicketId = ticketId,
            FileName = fileName,
            OriginalFileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.TicketAttachments.Add(attachment);

        await AddHistoryAsync(ticketId, userId, "Dodano załącznik", null, file.FileName,
            $"Dodano załącznik: {file.FileName}");

        await _context.SaveChangesAsync();

        return attachment;
    }

    public async Task<TicketAttachment?> GetAttachmentAsync(int attachmentId)
    {
        return await _context.TicketAttachments
            .Include(a => a.UploadedBy)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, string userId)
    {
        var attachment = await _context.TicketAttachments.FindAsync(attachmentId);
        if (attachment == null) return false;

        await _fileService.DeleteFileAsync(attachment.FilePath);
        
        await AddHistoryAsync(attachment.TicketId, userId, "Usunięto załącznik", 
            attachment.OriginalFileName, null,
            $"Usunięto załącznik: {attachment.OriginalFileName}");

        _context.TicketAttachments.Remove(attachment);
        await _context.SaveChangesAsync();

        return true;
    }

    // ============================================
    // HISTORY
    // ============================================

    public async Task<IEnumerable<TicketHistory>> GetTicketHistoryAsync(int ticketId)
    {
        return await _context.TicketHistories
            .Include(h => h.User)
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    // ============================================
    // STATISTICS
    // ============================================

    public async Task<UserTicketStatsViewModel> GetUserStatsAsync(string userId)
    {
        var tickets = await _context.Tickets
            .Where(t => t.CreatedByUserId == userId)
            .ToListAsync();

        return new UserTicketStatsViewModel
        {
            TotalTickets = tickets.Count,
            NewTickets = tickets.Count(t => t.Status == TicketStatus.New),
            OpenTickets = tickets.Count(t => t.Status == TicketStatus.Open),
            InProgressTickets = tickets.Count(t => t.Status == TicketStatus.InProgress),
            ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved),
            ClosedTickets = tickets.Count(t => t.Status == TicketStatus.Closed)
        };
    }

    public async Task<AgentTicketStatsViewModel> GetAgentStatsAsync(string agentId)
    {
        var today = DateTime.UtcNow.Date;
        var allTickets = await _context.Tickets.ToListAsync();

        return new AgentTicketStatsViewModel
        {
            AssignedToMe = allTickets.Count(t => t.AssignedToUserId == agentId && t.Status != TicketStatus.Closed),
            UnassignedTickets = allTickets.Count(t => t.AssignedToUserId == null && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Rejected),
            NewTickets = allTickets.Count(t => t.Status == TicketStatus.New),
            InProgressTickets = allTickets.Count(t => t.AssignedToUserId == agentId && t.Status == TicketStatus.InProgress),
            WaitingForUserTickets = allTickets.Count(t => t.AssignedToUserId == agentId && t.Status == TicketStatus.WaitingForUser),
            ResolvedToday = allTickets.Count(t => t.AssignedToUserId == agentId && t.ResolvedAt?.Date == today),
            OverdueTickets = allTickets.Count(t => t.AssignedToUserId == agentId && t.IsOverdue)
        };
    }

    public async Task<AdminTicketStatsViewModel> GetAdminStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var allTickets = await _context.Tickets.Include(t => t.Category).ToListAsync();
        var users = await _userManager.GetUsersInRoleAsync("User");
        var agents = await _userManager.GetUsersInRoleAsync("Agent");

        var stats = new AdminTicketStatsViewModel
        {
            TotalTickets = allTickets.Count,
            NewTickets = allTickets.Count(t => t.Status == TicketStatus.New),
            OpenTickets = allTickets.Count(t => t.Status == TicketStatus.Open),
            InProgressTickets = allTickets.Count(t => t.Status == TicketStatus.InProgress),
            ResolvedTickets = allTickets.Count(t => t.Status == TicketStatus.Resolved),
            ClosedTickets = allTickets.Count(t => t.Status == TicketStatus.Closed),
            OverdueTickets = allTickets.Count(t => t.IsOverdue),
            TotalUsers = users.Count,
            TotalAgents = agents.Count,
            TicketsCreatedToday = allTickets.Count(t => t.CreatedAt.Date == today),
            TicketsResolvedToday = allTickets.Count(t => t.ResolvedAt?.Date == today)
        };

        stats.TicketsByCategory = allTickets
            .GroupBy(t => t.Category.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.TicketsByPriority = allTickets
            .GroupBy(t => t.Priority.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    public async Task<string> GenerateTicketNumberAsync()
    {
        const string prefix = "TCK-";

        var lastTicket = await _context.Tickets
            .IgnoreQueryFilters()
            .Where(t => t.TicketNumber.StartsWith(prefix))
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastTicket != null)
        {
            var lastNumberStr = lastTicket.TicketNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    // ============================================
    // VALIDATION
    // ============================================

    public async Task<bool> CanUserAccessTicketAsync(int ticketId, string userId, bool isAgent, bool isAdmin)
    {
        if (isAdmin) return true;

        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return false;

        if (isAgent)
        {
            return ticket.AssignedToUserId == null || ticket.AssignedToUserId == userId;
        }

        return ticket.CreatedByUserId == userId;
    }

    // ============================================
    // PRIVATE HELPERS
    // ============================================

    private async Task AddHistoryAsync(int ticketId, string userId, string action, string? oldValue, string? newValue, string description)
    {
        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketHistories.Add(history);
    }

    private static IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, TicketFilterViewModel filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(t => 
                t.Title.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm) ||
                t.TicketNumber.ToLower().Contains(searchTerm));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        }

        if (filter.AssignedToUserId != null)
        {
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.DateTo.Value);
        }

        if (filter.ShowOverdueOnly == true)
        {
            query = query.Where(t => t.DueDate < DateTime.UtcNow && 
                t.Status != TicketStatus.Closed && 
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Rejected);
        }

        return query;
    }

    private static IQueryable<Ticket> ApplySorting(IQueryable<Ticket> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "title" => descending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "status" => descending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "priority" => descending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "category" => descending ? query.OrderByDescending(t => t.Category.Name) : query.OrderBy(t => t.Category.Name),
            "createdat" => descending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "updatedat" => descending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            "duedate" => descending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };
    }

    private static string GetStatusDisplayName(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.New => "Nowy",
            TicketStatus.Open => "Otwarty",
            TicketStatus.InProgress => "W trakcie",
            TicketStatus.WaitingForUser => "Oczekuje na użytkownika",
            TicketStatus.Resolved => "Rozwiązany",
            TicketStatus.Closed => "Zamknięty",
            TicketStatus.Rejected => "Odrzucony",
            _ => status.ToString()
        };
    }
}
