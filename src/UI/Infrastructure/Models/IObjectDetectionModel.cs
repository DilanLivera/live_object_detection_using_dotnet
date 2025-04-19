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