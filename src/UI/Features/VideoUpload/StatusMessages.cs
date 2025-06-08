namespace UI.Features.VideoUpload;

/// <summary>
/// Manages status messages for object detection operations
/// </summary>
public sealed class StatusMessages
{
    /// <summary>
    /// Dictionary of status messages for this operation
    /// </summary>
    private readonly Dictionary<ObjectDetectionStatus, StatusMessage> _value = [];

    /// <summary>
    /// Gets a list of status messages
    /// </summary>
    public IReadOnlyList<StatusMessage> Value => _value.Values.ToArray().AsReadOnly();

    /// <summary>
    /// Adds or updates a status message for the specified detection status
    /// </summary>
    /// <param name="status">The object detection status to associate with the message</param>
    /// <param name="message">The status message to add</param>
    private void AddMessage(ObjectDetectionStatus status, StatusMessage message) => _value[status] = message;

    /// <summary>
    /// Stops the spinner for the message associated with the specified status
    /// </summary>
    /// <param name="status">The object detection status whose message spinner should be stopped</param>
    private void StopMessageSpinner(ObjectDetectionStatus status) => _value[status].StopSpinner();

    /// <summary>
    /// Clears all status messages
    /// </summary>
    public void Clear() => _value.Clear();

    /// <summary>
    /// Adds a message indicating that upload has started
    /// </summary>
    public void AddUploadStarted() => AddMessage(ObjectDetectionStatus.Uploading,
                                                 new StatusMessage(text: "Uploading video...",
                                                                   cssClass: "bg-gray-900/50 border border-gray-500/50",
                                                                   Progress.None,
                                                                   textClass: "text-gray-300",
                                                                   shouldShowSpinner: true));

    /// <summary>
    /// Adds a message indicating that upload is complete
    /// </summary>
    /// <param name="fileName">The original name of the uploaded file</param>
    public void AddUploadComplete(string fileName)
    {
        StopMessageSpinner(ObjectDetectionStatus.Uploading);
        AddMessage(ObjectDetectionStatus.Uploaded,
                   new StatusMessage(text: $"Video uploaded successfully: {fileName}",
                                     cssClass: "bg-green-900/50 border border-green-500/50",
                                     Progress.None,
                                     textClass: "text-green-200"));
    }

    /// <summary>
    /// Updates the frame extraction progress message
    /// </summary>
    /// <param name="currentFrame">The current frame being extracted</param>
    /// <param name="totalFrames">The total number of frames to extract</param>
    public void UpdateFrameExtractionProgress(int currentFrame, int totalFrames)
    {
        double progress = (double)currentFrame / totalFrames;
        AddMessage(ObjectDetectionStatus.ExtractingFrames,
                   new StatusMessage(text: "Extracting frames from video...",
                                     cssClass: "bg-gray-900/50 border border-gray-500/50",
                                     new Progress(progress),
                                     textClass: "text-gray-300",
                                     shouldShowSpinner: true,
                                     shouldShowProgress: true,
                                     details: $"Frame {currentFrame} of {totalFrames}"));

        if (currentFrame == totalFrames)
        {
            StopMessageSpinner(ObjectDetectionStatus.ExtractingFrames);
        }
    }

    /// <summary>
    /// Updates the object detection progress message
    /// </summary>
    /// <param name="processedFrames">The number of frames that have been processed</param>
    /// <param name="totalFrames">The total number of frames to process</param>
    public void UpdateObjectDetectionProgress(int processedFrames, int totalFrames)
    {
        double progress = (double)processedFrames / totalFrames;
        AddMessage(ObjectDetectionStatus.DetectingObjects,
                   new StatusMessage(text: "Detecting objects in frames...",
                                     cssClass: "bg-gray-900/50 border border-gray-500/50",
                                     new Progress(progress),
                                     textClass: "text-gray-300",
                                     shouldShowSpinner: true,
                                     shouldShowProgress: true,
                                     details: $"Analyzed {processedFrames} of {totalFrames} frames"));

        if (processedFrames == totalFrames)
        {
            StopMessageSpinner(ObjectDetectionStatus.DetectingObjects);
        }
    }

    /// <summary>
    /// Adds a message indicating that processing is complete
    /// </summary>
    public void AddProcessingComplete() => AddMessage(ObjectDetectionStatus.Complete,
                                                      new StatusMessage(text: "Object detection complete",
                                                                        cssClass: "bg-green-900/50 border border-green-500/50",
                                                                        Progress.None,
                                                                        textClass: "text-green-200"));

    /// <summary>
    /// Adds an error message
    /// </summary>
    /// <param name="errorMessage">The error message describing what went wrong</param>
    public void AddError(string errorMessage) => AddMessage(ObjectDetectionStatus.Failed,
                                                            new StatusMessage(text: errorMessage,
                                                                              cssClass: "bg-red-900/50 border border-red-500/50",
                                                                              Progress.None,
                                                                              textClass: "text-red-200"));
}

/// <summary>
/// Represents a status message with styling and progress information
/// </summary>
public sealed class StatusMessage
{
    /// <summary>
    /// The message text to display
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// CSS class for the message container
    /// </summary>
    public string CssClass { get; }

    /// <summary>
    /// CSS class for the message text
    /// </summary>
    public string TextClass { get; }

    /// <summary>
    /// Whether to show a loading spinner with the message
    /// </summary>
    public bool ShouldShowSpinner { get; private set; }

    /// <summary>
    /// Whether to show a progress bar with the message
    /// </summary>
    public bool ShouldShowProgress { get; }

    /// <summary>
    /// The current progress value
    /// </summary>
    public Progress Progress { get; private set; }

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    public StatusMessage(
        string text,
        string cssClass,
        Progress progress,
        string textClass = "",
        bool shouldShowSpinner = false,
        bool shouldShowProgress = false,
        string details = "")
    {
        CssClass = cssClass;
        TextClass = textClass;
        ShouldShowSpinner = shouldShowSpinner;
        ShouldShowProgress = shouldShowProgress;
        Text = text;
        Details = details;
        Progress = progress;
    }

    /// <summary>
    /// Additional details about the operation
    /// </summary>
    public string Details { get; }

    /// <summary>
    /// Stops showing the spinner for this message
    /// </summary>
    public void StopSpinner() => ShouldShowSpinner = false;
}