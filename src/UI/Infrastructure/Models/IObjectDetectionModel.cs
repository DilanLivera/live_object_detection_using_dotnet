using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace UI.Infrastructure.Models;

/// <summary>
/// Defines the contract for object detection models.
/// </summary>
public interface IObjectDetectionModel : IDisposable
{
    /// <summary>
    /// Preprocess required to run inference.
    /// </summary>
    /// <param name="image">Image used for inference.</param>
    /// <returns>A tuple containing the model input tensor and image shape tensor.</returns>
    (DenseTensor<float> InputTensor, DenseTensor<float> ShapeTensor) PreprocessImage(Image<Rgba32> image);

    /// <summary>
    /// Run inference.
    /// </summary>
    /// <param name="inputTensor">Input tensor.</param>
    /// <param name="shapeTensor">Shape tensor.</param>
    /// <returns>Raw model outputs containing boxes and scores.</returns>
    (Tensor<float> Boxes, Tensor<float> Scores) RunInference(DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor);

    /// <summary>
    /// Process inference outputs to extract detection results.
    /// </summary>
    /// <param name="boxes">The bounding box coordinates from the model.</param>
    /// <param name="scores">The class scores from the model.</param>
    /// <param name="imageWidth">Original image width.</param>
    /// <param name="imageHeight">Original image height.</param>
    /// <returns>An array of detection results.</returns>
    DetectionResult[] ProcessOutputs(Tensor<float> boxes, Tensor<float> scores, int imageWidth, int imageHeight);
}

/// <summary>
/// Represents a single object detection result.
/// IMPORTANT: Property names must match the JavaScript object properties exactly as they are used in camera.js.
/// JavaScript destructuring: const { label, confidence, box } = detection;
/// </summary>
public struct DetectionResult
{
    /// <summary>
    /// Class label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Confidence score.
    /// </summary>
    public float Confidence { get; }

    /// <summary>
    /// Bounding box.
    /// </summary>
    public BoundingBox BoundingBox { get; }

    /// <summary>
    /// Creates a new DetectionResult from model outputs
    /// </summary>
    /// <param name="label">Class label</param>
    /// <param name="confidence">Confidence score</param>
    /// <param name="boundingBox">Bounding box</param>
    /// <returns>A new DetectionResult instance</returns>
    public DetectionResult(string label, float confidence, BoundingBox boundingBox)
    {
        Label = label;
        Confidence = confidence;
        BoundingBox = boundingBox;
    }
}

/// <summary>
/// Represents the bounding box coordinates for a detected object.
/// IMPORTANT: Property names must match the JavaScript object properties exactly as they are used in camera.js.
/// JavaScript destructuring: const { x, y, width, height } = box;
/// All values are normalized (0-1) representing percentage of image dimensions.
/// </summary>
public struct BoundingBox
{
    public float X { get; } // Normalized x-coordinate (0-1)
    public float Y { get; } // Normalized y-coordinate (0-1)
    public float Width { get; } // Normalized width (0-1)
    public float Height { get; } // Normalized height (0-1)

    /// <summary>
    /// Creates a scaled Box from coordinates
    /// </summary>
    /// <param name="x1">Left coordinate</param>
    /// <param name="y1">Top coordinate</param>
    /// <param name="x2">Right coordinate</param>
    /// <param name="y2">Bottom coordinate</param>
    /// <param name="scaleX">X scaling factor</param>
    /// <param name="scaleY">Y scaling factor</param>
    /// <returns>A new Box instance with scaled coordinates</returns>
    public BoundingBox(float x1, float y1, float x2, float y2, float scaleX, float scaleY)
    {
        // Ensure coordinates are non-negative
        x1 = Math.Max(0, x1);
        y1 = Math.Max(0, y1);

        // Ensure width and height are positive and at least 1 pixel
        float width = Math.Max(1, x2 - x1);
        float height = Math.Max(1, y2 - y1);

        // Apply scaling factors
        X = x1 * scaleX;
        Y = y1 * scaleY;
        Width = width * scaleX;
        Height = height * scaleY;
    }
}