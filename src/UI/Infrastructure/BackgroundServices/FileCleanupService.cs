using UI.Infrastructure.FileStorage;

namespace UI.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that handles cleanup of uploaded files and directories.
/// Cleans the entire uploads folder on startup and then removes files older than 1 hour every hour.
/// </summary>
public class FileCleanupService : BackgroundService
{
    private readonly ILogger<FileCleanupService> _logger;
    private readonly FileStorageService _fileStorageService;
    private readonly string _uploadsPath;
    private readonly TimeSpan _fileMaxAge;
    private readonly TimeSpan _cleanupInterval;

    public FileCleanupService(
        ILogger<FileCleanupService> logger,
        FileStorageService fileStorageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _fileStorageService = fileStorageService;
        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        double fileMaxAgeInHours = configuration.GetValue("FileCleanup:FileMaxAgeInHours", defaultValue: 1.0);
        double cleanupIntervalInHours = configuration.GetValue("FileCleanup:CleanupIntervalInHours", defaultValue: 1.0);
        _fileMaxAge = TimeSpan.FromHours(fileMaxAgeInHours);
        _cleanupInterval = TimeSpan.FromHours(cleanupIntervalInHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File cleanup service starting");

        _logger.LogInformation("Cleanup uploads directory: {UploadsPath}", _uploadsPath);

        Result<bool> clearDirectoryResult = _fileStorageService.ClearDirectory(_uploadsPath);

        if (clearDirectoryResult.IsFailure)
        {
            _logger.LogWarning("Failed to delete uploads directory: {ErrorMessage}",
                               clearDirectoryResult.ErrorMessage);
        }
        else
        {
            _logger.LogInformation("Successfully cleaned uploads directory");
        }

        using PeriodicTimer timer = new(_cleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Starting periodic cleanup of files older than {MaxAge}", _fileMaxAge);

                Result<int> deleteFilesResult = _fileStorageService.DeleteFiles(_fileMaxAge);

                if (!deleteFilesResult.IsSuccess)
                {
                    _logger.LogError("Periodic cleanup failed: {ErrorMessage}", deleteFilesResult.ErrorMessage);

                    continue;
                }

                _logger.LogInformation("Periodic cleanup completed. Deleted {DeletedFiles} files",
                                       deleteFilesResult.Value);

            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("File cleanup service stopping");
        }
    }
}