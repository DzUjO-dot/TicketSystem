using Microsoft.EntityFrameworkCore;

namespace ITServiceHelpDesk.Models.ViewModels.Shared;

/// <summary>
/// Lista z paginacją - wspiera stronicowanie wyników
/// </summary>
public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }
    public int PageSize { get; private set; }

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalCount = count;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);

        AddRange(items);
    }

    /// <summary>
    /// Czy istnieje poprzednia strona
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// Czy istnieje następna strona
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// Numer pierwszego elementu na stronie
    /// </summary>
    public int FirstItemOnPage => (PageIndex - 1) * PageSize + 1;

    /// <summary>
    /// Numer ostatniego elementu na stronie
    /// </summary>
    public int LastItemOnPage => Math.Min(PageIndex * PageSize, TotalCount);

    /// <summary>
    /// Tworzy paginowaną listę z IQueryable
    /// </summary>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    /// <summary>
    /// Tworzy pustą paginowaną listę
    /// </summary>
    public static PaginatedList<T> Empty(int pageSize = 10)
    {
        return new PaginatedList<T>(new List<T>(), 0, 1, pageSize);
    }
}
