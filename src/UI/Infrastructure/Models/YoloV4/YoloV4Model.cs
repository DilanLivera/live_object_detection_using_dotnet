using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace UI.Infrastructure.Models.YoloV4;

/// <summary>
/// Implementation of the YOLOv4 object detection model.
/// </summary>
public sealed class YoloV4Model : IObjectDetectionModel
{
    private readonly ILogger<YoloV4Model> _logger;
    private readonly InferenceSession _session;
    private readonly string[] _labels;
    private readonly YoloV4ModelConfig _modelConfig;

    // YOLOv4 specific constants
    private readonly float[] _anchors = new float[] { 12, 16, 19, 36, 40, 28, 36, 75, 76, 55, 72, 146, 142, 110, 192, 243, 459, 401 };

    private readonly float[] _strides = new float[] { 8, 16, 32 };
    private readonly float[] _xyscale = new float[] { 1.2f, 1.1f, 1.05f };

    public YoloV4Model(
        IWebHostEnvironment environment,
        ILogger<YoloV4Model> logger,
        IOptions<ObjectDetectionConfiguration> options)
    {
        _logger = logger;
        _modelConfig = options.Value.YoloV4!;

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
            // YOLOv4 doesn't need a shape tensor, but we need to return one to satisfy the interface
            // We'll use a dummy shape tensor that won't be used in inference
            DenseTensor<float> shapeTensor = new(dimensions: new[] { 1, 2 }) { [0, 0] = image.Height, [0, 1] = image.Width };

            using Image<Rgba32> resizedAndPaddedImage = ResizeAndPadImageWithAspectRatio(image, _modelConfig.ImageSize);
            DenseTensor<float> inputTensor = CreateTensor(resizedAndPaddedImage, _modelConfig.ImageSize);

            return (inputTensor, shapeTensor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preprocessing image");
            throw;
        }
    }

    /// <inheritdoc />
    public ModelOutput RunInference(DenseTensor<float> inputTensor, DenseTensor<float> shapeTensor)
    {
        try
        {
            // For YOLOv4, we only need to pass the image input tensor
            List<NamedOnnxValue> inputs =
            [
                NamedOnnxValue.CreateFromTensor(_modelConfig.ImageInputTensorName, inputTensor)
            ];

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session.Run(inputs);

            _logger.LogDebug("Model output: {ModelOutput}",
                             // Must not include the output value because it contains too much data.
                             JsonSerializer.Serialize(outputs.Select(o => new { o.Name, o.ElementType, o.ValueType })));

            List<Tensor<float>> boxes = _modelConfig.BoxesOutputTensorNames
                                                    .Select(name => outputs.First(o => o.Name == name).AsTensor<float>())
                                                    .ToList();

            List<Tensor<float>> scores = _modelConfig.ScoresOutputTensorNames
                                                     .Select(name => outputs.First(o => o.Name == name).AsTensor<float>())
                                                     .ToList();

            return new ModelOutput { Boxes = boxes, Scores = scores };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running model inference");
            throw;
        }
    }

    /// <inheritdoc />
    public DetectionResult[] ProcessOutputs(ModelOutput modelOutput, int imageWidth, int imageHeight)
    {
        try
        {
            // Process each detection layer (YOLOv4 has 3 detection layers)
            List<(float[] Box, float Confidence, int ClassId)> allDetections = new();

            for (int layerIndex = 0; layerIndex < modelOutput.Boxes.Count; layerIndex++)
            {
                Tensor<float> boxes = modelOutput.Boxes[layerIndex];
                Tensor<float> scores = modelOutput.Scores[layerIndex];

                _logger.LogDebug("Processing layer {LayerIndex} - Boxes shape: {BoxesShape}, Scores shape: {ScoresShape}",
                                 layerIndex,
                                 string.Join(",", boxes.Dimensions.ToArray()),
                                 string.Join(",", scores.Dimensions.ToArray()));

                // Get anchors for this layer (3 anchors per layer)
                float[] layerAnchors = GetAnchorsForLayer(layerIndex);
                float stride = _strides[layerIndex];
                float xyscale = _xyscale[layerIndex];

                // Grid size for this layer
                int gridSize = boxes.Dimensions[1]; // Height dimension

                // Process each box in this layer
                for (int i = 0; i < boxes.Dimensions[1]; i++) // Loop through height
                {
                    for (int j = 0; j < boxes.Dimensions[2]; j++) // Loop through width
                    {
                        for (int a = 0; a < 3; a++) // Loop through anchors
                        {
                            ProcessDetection(boxes,
                                             scores,
                                             allDetections,
                                             gridY: i,
                                             gridX: j,
                                             anchorIdx: a,
                                             layerAnchors,
                                             stride,
                                             xyscale,
                                             gridSize,
                                             imageWidth,
                                             imageHeight);
                        }
                    }
                }
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Detection Results: {DetectionResults}", JsonSerializer.Serialize(allDetections));
            }
            // Apply non-maximum suppression
            DetectionResult[] applyNonMaximumSuppression = ApplyNonMaximumSuppression(allDetections);
            return applyNonMaximumSuppression;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing model outputs: {Error}", ex.Message);
            return [];
        }
    }

