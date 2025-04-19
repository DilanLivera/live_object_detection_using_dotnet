using System.ComponentModel.DataAnnotations;

namespace UI.Infrastructure.Models;

/// <summary>
/// Base configuration for object detection models.
/// Contains common properties shared across different model types.
/// </summary>
public abstract class BaseModelConfig : IValidatableObject
{
    protected const string InputTensorsPropertyName = "InputTensors";
    protected const string OutputTensorsPropertyName = "OutputTensors";

    /// <summary>
    /// Path to the ONNX model file, relative to the content root.
    /// </summary>
    [Required(ErrorMessage = "Model path is required")]
    public required string ModelPath { get; init; }

    /// <summary>
    /// Path to the labels file, relative to the content root.
    /// </summary>
    [Required(ErrorMessage = "Labels path is required")]
    public required string LabelsPath { get; init; }

    /// <summary>
    /// Required input image size for the model (both width and height).
    /// </summary>
    [Range(1, 4096, ErrorMessage = "Image size must be between 1 and 4096")]
    public int ImageSize { get; init; }

    /// <summary>
    /// Confidence threshold for filtering detections.
    /// </summary>
    [Range(0, 1, ErrorMessage = "Confidence threshold must be between 0 and 1")]
    public float ConfidenceThreshold { get; init; }

    /// <summary>
    /// IoU threshold for Non-Maximum Suppression.
    /// </summary>
    [Range(0, 1, ErrorMessage = "IoU threshold must be between 0 and 1")]
    public float IntersectionOverUnionThreshold { get; init; }

    /// <summary>
    /// Dictionary of input tensor names and their descriptions.
    /// </summary>
    [Required(ErrorMessage = "Input tensors configuration is required")]
    public required Dictionary<string, string> InputTensors { get; init; }

    /// <summary>
    /// Dictionary of output tensor names and their descriptions.
    /// </summary>
    [Required(ErrorMessage = "Output tensors configuration is required")]
    public required Dictionary<string, string> OutputTensors { get; init; }

    /// <summary>
    /// Gets the full path to the model file.
    /// </summary>
    /// <param name="environment">The web host environment.</param>
    /// <returns>The full path to the model file.</returns>
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

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InputTensors.Count == 0)
        {
            yield return new ValidationResult(errorMessage: "At least one input tensor must be configured",
                                              memberNames: new[] { InputTensorsPropertyName });
        }

        if (OutputTensors.Count == 0)
        {
            yield return new ValidationResult(errorMessage: "At least one output tensor must be configured",
                                              memberNames: new[] { OutputTensorsPropertyName });
        }
    }
}