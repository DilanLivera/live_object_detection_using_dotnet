using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace UI.Infrastructure.Models.TinyYoloV3;

/// <summary>
/// Implementation of the Tiny YOLOv3 object detection model.
/// </summary>
public sealed class TinyYoloV3Model : IObjectDetectionModel
{
    private readonly ILogger<TinyYoloV3Model> _logger;
    private readonly InferenceSession _session;
    private readonly string[] _labels;
    private readonly TinyYoloV3ModelConfig _modelConfig;

    public TinyYoloV3Model(
        IWebHostEnvironment environment,
        ILogger<TinyYoloV3Model> logger,
        IOptions<ObjectDetectionConfiguration> options)
    {
        _logger = logger;
        _modelConfig = options.Value.TinyYoloV3;

        string fullModelPath = _modelConfig.GetFullModelPath(environment);

        _session = new InferenceSession(fullModelPath);

        if (!_session.InputMetadata.Any())
        {
            throw new InvalidOperationException("No inputs found in the model metadata!");
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var inputMetadata = _session.InputMetadata
                                        .Select(input => new { Name = input.Key, Shape = input.Value.Dimensions });
            logger.LogDebug("Model Input Names: {ModelInputNames}", JsonSerializer.Serialize(inputMetadata));

            var outputMetadata = _session.OutputMetadata
                                         .Select(output => new { Name = output.Key, Shape = output.Value.Dimensions });
            logger.LogDebug("Model Output Names: {ModelOutputNames}", JsonSerializer.Serialize(outputMetadata));
        }

        string labelsPath = _modelConfig.GetFullLabelsPath(environment);

        _labels = File.ReadAllLines(labelsPath);
    }

