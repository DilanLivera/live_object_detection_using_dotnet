using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI.Infrastructure;
using UI.Infrastructure.ObjectDetection;
using UI.Infrastructure.ObjectDetection.Models;
using UI.Infrastructure.VideoProcessing;

namespace UI.Components.Pages.Upload;

/// <summary>
/// Orchestrates the complete video upload and processing workflow.
/// </summary>
/// <remarks>
/// Coordinates file validation, upload, video processing, and object detection.
/// </remarks>
public sealed class VideoUploadOrchestrator
{
    private readonly ILogger<VideoUploadOrchestrator> _logger;
    private readonly VideoFileValidationService _fileValidationService;
    private readonly FileService _fileService;
    private readonly ObjectDetector _objectDetector;
    private readonly FFmpegFrameExtractor _frameExtractor;
    private readonly double _imageQuality;
    private readonly bool _shouldCleanupFrames;

    public VideoUploadOrchestrator(
        ILogger<VideoUploadOrchestrator> logger,
        VideoFileValidationService fileValidationService,
        FileService fileService,
        ObjectDetector objectDetector,
        FFmpegFrameExtractor frameExtractor,
        IConfiguration configuration)
    {
        _logger = logger;
        _fileValidationService = fileValidationService;
        _fileService = fileService;
        _objectDetector = objectDetector;
        _frameExtractor = frameExtractor;

        _imageQuality = configuration.GetValue("UploadedVideoProcessor:ImageQuality", defaultValue: 1.0);
        _shouldCleanupFrames = configuration.GetValue("UploadedVideoProcessor:ShouldCleanupFrames", defaultValue: true);
    }

    /// <summary>
    /// Executes the complete upload and processing workflow.
    /// </summary>
    /// <param name="eventArgs">File selection event arguments</param>
    /// <param name="progressCallback">Progress reporting callback</param>
    /// <returns>Result containing the processed video file</returns>
    public async Task<Result<UploadedVideoFile>> ProcessUploadAsync(
        InputFileChangeEventArgs eventArgs,
        IProgress<VideoProcessingProgress>? progressCallback = null)
    {
        _logger.LogInformation("Starting video upload and processing workflow");

        Result<bool> validationResult = _fileValidationService.ValidateInput(eventArgs);
        if (!validationResult.IsSuccess)
        {
            _logger.LogWarning("File upload validation failed: {ErrorMessage}", validationResult.ErrorMessage);

            return Result<UploadedVideoFile>.Failure(validationResult.ErrorMessage!);
        }

        IBrowserFile browserFile = eventArgs.File;
        UploadedVideoFile videoFile = new(browserFile.Name, browserFile.Size);

        Result<UploadedVideoFile> uploadResult = await UploadFileAsync(browserFile, videoFile);
        if (!uploadResult.IsSuccess)
        {
            return uploadResult;
        }

        progressCallback?.Report(VideoProcessingProgress.CreateUploadProgress(videoFile.OriginalFileName));

        Result<UploadedVideoFile> processingResult = await ProcessVideoAsync(videoFile,
                                                                             progressCallback ?? new Progress<VideoProcessingProgress>());
        if (!processingResult.IsSuccess)
        {
            return processingResult;
        }

        _logger.LogInformation("Video upload and processing completed successfully for {FileName}",
                               videoFile.OriginalFileName);

        return processingResult;
    }

    /// <summary>
    /// Uploads the browser file to the server.
    /// </summary>
    private async Task<Result<UploadedVideoFile>> UploadFileAsync(IBrowserFile browserFile, UploadedVideoFile videoFile)
    {
        _logger.LogDebug("Starting file upload for {FileName}", videoFile.OriginalFileName);

        Result<string> result = await _fileService.SaveFileAsync(browserFile);

        if (!result.IsSuccess)
        {
            string errorMessage = $"Error saving file: {result.ErrorMessage}";
            _logger.LogWarning("File upload failed for {FileName}: {ErrorMessage}",
                               videoFile.OriginalFileName,
                               result.ErrorMessage);

            return Result<UploadedVideoFile>.Failure(errorMessage);
        }

        videoFile.SetFilePath(result.Value);

        _logger.LogInformation("File uploaded successfully: {OriginalFileName} -> {FilePath}",
                               videoFile.OriginalFileName,
                               videoFile.FilePath);

        return Result<UploadedVideoFile>.Success(videoFile);
    }

