using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Models.ViewModels.Account;
using ITServiceHelpDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;



namespace ITServiceHelpDesk.Controllers;

/// <summary>
/// Kontroler do zarządzania kontem użytkownika.
/// Obsługuje lokalne logowanie (tylko admin techniczny) oraz SSO przez Microsoft.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AccountController> _logger;

    // Email jedynego konta lokalnego (technicznego admina)
    private const string LocalAdminEmail = "admin@helpdesk.local";

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ============================================
    // LOGIN (tylko dla konta lokalnego admina)
    // ============================================

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // Tylko konto lokalne (admin@helpdesk.local) może logować się przez formularz
        if (!model.Email.Equals(LocalAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Logowanie przez formularz jest dostępne tylko dla administratora technicznego. Pozostałe konta korzystają z logowania Microsoft.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Nieprawidłowy email lub hasło.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Konto zostało dezaktywowane.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.Now;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin {Email} zalogowany lokalnie.", model.Email);
            await _auditService.LogWithUserAsync(user.Id, "AdminLogin", "User", user.Id);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Konto {Email} zablokowane.", model.Email);
            ModelState.AddModelError(string.Empty, "Konto zostało zablokowane z powodu zbyt wielu nieudanych prób. Spróbuj ponownie za 15 minut.");
            return View(model);
        }

        await _auditService.LogWithUserAsync(user.Id, "Failed_Login", "User", user.Id);
        ModelState.AddModelError(string.Empty, "Nieprawidłowy email lub hasło.");
        return View(model);
    }

    // ============================================
    // MICROSOFT SSO - INICJOWANIE LOGOWANIA
    // ============================================

    [HttpGet]
    public IActionResult MicrosoftLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Microsoft", redirectUrl);
        return Challenge(properties, "Microsoft");
    }

    // ============================================
    // MICROSOFT SSO - CALLBACK (auto-provisioning)
    // ============================================

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            _logger.LogError("Błąd Microsoft SSO: {Error}", remoteError);
            TempData["ErrorMessage"] = $"Błąd logowania Microsoft: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Nie można uzyskać danych z konta Microsoft. Spróbuj ponownie.";
            return RedirectToAction(nameof(Login));
        }

        // Próba logowania istniejącym kontem powiązanym z Microsoft
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null)
            {
                if (!existingUser.IsActive)
                {
                    await _signInManager.SignOutAsync();
                    TempData["ErrorMessage"] = "Twoje konto zostało dezaktywowane. Skontaktuj się z administratorem IT.";
                    return RedirectToAction(nameof(Login));
                }

                existingUser.LastLoginAt = DateTime.Now;
                await _userManager.UpdateAsync(existingUser);
                await _auditService.LogWithUserAsync(existingUser.Id, "SsoLogin", "User", existingUser.Id);
            }

            _logger.LogInformation("Użytkownik zalogowany przez Microsoft SSO.");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        if (signInResult.IsLockedOut)
        {
            TempData["ErrorMessage"] = "Konto zostało zablokowane. Skontaktuj się z administratorem IT.";
            return RedirectToAction(nameof(Login));
        }

        // Użytkownik nie istnieje - auto-provisioning
        // Pobierz email z claims (MapInboundClaims = false, więc używamy oryginalnych nazw JWT)
        var email = info.Principal.FindFirstValue("email")
                    ?? info.Principal.FindFirstValue("preferred_username");

        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Konto Microsoft nie udostępniło adresu email. Skontaktuj się z administratorem IT.";
            return RedirectToAction(nameof(Login));
        }

        // Blokada - konto admina nie może logować się przez SSO
        if (email.Equals(LocalAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Konto administratora wymaga logowania lokalnego.";
            return RedirectToAction(nameof(Login));
        }

        // Pobierz imię i nazwisko z claims
        var firstName = info.Principal.FindFirstValue("given_name") ?? "";
        var lastName = info.Principal.FindFirstValue("family_name") ?? "";

        if (string.IsNullOrEmpty(firstName))
        {
            var fullName = info.Principal.FindFirstValue("name") ?? email.Split('@')[0];
            var parts = fullName.Split(' ', 2);
            firstName = parts[0];
            lastName = parts.Length > 1 ? parts[1] : "";
        }

        // Sprawdź czy użytkownik z tym emailem już istnieje (powiąż konto)
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // Automatycznie utwórz konto przy pierwszym logowaniu
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = string.IsNullOrEmpty(lastName) ? "-" : lastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Nie można automatycznie utworzyć konta SSO dla {Email}: {Errors}", email, errors);
                TempData["ErrorMessage"] = "Nie można automatycznie utworzyć konta. Skontaktuj się z administratorem IT.";
                return RedirectToAction(nameof(Login));
            }

            // Domyślna rola: User (admin może później zmienić w panelu)
            await _userManager.AddToRoleAsync(user, "User");
            await _auditService.LogWithUserAsync(user.Id, "SsoAutoProvision", "User", user.Id);
            _logger.LogInformation("Automatycznie utworzono konto SSO dla {Email}.", email);
        }
        else if (!user.IsActive)
        {
            TempData["ErrorMessage"] = "Twoje konto zostało dezaktywowane. Skontaktuj się z administratorem IT.";
            return RedirectToAction(nameof(Login));
        }

        // Powiąż logowanie zewnętrzne z kontem użytkownika
        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded && !addLoginResult.Errors.All(e => e.Code == "LoginAlreadyAssociated"))
        {
            _logger.LogWarning("Nie można powiązać logowania SSO dla {Email}.", email);
        }

        // Zaloguj użytkownika
        user.LastLoginAt = DateTime.Now;
        await _userManager.UpdateAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _auditService.LogWithUserAsync(user.Id, "SsoLogin", "User", user.Id);

        _logger.LogInformation("Użytkownik {Email} zalogowany przez Microsoft SSO.", email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    // ============================================
    // LOGOUT
    // ============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();

        if (userId != null)
            await _auditService.LogWithUserAsync(userId, "Logout", "User", userId);

        _logger.LogInformation("Użytkownik wylogowany.");
        return RedirectToAction(nameof(Login));
    }

    // ============================================
    // ACCESS DENIED
    // ============================================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ============================================
    // PROFILE
    // ============================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);
        var hasLocalPassword = await _userManager.HasPasswordAsync(user);

        var model = new ProfileViewModel
        {
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            PhoneExtension = user.PhoneExtension,
            PhoneNumber = user.PhoneNumber,
            Role = roles.FirstOrDefault() ?? "User",
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        ViewBag.HasLocalPassword = hasLocalPassword;
        ViewBag.IsSsoUser = logins.Any(l => l.LoginProvider == "Microsoft");

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Role = roles.FirstOrDefault() ?? "User";
            return View(model);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Department = model.Department;
        user.PhoneExtension = model.PhoneExtension;
        user.PhoneNumber = model.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Profil został zaktualizowany.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        var userRoles = await _userManager.GetRolesAsync(user);
        model.Role = userRoles.FirstOrDefault() ?? "User";
        return View(model);
    }

    // ============================================
    // CHANGE PASSWORD (tylko dla konta lokalnego)
    // ============================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        // Użytkownicy SSO nie mają lokalnego hasła
        if (!await _userManager.HasPasswordAsync(user))
        {
            TempData["InfoMessage"] = "Twoje konto używa logowania Microsoft (SSO). Hasłem zarządzasz w portalu Microsoft: <a href='https://account.microsoft.com' target='_blank'>account.microsoft.com</a>";
            return RedirectToAction(nameof(Profile));
        }

        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        if (!await _userManager.HasPasswordAsync(user))
        {
            TempData["InfoMessage"] = "Twoje konto używa logowania Microsoft (SSO). Hasłem zarządzasz w portalu Microsoft.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            await _auditService.LogWithUserAsync(user.Id, "PasswordChange", "User", user.Id);

            TempData["SuccessMessage"] = "Hasło zostało zmienione pomyślnie.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error.Code));

        return View(model);
    }

    // ============================================
    // NOTIFICATIONS - OZNACZ JAKO PRZECZYTANE
    // ============================================

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead(string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (userId != null)
            await _notificationService.MarkAllAsReadAsync(userId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } referer
            ? referer
            : Url.Action("Index", "Dashboard")!);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Json(0);
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(count);
    }

    // ============================================
    // REGISTER - WYŁĄCZONE (użytkownicy przez SSO)
    // ============================================

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        TempData["InfoMessage"] = "Rejestracja jest wyłączona. Zaloguj się przez Microsoft lub skontaktuj się z administratorem IT.";
        return RedirectToAction(nameof(Login));
    }

    // ============================================
    // HELPERS
    // ============================================

    private static string TranslateIdentityError(string code) => code switch
    {
        "PasswordTooShort" => "Hasło jest zbyt krótkie. Minimalna długość to 6 znaków.",
        "PasswordRequiresDigit" => "Hasło musi zawierać co najmniej jedną cyfrę.",
        "PasswordRequiresLower" => "Hasło musi zawierać co najmniej jedną małą literę.",
        "PasswordRequiresUpper" => "Hasło musi zawierać co najmniej jedną wielką literę.",
        "PasswordRequiresNonAlphanumeric" => "Hasło musi zawierać co najmniej jeden znak specjalny.",
        "PasswordMismatch" => "Obecne hasło jest nieprawidłowe.",
        "DuplicateUserName" => "Ta nazwa użytkownika jest już zajęta.",
        "DuplicateEmail" => "Ten adres email jest już zarejestrowany.",
        "InvalidEmail" => "Nieprawidłowy format adresu email.",
        _ => $"Wystąpił błąd: {code}"
    };
}
