using System.Diagnostics;

namespace UI.Components.Pages.Upload;

/// <summary>
/// Represents an uploaded video file with its metadata and operations
/// </summary>
public sealed class UploadedVideoFile
{
    private static readonly string[] SupportedExtensions = [".mp4"]; // Can be expanded to include .webm, .mov, etc.

    /// <summary>
    /// Path to the saved video file on disk
    /// </summary>
    public string FilePath { get; private set; } = "";

    /// <summary>
    /// Path to the directory where frames will be extracted
    /// </summary>
    public string FramesDirectoryPath { get; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The filename assigned to the video file when saved to disk
    /// </summary>
    public string SavedFileName => Path.GetFileName(FilePath);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        OriginalFileName = fileName;

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

    // todo: add doc comments
    public void SetFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FilePath = filePath;
    }

    // todo: add doc summary
    public bool IsVideoFile()
    {
        Debug.Assert(OriginalFileName is not null, "OriginalFileName must not be null");

        return SupportedExtensions.Contains(Path.GetExtension(OriginalFileName)
                                                .ToLowerInvariant());
    }

    // todo: add doc summary
    public bool IsFileEmpty() => SizeInBytes <= 0;

    /// <summary>
    /// Sets the processing completion time to the current time
    /// </summary>
    public void MarkAsProcessed() => ProcessingCompletedAt = DateTimeOffset.UtcNow;
}