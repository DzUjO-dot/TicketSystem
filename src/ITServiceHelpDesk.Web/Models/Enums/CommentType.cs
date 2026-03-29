using System.ComponentModel.DataAnnotations;

namespace ITServiceHelpDesk.Models.Enums;

/// <summary>
/// Typ komentarza do zgłoszenia
/// </summary>
public enum CommentType
{
    [Display(Name = "Publiczny")]
    Public = 0,

    [Display(Name = "Wewnętrzny")]
    Internal = 1
}
