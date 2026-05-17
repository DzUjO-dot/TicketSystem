using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Services.Interfaces;

namespace ITServiceHelpDesk.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICategoryService _categoryService;
    private readonly IAuditService _auditService;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ICategoryService categoryService,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _categoryService = categoryService;
        _auditService = auditService;
    }

    public IActionResult Index()
    {
        return View();
    }

    #region Users Management

    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var userViewModels = new List<dynamic>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dynamic vm = new System.Dynamic.ExpandoObject();
            vm.Id = user.Id;
            vm.Email = user.Email;
            vm.FirstName = user.FirstName;
            vm.LastName = user.LastName;
            vm.FullName = $"{user.FirstName} {user.LastName}";
            vm.Department = user.Department;
            vm.IsActive = user.IsActive;
            vm.CreatedAt = user.CreatedAt;
            vm.Roles = roles;
            userViewModels.Add(vm);
        }

        ViewBag.Roles = await _roleManager.Roles.ToListAsync();
        return View(userViewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Użytkownik nie został znaleziony.";
            return RedirectToAction(nameof(Users));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "ToggleUserStatus",
            "User",
            userId,
            $"Status zmieniony na: {(user.IsActive ? "Aktywny" : "Nieaktywny")}"
        );

        TempData["SuccessMessage"] = $"Status użytkownika {user.Email} został zmieniony.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUserRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Użytkownik nie został znaleziony.";
            return RedirectToAction(nameof(Users));
        }

        // Remove existing roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Add new role
        if (!string.IsNullOrEmpty(role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "ChangeUserRole",
            "User",
            userId,
            $"Rola zmieniona na: {role}"
        );

        TempData["SuccessMessage"] = $"Rola użytkownika {user.Email} została zmieniona na {role}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Użytkownik nie został znaleziony.";
            return RedirectToAction(nameof(Users));
        }

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.Now;
        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "DeleteUser",
            "User",
            userId,
            $"Użytkownik {user.Email} został usunięty (soft delete)"
        );

        TempData["SuccessMessage"] = $"Użytkownik {user.Email} został usunięty.";
        return RedirectToAction(nameof(Users));
    }

    #endregion

    #region Categories Management

    public async Task<IActionResult> Categories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();

        var ticketCounts = await _context.Tickets
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        ViewBag.TicketCounts = ticketCounts;
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Nazwa kategorii jest wymagana.";
            return RedirectToAction(nameof(Categories));
        }

        var category = new Category
        {
            Name = name,
            Description = description,
            IsActive = true
        };

        await _categoryService.CreateAsync(category);

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "CreateCategory",
            "Category",
            category.Id.ToString(),
            $"Utworzono kategorię: {name}"
        );

        TempData["SuccessMessage"] = $"Kategoria '{name}' została utworzona.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int id, string name, string? description)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Kategoria nie została znaleziona.";
            return RedirectToAction(nameof(Categories));
        }

        category.Name = name;
        category.Description = description;
        await _categoryService.UpdateAsync(category);

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "UpdateCategory",
            "Category",
            id.ToString(),
            $"Zaktualizowano kategorię: {name}"
        );

        TempData["SuccessMessage"] = $"Kategoria '{name}' została zaktualizowana.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryStatus(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Kategoria nie została znaleziona.";
            return RedirectToAction(nameof(Categories));
        }

        category.IsActive = !category.IsActive;
        await _categoryService.UpdateAsync(category);

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "ToggleCategoryStatus",
            "Category",
            id.ToString(),
            $"Status kategorii {category.Name} zmieniony na: {(category.IsActive ? "Aktywna" : "Nieaktywna")}"
        );

        TempData["SuccessMessage"] = $"Status kategorii '{category.Name}' został zmieniony.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Kategoria nie została znaleziona.";
            return RedirectToAction(nameof(Categories));
        }

        // Check if category has tickets
        var ticketsCount = await _categoryService.GetTicketCountAsync(id);
        if (ticketsCount > 0)
        {
            TempData["ErrorMessage"] = $"Nie można usunąć kategorii '{category.Name}' - zawiera {ticketsCount} zgłoszeń.";
            return RedirectToAction(nameof(Categories));
        }

        await _categoryService.DeactivateAsync(id);

        await _auditService.LogWithUserAsync(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            "DeleteCategory",
            "Category",
            id.ToString(),
            $"Usunięto kategorię: {category.Name}"
        );

        TempData["SuccessMessage"] = $"Kategoria '{category.Name}' została usunięta.";
        return RedirectToAction(nameof(Categories));
    }

    #endregion

    #region Audit Log

    public async Task<IActionResult> AuditLog(int page = 1, string? searchTerm = null)
    {
        const int pageSize = 50;

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(a =>
                a.Action.Contains(searchTerm) ||
                a.EntityType.Contains(searchTerm) ||
                (a.OldValues != null && a.OldValues.Contains(searchTerm)) ||
                (a.NewValues != null && a.NewValues.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        // Get user names
        var userIds = logs.Where(l => l.UserId != null).Select(l => l.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var logsWithUsers = logs.Select(l =>
        {
            dynamic vm = new System.Dynamic.ExpandoObject();
            vm.Id = l.Id;
            vm.UserId = l.UserId;
            vm.UserName = l.UserId != null && users.ContainsKey(l.UserId) ? users[l.UserId] : "System";
            vm.Action = l.Action;
            vm.EntityType = l.EntityType;
            vm.EntityId = l.EntityId;
            vm.Details = l.NewValues ?? l.OldValues ?? "";
            vm.IpAddress = l.IpAddress;
            vm.Timestamp = l.CreatedAt;
            return vm;
        }).ToList<dynamic>();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.SearchTerm = searchTerm;

        return View(logsWithUsers);
    }

    #endregion
}
