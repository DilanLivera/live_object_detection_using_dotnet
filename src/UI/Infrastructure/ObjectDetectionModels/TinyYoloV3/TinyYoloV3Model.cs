using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace UI.Infrastructure.ObjectDetectionModels.TinyYoloV3;

/// <summary>
/// Implementation of the Tiny YOLOv3 object detection model.
/// </summary>
public sealed class TinyYoloV3Model : IObjectDetectionModel
{
    private const int BatchSize = 0;
    private const int CandidateBoxesDimension = 1;
    private const int ClassesDimension = 1;
    private readonly ILogger<TinyYoloV3Model> _logger;
    private readonly InferenceSession _session;
    private readonly string[] _labels;
    private readonly TinyYoloV3ModelConfig _modelConfig;

    public TinyYoloV3Model(
        IWebHostEnvironment environment,
        ILogger<TinyYoloV3Model> logger,
        IOptions<ObjectDetectionModelConfiguration> options)
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
                                        .Select(input => new
                                                         {
                                                             Name = input.Key, Shape = input.Value.Dimensions
                                                         });
            logger.LogDebug("Model Input Names: {ModelInputNames}", JsonSerializer.Serialize(inputMetadata));

            var outputMetadata = _session.OutputMetadata
                                         .Select(output => new
                                                           {
                                                               Name = output.Key, Shape = output.Value.Dimensions
                                                           });
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
            // Refer to the https://github.com/onnx/models/tree/main/validated/vision/object_detection_segmentation/tiny-yolov3 page
            // to find details about input to model and preprocessing steps.
            using Image<Rgba32> resizedAndPaddedImage = ResizeAndPadImage(image, _modelConfig.ImageSize);

            // Resized Image
            DenseTensor<float> inputTensor = CreateInputTensor(resizedAndPaddedImage, _modelConfig.ImageSize);
            // Original Image Size
            DenseTensor<float> shapeTensor = new(dimensions: new[]
                                                             {
                                                                 1, 2
                                                             })
                                             {
                                                 [0, 0] = image.Height, [0, 1] = image.Width
                                             };

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

            /*
             Output from the model looks like the following
             [
                 {
                   "ElementType": 1, // float
                   "ValueType": 1, // tensor
                   "Name": "yolonms_layer_1",
                   "Value": [ -65.74441, -24.535715, 0.6828527, ... ]
                 },
                 {
                   "ElementType": 1, // // float
                   "ValueType": 1, // tensor
                   "Name": "yolonms_layer_1:1",
                   "Value": [ 3.7889149e-7, 3.2970993e-6, 3.2319445e-8, ... ]
                 },
                 {
                   "ElementType": 6, // Int32
                   "ValueType": 1, // tensor
                   "Name": "yolonms_layer_1:2",
                   "Value": []
                 }
               ]
             */

            Tensor<float> boxes = outputs.First(o => o.Name == "yolonms_layer_1")
                                         .AsTensor<float>();
            Tensor<float> scores = outputs.First(o => o.Name == "yolonms_layer_1:1")
                                          .AsTensor<float>();

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
            _logger.LogDebug("Start processing model output. Boxes Shape: {BoxesShape}, Scores Shape: {ScoresShape}",
                             string.Join(",", boxes.Dimensions.ToArray()),
                             string.Join(",", scores.Dimensions.ToArray()));

