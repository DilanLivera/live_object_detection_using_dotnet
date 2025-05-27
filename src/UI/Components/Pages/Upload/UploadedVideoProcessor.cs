using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI.Infrastructure;
using UI.Infrastructure.ObjectDetection;
using UI.Infrastructure.ObjectDetection.Models;
using UI.Infrastructure.VideoProcessing;

namespace UI.Components.Pages.Upload;

public class UploadedVideoProcessor
{
    private readonly ILogger<UploadedVideoProcessor> _logger;
    private readonly ObjectDetector _objectDetector;
    private readonly FileService _fileService;
    private readonly double _imageQuality;
    private readonly bool _shouldCleanupFrames;
    private readonly FFmpegFrameExtractor _frameExtractor;

    public UploadedVideoProcessor(
        ILogger<UploadedVideoProcessor> logger,
        ObjectDetector objectDetector,
        FileService fileService,
        IConfiguration configuration,
        FFmpegFrameExtractor frameExtractor)
    {
        _logger = logger;
        _objectDetector = objectDetector;
        _fileService = fileService;
        _frameExtractor = frameExtractor;

        _imageQuality = configuration.GetValue("UploadedVideoProcessor:ImageQuality",
                                               defaultValue: 1.0);
        _shouldCleanupFrames = configuration.GetValue("UploadedVideoProcessor:ShouldCleanupFrames",
                                                      defaultValue: true);
    }

    /// <summary>
    /// Process video to detect objects
    /// </summary>
    /// <param name="uploadedVideoFile">Video file to process</param>
    /// <param name="progressCallback">Optional callback to report video processing progress</param>
    /// <returns>A Result containing video processing results with detection data or an error message</returns>
    public async Task<Result<VideoProcessingResult>> ProcessVideoAsync(
        UploadedVideoFile uploadedVideoFile,
        IProgress<VideoProcessingProgress> progressCallback)
    {
        _logger.LogInformation("Starting video processing for file: {FilePath}", uploadedVideoFile.FilePath);

        Result<(List<Frame> Frames, TimeSpan VideoDuration)> frameExtractionResult = await _frameExtractor.ExtractFramesAsync(uploadedVideoFile,
                                                                                                                              progressCallback);
        if (frameExtractionResult.IsFailure)
        {
            return Result<VideoProcessingResult>.Failure(frameExtractionResult.ErrorMessage!);
        }

        List<Frame> frames = frameExtractionResult.Value.Frames;
        Debug.Assert(frames.Count > 0, "Frame list should not be empty");

        _logger.LogInformation("Detecting objects in {FrameCount} frames", frames.Count);

        List<FrameDetectionResult> frameDetectionResults = [];
        int processedFrames = 0;

        foreach (Frame frame in frames)
        {
            Result<byte[]> imageAsBytesResult = await _fileService.GetImageAsBytesAsync(frame.ImageFilePath);

            if (imageAsBytesResult.IsFailure)
            {
                _logger.LogError("Failed to read frame image: {ErrorMessage}", imageAsBytesResult.ErrorMessage);

                return Result<VideoProcessingResult>.Failure($"Failed to read frame image: {imageAsBytesResult.ErrorMessage}");
            }

            Debug.Assert(imageAsBytesResult.Value != null, "Image bytes should not be null when result is successful");

            byte[] imageBytes = imageAsBytesResult.Value;

            using Image<Rgba32> image = Image.Load<Rgba32>(imageBytes);
            DetectionResult[] detectionResults = _objectDetector.Detect(imageBytes,
                                                                        image.Height,
                                                                        image.Width,
                                                                        _imageQuality);

            FrameDetectionResult frameResult = new()
                                               {
                                                   FrameNumber = frame.Number, Timestamp = frame.Timestamp, FramePath = frame.ImageFilePath, Detections = detectionResults
                                               };

            frameDetectionResults.Add(frameResult);

            processedFrames++;

            double detectionProgress = (double)processedFrames / frames.Count;
            progressCallback.Report(VideoProcessingProgress.CreateDetectionProgress(detectionProgress,
                                                                                    processedFrames,
                                                                                    totalFrames: frames.Count));

            _logger.LogDebug("Processed frame {FrameNumber} at {Timestamp}s. Found {Count} objects",
                             frame.Number,
                             frame.Timestamp.TotalSeconds,
                             detectionResults.Length);
        }

        _logger.LogInformation("Detecting objects complete: Detected objects in {ProcessedFrameCount} frames",
                               processedFrames);

        DetectionResult[] detections = frameDetectionResults.SelectMany(r => r.Detections)
                                                            .ToArray();
        VideoProcessingProgress progress = VideoProcessingProgress.CreateCompletionProgress(totalFrames: frameDetectionResults.Count,
                                                                                            detections.Length);
        progressCallback.Report(progress);

        if (_shouldCleanupFrames)
        {
            Result<bool> cleanupResult = _fileService.DeleteDirectory(uploadedVideoFile.FramesDirectoryPath);

            if (cleanupResult.IsFailure)
            {
                _logger.LogWarning("Failed to clean up frames, but processing completed successfully: {ErrorMessage}",
                                   cleanupResult.ErrorMessage);
            }
        }

        ObjectSummary[] allObjects = detections.GroupBy(r => r.Label)
                                               .Select(g => new ObjectSummary
                                                            {
                                                                Label = g.Key, Count = g.Count(), AverageConfidence = g.Average(r => r.Confidence)
                                                            })
                                               .ToArray();

        _logger.LogInformation("Video processing complete. Processed {FrameCount} frames with {ObjectCount} total detections",
                               frameDetectionResults.Count,
                               allObjects.Length);

        VideoProcessingResult result = new()
                                       {
                                           DetectedObjects = allObjects,
                                           FrameResults = frameDetectionResults.ToArray(),
                                           VideoDuration = frameExtractionResult.Value.VideoDuration,
                                           TotalFrames = frameDetectionResults.Count,
                                           VideoFilePath = uploadedVideoFile.FilePath,
                                           VideoFileName = uploadedVideoFile.OriginalFileName,
                                           VideoFileSize = uploadedVideoFile.SizeInBytes,
                                           UploadedAt = uploadedVideoFile.UploadedAt,
                                           CompletedAt = DateTimeOffset.UtcNow
                                       };

        return Result<VideoProcessingResult>.Success(result);
    }

}

