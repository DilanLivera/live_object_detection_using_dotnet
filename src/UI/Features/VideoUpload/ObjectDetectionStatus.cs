namespace UI.Features.VideoUpload;

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

    /// <summary>Video frames are being extracted</summary>
    ExtractingFrames,

    /// <summary>Objects are being detected in extracted frames</summary>
    DetectingObjects,

    /// <summary>Object detection completed successfully</summary>
    Complete,

    /// <summary>An error occurred</summary>
    Failed
}