            // Process each detection from the model output
            for (int candidateBoxIndex = 0; candidateBoxIndex < boxes.Dimensions[CandidateBoxesDimension]; candidateBoxIndex++)
            {
                (int ClassIndex, float Score)? @class = FindClassWithHighestConfidenceScore(scores, batchIndex: BatchSize, candidateBoxIndex);

                // Skip if no valid class or score below threshold
                if (@class == null || @class.Value.Score < _modelConfig.ConfidenceThreshold)
                {
                    continue;
                }

                int bestClassIndex = @class.Value.ClassIndex;
                float maxScore = @class.Value.Score;

                _logger.LogDebug("Found Detection {DetectionIndex}. Class: {ClassName} ({ClassIndex}) Score: {Score:F3}",
                                 candidateBoxIndex,
                                 _labels[bestClassIndex],
                                 bestClassIndex,
                                 maxScore);

                // Extract bounding box coordinates
                float y1 = boxes[BatchSize, candidateBoxIndex, 0]; // 0 = top coordinate = y1
                float x1 = boxes[BatchSize, candidateBoxIndex, 1]; // 1 = left coordinate = x1
                float y2 = boxes[BatchSize, candidateBoxIndex, 2]; // 2 = bottom coordinate = y2
                float x2 = boxes[BatchSize, candidateBoxIndex, 3]; // 3 = right coordinate = x2

                _logger.LogDebug("Raw Box Coordinates. Top-Left: {X1:F1},{Y1:F1}, Bottom-Right: {X2:F1},{Y2:F1}",
                                 x1,
                                 y1,
                                 x2,
                                 y2);

                // Calculate scale factors for normalized coordinates (convert from model input size to display size)
                // ONNX models typically output coordinates relative to their input size (416x416)
                float scaleX = (float)imageWidth / _modelConfig.ImageSize;
                float scaleY = (float)imageHeight / _modelConfig.ImageSize;

                BoundingBox boundingBox = new(x1, y1, x2, y2, scaleX, scaleY);

                _logger.LogDebug("Bounding Box: X={X:F1}, Y={Y:F1}, Width: {Width:F1}, Height: {Height:F1}, Scale: X={ScaleX:F3}, Y={ScaleY:F3}",
                                 boundingBox.X,
                                 boundingBox.Y,
                                 boundingBox.Width,
                                 boundingBox.Height,
                                 scaleX,
                                 scaleY);

                DetectionResult detection = new(_labels[bestClassIndex], maxScore, boundingBox);

                _logger.LogDebug("Detection: Label: {Label} Confidence: {Confidence:P1} at [X={X:F1}, Y={Y:F1}, W={Width:F1}, H={Height:F1}]",
                                 detection.Label,
                                 detection.Confidence,
                                 detection.BoundingBox.X,
                                 detection.BoundingBox.Y,
                                 detection.BoundingBox.Width,
                                 detection.BoundingBox.Height);

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

    private static DenseTensor<float> CreateInputTensor(Image<Rgba32> image, int targetSize)
    {
        const int batchSize = 1;
        const int numberOfChannels = 3; // RGB
        const int height = 416;
        const int width = 416;

        DenseTensor<float> tensor = new(dimensions: new[]
                                                    {
                                                        batchSize, numberOfChannels, height, width
                                                    });

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
        IEnumerable<IGrouping<string, DetectionResult>> detectionsGroupedByLabel = detections.GroupBy(r => r.Label);

        foreach (IGrouping<string, DetectionResult> detectionsForLabel in detectionsGroupedByLabel)
        {
            List<DetectionResult> detectionsByLabelConfidence = detectionsForLabel.OrderByDescending(r => r.Confidence)
                                                                                  .ToList();

            while (detectionsByLabelConfidence.Count > 0)
            {
                DetectionResult current = detectionsByLabelConfidence[0];
                results.Add(current);
                detectionsByLabelConfidence.RemoveAt(index: 0);

                detectionsByLabelConfidence.RemoveAll(r => CalculateIntersectionOverUnion(current.BoundingBox, r.BoundingBox) > _modelConfig.IntersectionOverUnionThreshold);
            }
        }

        return results.ToArray();
    }

    private static float CalculateIntersectionOverUnion(BoundingBox box1, BoundingBox box2)
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

    /// <summary>
    /// Finds the class index with the highest confidence score for a given candidate box.
    /// </summary>
    /// <param name="scores">The scores tensor from model output</param>
    /// <param name="batchIndex">The batch index (usually 0)</param>
    /// <param name="candidateBoxIndex">The index of the candidate box</param>
    /// <returns>The class index and score with the highest confidence, or null if none found</returns>
    private (int ClassIndex, float Score)? FindClassWithHighestConfidenceScore(
        Tensor<float> scores,
        int batchIndex,
        int candidateBoxIndex)
    {
        IEnumerable<(int ClassIndex, float Score)> classes = Enumerable.Range(0, scores.Dimensions[ClassesDimension])
                                                                       .Select(i => (ClassIndex: i, Score: scores[batchIndex, i, candidateBoxIndex]))
                                                                       .ToArray();

        if (classes.Any() && _logger.IsEnabled(LogLevel.Debug))
        {
            foreach ((int classIndex, float score) in classes.Where(c => c.Score >= 0.01))
            {
                _logger.LogDebug("Class Scores. Candidate Box: {CandidateBoxIndex}, Class: {ClassIndex}, Score: {Score:F4} ({ScoreAsPercentage:F1}%)",
                                 candidateBoxIndex,
                                 classIndex,
                                 score,
                                 score * 100);
            }
        }

        return classes.MaxBy(c => c.Score);
    }

    public void Dispose() => _session.Dispose();
}