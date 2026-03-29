using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Account;

/// <summary>
/// ViewModel do rejestracji
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Imię jest wymagane")]
    [StringLength(50, ErrorMessage = "Imię może mieć maksymalnie 50 znaków")]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane")]
    [StringLength(50, ErrorMessage = "Nazwisko może mieć maksymalnie 50 znaków")]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email jest wymagany")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć od 6 do 100 znaków")]
    [DataType(DataType.Password)]
    [Display(Name = "Hasło")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Potwierdź hasło")]
    [Compare("Password", ErrorMessage = "Hasła nie są identyczne")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Dział")]
    public string? Department { get; set; }

    [StringLength(20)]
    [Display(Name = "Numer wewnętrzny")]
    public string? PhoneExtension { get; set; }
}
