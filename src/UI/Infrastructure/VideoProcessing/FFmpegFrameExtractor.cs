using System.Diagnostics;
using Xabe.FFmpeg;

namespace UI.Infrastructure.VideoProcessing;

/// <summary>
/// FFmpeg video frame extractor
/// </summary>
public sealed class FFmpegFrameExtractor
{
    private readonly ILogger<FFmpegFrameExtractor> _logger;

    public FFmpegFrameExtractor(ILogger<FFmpegFrameExtractor> logger, IConfiguration configuration)
    {
        _logger = logger;

        string ffmpegPath = configuration["FFmpeg:Path"] ?? string.Empty;

        if (!string.IsNullOrEmpty(ffmpegPath))
        {
            FFmpeg.SetExecutablesPath(ffmpegPath);
        }
    }

    /// <summary>
    /// Extracts a single frame from a video file at the specified timestamp
    /// </summary>
    /// <param name="videoPath">Path to the video file to extract frame from</param>
    /// <param name="outputPath">Path where the extracted frame should be saved</param>
    /// <param name="timestamp">Timestamp in the video to extract the frame from</param>
    /// <returns>A Result containing the output path on success or an error message on failure</returns>
    public async Task<Result<string>> ExtractFrameAsync(string videoPath, string outputPath, TimeSpan timestamp)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(videoPath), "Video path cannot be null or empty");
        Debug.Assert(!string.IsNullOrWhiteSpace(outputPath), "Output path cannot be null or empty");
        Debug.Assert(File.Exists(videoPath), $"Video file does not exist: {videoPath}");

        try
        {
            _logger.LogDebug("Extracting frame at {Timestamp} from video {VideoPath} to {OutputPath}",
                             timestamp,
                             videoPath,
                             outputPath);

            IConversion? conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(inputPath: videoPath,
                                                                                    outputPath,
                                                                                    captureTime: timestamp);

            await conversion.Start();

            return Result<string>.Success(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                             "Error extracting frame at {Timestamp} from video {VideoPath}",
                             timestamp,
                             videoPath);

            return Result<string>.Failure($"Failed to extract frame: {ex.Message}");
        }
    }
}