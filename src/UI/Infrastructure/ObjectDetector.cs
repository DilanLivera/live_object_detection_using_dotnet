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
    /// <param name="imageData">The image to process.</param>
    /// <returns>An array of detection results, each containing a label, confidence score, and bounding box.</returns>
    public DetectionResult[] Detect(byte[] imageData)
    {
        try
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(imageData);

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

/// <summary>
/// Represents a single object detection result.
/// IMPORTANT: Property names must match the JavaScript object properties exactly as they are used in camera.js.
/// JavaScript destructuring: const { label, confidence, box } = detection;
/// </summary>
public class DetectionResult
{
    public string Label { get; init; } = "";
    public float Confidence { get; init; }
    public Box Box { get; init; } = new();
}

/// <summary>
/// Represents the bounding box coordinates for a detected object.
/// IMPORTANT: Property names must match the JavaScript object properties exactly as they are used in camera.js.
/// JavaScript destructuring: const { x, y, width, height } = box;
/// All values are normalized (0-1) representing percentage of image dimensions.
/// </summary>
public class Box
{
    public float X { get; init; } // Normalized x-coordinate (0-1)
    public float Y { get; init; } // Normalized y-coordinate (0-1)
    public float Width { get; init; } // Normalized width (0-1)
    public float Height { get; init; } // Normalized height (0-1)
}