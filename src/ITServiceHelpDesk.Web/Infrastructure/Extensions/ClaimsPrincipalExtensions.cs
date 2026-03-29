using System.Security.Claims;

namespace ITServiceHelpDesk.Infrastructure.Extensions;

/// <summary>
/// Rozszerzenia dla ClaimsPrincipal do łatwiejszego dostępu do danych użytkownika
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Pobiera ID zalogowanego użytkownika
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Pobiera email zalogowanego użytkownika
    /// </summary>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email);
    }

    /// <summary>
    /// Pobiera imię i nazwisko użytkownika (jeśli zapisane w claims)
    /// </summary>
    public static string? GetUserFullName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name);
    }

    /// <summary>
    /// Sprawdza czy użytkownik ma rolę Admin
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }

    /// <summary>
    /// Sprawdza czy użytkownik ma rolę Agent lub Admin
    /// </summary>
    public static bool IsAgentOrAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Agent") || principal.IsInRole("Admin");
    }

    /// <summary>
    /// Sprawdza czy użytkownik ma rolę zwykłego użytkownika
    /// </summary>
    public static bool IsRegularUser(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("User") && 
               !principal.IsInRole("Agent") && 
               !principal.IsInRole("Admin");
    }
}
