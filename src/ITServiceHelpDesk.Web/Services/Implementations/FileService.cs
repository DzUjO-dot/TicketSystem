using ITServiceHelpDesk.Services.Interfaces;

namespace ITServiceHelpDesk.Services.Implementations;

/// <summary>
/// Implementacja serwisu do zarządzania plikami
/// </summary>
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileService> _logger;

    private readonly string[] _allowedExtensions;
    private readonly int _maxFileSizeMB;

    public FileService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<FileService> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _logger = logger;

        _allowedExtensions = _configuration.GetSection("AppSettings:AllowedFileExtensions").Get<string[]>() 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };
        
        _maxFileSizeMB = _configuration.GetValue<int>("AppSettings:MaxFileUploadSizeMB", 10);
    }

    public async Task<(string fileName, string filePath)> SaveFileAsync(IFormFile file, string subFolder)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Plik jest pusty lub nie został przesłany.");
        }

        if (!IsAllowedExtension(file.FileName))
        {
            throw new ArgumentException($"Typ pliku {Path.GetExtension(file.FileName)} nie jest dozwolony.");
        }

        if (!IsFileSizeAllowed(file.Length))
        {
            throw new ArgumentException($"Plik jest zbyt duży. Maksymalny rozmiar to {_maxFileSizeMB}MB.");
        }

        // Create unique filename
        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

        // Build path
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
        
        // Ensure directory exists
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var fullPath = Path.Combine(uploadPath, uniqueFileName);
        var relativePath = Path.Combine("uploads", subFolder, uniqueFileName).Replace("\\", "/");

        try
        {
            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("File saved: {FileName} -> {FilePath}", file.FileName, relativePath);

            return (uniqueFileName, relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        var fullPath = Path.Combine(_environment.WebRootPath, filePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found for deletion: {FilePath}", fullPath);
            return false;
        }

        try
        {
            await Task.Run(() => File.Delete(fullPath));
            _logger.LogInformation("File deleted: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public bool IsAllowedExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    public bool IsFileSizeAllowed(long fileSize)
    {
        var maxBytes = _maxFileSizeMB * 1024 * 1024;
        return fileSize <= maxBytes;
    }

    public int GetMaxFileSizeMB()
    {
        return _maxFileSizeMB;
    }

    public IEnumerable<string> GetAllowedExtensions()
    {
        return _allowedExtensions;
    }
}
