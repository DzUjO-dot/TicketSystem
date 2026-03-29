using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.ViewModels.Tickets;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ITServiceHelpDesk.Controllers;

/// <summary>
/// Kontroler dashboardu - widoki podsumowania dla różnych ról
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ITicketService ticketService,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardController> logger)
    {
        _ticketService = ticketService;
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(base.User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(currentUser);

        // Admin -> Admin Dashboard
        if (roles.Contains("Admin"))
        {
            return RedirectToAction(nameof(Admin));
        }

        // Agent -> Agent Dashboard
        if (roles.Contains("Agent"))
        {
            return RedirectToAction(nameof(Agent));
        }

        // User -> User Dashboard
        return RedirectToAction("User");
    }

    // ============================================
    // USER DASHBOARD
    // ============================================

    [HttpGet]
    [ActionName("User")]
    public async Task<IActionResult> UserDashboard()
    {
        var currentUser = await _userManager.GetUserAsync(base.User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var stats = await _ticketService.GetUserStatsAsync(currentUser.Id);
        var recentTickets = await _ticketService.GetUserTicketsAsync(currentUser.Id, null, 1, 5);
        var notifications = await _notificationService.GetUnreadAsync(currentUser.Id, 5);

        ViewBag.UserName = currentUser.FullName;
        ViewBag.Stats = stats;
        ViewBag.RecentTickets = recentTickets;
        ViewBag.Notifications = notifications;

        return View("User");
    }

    // ============================================
    // AGENT DASHBOARD
    // ============================================

    [HttpGet]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Agent()
    {
        var currentUser = await _userManager.GetUserAsync(base.User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var stats = await _ticketService.GetAgentStatsAsync(currentUser.Id);
        var assignedRaw = await _ticketService.GetAgentTicketsAsync(currentUser.Id, null, 1, 5);
        var unassignedRaw = await _ticketService.GetUnassignedTicketsAsync(null, 1, 5);

        ViewBag.UserName = currentUser.FullName;
        ViewBag.Stats = stats;
        ViewBag.AssignedTickets = assignedRaw.Select(TicketListItemViewModel.FromTicket).ToList();
        ViewBag.UnassignedTickets = unassignedRaw.Select(TicketListItemViewModel.FromTicket).ToList();

        return View("Agent");
    }

    // ============================================
    // ADMIN DASHBOARD
    // ============================================

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        var currentUser = await _userManager.GetUserAsync(base.User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var stats = await _ticketService.GetAdminStatsAsync();

        ViewBag.UserName = currentUser.FullName;
        ViewBag.Stats = stats;

        return View("Admin");
    }
}