/// <summary>
/// Represents the result of video processing and object detection
/// </summary>
public sealed class VideoProcessingResult
{
    /// <summary>
    /// Path to the original video file
    /// </summary>
    public string VideoFilePath { get; init; } = string.Empty;

    /// <summary>
    /// Original file name of the video
    /// </summary>
    public string VideoFileName { get; init; } = string.Empty;

    /// <summary>
    /// Size of the video file in bytes
    /// </summary>
    public long VideoFileSize { get; init; }

    /// <summary>
    /// When the video file was uploaded
    /// </summary>
    public DateTimeOffset UploadedAt { get; init; }

    /// <summary>
    /// When the video was processed
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Total number of frames that were processed
    /// </summary>
    public int TotalFrames { get; init; }

    /// <summary>
    /// Duration of the processed video
    /// </summary>
    public TimeSpan VideoDuration { get; init; }

    /// <summary>
    /// Collection of frame-by-frame detection results
    /// </summary>
    public FrameDetectionResult[] FrameResults { get; init; } = [];

    /// <summary>
    /// Summary of detected objects across all frames
    /// </summary>
    public ObjectSummary[] DetectedObjects { get; init; } = [];
}

/// <summary>
/// Represents detection results for a single video frame
/// </summary>
public sealed class FrameDetectionResult
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

/// <summary>
/// Summary information about a specific detected object class
/// </summary>
public sealed class ObjectSummary
{
    /// <summary>
    /// Object class label
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// Number of occurrences across all frames
    /// </summary>
    public int Count { get; init; }

    // todo: remove this. we don't need it.
    /// <summary>
    /// Average confidence score across all detections
    /// </summary>
    public float AverageConfidence { get; init; }
}