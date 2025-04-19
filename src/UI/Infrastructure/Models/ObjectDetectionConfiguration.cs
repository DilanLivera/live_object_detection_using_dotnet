using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Options;
using UI.Infrastructure.Models.TinyYoloV3;

namespace UI.Infrastructure.Models;

/// <summary>
/// Configurations for configuring object detection models.
/// </summary>
public sealed class ObjectDetectionConfiguration
{
    public const string ConfigurationSectionName = "ObjectDetection";

    /// <summary>
    /// Configuration for the Tiny YOLOv3 model.
    /// </summary>
    [Required(ErrorMessage = "TinyYoloV3 configuration is required")]
    public required TinyYoloV3ModelConfig TinyYoloV3 { get; init; }
}

public sealed class ValidateObjectDetectionOptions : IValidateOptions<ObjectDetectionConfiguration>
{
    public ValidateOptionsResult Validate(string? name, ObjectDetectionConfiguration configuration)
    {
        StringBuilder failuresBuilder = new();
        TinyYoloV3ModelConfig tinyYoloV3ModelConfig = configuration.TinyYoloV3;

        if (string.IsNullOrWhiteSpace(tinyYoloV3ModelConfig.ModelPath))
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.ModelPath)}: Model path is required");
        }

        if (string.IsNullOrWhiteSpace(tinyYoloV3ModelConfig.LabelsPath))
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.LabelsPath)}: Labels path is required");
        }

        if (tinyYoloV3ModelConfig.ImageSize is < 1 or > 4096)
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.ImageSize)}: Image size must be between 1 and 4096");
        }

        if (tinyYoloV3ModelConfig.ConfidenceThreshold is < 0 or > 1)
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.ConfidenceThreshold)}: Confidence threshold must be between 0 and 1");
        }

        if (tinyYoloV3ModelConfig.IntersectionOverUnionThreshold is < 0 or > 1)
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.IntersectionOverUnionThreshold)}: IntersectionOverUnionThreshold threshold must be between 0 and 1");
        }

        if (tinyYoloV3ModelConfig.InputTensors.Count == 0)
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.InputTensors)}: At least one input tensor must be configured");
        }

        if (tinyYoloV3ModelConfig.OutputTensors.Count == 0)
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.OutputTensors)}: At least one output tensor must be configured");
        }

        if (!tinyYoloV3ModelConfig.InputTensors.ContainsKey("image"))
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.InputTensors)}: Input tensor 'image' is required for YOLO model");
        }

        if (!tinyYoloV3ModelConfig.InputTensors.ContainsKey("shape"))
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.InputTensors)}: Input tensor 'shape' is required for YOLO model");
        }

        if (!tinyYoloV3ModelConfig.OutputTensors.ContainsKey("boxes"))
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.OutputTensors)}: Output tensor 'boxes' is required for YOLO model");
        }

        if (!tinyYoloV3ModelConfig.OutputTensors.ContainsKey("scores"))
        {
            failuresBuilder.AppendLine($"{nameof(tinyYoloV3ModelConfig.OutputTensors)}: Output tensor 'scores' is required for YOLO model");
        }

        string failures = failuresBuilder.ToString();

        return string.IsNullOrWhiteSpace(failures)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}