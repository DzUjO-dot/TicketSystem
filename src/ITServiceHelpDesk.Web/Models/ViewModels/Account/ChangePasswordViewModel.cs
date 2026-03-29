using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Account;

/// <summary>
/// ViewModel do zmiany hasła
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Obecne hasło jest wymagane")]
    [DataType(DataType.Password)]
    [Display(Name = "Obecne hasło")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nowe hasło jest wymagane")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć od 6 do 100 znaków")]
    [DataType(DataType.Password)]
    [Display(Name = "Nowe hasło")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Potwierdź nowe hasło")]
    [Compare("NewPassword", ErrorMessage = "Hasła nie są identyczne")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
