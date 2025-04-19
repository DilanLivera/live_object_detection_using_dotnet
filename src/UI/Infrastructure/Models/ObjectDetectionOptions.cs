using System.ComponentModel.DataAnnotations;
using UI.Infrastructure.Models.TinyYoloV3;

namespace UI.Infrastructure.Models;

/// <summary>
/// Options for configuring object detection models.
/// </summary>
public sealed class ObjectDetectionOptions
{
    public const string SectionName = "ObjectDetection";

    /// <summary>
    /// Configuration for the Tiny YOLOv3 model.
    /// </summary>
    [Required(ErrorMessage = "TinyYoloV3 configuration is required")]
    public required TinyYoloV3ModelConfig TinyYoloV3 { get; init; }
}