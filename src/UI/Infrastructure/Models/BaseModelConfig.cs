namespace UI.Infrastructure.Models;

/// <summary>
/// Base configuration for object detection models.
/// </summary>
public class BaseModelConfig
{
    /// <summary>
    /// Path to the ONNX model file, relative to the content root.
    /// </summary>
    public required string ModelPath { get; init; }

    /// <summary>
    /// Path to the labels file, relative to the content root.
    /// </summary>
    public required string LabelsPath { get; init; }

    /// <summary>
    /// Input image size for the model (both width and height).
    /// </summary>
    public int ImageSize { get; init; }

    /// <summary>
    /// Confidence threshold for filtering detections.
    /// </summary>
    public float ConfidenceThreshold { get; init; }

    /// <summary>
    /// Intersection Over Union threshold for Non-Maximum Suppression.
    /// </summary>
    public float IntersectionOverUnionThreshold { get; init; }

    /// <summary>
    /// Input tensor names.
    /// </summary>
    public required Dictionary<string, string> InputTensors { get; init; }

    /// <summary>
    /// Output tensor names.
    /// </summary>
    public required Dictionary<string, string> OutputTensors { get; init; }

    /// <summary>
    /// Gets the full path to the model file.
    /// </summary>
    /// <param name="environment">The web host environment.</param>
    /// <returns>The full path to the model file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="environment"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the model file does not exist at the specified path.</exception>
    public string GetFullModelPath(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        string fullModelPath = Path.Combine(environment.ContentRootPath, ModelPath);

        if (!File.Exists(fullModelPath))
        {
            throw new FileNotFoundException($"Model file not found at {fullModelPath}");
        }

        return fullModelPath;
    }

    /// <summary>
    /// Gets the full path to the labels file.
    /// </summary>
    /// <param name="environment">The web host environment.</param>
    /// <returns>The full path to the labels file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="environment"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the labels file does not exist at the specified path.</exception>
    public string GetFullLabelsPath(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        string labelsPath = Path.Combine(environment.ContentRootPath, LabelsPath);

        if (!File.Exists(labelsPath))
        {
            throw new FileNotFoundException($"Labels file not found at {labelsPath}");
        }

        return labelsPath;
    }
}