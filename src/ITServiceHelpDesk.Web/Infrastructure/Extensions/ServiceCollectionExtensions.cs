using ITServiceHelpDesk.Services.Implementations;
using ITServiceHelpDesk.Services.Interfaces;

namespace ITServiceHelpDesk.Infrastructure.Extensions;

/// <summary>
/// Rozszerzenia do rejestracji serwisów w DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje wszystkie serwisy aplikacji
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Business services
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        
        // Infrastructure services
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IEmailService, EmailService>();

        // HTTP Context accessor (needed for audit logging)
        services.AddHttpContextAccessor();

        return services;
    }
}
