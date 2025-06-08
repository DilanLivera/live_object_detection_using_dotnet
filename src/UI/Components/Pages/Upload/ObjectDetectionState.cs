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
    /// Dictionary of status messages for this operation
    /// </summary>
    public StatusMessages StatusMessages { get; } = new();

    /// <summary>
    /// Resets the state to initial values
    /// </summary>
    public void Reset()
    {
        Status = ObjectDetectionStatus.None;
        StatusMessages.Clear();
    }

    /// <summary>
    /// Sets the state to uploading
    /// </summary>
    public void SetUploading()
    {
        Status = ObjectDetectionStatus.Uploading;
        StatusMessages.AddUploadStarted();
    }

    /// <summary>
    /// Sets the state to uploaded
    /// </summary>
    /// <param name="fileName">Original name of the uploaded video</param>
    public void SetUploaded(string fileName)
    {
        Status = ObjectDetectionStatus.Uploaded;
        StatusMessages.AddUploadComplete(fileName);
    }

    /// <summary>
    /// Updates the frame extraction progress
    /// </summary>
    /// <param name="currentFrame">Current frame being processed</param>
    /// <param name="totalFrames">Total frames to process</param>
    public void UpdateFrameExtractionProgress(int currentFrame, int totalFrames)
    {
        Status = ObjectDetectionStatus.ExtractingFrames;
        StatusMessages.UpdateFrameExtractionProgress(currentFrame, totalFrames);
    }

    /// <summary>
    /// Updates the object detection progress
    /// </summary>
    /// <param name="processedFrames">Number of frames processed</param>
    /// <param name="totalFrames">Total frames to process</param>
    public void UpdateObjectDetectionProgress(int processedFrames, int totalFrames)
    {
        Status = ObjectDetectionStatus.DetectingObjects;
        StatusMessages.UpdateObjectDetectionProgress(processedFrames, totalFrames);
    }

    /// <summary>
    /// Sets the state to complete with the processed video file
    /// </summary>
    public void SetComplete()
    {
        Status = ObjectDetectionStatus.Complete;
        StatusMessages.AddProcessingComplete();
    }

    /// <summary>
    /// Sets the state to failed
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    public void SetFailed(string errorMessage)
    {
        Status = ObjectDetectionStatus.Failed;
        StatusMessages.AddError(errorMessage);
    }
}