namespace UI.Features.VideoUpload;

/// <summary>
/// Represents detailed progress information for video processing operations
/// </summary>
public sealed class VideoProcessingProgress
{
    /// <summary>
    /// Current status of the video processing
    /// </summary>
    public ObjectDetectionStatus Status { get; private init; }

    /// <summary>
    /// Progress value between 0 and 1
    /// </summary>
    public Progress Progress { get; private init; }

    /// <summary>
    /// Current frame being processed (for frame extraction or detection)
    /// </summary>
    public int CurrentFrame { get; private init; }

    /// <summary>
    /// Total frames to process
    /// </summary>
    public int TotalFrames { get; private init; }

    /// <summary>
    /// Original name of the uploaded file
    /// </summary>
    public string? FileName { get; private init; }

    /// <summary>
    /// Whether to stop the spinner for the current status
    /// </summary>
    public bool ShouldStopSpinner { get; private init; }

    /// <summary>
    /// The processed video file when complete
    /// </summary>
    public UploadedVideoFile? ProcessedVideoFile { get; private init; }

    /// <summary>
    /// Creates progress for upload completion
    /// </summary>
    /// <param name="fileName">The original name of the uploaded file</param>
    /// <returns>A new VideoProcessingProgress instance for upload completion</returns>
    public static VideoProcessingProgress CreateUploadProgress(string fileName) => new()
                                                                                   {
                                                                                       Status = ObjectDetectionStatus.Uploaded, FileName = fileName, Progress = Progress.None
                                                                                   };

    /// <summary>
    /// Creates progress for frame extraction phase
    /// </summary>
    /// <param name="progress">The current progress of frame extraction</param>
    /// <param name="currentFrame">The current frame being extracted</param>
    /// <param name="totalFrames">The total number of frames to extract</param>
    /// <returns>A new VideoProcessingProgress instance for frame extraction</returns>
    public static VideoProcessingProgress CreateExtractionProgress(
        Progress progress,
        int currentFrame,
        int totalFrames) => new()
                            {
                                Status = ObjectDetectionStatus.ExtractingFrames, Progress = progress, CurrentFrame = currentFrame, TotalFrames = totalFrames
                            };

    /// <summary>
    /// Creates progress for object detection phase
    /// </summary>
    /// <param name="progress">The current progress of object detection (between 0 and 1)</param>
    /// <param name="processedFrames">The number of frames that have been processed</param>
    /// <param name="totalFrames">The total number of frames to process</param>
    /// <returns>A new VideoProcessingProgress instance for object detection</returns>
    public static VideoProcessingProgress CreateDetectionProgress(
        Progress progress,
        int processedFrames,
        int totalFrames) => new()
                            {
                                Status = ObjectDetectionStatus.DetectingObjects, Progress = progress, CurrentFrame = processedFrames, TotalFrames = totalFrames
                            };

    /// <summary>
    /// Creates progress for completion
    /// </summary>
    /// <param name="videoFile">The processed video file containing metadata and results</param>
    /// <returns>A new VideoProcessingProgress instance indicating completion</returns>
    public static VideoProcessingProgress CreateCompletionProgress(
        UploadedVideoFile videoFile) => new()
                                        {
                                            Status = ObjectDetectionStatus.Complete,
                                            Progress = Progress.Complete,
                                            CurrentFrame = videoFile.TotalFrames,
                                            TotalFrames = videoFile.TotalFrames,
                                            ShouldStopSpinner = true,
                                            ProcessedVideoFile = videoFile
                                        };
}