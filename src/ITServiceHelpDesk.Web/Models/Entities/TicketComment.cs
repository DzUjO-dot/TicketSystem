using ITServiceHelpDesk.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Komentarz do zgłoszenia
/// </summary>
public class TicketComment
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Zgłoszenie")]
    public int TicketId { get; set; }

    [Required]
    [Display(Name = "Autor")]
    public string AuthorId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Treść komentarza jest wymagana")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Komentarz może mieć maksymalnie 2000 znaków")]
    [Display(Name = "Treść")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Typ komentarza")]
    public CommentType CommentType { get; set; } = CommentType.Public;

    [Display(Name = "Data utworzenia")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Data edycji")]
    public DateTime? UpdatedAt { get; set; }

    [Display(Name = "Usunięty")]
    public bool IsDeleted { get; set; } = false;

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Czy komentarz był edytowany
    /// </summary>
    [NotMapped]
    public bool IsEdited => UpdatedAt.HasValue;

    /// <summary>
    /// Czy komentarz jest wewnętrzny (tylko dla agentów/adminów)
    /// </summary>
    [NotMapped]
    public bool IsInternal => CommentType == CommentType.Internal;

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Zgłoszenie, do którego należy komentarz
    /// </summary>
    [ForeignKey(nameof(TicketId))]
    public virtual Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Autor komentarza
    /// </summary>
    [ForeignKey(nameof(AuthorId))]
    public virtual ApplicationUser Author { get; set; } = null!;
}