    private void ProcessDetection(
        Tensor<float> boxes,
        Tensor<float> scores,
        List<(float[] Box, float Confidence, int ClassId)> allDetections,
        int gridY,
        int gridX,
        int anchorIdx,
        float[] layerAnchors,
        float stride,
        float xyscale,
        int gridSize,
        int imageWidth,
        int imageHeight)
    {
        try
        {
            // YOLOv4 network outputs have different dimensions for boxes and scores
            // For some models: boxes[0, gridY, gridX, anchorIdx, 0:4]
            // For some models: scores[0, classIdx, gridY, gridX, anchorIdx]

            // Get class scores for this grid cell and anchor
            float[] classScores;

            if (scores.Dimensions.Length == 5 && scores.Dimensions[1] > scores.Dimensions[4])
            {
                // Scores are in format [batch, num_classes, height, width, anchors]
                classScores = new float[scores.Dimensions[1]];
                for (int c = 0; c < classScores.Length; c++)
                {
                    classScores[c] = scores[0, c, gridY, gridX, anchorIdx];
                }
            }
            else
            {
                // Scores are in format [batch, height, width, anchors, num_classes]
                classScores = new float[scores.Dimensions[4]];
                for (int c = 0; c < classScores.Length; c++)
                {
                    classScores[c] = scores[0, gridY, gridX, anchorIdx, c];
                }
            }

            // Find the class with highest score
            int bestClassId = -1;
            float bestScore = 0;

            for (int c = 0; c < classScores.Length; c++)
            {
                if (classScores[c] > bestScore)
                {
                    bestScore = classScores[c];
                    bestClassId = c;
                }
            }

            // Skip if below confidence threshold
            if (bestScore < _modelConfig.ConfidenceThreshold)
            {
                return;
            }

            _logger.LogDebug("Found class {ClassId} ({ClassName}) with confidence {Confidence:F3}",
                             bestClassId,
                             _labels[bestClassId],
                             bestScore);

            // Get box coordinates
            float tx,
                ty,
                tw,
                th;

            if (boxes.Dimensions.Length == 5 && boxes.Dimensions[4] >= 4)
            {
                // Boxes tensor is in format [batch, height, width, anchors, 4]
                tx = boxes[0, gridY, gridX, anchorIdx, 0]; // x offset
                ty = boxes[0, gridY, gridX, anchorIdx, 1]; // y offset
                tw = boxes[0, gridY, gridX, anchorIdx, 2]; // width
                th = boxes[0, gridY, gridX, anchorIdx, 3]; // height
            }
            else
            {
                // Handle other possible format
                _logger.LogWarning("Unexpected box tensor format. Dimensions: {Dimensions}",
                                   string.Join(", ", boxes.Dimensions.ToArray().Select(d => d.ToString())));
                return;
            }

            // Apply sigmoid to tx and ty, then scale and add grid position
            float x = ((float)Sigmoid(tx) * xyscale - 0.5f * (xyscale - 1) + gridX) * stride;
            float y = ((float)Sigmoid(ty) * xyscale - 0.5f * (xyscale - 1) + gridY) * stride;

            // Apply exponential to tw and th, then multiply by anchor dimensions
            float w = (float)Math.Exp(tw) * layerAnchors[anchorIdx * 2];
            float h = (float)Math.Exp(th) * layerAnchors[anchorIdx * 2 + 1];

            // Convert to corner coordinates (xmin, ymin, xmax, ymax)
            float xmin = x - w / 2;
            float ymin = y - h / 2;
            float xmax = x + w / 2;
            float ymax = y + h / 2;

            // Scale back to original image dimensions
            float scale = Math.Min(_modelConfig.ImageSize / (float)imageWidth, _modelConfig.ImageSize / (float)imageHeight);
            float dw = (_modelConfig.ImageSize - scale * imageWidth) / 2;
            float dh = (_modelConfig.ImageSize - scale * imageHeight) / 2;

            xmin = (xmin - dw) / scale;
            ymin = (ymin - dh) / scale;
            xmax = (xmax - dw) / scale;
            ymax = (ymax - dh) / scale;

            // Clip to image boundaries
            xmin = Math.Max(0, xmin);
            ymin = Math.Max(0, ymin);
            xmax = Math.Min(imageWidth, xmax);
            ymax = Math.Min(imageHeight, ymax);

            // Skip invalid boxes
            if (xmin >= xmax || ymin >= ymax)
            {
                return;
            }

            // Add to detection list
            allDetections.Add((
                                  new float[] { xmin, ymin, xmax, ymax },
                                  bestScore,
                                  bestClassId
                              ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing detection: {Error}", ex.Message);
        }
    }

    private float[] GetAnchorsForLayer(int layerIndex)
    {
        // YOLOv4 has 3 anchors per layer, 2 values (width, height) per anchor
        int startIdx = layerIndex * 6; // 3 anchors * 2 values
        return new float[] { _anchors[startIdx], _anchors[startIdx + 1], _anchors[startIdx + 2], _anchors[startIdx + 3], _anchors[startIdx + 4], _anchors[startIdx + 5] };
    }

    private DetectionResult[] ApplyNonMaximumSuppression(List<(float[] Box, float Confidence, int ClassId)> detections)
    {
        // Group by class
        IEnumerable<IGrouping<int, (float[] Box, float Confidence, int ClassId)>> detectionsByClass = detections.GroupBy(d => d.ClassId);
        List<DetectionResult> results = [];

        foreach (IGrouping<int, (float[] Box, float Confidence, int ClassId)> classGroup in detectionsByClass)
        {
            List<(float[] Box, float Confidence, int ClassId)> sortedDetections = classGroup.OrderByDescending(d => d.Confidence).ToList();

            while (sortedDetections.Count > 0)
            {
                (float[] Box, float Confidence, int ClassId) current = sortedDetections[0];

                // Add to results with normalized coordinates (0-1 range for UI display)
                results.Add(new DetectionResult
                            {
                                Label = _labels[current.ClassId],
                                Confidence = current.Confidence,
                                Box = new Box
                                      {
                                          // Normalize to 0-1 range for UI display
                                          X = current.Box[0] / 1000f, Y = current.Box[1] / 1000f, Width = (current.Box[2] - current.Box[0]) / 1000f, Height = (current.Box[3] - current.Box[1]) / 1000f
                                      }
                            });

                _logger.LogDebug("Added detection: {Label} ({Confidence:P1}) at [{X:F3}, {Y:F3}, {Width:F3}, {Height:F3}]",
                                 _labels[current.ClassId],
                                 current.Confidence,
                                 current.Box[0] / 1000f,
                                 current.Box[1] / 1000f,
                                 (current.Box[2] - current.Box[0]) / 1000f,
                                 (current.Box[3] - current.Box[1]) / 1000f);

                // Remove current detection
                sortedDetections.RemoveAt(0);

                // Remove overlapping detections
                sortedDetections.RemoveAll(d => CalculateIoU(current.Box, d.Box) > _modelConfig.IntersectionOverUnionThreshold);
            }
        }

        return results.ToArray();
    }

    private static float CalculateIoU(float[] box1, float[] box2)
    {
        // Intersection coordinates
        float intersectLeft = Math.Max(box1[0], box2[0]);
        float intersectTop = Math.Max(box1[1], box2[1]);
        float intersectRight = Math.Min(box1[2], box2[2]);
        float intersectBottom = Math.Min(box1[3], box2[3]);

        // Intersection area
        float intersectWidth = Math.Max(0, intersectRight - intersectLeft);
        float intersectHeight = Math.Max(0, intersectBottom - intersectTop);
        float intersectArea = intersectWidth * intersectHeight;

        // Union area
        float box1Area = (box1[2] - box1[0]) * (box1[3] - box1[1]);
        float box2Area = (box2[2] - box2[0]) * (box2[3] - box2[1]);
        float unionArea = box1Area + box2Area - intersectArea;

        return intersectArea / unionArea;
    }

    private static Image<Rgba32> ResizeAndPadImageWithAspectRatio(Image<Rgba32> image, int targetSize)
    {
        // Calculate scaling factor to fit within target size while maintaining aspect ratio
        float scale = Math.Min(targetSize / (float)image.Width, targetSize / (float)image.Height);

        // Calculate new dimensions
        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);

        // Calculate padding
        int xPad = (targetSize - newWidth) / 2;
        int yPad = (targetSize - newHeight) / 2;

        // Create a new black image of the target size
        Image<Rgba32> paddedImage = new(targetSize, targetSize, Color.Black);

        // Resize the original image
        image.Mutate(context => context.Resize(newWidth, newHeight));

        // Copy the resized image into the center of the padded image
        paddedImage.Mutate(context => context.DrawImage(image, new Point(xPad, yPad), 1f));

        return paddedImage;
    }

    private static DenseTensor<float> CreateTensor(Image<Rgba32> image, int targetSize)
    {
        // YOLOv4 expects input in NHWC format (batch, height, width, channels)
        DenseTensor<float> tensor = new(dimensions: new[] { 1, targetSize, targetSize, 3 });

        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                Rgba32 pixel = image[x, y];
                tensor[0, y, x, 0] = pixel.R / 255f; // Red channel
                tensor[0, y, x, 1] = pixel.G / 255f; // Green channel
                tensor[0, y, x, 2] = pixel.B / 255f; // Blue channel
            }
        }

        return tensor;
    }

    private static float Sigmoid(float x) => 1.0f / (1.0f + (float)Math.Exp(-x));

    public void Dispose() => _session.Dispose();
}