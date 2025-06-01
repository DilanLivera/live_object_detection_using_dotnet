using UI.Infrastructure.ObjectDetection.Models;

namespace UI.Components.Pages.Upload;

/// <summary>
/// Represents a single video frame
/// </summary>
public sealed class VideoFrame
{
    /// <summary>
    /// Sequential frame number
    /// </summary>
    public int FrameNumber { get; init; }

    /// <summary>
    /// Timestamp of the frame in the video
    /// </summary>
    public TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Path to the extracted frame image
    /// </summary>
    public string FramePath { get; init; } = "";

    /// <summary>
    /// Object detection results for this frame
    /// </summary>
    public DetectionResult[] Detections { get; init; } = [];
}