using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Services.Implementations;

/// <summary>
/// Implementacja serwisu do logowania audytowego
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, string entityId, string? oldValues = null, string? newValues = null)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await LogWithUserAsync(userId, action, entityType, entityId, oldValues, newValues);
    }

    public async Task LogWithUserAsync(string? userId, string action, string entityType, string entityId, string? oldValues = null, string? newValues = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = GetIpAddress(httpContext),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.Now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? userId = null,
        string? entityType = null,
        string? action = null,
        int limit = 100)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetEntityLogsAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    private static string? GetIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // Try to get real IP from proxy headers
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
