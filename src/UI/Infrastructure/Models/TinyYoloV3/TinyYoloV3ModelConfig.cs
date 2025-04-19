namespace UI.Infrastructure.Models.TinyYoloV3;

/// <summary>
/// Configuration specific to the Tiny YOLOv3 model.
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
}