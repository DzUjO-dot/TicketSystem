using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITServiceHelpDesk.Models.Entities;

/// <summary>
/// Załącznik do zgłoszenia
/// </summary>
public class TicketAttachment
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Zgłoszenie")]
    public int TicketId { get; set; }

    [Required]
    [StringLength(255)]
    [Display(Name = "Nazwa pliku (systemowa)")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Display(Name = "Oryginalna nazwa pliku")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Ścieżka do pliku")]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Typ MIME")]
    public string ContentType { get; set; } = string.Empty;

    [Display(Name = "Rozmiar pliku (bytes)")]
    public long FileSize { get; set; }

    [Required]
    [Display(Name = "Przesłane przez")]
    public string UploadedByUserId { get; set; } = string.Empty;

    [Display(Name = "Data przesłania")]
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>
    /// Rozmiar pliku w formacie czytelnym dla człowieka
    /// </summary>
    [NotMapped]
    public string FileSizeFormatted
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = FileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Rozszerzenie pliku
    /// </summary>
    [NotMapped]
    public string FileExtension => Path.GetExtension(OriginalFileName).ToLowerInvariant();

    /// <summary>
    /// Czy plik jest obrazem
    /// </summary>
    [NotMapped]
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ikona dla typu pliku (Bootstrap Icons)
    /// </summary>
    [NotMapped]
    public string FileIcon => FileExtension switch
    {
        ".pdf" => "bi-file-earmark-pdf",
        ".doc" or ".docx" => "bi-file-earmark-word",
        ".xls" or ".xlsx" => "bi-file-earmark-excel",
        ".ppt" or ".pptx" => "bi-file-earmark-ppt",
        ".zip" or ".rar" or ".7z" => "bi-file-earmark-zip",
        ".txt" => "bi-file-earmark-text",
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "bi-file-earmark-image",
        ".mp4" or ".webm" or ".mov" or ".ogg" => "bi-file-earmark-play",
        _ => "bi-file-earmark"
    };

    // ============================================
    // NAVIGATION PROPERTIES
    // ============================================

    /// <summary>
    /// Zgłoszenie, do którego należy załącznik
    /// </summary>
    [ForeignKey(nameof(TicketId))]
    public virtual Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Użytkownik, który przesłał załącznik
    /// </summary>
    [ForeignKey(nameof(UploadedByUserId))]
    public virtual ApplicationUser UploadedBy { get; set; } = null!;
}
