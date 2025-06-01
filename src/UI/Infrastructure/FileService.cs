using System.Diagnostics;
using Microsoft.AspNetCore.Components.Forms;

namespace UI.Infrastructure;

/// <summary>
/// Service for managing file operations such as saving uploaded files, retrieving video streams, and cleanup.
/// </summary>
public class FileService
{
    private readonly ILogger<FileService> _logger;
    private readonly string _uploadsPath;

    private const long MaxFileSizeInBytes = 500 * 1024 * 1024; // 500 MB

    /// <summary>
    /// Initializes a new instance of the FileStorageService.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations.</param>
    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;

        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!Directory.Exists(_uploadsPath))
        {
            _ = Directory.CreateDirectory(_uploadsPath);
        }
    }

    /// <summary>
    /// Saves file to disk and returns the file path.
    /// </summary>
    /// <param name="file">The file from the browser.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A Result containing the file path on success or an error message on failure.</returns>
    public async Task<Result<string>> SaveFileAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file, nameof(file));

        string extension = Path.GetExtension(file.Name).ToLowerInvariant();
        string tempFileName = Path.GetRandomFileName() + extension;
        string filePath = Path.Combine(_uploadsPath, tempFileName);

        _logger.LogDebug("Saving uploaded file: {FileName} ({FileSize} bytes) to {FilePath}",
                         file.Name,
                         file.Size,
                         filePath);

        try
        {

            await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
            await using Stream uploadStream = file.OpenReadStream(MaxFileSizeInBytes, cancellationToken);

            await uploadStream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded file: {FileName}", file.Name);

            return Result<string>.Failure($"Failed to save file: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a file stream for reading a video file stored in directory.
    /// </summary>
    /// <param name="filename">The filename to retrieve.</param>
    /// <returns>A Result containing the file stream and content type on success or an error message on failure.</returns>
    public Result<(FileStream Stream, string ContentType)> GetVideoAsStream(string filename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename, nameof(filename));

        // IMPORTANT: Without this, malicious input like "../../../etc/passwd" could access files outside uploads directory
        string safeFilename = Path.GetFileName(filename);
        string filePath = Path.Combine(_uploadsPath, safeFilename);

        Debug.Assert(File.Exists(filePath), "File must exist");

        try
        {
            FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string contentType = Path.GetExtension(filename).ToLowerInvariant() switch
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
    /// <param name="imagePath">The full path to the image file.</param>
    /// <returns>A Result containing the image bytes on success or an error message on failure.</returns>
    public async Task<Result<byte[]>> GetImageAsBytesAsync(string imagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath, nameof(imagePath));

        Debug.Assert(File.Exists(imagePath), "Image must exist");

        try
        {
            await using FileStream fileStream = new(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using MemoryStream memoryStream = new();

            await fileStream.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            return Result<byte[]>.Success(imageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading image file: {FilePath}", imagePath);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath, nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug("Directory does not exist, nothing to delete: {DirectoryPath}", directoryPath);

            return Result<bool>.Success(true);
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath, nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug("Directory does not exist, nothing to delete: {DirectoryPath}", directoryPath);

            return Result<bool>.Success(true);
        }

        try
        {
            DeleteDirectory(directoryPath);

            Directory.CreateDirectory(directoryPath);

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
    public Result<int> DeleteOldFilesInUploadsDirectory(TimeSpan maxAge)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxAge, TimeSpan.Zero, nameof(maxAge));

        if (!Directory.Exists(_uploadsPath))
        {
            _logger.LogDebug("Uploads directory does not exist, skipping cleanup");

            return Result<int>.Success(0);
        }

        try
        {
            DateTime cutoffTime = DateTime.UtcNow - maxAge;
            string[] files = Directory.GetFiles(_uploadsPath,
                                                searchPattern: "*.*",
                                                SearchOption.TopDirectoryOnly);

            int deletedFiles = 0;
            foreach (string filePath in files)
            {
                FileInfo fileInfo = new(filePath);
                if (fileInfo.CreationTimeUtc < cutoffTime)
                {
                    File.Delete(filePath);
                    deletedFiles++;
                }
            }

            return Result<int>.Success(deletedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File cleanup failed");

            return Result<int>.Failure($"Failed to delete old files: {ex.Message}");
        }
    }

}