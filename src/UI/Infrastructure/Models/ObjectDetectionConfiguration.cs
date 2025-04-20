using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Options;
using UI.Infrastructure.Models.TinyYoloV3;
using UI.Infrastructure.Models.YoloV4;

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

    /// <summary>
    /// Configuration for the YOLOv4 model.
    /// </summary>
    public YoloV4ModelConfig? YoloV4 { get; init; }
}

public sealed class ValidateObjectDetectionOptions : IValidateOptions<ObjectDetectionConfiguration>
{
    public ValidateOptionsResult Validate(string? name, ObjectDetectionConfiguration configuration)
    {
        StringBuilder failuresBuilder = new();

        ValidateTinyYoloV3Config(configuration.TinyYoloV3, failuresBuilder);

        if (configuration.YoloV4 is not null)
        {
            ValidateYoloV4Config(configuration.YoloV4, failuresBuilder);
        }

        string failures = failuresBuilder.ToString();

        return string.IsNullOrWhiteSpace(failures)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateTinyYoloV3Config(TinyYoloV3ModelConfig config, StringBuilder failuresBuilder)
    {
        ValidateBaseConfig(config, failuresBuilder);

        if (!config.InputTensors.ContainsKey("image"))
        {
            failuresBuilder.AppendLine($"{nameof(config.InputTensors)}: Input tensor 'image' is required for YOLO model");
        }

        if (!config.InputTensors.ContainsKey("shape"))
        {
            failuresBuilder.AppendLine($"{nameof(config.InputTensors)}: Input tensor 'shape' is required for YOLO model");
        }

        if (!config.OutputTensors.ContainsKey("boxes"))
        {
            failuresBuilder.AppendLine($"{nameof(config.OutputTensors)}: Output tensor 'boxes' is required for YOLO model");
        }

        if (!config.OutputTensors.ContainsKey("scores"))
        {
            failuresBuilder.AppendLine($"{nameof(config.OutputTensors)}: Output tensor 'scores' is required for YOLO model");
        }
    }

    private static void ValidateYoloV4Config(YoloV4ModelConfig config, StringBuilder failuresBuilder)
    {
        ValidateBaseConfig(config, failuresBuilder);

        if (!config.InputTensors.ContainsKey("image"))
        {
            failuresBuilder.AppendLine($"{nameof(config.InputTensors)}: Input tensor 'image' is required for YOLO model");
        }

        // YOLOv4 doesn't require shape tensor
        if (config.InputTensors.ContainsKey("shape"))
        {
            failuresBuilder.AppendLine($"{nameof(config.InputTensors)}: YOLOv4 does not use shape tensor");
        }

        // Check for YOLOv4's three output layers
        int boxesOutputs = config.OutputTensors.Keys.Count(k => k.StartsWith("boxes_"));
        int scoresOutputs = config.OutputTensors.Keys.Count(k => k.StartsWith("scores_"));

        if (boxesOutputs != 3)
        {
            failuresBuilder.AppendLine($"{nameof(config.OutputTensors)}: YOLOv4 requires exactly three 'boxes_*' output tensors");
        }

        if (scoresOutputs != 3)
        {
            failuresBuilder.AppendLine($"{nameof(config.OutputTensors)}: YOLOv4 requires exactly three 'scores_*' output tensors");
        }

        // Verify that each boxes tensor has a corresponding scores tensor
        for (int i = 1; i <= 3; i++)
        {
            string boxKey = $"boxes_{i}";
            string scoreKey = $"scores_{i}";

            if (!config.OutputTensors.ContainsKey(boxKey))
            {
                failuresBuilder.AppendLine($"{nameof(config.OutputTensors)}: Missing output tensor '{boxKey}'");
            }

            if (!config.OutputTensors.ContainsKey(scoreKey))
            {
                failuresBuilder.AppendLine($"{nameof(config.OutputTensors)}: Missing output tensor '{scoreKey}'");
            }
        }
    }

    private static void ValidateBaseConfig(BaseModelConfig config, StringBuilder failuresBuilder)
    {
        if (string.IsNullOrWhiteSpace(config.ModelPath))
        {
            failuresBuilder.AppendLine($"{nameof(config.ModelPath)}: Model path is required");
        }

        if (string.IsNullOrWhiteSpace(config.LabelsPath))
        {
            failuresBuilder.AppendLine($"{nameof(config.LabelsPath)}: Labels path is required");
        }

        if (config.ImageSize is < 1 or > 4096)
        {
            failuresBuilder.AppendLine($"{nameof(config.ImageSize)}: Image size must be between 1 and 4096");
        }

        if (config.ConfidenceThreshold is < 0 or > 1)
        {
            failuresBuilder.AppendLine($"{nameof(config.ConfidenceThreshold)}: Confidence threshold must be between 0 and 1");
        }

        if (config.IntersectionOverUnionThreshold is < 0 or > 1)
        {
            failuresBuilder.AppendLine($"{nameof(config.IntersectionOverUnionThreshold)}: IntersectionOverUnionThreshold threshold must be between 0 and 1");
        }

        if (config.InputTensors.Count == 0)
        {
            failuresBuilder.AppendLine($"{nameof(config.InputTensors)}: At least one input tensor must be configured");
        }
    }
}