using System.ComponentModel.DataAnnotations;

namespace UI.Infrastructure.Models.TinyYoloV3;

/// <summary>
/// Configuration specific to the Tiny YOLOv3 model.
/// Contains YOLO-specific tensor names and settings.
/// </summary>
public sealed class TinyYoloV3ModelConfig : BaseModelConfig
{
    /// <summary>
    /// Gets the name of the input tensor for preprocessed image data.
    /// </summary>
    public string ImageInputTensorName => InputTensors["image"];

    /// <summary>
    /// Gets the name of the input tensor for original image dimensions.
    /// </summary>
    public string ImageShapeTensorName => InputTensors["shape"];

    /// <summary>
    /// Gets the name of the output tensor containing bounding box coordinates.
    /// </summary>
    public string BoxesOutputTensorName => OutputTensors["boxes"];

    /// <summary>
    /// Gets the name of the output tensor containing class probabilities.
    /// </summary>
    public string ScoresOutputTensorName => OutputTensors["scores"];

    /// <summary>
    /// Validates the YOLO-specific configuration.
    /// </summary>
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (ValidationResult result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (!InputTensors.ContainsKey("image"))
        {
            yield return new ValidationResult(errorMessage: "Input tensor 'image' is required for YOLO model",
                                              memberNames: new[] { InputTensorsPropertyName });
        }

        if (!InputTensors.ContainsKey("shape"))
        {
            yield return new ValidationResult(errorMessage: "Input tensor 'shape' is required for YOLO model",
                                              memberNames: new[] { InputTensorsPropertyName });
        }

        if (!OutputTensors.ContainsKey("boxes"))
        {
            yield return new ValidationResult(errorMessage: "Output tensor 'boxes' is required for YOLO model",
                                              memberNames: new[] { OutputTensorsPropertyName });
        }

        if (!OutputTensors.ContainsKey("scores"))
        {
            yield return new ValidationResult(errorMessage: "Output tensor 'scores' is required for YOLO model",
                                              memberNames: new[] { OutputTensorsPropertyName });
        }
    }
}