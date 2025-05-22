using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI.Infrastructure.Models;
using Xabe.FFmpeg;

namespace UI.Infrastructure.UploadedVideoProcessing;

public class UploadedVideoProcessor
{
    private readonly ILogger<UploadedVideoProcessor> _logger;
    private readonly ObjectDetector _objectDetector;
    private readonly double _frameIntervalInSeconds;
    private readonly double _imageQuality;
    private readonly bool _shouldCleanupFrames;
    private readonly FFmpegFrameExtractor _frameExtractor;

    public UploadedVideoProcessor(
        ILogger<UploadedVideoProcessor> logger,
        ObjectDetector objectDetector,
        IConfiguration configuration,
        FFmpegFrameExtractor frameExtractor)
    {
        _logger = logger;
        _objectDetector = objectDetector;
        _frameExtractor = frameExtractor;

        _frameIntervalInSeconds = configuration.GetValue("UploadedVideoProcessor:FrameExtractionInterval", defaultValue: 1.0);
        _imageQuality = configuration.GetValue("UploadedVideoProcessor:ImageQuality", defaultValue: 1.0);
        _shouldCleanupFrames = configuration.GetValue("UploadedVideoProcessor:ShouldCleanupFrames", defaultValue: true);
    }

    /// <summary>
    /// Process video to detect objects
    /// </summary>
    /// <param name="uploadedVideoFile">Video file to process</param>
    /// <param name="progressCallback">Optional callback to report video processing progress</param>
    /// <returns>Video processing results with detection data</returns>
    public async Task<VideoProcessingResult> ProcessVideoAsync(
        UploadedVideoFile uploadedVideoFile,
        IProgress<VideoProcessingProgress> progressCallback)
    {
        _logger.LogInformation("Starting video processing for file: {FilePath}", uploadedVideoFile.FilePath);

        try
        {
            (List<FrameInfo> frameInfo, TimeSpan videoDuration) = await ExtractAllFramesAsync(uploadedVideoFile, progressCallback);

            List<FrameDetectionResult> frameResults = await DetectObjectsInFramesAsync(frameInfo, progressCallback);

            int totalObjectCount = frameResults.SelectMany(r => r.Detections).Count();
            VideoProcessingProgress progress = VideoProcessingProgress.CreateCompletionProgress(totalFrames: frameResults.Count,
                                                                                                totalObjectCount);
            progressCallback.Report(progress);

            if (_shouldCleanupFrames)
            {
                try
                {
                    Directory.Delete(uploadedVideoFile.FrameDirectoryPath, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up frames, but processing completed successfully");
                }
            }

            ObjectSummary[] allObjects = frameResults.SelectMany(r => r.Detections)
                                                     .GroupBy(r => r.Label)
                                                     .Select(g => new ObjectSummary
                                                                  {
                                                                      Label = g.Key, Count = g.Count(), AverageConfidence = g.Average(r => r.Confidence)
                                                                  })
                                                     .ToArray();

            _logger.LogInformation("Video processing complete. Processed {FrameCount} frames with {ObjectCount} total detections",
                                   frameResults.Count,
                                   allObjects.Sum(s => s.Count));

            return new VideoProcessingResult
                   {
                       DetectedObjects = allObjects,
                       FrameResults = frameResults.ToArray(),
                       ProcessedDuration = videoDuration,
                       TotalFrames = frameResults.Count,
                       VideoFilePath = uploadedVideoFile.FilePath,
                       VideoFileName = uploadedVideoFile.Name,
                       VideoFileSize = uploadedVideoFile.Size,
                       UploadedAt = uploadedVideoFile.UploadedAt,
                       CompletedAt = DateTimeOffset.UtcNow
                   };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");

            throw;
        }
    }

    /// <summary>
    /// Extracts all frames from the video at specified intervals
    /// </summary>
    /// <param name="uploadedVideoFile">The uploaded video file containing metadata and file paths</param>
    /// <param name="progressCallback">Optional callback to report video processing progress</param>
    /// <returns>Tuple containing list of frame information and video duration</returns>
    private async Task<(List<FrameInfo> frameInfo, TimeSpan videoDuration)> ExtractAllFramesAsync(
        UploadedVideoFile uploadedVideoFile,
        IProgress<VideoProcessingProgress> progressCallback)
    {
        _logger.LogInformation("Phase 1: Extracting frames from video '{FileName}'", uploadedVideoFile.Name);

        IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(uploadedVideoFile.FilePath);
        IVideoStream videoStream = mediaInfo.VideoStreams.First();
        TimeSpan videoDuration = videoStream.Duration;

        int totalFramesToExtract = (int)Math.Ceiling(videoDuration.TotalSeconds / _frameIntervalInSeconds);

        _logger.LogInformation("Extract {FrameCount} frames(approximately) at {Interval}s intervals",
                               totalFramesToExtract,
                               _frameIntervalInSeconds);

        List<FrameInfo> frameInfoList = [];
        int frameCounter = 0;

        string videoFramesDirectory = uploadedVideoFile.FrameDirectoryPath;
        for (double seconds = 0; seconds < videoDuration.TotalSeconds; seconds += _frameIntervalInSeconds)
        {
            TimeSpan timestamp = TimeSpan.FromSeconds(seconds);
            string outputPath = Path.Combine(videoFramesDirectory, $"frame_{frameCounter:D6}.jpg");

            await _frameExtractor.ExtractFrameAsync(uploadedVideoFile.FilePath, outputPath, timestamp);

            frameInfoList.Add(new FrameInfo
                              {
                                  FrameNumber = frameCounter, Timestamp = timestamp, FilePath = outputPath
                              });

            frameCounter++;

            double extractionProgress = (double)frameCounter / totalFramesToExtract;
            VideoProcessingProgress progress = VideoProcessingProgress.CreateExtractionProgress(extractionProgress,
                                                                                                currentFrame: frameCounter,
                                                                                                totalFramesToExtract);
            progressCallback.Report(progress);

            _logger.LogDebug("Extracted frame {FrameNumber} at {Timestamp}s", frameCounter, seconds);
        }

        _logger.LogInformation("Phase 1 complete: Extracted {ActualFrameCount} frames", frameInfoList.Count);

        return (frameInfoList, videoDuration);
    }

    /// <summary>
    /// Detects objects in all extracted frames
    /// </summary>
    /// <param name="frameInfoList">List of frame information</param>
    /// <param name="progressCallback">Optional callback to report detailed progress</param>
    /// <returns>List of frame detection results</returns>
    private async Task<List<FrameDetectionResult>> DetectObjectsInFramesAsync(
        List<FrameInfo> frameInfoList,
        IProgress<VideoProcessingProgress> progressCallback)
    {
        _logger.LogInformation("Phase 2: Detecting objects in {FrameCount} frames", frameInfoList.Count);

        List<FrameDetectionResult> frameResults = [];
        int processedFrames = 0;

        foreach (FrameInfo frameInfo in frameInfoList)
        {
            await using (FileStream fileStream = new(frameInfo.FilePath, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream memoryStream = new())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();

                    using (Image<Rgba32> image = Image.Load<Rgba32>(imageBytes))
                    {
                        DetectionResult[] detectionResults = _objectDetector.Detect(imageBytes,
                                                                                    image.Height,
                                                                                    image.Width,
                                                                                    _imageQuality);

                        FrameDetectionResult frameResult = new()
                                                           {
                                                               FrameNumber = frameInfo.FrameNumber, Timestamp = frameInfo.Timestamp, FramePath = frameInfo.FilePath, Detections = detectionResults
                                                           };

                        frameResults.Add(frameResult);

                        processedFrames++;

                        double detectionProgress = (double)processedFrames / frameInfoList.Count;
                        progressCallback?.Report(VideoProcessingProgress.CreateDetectionProgress(detectionProgress, processedFrames, frameInfoList.Count));

                        _logger.LogDebug("Processed frame {FrameNumber} at {Timestamp}s. Found {Count} objects",
                                         frameInfo.FrameNumber,
                                         frameInfo.Timestamp.TotalSeconds,
                                         detectionResults.Length);
                    }
                }
            }
        }

        _logger.LogInformation("Phase 2 complete: Detected objects in {ProcessedFrameCount} frames", processedFrames);

        return frameResults;
    }
}

/// <summary>
/// Represents the result of video processing and object detection
/// </summary>
public class VideoProcessingResult
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
    public TimeSpan ProcessedDuration { get; init; }

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
public class FrameDetectionResult
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
public class ObjectSummary
{
    /// <summary>
    /// Object class label
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// Number of occurrences across all frames
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Average confidence score across all detections
    /// </summary>
    public float AverageConfidence { get; init; }
}

/// <summary>
/// Represents information about an extracted video frame
/// </summary>
internal class FrameInfo
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
    /// Path to the extracted frame file
    /// </summary>
    public string FilePath { get; init; } = "";
}