    /// <summary>
    /// Processes the uploaded video for object detection.
    /// </summary>
    private async Task<Result<UploadedVideoFile>> ProcessVideoAsync(
        UploadedVideoFile uploadedVideoFile,
        IProgress<VideoProcessingProgress> progressCallback)
    {
        _logger.LogDebug("Starting video processing for {FileName}", uploadedVideoFile.OriginalFileName);

        _logger.LogInformation("Starting video processing for file: {FilePath}", uploadedVideoFile.FilePath);

        Result<(List<Frame> Frames, TimeSpan VideoDuration)> frameExtractionResult = await _frameExtractor.ExtractFramesAsync(uploadedVideoFile,
                                                                                                                              progressCallback);
        if (frameExtractionResult.IsFailure)
        {
            return Result<UploadedVideoFile>.Failure(frameExtractionResult.ErrorMessage!);
        }

        List<Frame> frames = frameExtractionResult.Value.Frames;
        Debug.Assert(frames.Count > 0, "Frame list should not be empty");

        _logger.LogInformation("Detecting objects in {FrameCount} frames", frames.Count);

        List<VideoFrame> videoFrames = [];
        int processedFrames = 0;

        foreach (Frame frame in frames)
        {
            Result<byte[]> imageAsBytesResult = await _fileService.GetImageAsBytesAsync(frame.ImageFilePath);

            if (imageAsBytesResult.IsFailure)
            {
                _logger.LogError("Failed to read frame image: {ErrorMessage}", imageAsBytesResult.ErrorMessage);

                return Result<UploadedVideoFile>.Failure($"Failed to read frame image: {imageAsBytesResult.ErrorMessage}");
            }

            Debug.Assert(imageAsBytesResult.Value != null, "Image bytes should not be null when result is successful");

            byte[] imageBytes = imageAsBytesResult.Value;

            using Image<Rgba32> image = Image.Load<Rgba32>(imageBytes);
            DetectionResult[] detectionResults = _objectDetector.Detect(imageBytes, image.Height, image.Width, _imageQuality);

            VideoFrame videoFrame = new()
                                    {
                                        FrameNumber = frame.Number, Timestamp = frame.Timestamp, FramePath = frame.ImageFilePath, Detections = detectionResults
                                    };

            videoFrames.Add(videoFrame);

            processedFrames++;

            double detectionProgress = (double)processedFrames / frames.Count;
            progressCallback.Report(VideoProcessingProgress.CreateDetectionProgress(new Progress(detectionProgress),
                                                                                    processedFrames: processedFrames,
                                                                                    totalFrames: frames.Count));

            _logger.LogDebug("Processed frame {FrameNumber} at {Timestamp}s. Found {Count} objects",
                             frame.Number,
                             frame.Timestamp.TotalSeconds,
                             detectionResults.Length);
        }

        _logger.LogInformation("Detecting objects complete: Detected objects in {ProcessedFrameCount} frames",
                               processedFrames);

        if (_shouldCleanupFrames)
        {
            Result<bool> cleanupResult = _fileService.DeleteDirectory(uploadedVideoFile.FramesDirectoryPath);

            if (cleanupResult.IsFailure)
            {
                _logger.LogWarning("Failed to clean up frames, but processing completed successfully: {ErrorMessage}",
                                   cleanupResult.ErrorMessage);
            }
        }

        uploadedVideoFile.SetDuration(frameExtractionResult.Value.VideoDuration);
        uploadedVideoFile.SetProcessingResults(videoFrames.Count, videoFrames.ToArray());
        uploadedVideoFile.MarkAsProcessed();

        progressCallback.Report(VideoProcessingProgress.CreateCompletionProgress(uploadedVideoFile));

        _logger.LogInformation("Video processing completed successfully for {FileName}",
                               uploadedVideoFile.OriginalFileName);

        return Result<UploadedVideoFile>.Success(uploadedVideoFile);
    }
}