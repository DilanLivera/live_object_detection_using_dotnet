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
    /// Detects objects in the provided image using the configured model.
    /// </summary>
    /// <param name="imageData">The image data to process</param>
    /// <returns>An array of detection results, each containing a label, confidence score, and bounding box.</returns>
    public DetectionResult[] Detect(byte[] imageData, int imageHeight, int imageWidth, double imageQuality)
    {
        try
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(imageData);

            (DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor) = _model.PreprocessImage(image);

            (Tensor<float> boxes, Tensor<float> scores) = _model.RunInference(inputTensor, shapeTensor);

            return _model.ProcessOutputs(boxes, scores, imageWidth, imageHeight);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during object detection");

            throw;
        }
    }
}