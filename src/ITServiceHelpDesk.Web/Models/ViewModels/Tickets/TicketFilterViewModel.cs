using ITServiceHelpDesk.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.ViewModels.Tickets;

/// <summary>
/// ViewModel do filtrowania i wyszukiwania zgłoszeń
/// </summary>
public class TicketFilterViewModel
{
    [Display(Name = "Szukaj")]
    public string? SearchTerm { get; set; }

    [Display(Name = "Status")]
    public TicketStatus? Status { get; set; }

    [Display(Name = "Priorytet")]
    public TicketPriority? Priority { get; set; }

    [Display(Name = "Kategoria")]
    public int? CategoryId { get; set; }

    [Display(Name = "Przypisany do")]
    public string? AssignedToUserId { get; set; }

    [Display(Name = "Data od")]
    [DataType(DataType.Date)]
    public DateTime? DateFrom { get; set; }

    [Display(Name = "Data do")]
    [DataType(DataType.Date)]
    public DateTime? DateTo { get; set; }

    [Display(Name = "Tylko przeterminowane")]
    public bool? ShowOverdueOnly { get; set; }

    // ============================================
    // SORTING
    // ============================================
    
    public string? SortBy { get; set; } = "createdat";
    public bool SortDescending { get; set; } = true;

    // ============================================
    // PAGINATION
    // ============================================
    
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // ============================================
    // SELECT LISTS
    // ============================================
    
    public SelectList? Categories { get; set; }
    public SelectList? Statuses { get; set; }
    public SelectList? Priorities { get; set; }
    public SelectList? Agents { get; set; }

    // ============================================
    // HELPERS
    // ============================================
    
    /// <summary>
    /// Czy jakiekolwiek filtry są aktywne
    /// </summary>
    public bool HasActiveFilters => 
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        Status.HasValue ||
        Priority.HasValue ||
        CategoryId.HasValue ||
        AssignedToUserId != null ||
        DateFrom.HasValue ||
        DateTo.HasValue ||
        ShowOverdueOnly == true;

    /// <summary>
    /// Czyści wszystkie filtry
    /// </summary>
    public void ClearFilters()
    {
        SearchTerm = null;
        Status = null;
        Priority = null;
        CategoryId = null;
        AssignedToUserId = null;
        DateFrom = null;
        DateTo = null;
        ShowOverdueOnly = null;
        PageIndex = 1;
    }
}
