using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Account;

/// <summary>
/// ViewModel do wyświetlania i edycji profilu
/// </summary>
public class ProfileViewModel
{
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Imię jest wymagane")]
    [StringLength(50)]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane")]
    [StringLength(50)]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Dział")]
    public string? Department { get; set; }

    [StringLength(20)]
    [Display(Name = "Numer wewnętrzny")]
    public string? PhoneExtension { get; set; }

    [Phone]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    // ============================================
    // READONLY
    // ============================================

    [Display(Name = "Rola")]
    public string Role { get; set; } = "User";

    [Display(Name = "Data rejestracji")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Ostatnie logowanie")]
    public DateTime? LastLoginAt { get; set; }
}
