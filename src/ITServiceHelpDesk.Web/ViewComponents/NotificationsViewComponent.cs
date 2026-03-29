using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ITServiceHelpDesk.ViewComponents;

public class NotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsViewComponent(
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (userId == null)
            return View(new List<Notification>());

        var notifications = await _notificationService.GetUnreadAsync(userId, 8);
        return View(notifications.ToList());
    }
}
