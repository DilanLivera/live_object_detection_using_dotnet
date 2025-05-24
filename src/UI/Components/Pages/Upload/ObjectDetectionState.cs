using System.Diagnostics;

namespace UI.Components.Pages.Upload;

/// <summary>
/// Represents the state of object detection in a video that was uploaded
/// </summary>
public sealed class ObjectDetectionState
{
    /// <summary>
    /// Current status of the video upload and object detection
    /// </summary>
    public ObjectDetectionStatus Status { get; private set; } = ObjectDetectionStatus.None;

    /// <summary>
    /// Current processing progress (0-1)
    /// </summary>
    public double ProcessingProgress { get; private set; }

    /// <summary>
    /// Error message if any occurred during upload or processing
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Dictionary of status messages for this operation
    /// </summary>
    public StatusMessages StatusMessages { get; } = new();

    /// <summary>
    /// Summary of detected objects across all frames
    /// </summary>
    public ObjectSummary[] DetectedObjects { get; private set; } = [];

    /// <summary>
    /// Individual detection results for each frame
    /// </summary>
    public FrameDetectionResult[] FrameResults { get; private set; } = [];

    /// <summary>
    /// Total number of frames that were processed
    /// </summary>
    public int TotalFrames { get; private set; }

    /// <summary>
    /// Duration of the processed video
    /// </summary>
    public TimeSpan? VideoDuration { get; private set; }

    /// <summary>
    /// Frame rate of the video (frames per second)
    /// </summary>
    public double VideoFrameRate { get; private set; }

    /// <summary>
    /// Resets the state to initial values
    /// </summary>
    public void Reset()
    {
        Status = ObjectDetectionStatus.None;
        ProcessingProgress = 0;
        ErrorMessage = null;
        StatusMessages.Clear();
        DetectedObjects = [];
        FrameResults = [];
        TotalFrames = 0;
        VideoDuration = null;
        VideoFrameRate = 0;
    }

    /// <summary>
    /// Sets the state to uploading
    /// </summary>
    public void SetUploading()
    {
        Status = ObjectDetectionStatus.Uploading;

        StatusMessages.AddMessage(ObjectDetectionStatus.Uploading,
                                  new StatusMessage(text: "Uploading video...",
                                                    cssClass: "bg-gray-900/50 border border-gray-500/50",
                                                    textClass: "text-gray-300",
                                                    shouldShowSpinner: true));
    }

    /// <summary>
    /// Sets the state to uploaded
    /// </summary>
    /// <param name="videoName">Name of the uploaded video</param>
    public void SetUploaded(string videoName)
    {
        Status = ObjectDetectionStatus.Uploaded;

        StatusMessages.StopMessageSpinner(ObjectDetectionStatus.Uploading);
        StatusMessages.AddMessage(ObjectDetectionStatus.Uploaded,
                                  new StatusMessage(text: $"Video uploaded successfully: {videoName}",
                                                    cssClass: "bg-green-900/50 border border-green-500/50",
                                                    textClass: "text-green-200"));
    }

    /// <summary>
    /// Updates the processing progress
    /// </summary>
    /// <param name="progress">Video processing progress</param>
    public void UpdateProcessingProgress(VideoProcessingProgress progress)
    {
        ProcessingProgress = progress.OverallProgress;

        switch (progress.CurrentPhase)
        {
            case VideoProcessingPhase.ExtractingFrames:
                StatusMessages.AddMessage(ObjectDetectionStatus.ExtractingFrames,
                                          new StatusMessage(text: progress.StatusMessage,
                                                            cssClass: "bg-gray-900/50 border border-gray-500/50",
                                                            textClass: "text-gray-300",
                                                            shouldShowSpinner: true,
                                                            shouldShowProgress: true,
                                                            progress: progress.FrameExtractionProgress,
                                                            details: progress.Details));

                break;

            case VideoProcessingPhase.DetectingObjects:
                StatusMessages.StopMessageSpinner(ObjectDetectionStatus.ExtractingFrames);

                StatusMessages.AddMessage(ObjectDetectionStatus.DetectingObjects,
                                          new StatusMessage(text: progress.StatusMessage,
                                                            cssClass: "bg-gray-900/50 border border-gray-500/50",
                                                            textClass: "text-gray-300",
                                                            shouldShowSpinner: true,
                                                            shouldShowProgress: true,
                                                            progress: progress.ObjectDetectionProgress,
                                                            details: progress.Details));

                break;

            case VideoProcessingPhase.Complete:
                StatusMessages.StopMessageSpinner(ObjectDetectionStatus.DetectingObjects);

                break;
        }
    }

    /// <summary>
    /// Sets the state to processed with results
    /// </summary>
    /// <param name="result">Processing results</param>
    public void SetProcessed(VideoProcessingResult result)
    {
        Debug.Assert(result.CompletedAt > result.UploadedAt, "Processing completion time should be after upload time");

        Status = ObjectDetectionStatus.Processed;
        DetectedObjects = result.DetectedObjects;
        FrameResults = result.FrameResults;
        TotalFrames = result.TotalFrames;
        VideoDuration = result.ProcessedDuration;

        if (result.ProcessedDuration.TotalSeconds > 0 && result.TotalFrames > 0)
        {
            VideoFrameRate = result.TotalFrames / result.ProcessedDuration.TotalSeconds;
        }

        int objectCount = result.DetectedObjects.Sum(s => s.Count);
        StatusMessages.AddMessage(ObjectDetectionStatus.Processed,
                                  new StatusMessage(text: $"Object detection complete: Found {objectCount} objects across {result.TotalFrames} frames.",
                                                    cssClass: "bg-green-900/50 border border-green-500/50",
                                                    textClass: "text-green-200"));
    }

    /// <summary>
    /// Sets the state to failed with an error message
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    public void SetFailed(string errorMessage)
    {
        Status = ObjectDetectionStatus.Failed;
        ErrorMessage = errorMessage;
        StatusMessages.AddMessage(ObjectDetectionStatus.Failed,
                                  new StatusMessage(text: errorMessage,
                                                    cssClass: "bg-red-900/50 border border-red-500/50",
                                                    textClass: "text-red-200"));
    }
}