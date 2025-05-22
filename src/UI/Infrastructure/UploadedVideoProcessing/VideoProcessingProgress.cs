namespace UI.Infrastructure.UploadedVideoProcessing;

/// <summary>
/// Represents detailed progress information for video processing operations
/// </summary>
public sealed class VideoProcessingProgress
{
    /// <summary>
    /// Current phase of video processing
    /// </summary>
    public VideoProcessingPhase CurrentPhase { get; private init; } = VideoProcessingPhase.NotStarted;

    /// <summary>
    /// Progress of frame extraction phase (0-1)
    /// </summary>
    public double FrameExtractionProgress { get; private init; }

    /// <summary>
    /// Progress of object detection phase (0-1)
    /// </summary>
    public double ObjectDetectionProgress { get; private init; }

    /// <summary>
    /// Overall progress across all phases (0-1)
    /// </summary>
    public double OverallProgress { get; private init; }

    /// <summary>
    /// Status message describing current operation
    /// </summary>
    public string StatusMessage { get; private init; } = "";

    /// <summary>
    /// Additional details about the current operation
    /// </summary>
    public string Details { get; private init; } = "";

    /// <summary>
    /// Creates progress for frame extraction phase
    /// </summary>
    /// <param name="progress">Extraction progress (0-1)</param>
    /// <param name="currentFrame">Current frame being extracted</param>
    /// <param name="totalFrames">Total frames to extract</param>
    /// <returns>VideoProcessingProgress for extraction</returns>
    public static VideoProcessingProgress CreateExtractionProgress(
        double progress,
        int currentFrame,
        int totalFrames) => new()
                            {
                                CurrentPhase = VideoProcessingPhase.ExtractingFrames,
                                FrameExtractionProgress = progress,
                                ObjectDetectionProgress = 0,
                                OverallProgress = progress * 0.5, // First 50% of overall progress
                                StatusMessage = "Extracting frames from video...",
                                Details = $"Frame {currentFrame} of {totalFrames}"
                            };

    /// <summary>
    /// Creates progress for object detection phase
    /// </summary>
    /// <param name="progress">Detection progress (0-1)</param>
    /// <param name="processedFrames">Frames processed for detection</param>
    /// <param name="totalFrames">Total frames to process</param>
    /// <returns>VideoProcessingProgress for detection</returns>
    public static VideoProcessingProgress CreateDetectionProgress(
        double progress,
        int processedFrames,
        int totalFrames) => new()
                            {
                                CurrentPhase = VideoProcessingPhase.DetectingObjects,
                                FrameExtractionProgress = 1.0, // Extraction complete
                                ObjectDetectionProgress = progress,
                                OverallProgress = 0.5 + progress * 0.5, // Last 50% of overall progress
                                StatusMessage = "Detecting objects in frames...",
                                Details = $"Analyzed {processedFrames} of {totalFrames} frames"
                            };

    /// <summary>
    /// Creates progress for completion
    /// </summary>
    /// <param name="totalFrames">Total frames processed</param>
    /// <param name="objectCount">Total objects detected</param>
    /// <returns>VideoProcessingProgress for completion</returns>
    public static VideoProcessingProgress CreateCompletionProgress(
        int totalFrames,
        int objectCount) => new()
                            {
                                CurrentPhase = VideoProcessingPhase.Complete,
                                FrameExtractionProgress = 1.0,
                                ObjectDetectionProgress = 1.0,
                                OverallProgress = 1.0,
                                StatusMessage = "Video processing complete",
                                Details = $"Processed {totalFrames} frames, detected {objectCount} objects"
                            };
}

/// <summary>
/// Represents the different phases of video processing
/// </summary>
public enum VideoProcessingPhase
{
    /// <summary>Processing has not started</summary>
    NotStarted,

    /// <summary>Extracting frames from video</summary>
    ExtractingFrames,

    /// <summary>Detecting objects in extracted frames</summary>
    DetectingObjects,

    /// <summary>Processing complete</summary>
    Complete
}