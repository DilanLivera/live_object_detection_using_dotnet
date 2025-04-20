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

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Image Height: {ImageHeight}, Width: {ImageWidth}, Size: {ImageSize}",
                                 image.Height,
                                 image.Width,
                                 image.Size);
            }

            (DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor) = _model.PreprocessImage(image);

            ModelOutput modelOutput = _model.RunInference(inputTensor, shapeTensor);

            DetectionResult[] detectionResults = _model.ProcessOutputs(modelOutput, image.Width, image.Height);

            return detectionResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during object detection");
            throw;
        }
    }
}