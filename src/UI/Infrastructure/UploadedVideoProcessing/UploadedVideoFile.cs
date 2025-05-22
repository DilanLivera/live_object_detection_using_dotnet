namespace UI.Infrastructure.UploadedVideoProcessing;

/// <summary>
/// Represents an uploaded video file with its metadata and operations
/// </summary>
public class UploadedVideoFile
{
    private static readonly string[] SupportedExtensions = [".mp4"]; // Can be expanded to include .webm, .mov, etc.

    /// <summary>
    /// Path to the saved video file on disk
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Path to the directory where frames will be extracted
    /// </summary>
    public string FrameDirectoryPath { get; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTimeOffset UploadedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the video processing was completed
    /// </summary>
    public DateTimeOffset? ProcessingCompletedAt { get; private set; }

    /// <summary>
    /// Creates a new instance of UploadedVideoFile
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileSize">Size in bytes</param>
    /// <exception cref="ArgumentException">Thrown if file name is invalid or size is not positive</exception>
    public UploadedVideoFile(string fileName, long fileSize)
    {
        // set file name
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("File name cannot be empty", nameof(fileName));
        }

        bool isVideoFile = SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
        if (!isVideoFile)
        {
            throw new ArgumentException($"File must be one of the supported formats: {string.Join(", ", SupportedExtensions)}", nameof(fileName));
        }

        Name = fileName;

        // set file path
        string uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        string extension = Path.GetExtension(fileName);
        string tempFileName = Path.GetRandomFileName() + extension;
        FilePath = Path.Combine(uploadsPath, tempFileName);

        // set file size
        if (fileSize <= 0)
        {
            throw new ArgumentException("File size must be greater than zero", nameof(fileSize));
        }

        Size = fileSize;

        // set frame directory path
        string framesDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "frames");
        string originalFileName = Path.GetFileNameWithoutExtension(Name);
        string safeFileName = string.Join("_", originalFileName.Split(Path.GetInvalidFileNameChars()));
        string videoId = $"{safeFileName}_{UploadedAt:yyyyMMddHHmmss}";

        if (!Directory.Exists(framesDirectoryPath))
        {
            Directory.CreateDirectory(framesDirectoryPath);
        }

        FrameDirectoryPath = Path.Combine(framesDirectoryPath, videoId);
    }

    /// <summary>
    /// Sets the processing completion time to the current time
    /// </summary>
    public void MarkAsProcessed() => ProcessingCompletedAt = DateTimeOffset.UtcNow;
}