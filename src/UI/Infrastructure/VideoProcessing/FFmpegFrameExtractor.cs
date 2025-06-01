using System.Diagnostics;
using UI.Components.Pages.Upload;
using Xabe.FFmpeg;

namespace UI.Infrastructure.VideoProcessing;

/// <summary>
/// FFmpeg video frame extractor
/// </summary>
public sealed class FFmpegFrameExtractor
{
    private readonly ILogger<FFmpegFrameExtractor> _logger;
    private readonly double _frameIntervalInSeconds;

    public FFmpegFrameExtractor(ILogger<FFmpegFrameExtractor> logger, IConfiguration configuration)
    {
        _logger = logger;

        string ffmpegPath = configuration["FFmpeg:Path"] ?? string.Empty;

        if (!string.IsNullOrEmpty(ffmpegPath))
        {
            FFmpeg.SetExecutablesPath(ffmpegPath);
        }

        _frameIntervalInSeconds = configuration.GetValue("UploadedVideoProcessor:FrameExtractionInterval",
                                                         defaultValue: 1.0);
    }

    /// <summary>
    /// Extracts all frames from the video at specified intervals
    /// </summary>
    /// <param name="uploadedVideoFile">The uploaded video file containing metadata and file paths</param>
    /// <param name="progressCallback">Optional callback to report video processing progress</param>
    /// <returns>A Result containing frame information and video duration or an error message</returns>
    public async Task<Result<(List<Frame> Frames, TimeSpan VideoDuration)>> ExtractFramesAsync(
        UploadedVideoFile uploadedVideoFile,
        IProgress<VideoProcessingProgress> progressCallback)
    {
        _logger.LogInformation("Extracting frames from video '{FileName}'", uploadedVideoFile.OriginalFileName);

        Debug.Assert(File.Exists(uploadedVideoFile.FilePath), "File must exist");

        IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(uploadedVideoFile.FilePath);
        IVideoStream videoStream = mediaInfo.VideoStreams.First();
        TimeSpan videoDuration = videoStream.Duration;

        int totalFramesToExtract = (int)Math.Ceiling(videoDuration.TotalSeconds / _frameIntervalInSeconds);

        _logger.LogInformation("Extract {FrameCount} frames(approximately) at {Interval}s intervals",
                               totalFramesToExtract,
                               _frameIntervalInSeconds);

        List<Frame> frames = [];
        int frameCounter = 0;

        string framesDirectory = uploadedVideoFile.FramesDirectoryPath;
        for (double seconds = 0; seconds < videoDuration.TotalSeconds; seconds += _frameIntervalInSeconds)
        {
            TimeSpan timestamp = TimeSpan.FromSeconds(seconds);
            string outputPath = Path.Combine(framesDirectory, $"frame_{frameCounter:D6}.jpg");

            IConversion? conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(inputPath: uploadedVideoFile.FilePath,
                                                                                    outputPath,
                                                                                    captureTime: timestamp);

            try
            {
                await conversion.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 "Error extracting frame at {Timestamp} from video {SavedFileName}",
                                 timestamp,
                                 uploadedVideoFile.SavedFileName);

                return Result<(List<Frame> Frames, TimeSpan VideoDuration)>.Failure($"Failed to extract frame: {ex.Message}");
            }

            frames.Add(new Frame
                       {
                           Number = frameCounter, Timestamp = timestamp, ImageFilePath = outputPath
                       });

            frameCounter++;

            double extractionProgress = (double)frameCounter / totalFramesToExtract;
            progressCallback.Report(VideoProcessingProgress.CreateExtractionProgress(progress: new Progress(extractionProgress),
                                                                                     currentFrame: frameCounter,
                                                                                     totalFrames: totalFramesToExtract));

            _logger.LogDebug("Extracted frame {FrameNumber} at {Timestamp}s", frameCounter, seconds);
        }

        _logger.LogInformation("Extracting frames complete: Extracted {ActualFrameCount} frames", frames.Count);

        return Result<(List<Frame>, TimeSpan)>.Success((frames, videoDuration));
    }
}

/// <summary>
/// Represents information about an extracted video frame
/// </summary>
public sealed class Frame
{
    /// <summary>
    /// Frame number
    /// </summary>
    public int Number { get; init; }

    /// <summary>
    /// Timestamp of the frame in the video
    /// </summary>
    public TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Path to the extracted frame file
    /// </summary>
    public string ImageFilePath { get; init; } = "";
}