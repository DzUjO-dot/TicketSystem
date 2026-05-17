using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Log audytowy - zapis wszystkich istotnych operacji w systemie
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    [Display(Name = "Użytkownik")]
    public string? UserId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Akcja")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Typ encji")]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "ID encji")]
    public string EntityId { get; set; } = string.Empty;

    [Display(Name = "Poprzednie wartości (JSON)")]
    public string? OldValues { get; set; }

    [Display(Name = "Nowe wartości (JSON)")]
    public string? NewValues { get; set; }

    [StringLength(45)]
    [Display(Name = "Adres IP")]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    [Display(Name = "User Agent")]
    public string? UserAgent { get; set; }

    [Display(Name = "Data")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Ikona dla typu akcji
    /// </summary>
    [NotMapped]
    public string ActionIcon => Action.ToLowerInvariant() switch
    {
        "create" => "bi-plus-circle text-success",
        "update" => "bi-pencil text-warning",
        "delete" => "bi-trash text-danger",
        "login" => "bi-box-arrow-in-right text-info",
        "logout" => "bi-box-arrow-right text-secondary",
        "failed_login" => "bi-exclamation-triangle text-danger",
        _ => "bi-activity text-primary"
    };

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Użytkownik, który wykonał akcję (null dla akcji systemowych)
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
