using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Infrastructure.Identity;

/// <summary>
/// Klasa odpowiedzialna za seedowanie danych początkowych: ról, użytkowników, kategorii i przykładowych ticketów
/// </summary>
public static class IdentitySeeder
{
    // ============================================
    // ROLE NAMES - Constants
    // ============================================
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Agent = "Agent";
        public const string User = "User";
    }

    // ============================================
    // SEED ROLES
    // ============================================
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { Roles.Admin, Roles.Agent, Roles.User };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    // ============================================
    // SEED USERS
    // Tylko konto techniczne Admina - pozostali użytkownicy logują się przez SSO Microsoft
    // ============================================
    public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        // Jedyne konto lokalne - techniczny Administrator systemu
        await CreateUserIfNotExists(userManager, new ApplicationUser
        {
            UserName = "admin@helpdesk.local",
            Email = "admin@helpdesk.local",
            FirstName = "Administrator",
            LastName = "Systemu",
            Department = "IT",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.Now
        }, "Admin123!", Roles.Admin);
    }

    private static async Task CreateUserIfNotExists(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password,
        string role)
    {
        var existingUser = await userManager.FindByEmailAsync(user.Email!);
        if (existingUser == null)
        {
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }

    // ============================================
    // SEED CATEGORIES
    // ============================================
    public static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new Category
            {
                Name = "Sprzęt komputerowy",
                Description = "Problemy ze sprzętem: komputery, monitory, drukarki, skanery",
                Icon = "bi-pc-display",
                Color = "#7c3aed",
                DisplayOrder = 1,
                IsActive = true
            },
            new Category
            {
                Name = "Oprogramowanie",
                Description = "Instalacja, aktualizacja i problemy z oprogramowaniem",
                Icon = "bi-window",
                Color = "#3b82f6",
                DisplayOrder = 2,
                IsActive = true
            },
            new Category
            {
                Name = "Sieć i Internet",
                Description = "Problemy z połączeniem sieciowym, VPN, Wi-Fi",
                Icon = "bi-wifi",
                Color = "#06b6d4",
                DisplayOrder = 3,
                IsActive = true
            },
            new Category
            {
                Name = "Poczta e-mail",
                Description = "Problemy z pocztą elektroniczną, Outlook, konfiguracja",
                Icon = "bi-envelope",
                Color = "#10b981",
                DisplayOrder = 4,
                IsActive = true
            },
            new Category
            {
                Name = "Konta i uprawnienia",
                Description = "Tworzenie kont, resetowanie haseł, uprawnienia dostępu",
                Icon = "bi-person-lock",
                Color = "#f59e0b",
                DisplayOrder = 5,
                IsActive = true
            },
            new Category
            {
                Name = "Bezpieczeństwo",
                Description = "Incydenty bezpieczeństwa, wirusy, phishing",
                Icon = "bi-shield-exclamation",
                Color = "#ef4444",
                DisplayOrder = 6,
                IsActive = true
            },
            new Category
            {
                Name = "Telefonia",
                Description = "Telefony stacjonarne, komórkowe, systemy VoIP",
                Icon = "bi-telephone",
                Color = "#8b5cf6",
                DisplayOrder = 7,
                IsActive = true
            },
            new Category
            {
                Name = "Inne",
                Description = "Pozostałe zgłoszenia IT",
                Icon = "bi-question-circle",
                Color = "#6b7280",
                DisplayOrder = 99,
                IsActive = true
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    // ============================================
    // SEED SAMPLE TICKETS
    // Wyłączone - użytkownicy są dostarczani przez SSO Microsoft
    // ============================================
    public static Task SeedSampleTicketsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Przykładowe dane testowe zostały wyłączone.
        // Użytkownicy są automatycznie tworzeni przy pierwszym logowaniu przez Microsoft SSO.
        return Task.CompletedTask;
    }

    private static async Task SeedSampleTicketsInternal(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Tickets.AnyAsync())
            return;

        var user1 = await userManager.FindByEmailAsync("piotr.wisniewski@firma.local");
        var user2 = await userManager.FindByEmailAsync("maria.zielinska@firma.local");
        var user3 = await userManager.FindByEmailAsync("tomasz.lewandowski@firma.local");
        var agent1 = await userManager.FindByEmailAsync("jan.kowalski@helpdesk.local");
        var agent2 = await userManager.FindByEmailAsync("anna.nowak@helpdesk.local");

        if (user1 == null || user2 == null || user3 == null || agent1 == null)
            return;

        var categories = await context.Categories.ToListAsync();
        var hardware = categories.First(c => c.Name == "Sprzęt komputerowy");
        var software = categories.First(c => c.Name == "Oprogramowanie");
        var network = categories.First(c => c.Name == "Sieć i Internet");
        var email = categories.First(c => c.Name == "Poczta e-mail");
        var accounts = categories.First(c => c.Name == "Konta i uprawnienia");

        var currentYear = DateTime.Now.Year;
        var ticketCounter = 1;

        var tickets = new List<Ticket>
        {
            // Ticket 1 - New
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "Komputer nie uruchamia się",
                Description = "Od rana mój komputer nie chce się włączyć. Po naciśnięciu przycisku power nic się nie dzieje, żadne diody nie świecą. Proszę o pilną pomoc, mam ważne spotkanie online.",
                Status = TicketStatus.New,
                Priority = TicketPriority.High,
                CategoryId = hardware.Id,
                CreatedByUserId = user1.Id,
                CreatedAt = DateTime.Now.AddHours(-2),
                UpdatedAt = DateTime.Now.AddHours(-2),
                DueDate = DateTime.Now.AddDays(1)
            },
            // Ticket 2 - InProgress, assigned
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "Proszę o instalację programu Adobe Acrobat",
                Description = "Potrzebuję programu Adobe Acrobat Pro do edycji dokumentów PDF. Czy mogę prosić o instalację?",
                Status = TicketStatus.InProgress,
                Priority = TicketPriority.Medium,
                CategoryId = software.Id,
                CreatedByUserId = user2.Id,
                AssignedToUserId = agent1.Id,
                CreatedAt = DateTime.Now.AddDays(-1),
                UpdatedAt = DateTime.Now.AddHours(-4),
                DueDate = DateTime.Now.AddDays(3)
            },
            // Ticket 3 - In Progress
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "Brak dostępu do dysku sieciowego",
                Description = "Nie mogę połączyć się z dyskiem \\\\fileserver\\shared. Wczoraj działało, dzisiaj pokazuje błąd 'Odmowa dostępu'. Potrzebuję pilnie dokumentów z tego dysku.",
                Status = TicketStatus.InProgress,
                Priority = TicketPriority.High,
                CategoryId = network.Id,
                CreatedByUserId = user3.Id,
                AssignedToUserId = agent2?.Id,
                CreatedAt = DateTime.Now.AddDays(-2),
                UpdatedAt = DateTime.Now.AddHours(-1),
                DueDate = DateTime.Now.AddHours(4)
            },
            // Ticket 4 - Waiting for User
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "Nie działa Outlook - proszę o reset hasła",
                Description = "Program Outlook wyświetla komunikat o błędnym haśle. Proszę o zresetowanie hasła do skrzynki pocztowej.",
                Status = TicketStatus.WaitingForUser,
                Priority = TicketPriority.Medium,
                CategoryId = email.Id,
                CreatedByUserId = user1.Id,
                AssignedToUserId = agent1.Id,
                CreatedAt = DateTime.Now.AddDays(-3),
                UpdatedAt = DateTime.Now.AddDays(-1),
                DueDate = DateTime.Now.AddDays(1)
            },
            // Ticket 5 - Resolved
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "Proszę o utworzenie konta dla nowego pracownika",
                Description = "Proszę o utworzenie konta domenowego dla nowego pracownika działu sprzedaży: Michał Nowicki. Data rozpoczęcia pracy: następny poniedziałek.",
                Status = TicketStatus.Resolved,
                Priority = TicketPriority.Medium,
                CategoryId = accounts.Id,
                CreatedByUserId = user2.Id,
                AssignedToUserId = agent1.Id,
                CreatedAt = DateTime.Now.AddDays(-5),
                UpdatedAt = DateTime.Now.AddDays(-2),
                ResolvedAt = DateTime.Now.AddDays(-2),
                ResolutionSummary = "Konto utworzone. Login: m.nowicki, hasło tymczasowe przekazane mailowo do przełożonego."
            },
            // Ticket 6 - Resolved
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "Wymiana klawiatury",
                Description = "Klawiatura ma zepsute kilka klawiszy (Enter, Spacja, kilka liter). Proszę o wymianę na nową.",
                Status = TicketStatus.Resolved,
                Priority = TicketPriority.Low,
                CategoryId = hardware.Id,
                CreatedByUserId = user3.Id,
                AssignedToUserId = agent2?.Id,
                CreatedAt = DateTime.Now.AddDays(-7),
                UpdatedAt = DateTime.Now.AddDays(-4),
                ResolvedAt = DateTime.Now.AddDays(-4),
                ResolutionSummary = "Klawiatura wymieniona na nową. Stara oddana do utylizacji."
            },
            // Ticket 7 - Critical, New
            new Ticket
            {
                TicketNumber = $"HD-{currentYear}-{ticketCounter++:D4}",
                Title = "PILNE: Podejrzany email z załącznikiem",
                Description = "Otrzymałem podejrzaną wiadomość email z załącznikiem .exe. Nie otworzyłem załącznika ale martwię się o bezpieczeństwo. Nadawca podszywał się pod bank.",
                Status = TicketStatus.New,
                Priority = TicketPriority.Critical,
                CategoryId = categories.First(c => c.Name == "Bezpieczeństwo").Id,
                CreatedByUserId = user1.Id,
                CreatedAt = DateTime.Now.AddMinutes(-30),
                UpdatedAt = DateTime.Now.AddMinutes(-30),
                DueDate = DateTime.Now.AddHours(2)
            }
        };

        await context.Tickets.AddRangeAsync(tickets);
        await context.SaveChangesAsync();

        // Add some history and comments
        var ticket2 = tickets[1];
        var histories = new List<TicketHistory>
        {
            new TicketHistory
            {
                TicketId = ticket2.Id,
                UserId = agent1.Id,
                Action = "Przypisano",
                OldValue = null,
                NewValue = agent1.FullName,
                Description = $"Zgłoszenie przypisane do agenta: {agent1.FullName}",
                CreatedAt = DateTime.Now.AddHours(-4)
            },
            new TicketHistory
            {
                TicketId = ticket2.Id,
                UserId = agent1.Id,
                Action = "Zmiana statusu",
                OldValue = "Nowy",
                NewValue = "W realizacji",
                Description = "Status zmieniony z Nowy na W realizacji",
                CreatedAt = DateTime.Now.AddHours(-4)
            }
        };

        var comments = new List<TicketComment>
        {
            new TicketComment
            {
                TicketId = ticket2.Id,
                AuthorId = agent1.Id,
                Content = "Dzień dobry, przejmuję zgłoszenie. Sprawdzę dostępność licencji Adobe Acrobat Pro i wrócę z informacją.",
                CommentType = CommentType.Public,
                CreatedAt = DateTime.Now.AddHours(-4)
            },
            new TicketComment
            {
                TicketId = tickets[3].Id,
                AuthorId = agent1.Id,
                Content = "Hasło zostało zresetowane. Nowe hasło tymczasowe wysłałem na służbowy telefon SMS. Proszę o potwierdzenie, czy udało się zalogować.",
                CommentType = CommentType.Public,
                CreatedAt = DateTime.Now.AddDays(-1)
            },
            new TicketComment
            {
                TicketId = tickets[2].Id,
                AuthorId = agent2?.Id ?? agent1.Id,
                Content = "Sprawdzam uprawnienia użytkownika w Active Directory. Wygląda na to, że konto zostało przypadkowo usunięte z grupy dostępowej.",
                CommentType = CommentType.Internal,
                CreatedAt = DateTime.Now.AddHours(-1)
            }
        };

        await context.TicketHistories.AddRangeAsync(histories);
        await context.TicketComments.AddRangeAsync(comments);
        await context.SaveChangesAsync();
    }
}
