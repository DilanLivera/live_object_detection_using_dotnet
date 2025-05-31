namespace UI.Components.Pages.Upload;

/// <summary>
/// Represents the status of object detection in an uploaded video
/// </summary>
public enum ObjectDetectionStatus
{
    /// <summary>Initial state</summary>
    None,

    /// <summary>File is being uploaded</summary>
    Uploading,

    /// <summary>File is uploaded successfully</summary>
    Uploaded,

    /// <summary>Video is being processed for object detection</summary>
    Processing,

    /// <summary>Video frames are being extracted</summary>
    ExtractingFrames,

    /// <summary>Objects are being detected in extracted frames</summary>
    DetectingObjects,

    /// <summary>Video has been processed successfully</summary>
    Processed,

    /// <summary>An error occurred</summary>
    Failed
}