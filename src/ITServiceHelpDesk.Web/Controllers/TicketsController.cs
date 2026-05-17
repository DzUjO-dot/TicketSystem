using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.Enums;
using ITServiceHelpDesk.Models.ViewModels.Shared;
using ITServiceHelpDesk.Models.ViewModels.Tickets;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITServiceHelpDesk.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketService ticketService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager,
        ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _categoryService = categoryService;
        _userManager = userManager;
        _logger = logger;
    }

    // ============================================
    // LIST ALL TICKETS (Admin only)
    // ============================================

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(TicketFilterViewModel filter)
    {
        var tickets = await _ticketService.GetTicketsAsync(filter, filter.PageIndex, filter.PageSize);
        var categories = (await _categoryService.GetActiveCategoriesAsync()).ToList();

        filter.Categories = new SelectList(categories, "Id", "Name", filter.CategoryId);
        filter.Statuses = GetStatusSelectList(filter.Status);
        filter.Priorities = GetPrioritySelectList(filter.Priority);

        var model = new TicketListViewModel
        {
            Tickets = ToPagedViewModels(tickets),
            Filter = filter,
            Categories = categories,
            ViewTitle = "Wszystkie zgłoszenia",
            ViewDescription = "Lista wszystkich zgłoszeń w systemie",
            ShowCreatedBy = true,
            ShowAssignedTo = true
        };

        return View(model);
    }

    // ============================================
    // MY TICKETS
    // ============================================

    [HttpGet]
    public async Task<IActionResult> MyTickets(TicketFilterViewModel filter)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var tickets = await _ticketService.GetUserTicketsAsync(userId, filter, filter.PageIndex, filter.PageSize);
        ViewBag.Stats = await _ticketService.GetUserStatsAsync(userId);

        filter.Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", filter.CategoryId);
        filter.Statuses = GetStatusSelectList(filter.Status);
        filter.Priorities = GetPrioritySelectList(filter.Priority);

        var model = new TicketListViewModel
        {
            Tickets = ToPagedViewModels(tickets),
            Filter = filter,
            ViewTitle = "Moje zgłoszenia",
            ViewDescription = "Zgłoszenia utworzone przez Ciebie",
            ShowCreatedBy = false,
            ShowAssignedTo = true
        };

        return View(model);
    }

    // ============================================
    // ASSIGNED TO ME
    // ============================================

    [HttpGet]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> AssignedToMe(TicketFilterViewModel filter)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var tickets = await _ticketService.GetAgentTicketsAsync(userId, filter, filter.PageIndex, filter.PageSize);
        ViewBag.Stats = await _ticketService.GetAgentStatsAsync(userId);

        filter.Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", filter.CategoryId);
        filter.Statuses = GetStatusSelectList(filter.Status);
        filter.Priorities = GetPrioritySelectList(filter.Priority);

        var model = new TicketListViewModel
        {
            Tickets = ToPagedViewModels(tickets),
            Filter = filter,
            ViewTitle = "Przypisane do mnie",
            ViewDescription = "Zgłoszenia przypisane do Ciebie",
            ShowCreatedBy = true,
            ShowAssignedTo = false
        };

        return View(model);
    }

    // ============================================
    // UNASSIGNED
    // ============================================

    [HttpGet]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Unassigned(TicketFilterViewModel filter)
    {
        var tickets = await _ticketService.GetUnassignedTicketsAsync(filter, filter.PageIndex, filter.PageSize);

        filter.Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", filter.CategoryId);
        filter.Priorities = GetPrioritySelectList(filter.Priority);

        var model = new TicketListViewModel
        {
            Tickets = ToPagedViewModels(tickets),
            Filter = filter,
            ViewTitle = "Nieprzypisane",
            ViewDescription = "Zgłoszenia oczekujące na przypisanie",
            ShowCreatedBy = true,
            ShowAssignedTo = false
        };

        return View(model);
    }

    // ============================================
    // TICKET DETAILS
    // ============================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Zgłoszenie nie zostało znalezione.";
            return RedirectToAction(nameof(MyTickets));
        }

        var userId = _userManager.GetUserId(User);
        var isAgent = User.IsInRole("Agent") || User.IsInRole("Admin");
        var isAdmin = User.IsInRole("Admin");

        if (!await _ticketService.CanUserAccessTicketAsync(id, userId!, isAgent, isAdmin))
        {
            TempData["ErrorMessage"] = "Nie masz dostępu do tego zgłoszenia.";
            return RedirectToAction(nameof(MyTickets));
        }

        var viewModel = TicketDetailsViewModel.FromTicket(ticket, isAgent, isAdmin, userId!);

        if (isAgent || isAdmin)
        {
            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var allAgents = agents.Union(admins).Where(u => u.IsActive).ToList();
            viewModel.AgentOptions = new SelectList(
                allAgents.Select(a => new { a.Id, Name = a.FullName }), "Id", "Name", ticket.AssignedToUserId);
            viewModel.CategoryOptions = new SelectList(
                await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", ticket.CategoryId);
            viewModel.PriorityOptions = GetPrioritySelectList(ticket.Priority);
            viewModel.StatusOptions = GetStatusSelectList(ticket.Status);
        }

        return View(viewModel);
    }

    // ============================================
    // INLINE EDIT
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> UpdateContent(int ticketId, string title, string description)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            TempData["ErrorMessage"] = "Tytuł i opis są wymagane.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        await _ticketService.UpdateContentAsync(ticketId, title.Trim(), description.Trim(), userId);
        TempData["SuccessMessage"] = "Treść zgłoszenia została zaktualizowana.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> UpdateInfo(int ticketId, int categoryId, TicketPriority priority, DateTime? dueDate, string? assignedToUserId, TicketStatus? newStatus, string? resolutionSummary)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        if (newStatus == TicketStatus.Resolved && string.IsNullOrWhiteSpace(resolutionSummary))
        {
            TempData["ErrorMessage"] = "Musisz podać opis rozwiązania problemu przed oznaczeniem jako rozwiązane.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        await _ticketService.UpdateInfoAsync(ticketId, categoryId, priority, dueDate, assignedToUserId, userId);

        if (newStatus.HasValue)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
            if (ticket != null && ticket.Status != newStatus.Value)
                await _ticketService.ChangeStatusAsync(ticketId, newStatus.Value, userId, resolutionSummary);
        }

        TempData["SuccessMessage"] = "Informacje zgłoszenia zostały zaktualizowane.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // CREATE TICKET
    // ============================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TicketCreateViewModel
        {
            Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name"),
            Priorities = GetPrioritySelectList(TicketPriority.Medium)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", model.CategoryId);
            model.Priorities = GetPrioritySelectList(model.Priority);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        try
        {
            var ticket = await _ticketService.CreateTicketAsync(model, userId);

            _logger.LogInformation("User {UserId} created ticket {TicketNumber}", userId, ticket.TicketNumber);
            TempData["SuccessMessage"] = $"Zgłoszenie {ticket.TicketNumber} zostało utworzone pomyślnie.";

            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket for user {UserId}", userId);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas tworzenia zgłoszenia. Spróbuj ponownie.";

            model.Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", model.CategoryId);
            model.Priorities = GetPrioritySelectList(model.Priority);
            return View(model);
        }
    }

    // ============================================
    // EDIT TICKET
    // ============================================

    [HttpGet]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Edit(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Zgłoszenie nie zostało znalezione.";
            return RedirectToAction(nameof(MyTickets));
        }

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");
        var isOwner = ticket.CreatedByUserId == userId;

        if (!isAdmin && !isOwner)
        {
            TempData["ErrorMessage"] = "Tylko właściciel zgłoszenia lub administrator może je edytować.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new TicketEditViewModel
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            Priority = ticket.Priority,
            Status = ticket.Status,
            DueDate = ticket.DueDate,
            AssignedToUserId = ticket.AssignedToUserId,
            CreatedAt = ticket.CreatedAt,
            CreatedByName = ticket.CreatedBy.FullName,
            Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", ticket.CategoryId),
            Priorities = GetPrioritySelectList(ticket.Priority),
            Statuses = GetStatusSelectList(ticket.Status)
        };

        if (isAdmin)
        {
            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var allAgents = agents.Union(admins).Where(u => u.IsActive).ToList();
            model.Agents = new SelectList(allAgents.Select(a => new { a.Id, Name = a.FullName }), "Id", "Name", ticket.AssignedToUserId);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Edit(TicketEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = new SelectList(await _categoryService.GetActiveCategoriesAsync(), "Id", "Name", model.CategoryId);
            model.Priorities = GetPrioritySelectList(model.Priority);
            model.Statuses = GetStatusSelectList(model.Status);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var isAdmin = User.IsInRole("Admin");
        var ticket = await _ticketService.GetTicketByIdAsync(model.Id);
        if (ticket == null || (!isAdmin && ticket.CreatedByUserId != userId))
        {
            TempData["ErrorMessage"] = "Tylko właściciel zgłoszenia lub administrator może je edytować.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var result = await _ticketService.UpdateTicketAsync(model, userId);
        if (result)
        {
            TempData["SuccessMessage"] = "Zgłoszenie zostało zaktualizowane.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        TempData["ErrorMessage"] = "Wystąpił błąd podczas aktualizacji zgłoszenia.";
        return View(model);
    }

    // ============================================
    // REOPEN TICKET
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenTicket(int ticketId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
        if (ticket == null || ticket.CreatedByUserId != userId)
        {
            TempData["ErrorMessage"] = "Nie masz uprawnień do wznowienia tego zgłoszenia.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var result = await _ticketService.ReopenTicketAsync(ticketId, userId);
        if (result)
        {
            TempData["SuccessMessage"] = "Zgłoszenie zostało wznowione. Agent wróci do pracy nad nim.";
        }
        else
        {
            TempData["ErrorMessage"] = "Nie udało się wznowić zgłoszenia. Minęło 14 dni od rozwiązania lub status jest nieprawidłowy.";
        }

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // CLOSE TICKET
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseTicket(int ticketId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
        var isAgent = User.IsInRole("Agent") || User.IsInRole("Admin");
        if (ticket == null || (!isAgent && ticket.CreatedByUserId != userId))
        {
            TempData["ErrorMessage"] = "Nie masz uprawnień do zamknięcia tego zgłoszenia.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var result = await _ticketService.ChangeStatusAsync(ticketId, TicketStatus.Resolved, userId, "Zamknięte przez zgłaszającego");
        if (result)
            TempData["SuccessMessage"] = "Zgłoszenie zostało zamknięte.";
        else
            TempData["ErrorMessage"] = "Nie udało się zamknąć zgłoszenia.";

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // CHANGE STATUS
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int ticketId, TicketStatus newStatus, string? resolutionSummary)
    {
        if (newStatus == TicketStatus.Resolved && string.IsNullOrWhiteSpace(resolutionSummary))
        {
            TempData["ErrorMessage"] = "Musisz podać opis rozwiązania problemu przed oznaczeniem jako rozwiązane.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Json(new { success = false, message = "Nie jesteś zalogowany." });

        var result = await _ticketService.ChangeStatusAsync(ticketId, newStatus, userId, resolutionSummary);

        if (result)
            return RedirectToAction(nameof(Details), new { id = ticketId });

        TempData["ErrorMessage"] = "Nie udało się zmienić statusu.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // ASSIGN TICKET
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Assign(int ticketId, string? agentId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        await _ticketService.AssignTicketAsync(ticketId, agentId, userId);
        TempData["SuccessMessage"] = "Zgłoszenie zostało przypisane.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // TAKE TICKET
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Take(int ticketId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var result = await _ticketService.TakeTicketAsync(ticketId, userId);

        if (result)
        {
            TempData["SuccessMessage"] = "Przejąłeś zgłoszenie.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        TempData["ErrorMessage"] = "Nie udało się przejąć zgłoszenia (może być już przypisane).";
        return RedirectToAction(nameof(Unassigned));
    }

    // ============================================
    // ADD COMMENT
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int ticketId, string content, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Treść komentarza nie może być pusta.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var commentType = isInternal && (User.IsInRole("Agent") || User.IsInRole("Admin"))
            ? CommentType.Internal
            : CommentType.Public;

        await _ticketService.AddCommentAsync(ticketId, content, commentType, userId);

        TempData["SuccessMessage"] = "Komentarz został dodany.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // UPLOAD ATTACHMENT
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(int ticketId, List<IFormFile> file)
    {
        if (file == null || !file.Any(f => f.Length > 0))
        {
            TempData["ErrorMessage"] = "Wybierz plik do przesłania.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var uploaded = 0;
        try
        {
            foreach (var f in file.Where(f => f.Length > 0))
            {
                await _ticketService.AddAttachmentAsync(ticketId, f, userId);
                uploaded++;
            }
            TempData["SuccessMessage"] = uploaded == 1
                ? "Załącznik został przesłany."
                : $"Przesłano {uploaded} załączniki(-ów).";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Wystąpił błąd podczas przesyłania pliku.";
        }

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // ============================================
    // VIEW / DOWNLOAD ATTACHMENT
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int id, int ticketId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var result = await _ticketService.DeleteAttachmentAsync(id, userId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] =
            result ? "Załącznik został usunięty." : "Nie udało się usunąć załącznika.";

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpGet]
    public async Task<IActionResult> ViewAttachment(int id)
    {
        var attachment = await _ticketService.GetAttachmentAsync(id);
        if (attachment == null)
            return NotFound();

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound("Plik nie został znaleziony na serwerze.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var encodedName = Uri.EscapeDataString(attachment.OriginalFileName);
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{encodedName}";
        return File(fileBytes, attachment.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await _ticketService.GetAttachmentAsync(id);
        if (attachment == null)
            return NotFound();

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound("Plik nie został znaleziony na serwerze.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, attachment.ContentType, attachment.OriginalFileName);
    }

    // ============================================
    // DELETE TICKET
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account");

        var result = await _ticketService.DeleteTicketAsync(id, userId);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] =
            result ? "Zgłoszenie zostało usunięte." : "Nie udało się usunąć zgłoszenia.";

        return RedirectToAction(nameof(Index));
    }

    // ============================================
    // HELPERS
    // ============================================

    private static PaginatedList<TicketListItemViewModel> ToPagedViewModels(PaginatedList<Ticket> tickets)
    {
        return new PaginatedList<TicketListItemViewModel>(
            tickets.Select(TicketListItemViewModel.FromTicket).ToList(),
            tickets.TotalCount,
            tickets.PageIndex,
            tickets.PageSize);
    }

    private static SelectList GetStatusSelectList(TicketStatus? selected)
    {
        var statuses = Enum.GetValues<TicketStatus>()
            .Select(s => new { Value = (int)s, Text = GetStatusDisplayName(s) });
        return new SelectList(statuses, "Value", "Text", selected.HasValue ? (int)selected.Value : null);
    }

    private static SelectList GetPrioritySelectList(TicketPriority? selected)
    {
        var priorities = Enum.GetValues<TicketPriority>()
            .Select(p => new { Value = (int)p, Text = GetPriorityDisplayName(p) });
        return new SelectList(priorities, "Value", "Text", selected.HasValue ? (int)selected.Value : null);
    }

    private static string GetStatusDisplayName(TicketStatus status) => status switch
    {
        TicketStatus.New => "Nowy",
        TicketStatus.InProgress => "W realizacji",
        TicketStatus.WaitingForUser => "Oczekuje na użytkownika",
        TicketStatus.Resolved => "Rozwiązany",
        _ => status.ToString()
    };

    private static string GetPriorityDisplayName(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Niski",
        TicketPriority.Medium => "Średni",
        TicketPriority.High => "Wysoki",
        TicketPriority.Critical => "Krytyczny",
        _ => priority.ToString()
    };
}
