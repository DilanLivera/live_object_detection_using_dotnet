using System.Diagnostics;
using Microsoft.AspNetCore.Components.Forms;

namespace UI.Infrastructure.FileStorage;

/// <summary>
/// Defines the file storage operations.
/// </summary>
public class FileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _uploadsPath;
    private readonly string[] _allowedExtensions;

    /// <summary>
    /// Initializes a new instance of the FileStorageService.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations.</param>
    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger;

        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _allowedExtensions = [".mp4", ".webm", ".mov"];

        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    /// <summary>
    /// Saves file to disk and returns the file path.
    /// </summary>
    /// <param name="file">The file from the browser.</param>
    /// <param name="maxFileSizeInBytes">Maximum allowed file size in bytes.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the file path on success or an error message on failure.</returns>
    public async Task<Result<string>> SaveFileAsync(
        IBrowserFile file,
        long maxFileSizeInBytes,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(file != null, "File cannot be null");
        Debug.Assert(maxFileSizeInBytes > 0, "Maximum file size must be greater than zero");

        ArgumentNullException.ThrowIfNull(file);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxFileSizeInBytes, 0);

        string extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return Result<string>.Failure($"File type '{extension}' is not supported. Allowed types: {string.Join(", ", _allowedExtensions)}");
        }

        if (file.Size > maxFileSizeInBytes)
        {
            double maxSizeMb = maxFileSizeInBytes / (1024.0 * 1024.0);
            double fileSizeMb = file.Size / (1024.0 * 1024.0);

            return Result<string>.Failure($"File size ({fileSizeMb:F1} MB) exceeds maximum allowed size ({maxSizeMb:F1} MB)");
        }

        string tempFileName = Path.GetRandomFileName() + extension;
        string filePath = Path.Combine(_uploadsPath, tempFileName);

        try
        {
            _logger.LogInformation("Saving uploaded file: {FileName} ({FileSize} bytes) to {FilePath}",
                                   file.Name,
                                   file.Size,
                                   filePath);

            await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
            await using Stream uploadStream = file.OpenReadStream(maxFileSizeInBytes, cancellationToken);

            await uploadStream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            _logger.LogInformation("Successfully saved file to {FilePath}", filePath);

            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded file: {FileName}", file.Name);

            return Result<string>.Failure($"Failed to save file: {ex.Message}");
        }
    }
}