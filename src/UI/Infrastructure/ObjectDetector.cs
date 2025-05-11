using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI.Infrastructure.Models;

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
    /// <param name="imageInBytes">The image data to process</param>
    /// <param name="imageHeight">The height of the image in pixels</param>
    /// <param name="imageWidth">The width of the image in pixels</param>
    /// <param name="imageQuality">The quality of the image, ranging from 0.0 to 1.0</param>
    /// <returns>An array of detection results, each containing a label, confidence score, and bounding box.</returns>
    public DetectionResult[] Detect(byte[] imageInBytes, int imageHeight, int imageWidth, double imageQuality)
    {
        try
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(imageInBytes);

            _logger.LogDebug("Image dimensions. Original: {Width}x{Height}, Expected: {ExpectedWidth}x{ExpectedHeight}",
                             image.Width,
                             image.Height,
                             imageWidth,
                             imageHeight);

            (DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor) = _model.PreprocessImage(image);

            (Tensor<float> boxes, Tensor<float> scores) = _model.RunInference(inputTensor, shapeTensor);

            DetectionResult[] results = _model.ProcessOutputs(boxes, scores, imageWidth, imageHeight);

            _logger.LogDebug("Detection complete. Found: {Count} objects", results.Length);

            foreach (DetectionResult detection in results)
            {
                _logger.LogDebug("Detection. Label: {Label}, Confidence: {Confidence:F2}, Box: [X={X:F1},Y={Y:F1},Width={Width:F1},Height={Height:F1}]",
                                 detection.Label,
                                 detection.Confidence,
                                 detection.BoundingBox.X,
                                 detection.BoundingBox.Y,
                                 detection.BoundingBox.Width,
                                 detection.BoundingBox.Height);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during object detection");

            throw;
        }
    }
}