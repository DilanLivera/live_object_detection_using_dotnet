namespace UI.Features.VideoUpload;

/// <summary>
/// Represents an uploaded video file with its metadata and processing results
/// </summary>
public sealed class UploadedVideoFile
{
    /// <summary>
    /// Duration of the video.
    /// </summary>
    public TimeSpan? Duration { get; private set; }

    /// <summary>
    /// Path to the saved video file on disk.
    /// </summary>
    public string FilePath { get; private set; } = "";

    /// <summary>
    /// Path to the directory where frames will be extracted.
    /// </summary>
    public string FramesDirectoryPath { get; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public string OriginalFileName { get; }

    /// <summary>
    /// The filename assigned to the video file when saved to disk.
    /// </summary>
    public string SavedFileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public long SizeInBytes { get; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the video processing was completed.
    /// </summary>
    public DateTimeOffset? ProcessingCompletedAt { get; private set; }

    /// <summary>
    /// Total number of frames that were processed.
    /// </summary>
    public int TotalFrames { get; private set; }

    /// <summary>
    /// Frame rate of the video (frames per second).
    /// </summary>
    public double FrameRate => TotalFrames / Duration!.Value.TotalSeconds;

    /// <summary>
    /// Frames from video
    /// </summary>
    public VideoFrame[] VideoFrames { get; private set; } = [];

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileSizeInBytes">Size in bytes</param>
    /// <exception cref="ArgumentException">Thrown if file name is invalid or size is not positive</exception>
    public UploadedVideoFile(string fileName, long fileSizeInBytes)
    {
        // Set file name
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        OriginalFileName = fileName;

        // Set file size
        ArgumentOutOfRangeException.ThrowIfNegative(fileSizeInBytes);
        SizeInBytes = fileSizeInBytes;

        // Set frame directory path
        string framesDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "frames");
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(SavedFileName);
        FramesDirectoryPath = Path.Combine(framesDirectoryPath, fileNameWithoutExtension);
    }

    /// <summary>
    /// Sets the file path.
    /// </summary>
    /// <param name="filePath">Path where the file was saved</param>
    /// <exception cref="ArgumentException">Thrown if file path is null or whitespace</exception>
    public void SetFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>
    /// Sets the processing completion time to the current time.
    /// </summary>
    public void MarkAsProcessed() => ProcessingCompletedAt = DateTimeOffset.UtcNow;

    /// <summary>
    /// Set the duration of the video.
    /// </summary>
    /// <param name="duration">The duration of the video</param>
    public void SetDuration(TimeSpan duration) => Duration = duration;

    /// <summary>
    /// Sets the processing results.
    /// </summary>
    /// <param name="totalFrames">Total number of processed frames</param>
    /// <param name="videoFrames">Frames from video</param>
    public void SetProcessingResults(int totalFrames, VideoFrame[] videoFrames)
    {
        TotalFrames = totalFrames;
        VideoFrames = videoFrames;
    }
}