    /// <inheritdoc />
    public (DenseTensor<float> InputTensor, DenseTensor<float> ShapeTensor) PreprocessImage(Image<Rgba32> image)
    {
        try
        {
            using Image<Rgba32> resizedAndPaddedImage = ResizeAndPadImage(image, _modelConfig.ImageSize);

            DenseTensor<float> inputTensor = CreateTensor(resizedAndPaddedImage, _modelConfig.ImageSize);
            DenseTensor<float> shapeTensor = new(new[] { 1, 2 }) { [0, 0] = image.Height, [0, 1] = image.Width };

            return (inputTensor, shapeTensor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preprocessing image");
            throw;
        }
    }

    /// <inheritdoc />
    public (Tensor<float> Boxes, Tensor<float> Scores) RunInference(DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor)
    {
        try
        {
            List<NamedOnnxValue> inputs =
            [
                NamedOnnxValue.CreateFromTensor(_modelConfig.ImageInputTensorName, inputTensor),
                NamedOnnxValue.CreateFromTensor(_modelConfig.ImageShapeTensorName, shapeTensor)
            ];

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session.Run(inputs);

            Tensor<float> boxes = outputs.First(o => o.Name == _modelConfig.BoxesOutputTensorName).AsTensor<float>();
            Tensor<float> scores = outputs.First(o => o.Name == _modelConfig.ScoresOutputTensorName).AsTensor<float>();

            return (boxes, scores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running model inference");
            throw;
        }
    }

    /// <inheritdoc />
    public DetectionResult[] ProcessOutputs(Tensor<float> boxes, Tensor<float> scores, int imageWidth, int imageHeight)
    {
        List<DetectionResult> detections = [];

        try
        {
            _logger.LogDebug("Boxes shape: {BoxesShape}", string.Join(",", boxes.Dimensions.ToArray()));
            _logger.LogDebug("Scores shape: {ScoresShape}", string.Join(",", scores.Dimensions.ToArray()));

            // Process each detection from the model output
            for (int i = 0; i < boxes.Dimensions[1]; i++)
            {
                // Find the class with the highest confidence score
                float maxScore = float.MinValue;
                const int noValidClass = -1;
                int bestClass = noValidClass;

                for (int c = 0; c < scores.Dimensions[1]; c++)
                {
                    float score = scores[0, c, i];

                    if (!(score > maxScore))
                    {
                        continue;
                    }

                    maxScore = score;
                    bestClass = c;
                }

                if (maxScore < _modelConfig.ConfidenceThreshold || bestClass == noValidClass)
                {
                    continue;
                }

                _logger.LogDebug("Found detection {DetectionIndex}: Class={ClassIndex} ({ClassName}) Score={Score:F3}",
                                 i,
                                 bestClass,
                                 _labels[bestClass],
                                 maxScore);

                // Extract bounding box coordinates
                float y1 = boxes[0, i, 0];
                float x1 = boxes[0, i, 1];
                float y2 = boxes[0, i, 2];
                float x2 = boxes[0, i, 3];

                // Calculate width and height from coordinates
                float width = x2 - x1;
                float height = y2 - y1;

                // Target dimensions for the processed image
                const float processedWidth = 800f;
                const float processedHeight = 450f;

                // Calculate scale factors to convert from original to processed dimensions
                float scaleX = processedWidth / imageWidth;
                float scaleY = processedHeight / imageHeight;

                Box box = new() { X = x1 * scaleX, Y = y1 * scaleY, Width = width * scaleX, Height = height * scaleY };
                DetectionResult detection = new() { Label = _labels[bestClass], Confidence = maxScore, Box = box };

                _logger.LogDebug("Detection: {Label} ({Confidence:P1}) at [X={X:F1}, Y={Y:F1}, W={Width:F1}, H={Height:F1}]",
                                 detection.Label,
                                 detection.Confidence,
                                 detection.Box.X,
                                 detection.Box.Y,
                                 detection.Box.Width,
                                 detection.Box.Height);

                detections.Add(detection);
            }

            // Apply Non-Maximum Suppression to remove overlapping detections
            detections = ApplyNonMaximumSuppression(detections.ToArray()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing model outputs");
            throw;
        }

        _logger.LogDebug("Total detections found: {DetectionsCount}", detections.Count);

        return detections.ToArray();
    }

    private static Image<Rgba32> ResizeAndPadImage(Image<Rgba32> image, int targetSize)
    {
        // Calculate scaling factor to fit within target size while maintaining aspect ratio
        float scale = Math.Min(targetSize / (float)image.Width,
                               targetSize / (float)image.Height);

        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);

        int xPad = (targetSize - newWidth) / 2;
        int yPad = (targetSize - newHeight) / 2;

        Image<Rgba32> paddedImage = new(width: targetSize, height: targetSize, Color.Black);

        image.Mutate(operation: context => context.Resize(newWidth, newHeight));

        paddedImage.Mutate(operation: context => context.DrawImage(image,
                                                                   backgroundLocation: new Point(xPad, yPad),
                                                                   opacity: 1f));

        return paddedImage;
    }

    private static DenseTensor<float> CreateTensor(Image<Rgba32> image, int targetSize)
    {
        DenseTensor<float> tensor = new(dimensions: new[] { 1, 3, targetSize, targetSize });

        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                Rgba32 pixel = image[x, y];
                tensor[0, 0, y, x] = pixel.R / 255f; // Red channel
                tensor[0, 1, y, x] = pixel.G / 255f; // Green channel
                tensor[0, 2, y, x] = pixel.B / 255f; // Blue channel
            }
        }

        return tensor;
    }

    private DetectionResult[] ApplyNonMaximumSuppression(DetectionResult[] detections)
    {
        List<DetectionResult> results = [];
        IEnumerable<IGrouping<string, DetectionResult>> detectionsGroups = detections.GroupBy(r => r.Label);

        foreach (IGrouping<string, DetectionResult> group in detectionsGroups)
        {
            List<DetectionResult> sorted = group.OrderByDescending(r => r.Confidence).ToList();

            while (sorted.Count > 0)
            {
                DetectionResult current = sorted[0];
                results.Add(current);
                sorted.RemoveAt(index: 0);

                sorted.RemoveAll(r => CalculateIntersectionOverUnion(current.Box, r.Box) > _modelConfig.IntersectionOverUnionThreshold);
            }
        }

        return results.ToArray();
    }

    private static float CalculateIntersectionOverUnion(Box box1, Box box2)
    {
        float intersectionX = Math.Max(box1.X, box2.X);
        float intersectionY = Math.Max(box1.Y, box2.Y);
        float intersectionWidth = Math.Min(box1.X + box1.Width, box2.X + box2.Width) - intersectionX;
        float intersectionHeight = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height) - intersectionY;

        if (intersectionWidth <= 0 || intersectionHeight <= 0)
        {
            return 0;
        }

        float intersectionArea = intersectionWidth * intersectionHeight;
        float box1Area = box1.Width * box1.Height;
        float box2Area = box2.Width * box2.Height;

        return intersectionArea / (box1Area + box2Area - intersectionArea);
    }

    public void Dispose() => _session.Dispose();
}