using System.Diagnostics;

namespace UI.Components.Pages.Upload;

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
    /// <param name="filePath">Path where the file was saved</param>
    /// <exception cref="ArgumentException">Thrown if file name is invalid or size is not positive</exception>
    public UploadedVideoFile(string fileName, long fileSize, string filePath)
    {
        // Set file name
        Debug.Assert(!string.IsNullOrWhiteSpace(fileName), "File name cannot be null or whitespace");
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        bool isVideoFile = SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
        if (!isVideoFile)
        {
            throw new ArgumentException($"File must be one of the supported formats: {string.Join(", ", SupportedExtensions)}", nameof(fileName));
        }

        Name = fileName;

        // Set file path
        Debug.Assert(!string.IsNullOrWhiteSpace(filePath), "File path cannot be null or whitespace");
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;

        // Set file size
        if (fileSize <= 0)
        {
            throw new ArgumentException("File size must be greater than zero", nameof(fileSize));
        }
        Size = fileSize;

        // Set frame directory path
        string framesDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "frames");
        string originalFileName = Path.GetFileNameWithoutExtension(Name);
        string safeFileName = string.Join("_", originalFileName.Split(Path.GetInvalidFileNameChars()));
        string videoId = $"{safeFileName}_{UploadedAt:yyyyMMddHHmmss}";
        FrameDirectoryPath = Path.Combine(framesDirectoryPath, videoId);
    }

    /// <summary>
    /// Sets the processing completion time to the current time
    /// </summary>
    public void MarkAsProcessed() => ProcessingCompletedAt = DateTimeOffset.UtcNow;
}