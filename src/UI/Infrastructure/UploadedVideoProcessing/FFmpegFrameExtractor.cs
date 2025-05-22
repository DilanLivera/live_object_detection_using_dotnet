using Xabe.FFmpeg;

namespace UI.Infrastructure.UploadedVideoProcessing;

/// <summary>
/// FFmpeg video frame extractor
/// </summary>
public class FFmpegFrameExtractor
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
    /// <returns>A task representing the asynchronous extraction operation</returns>
    /// <exception cref="Exception">FFmpeg's operation exceptions</exception>
    public async Task ExtractFrameAsync(string videoPath, string outputPath, TimeSpan timestamp)
    {
        try
        {
            IConversion? conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(inputPath: videoPath,
                                                                                    outputPath,
                                                                                    captureTime: timestamp);

            await conversion.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                             "Error extracting frame at {Timestamp} from video {VideoPath}",
                             timestamp,
                             videoPath);

            throw;
        }
    }
}