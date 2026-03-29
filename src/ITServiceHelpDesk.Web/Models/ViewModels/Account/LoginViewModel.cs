using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Account;

/// <summary>
/// ViewModel do logowania
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email jest wymagany")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane")]
    [DataType(DataType.Password)]
    [Display(Name = "Hasło")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Zapamiętaj mnie")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
