using Microsoft.AspNetCore.Components.Forms;

namespace UI.Features.VideoUpload;

/// <summary>
/// Service to validate video files for upload
/// </summary>
public sealed class VideoFileValidationService
{
    private readonly ILogger<VideoFileValidationService> _logger;

    private const int MaxAllowedFiles = 1;
    private const long MaxFileSizeInBytes = 500 * 1024 * 1024; // 500 MB
    private static readonly string[] SupportedExtensions = [".mp4"];

    public VideoFileValidationService(ILogger<VideoFileValidationService> logger) => _logger = logger;

    /// <summary>
    /// Validates the input file
    /// </summary>
    /// <param name="eventArgs">File selection event arguments</param>
    /// <returns>Result indicating validation success or failure with error message</returns>
    public Result<bool> ValidateInput(InputFileChangeEventArgs eventArgs)
    {
        _logger.LogDebug("Validating file upload. File count: {FileCount}", eventArgs.FileCount);

        switch (eventArgs.FileCount)
        {
            case 0:
                _logger.LogWarning("No file selected for upload");

                return Result<bool>.Failure("No file selected. Please choose a video file to upload.");

            case > MaxAllowedFiles:
                _logger.LogWarning("Too many files selected: {FileCount}", eventArgs.FileCount);

                return Result<bool>.Failure($"You selected {eventArgs.FileCount} files. Please select only one video file.");
        }

        IBrowserFile browserFile = eventArgs.File;

        bool isValidVideoFormat = SupportedExtensions.Contains(Path.GetExtension(browserFile.Name)
                                                                   .ToLowerInvariant());
        if (!isValidVideoFormat)
        {
            string supportedFormats = string.Join(", ", SupportedExtensions);

            _logger.LogWarning("Invalid file format for {FileName}. Expected: {SupportedFormats}",
                               browserFile.Name,
                               supportedFormats);

            return Result<bool>.Failure($"File type is not supported. Allowed formats: {supportedFormats}");
        }

        bool isFileEmpty = browserFile.Size <= 0;
        if (isFileEmpty)
        {
            _logger.LogWarning("Empty file detected: {FileName}", browserFile.Name);

            return Result<bool>.Failure("The selected file is empty. Please choose a valid video file.");
        }

        bool isFileTooLarge = browserFile.Size > MaxFileSizeInBytes;
        if (isFileTooLarge)
        {
            double fileSizeMb = browserFile.Size / (1024.0 * 1024.0);

            _logger.LogWarning("File too large: {FileName} ({FileSizeMb:F1} MB)",
                               browserFile.Name,
                               fileSizeMb);

            const double maxSizeMb = MaxFileSizeInBytes / (1024.0 * 1024.0);

            return Result<bool>.Failure($"File size ({fileSizeMb:F1} MB) exceeds the maximum allowed size of {maxSizeMb:F1} MB.");
        }

        _logger.LogInformation("File upload validation successful: {FileName}", browserFile.Name);

        return Result<bool>.Success(true);
    }
}