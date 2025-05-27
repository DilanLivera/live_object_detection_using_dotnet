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
            _ = Directory.CreateDirectory(_uploadsPath);
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

    /// <summary>
    /// Gets a file stream for reading a stored file.
    /// </summary>
    /// <param name="filename">The filename to retrieve.</param>
    /// <returns>A Result containing the file stream and content type on success or an error message on failure.</returns>
    public Result<(FileStream Stream, string ContentType)> GetFileStream(string filename)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(filename), "Filename cannot be null or empty");

        string extension = Path.GetExtension(filename).ToLowerInvariant();

        // IMPORTANT: Without this, malicious input like "../../../etc/passwd" could access files outside uploads directory
        string safeFilename = Path.GetFileName(filename);
        string filePath = Path.Combine(_uploadsPath, safeFilename);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Video file not found: {FilePath}", filePath);

            return Result<(FileStream, string)>.Failure($"The requested video file '{filename}' could not be found");
        }

        try
        {
            FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string contentType = extension.ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };

            return Result<(FileStream, string)>.Success((fileStream, contentType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening video file: {Filename}", filename);

            return Result<(FileStream, string)>.Failure("An error occurred while accessing the video file");
        }
    }

    /// <summary>
    /// Reads an image file and returns image bytes.
    /// </summary>
    /// <param name="filePath">The full path to the image file.</param>
    /// <returns>A Result containing the image bytes on success or an error message on failure.</returns>
    public async Task<Result<byte[]>> ReadImageFileAsync(string filePath)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(filePath), "File path cannot be null or empty");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Image file not found: {FilePath}", filePath);

            return Result<byte[]>.Failure($"The image file could not be found: {filePath}");
        }

        try
        {
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using MemoryStream memoryStream = new();

            await fileStream.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            _logger.LogDebug("Successfully read image file: {FilePath} ({Size} bytes)", filePath, imageBytes.Length);

            return Result<byte[]>.Success(imageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading image file: {FilePath}", filePath);

            return Result<byte[]>.Failure($"Failed to read image file: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to clear.</param>
    /// <returns>A Result indicating success or failure with error message.</returns>
    public Result<bool> DeleteDirectory(string directoryPath)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(directoryPath), "Directory path cannot be null or empty");

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug("Directory does not exist, nothing to delete: {DirectoryPath}", directoryPath);

            return Result<bool>.Success(true);
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);

            _logger.LogDebug("Successfully deleted directory: {DirectoryPath}", directoryPath);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete directory: {DirectoryPath}", directoryPath);

            return Result<bool>.Failure($"Failed to delete directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes all contents in the directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to clear.</param>
    /// <returns>A Result indicating success or failure with error message.</returns>
    public Result<bool> ClearDirectory(string directoryPath)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(directoryPath), "Directory path cannot be null or empty");

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug("Directory does not exist, nothing to delete: {DirectoryPath}", directoryPath);

            return Result<bool>.Success(true);
        }

        try
        {
            DeleteDirectory(directoryPath);

            Directory.CreateDirectory(directoryPath);

            _logger.LogDebug("Successfully cleared directory contents: {DirectoryPath}", directoryPath);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear directory contents: {DirectoryPath}", directoryPath);

            return Result<bool>.Failure($"Failed to clear directory contents: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes files in the uploads directory that are older than the specified age.
    /// </summary>
    /// <param name="maxAge">Maximum age of files to keep.</param>
    /// <returns>A Result containing the number of deleted files on success or an error message on failure.</returns>
    public Result<int> DeleteFiles(TimeSpan maxAge)
    {
        Debug.Assert(maxAge > TimeSpan.Zero, "Max age must be greater than zero");

        if (!Directory.Exists(_uploadsPath))
        {
            _logger.LogDebug("Uploads directory does not exist, skipping cleanup");

            return Result<int>.Success(0);
        }

        try
        {
            DateTime cutoffTime = DateTime.UtcNow - maxAge;
            int deletedFiles = 0;

            string[] files = Directory.GetFiles(_uploadsPath,
                                                searchPattern: "*.*",
                                                SearchOption.TopDirectoryOnly);

            foreach (string filePath in files)
            {
                FileInfo fileInfo = new(filePath);
                if (fileInfo.CreationTimeUtc < cutoffTime)
                {
                    File.Delete(filePath);

                    deletedFiles++;

                    _logger.LogDebug("Deleted old file: {FilePath}", filePath);
                }
            }

            _logger.LogDebug("Deleted {DeletedFiles} old files", deletedFiles);

            return Result<int>.Success(deletedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File cleanup failed");

            return Result<int>.Failure($"Failed to delete old files: {ex.Message}");
        }
    }

}