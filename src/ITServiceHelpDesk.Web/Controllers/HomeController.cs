using Microsoft.AspNetCore.Mvc;

namespace ITServiceHelpDesk.Controllers;

/// <summary>
/// Kontroler strony głównej
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Jeśli użytkownik jest zalogowany, przekieruj do dashboardu
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Home/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? traceId = null)
    {
        ViewBag.TraceId = traceId ?? HttpContext.TraceIdentifier;
        return View();
    }
}
