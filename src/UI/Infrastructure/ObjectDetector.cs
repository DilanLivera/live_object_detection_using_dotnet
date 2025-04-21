using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI.Infrastructure.Models;
using System.IO.Compression;

namespace UI.Infrastructure;

/// <summary>
/// Provides object detection functionality using configurable models.
/// Handles image preprocessing, model inference, and post-processing of detection results.
/// </summary>
public sealed class ObjectDetector
{
    private readonly ILogger<ObjectDetector> _logger;
    private readonly IObjectDetectionModel _model;

    public ObjectDetector(
        ILogger<ObjectDetector> logger,
        IObjectDetectionModel model)
    {
        _logger = logger;
        _model = model;
    }

    /// <summary>
    /// Decompresses GZIP compressed data
    /// </summary>
    /// <param name="compressedData">The compressed data</param>
    /// <returns>Decompressed data</returns>
    private byte[] DecompressGzip(byte[] compressedData)
    {
        using MemoryStream? compressedStream = new(compressedData);
        using GZipStream? gzipStream = new(compressedStream, CompressionMode.Decompress);
        using MemoryStream? resultStream = new();

        gzipStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }

    /// <summary>
    /// Detects objects in the provided image using the configured model.
    /// </summary>
    /// <param name="imageData">The image data to process</param>
    /// <param name="isCompressed">Whether the image data is GZIP compressed</param>
    /// <returns>An array of detection results, each containing a label, confidence score, and bounding box.</returns>
    public DetectionResult[] Detect(byte[] imageData, bool isCompressed = false)
    {
        try
        {
            // Decompress if needed
            byte[] processedImageData = isCompressed ? DecompressGzip(imageData) : imageData;

            using Image<Rgba32> image = Image.Load<Rgba32>(processedImageData);

            (DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor) = _model.PreprocessImage(image);

            (Tensor<float> boxes, Tensor<float> scores) = _model.RunInference(inputTensor, shapeTensor);

            return _model.ProcessOutputs(boxes, scores, image.Width, image.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during object detection");
            throw;
        }
    }
}