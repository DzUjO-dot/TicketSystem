using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Kategoria zgłoszenia
/// </summary>
public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa kategorii jest wymagana")]
    [StringLength(100, ErrorMessage = "Nazwa może mieć maksymalnie 100 znaków")]
    [Display(Name = "Nazwa")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Opis")]
    public string? Description { get; set; }

    [StringLength(50)]
    [Display(Name = "Ikona")]
    public string? Icon { get; set; } = "bi-folder";

    [StringLength(7)]
    [Display(Name = "Kolor")]
    public string? Color { get; set; } = "#7c3aed";

    [Display(Name = "Aktywna")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Kolejność wyświetlania")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Zgłoszenia w tej kategorii
    /// </summary>
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
