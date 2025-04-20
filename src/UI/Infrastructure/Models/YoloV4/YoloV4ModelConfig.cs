namespace UI.Infrastructure.Models.YoloV4;

/// <summary>
/// Configuration specific to the YOLOv4 model.
/// </summary>
public sealed class YoloV4ModelConfig : BaseModelConfig
{
    /// <summary>
    /// Gets the name of the input tensor for preprocessed image data.
    /// </summary>
    public string ImageInputTensorName => InputTensors["image"];

    /// <summary>
    /// Gets the names of the output tensors containing bounding box coordinates from each detection layer.
    /// YOLOv4 has three output layers for different scales.
    /// </summary>
    public IReadOnlyList<string> BoxesOutputTensorNames => OutputTensors.Where(kvp => kvp.Key.StartsWith("boxes_"))
                                                                        .OrderBy(kvp => kvp.Key)
                                                                        .Select(kvp => kvp.Value)
                                                                        .ToList()
                                                                        .AsReadOnly();

    /// <summary>
    /// Gets the names of the output tensors containing class probabilities from each detection layer.
    /// YOLOv4 has three output layers for different scales.
    /// </summary>
    public IReadOnlyList<string> ScoresOutputTensorNames => OutputTensors.Where(kvp => kvp.Key.StartsWith("scores_"))
                                                                         .OrderBy(kvp => kvp.Key)
                                                                         .Select(kvp => kvp.Value)
                                                                         .ToList()
                                                                         .AsReadOnly();
}