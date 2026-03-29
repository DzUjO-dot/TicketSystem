namespace ITServiceHelpDesk.Services.Interfaces;

/// <summary>
/// Interfejs serwisu do zarządzania plikami
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Zapisuje plik i zwraca ścieżkę
    /// </summary>
    Task<(string fileName, string filePath)> SaveFileAsync(IFormFile file, string subFolder);
    
    /// <summary>
    /// Usuwa plik
    /// </summary>
    Task<bool> DeleteFileAsync(string filePath);
    
    /// <summary>
    /// Sprawdza czy rozszerzenie pliku jest dozwolone
    /// </summary>
    bool IsAllowedExtension(string fileName);
    
    /// <summary>
    /// Sprawdza czy rozmiar pliku jest w limicie
    /// </summary>
    bool IsFileSizeAllowed(long fileSize);
    
    /// <summary>
    /// Pobiera maksymalny rozmiar pliku w MB
    /// </summary>
    int GetMaxFileSizeMB();
    
    /// <summary>
    /// Pobiera listę dozwolonych rozszerzeń
    /// </summary>
    IEnumerable<string> GetAllowedExtensions();
}
