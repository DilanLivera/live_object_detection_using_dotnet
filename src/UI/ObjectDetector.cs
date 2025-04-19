using System.Text.Json;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace UI;

/// <summary>
/// Provides object detection functionality using the Tiny YOLOv3 model.
/// Handles image preprocessing, model inference, and post-processing of detection results.
/// </summary>
public class ObjectDetector : IDisposable
{
    private readonly ILogger<ObjectDetector> _logger;
    private readonly InferenceSession _session;
    private readonly string[] _labels;
    private const float ConfidenceThreshold = 0.25f;
    private const float IntersectionOverUnionThreshold = 0.45f;

    public ObjectDetector(
        IWebHostEnvironment environment,
        ILogger<ObjectDetector> logger)
    {
        _logger = logger;

        // TODO: add configuration for the full model path(including model name)
        string modelPath = Path.Combine(environment.ContentRootPath, "Models", "tiny-yolov3-11.onnx");
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Model file not found. Please ensure 'tiny-yolov3-11.onnx' is in the 'src/UI/Models' directory.",
                                            fileName: modelPath);
        }

        _session = new InferenceSession(modelPath);

        if (!_session.InputMetadata.Any())
        {
            throw new InvalidOperationException("No inputs found in the model metadata!");
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            // Name == input.Key, Shape == input.Value.Dimensions, Type == input.Value.ElementType
            var inputMetadata = _session.InputMetadata.Select(input => new { Name = input.Key, Shape = input.Value.Dimensions });
            logger.LogDebug("Model Input Names: {ModelInputNames}", JsonSerializer.Serialize(inputMetadata));

            // Name == output.Key, Shape == output.Value.Dimensions, Type == output.Value.ElementType
            var outputMetadata = _session.OutputMetadata.Select(output => new { Name = output.Key, Shape = output.Value.Dimensions });
            logger.LogDebug("Model Output Names: {ModelOutputNames}", JsonSerializer.Serialize(outputMetadata));
        }

        string labelsPath = Path.Combine(environment.ContentRootPath, "Models", "coco.names");

        _labels = File.ReadAllLines(labelsPath);
    }

    /// <summary>
    /// Performs object detection on image data.
    /// </summary>
    /// <param name="imageData">Raw image bytes to process</param>
    /// <returns >Detection results</returns>
    public async Task<DetectionResult[]> DetectObjectsAsync(byte[] imageData)
    {
        using Image<Rgb24> image = Image.Load<Rgb24>(imageData);

        const int imageSize = 416; // Tiny YOLOv3 default input size
        using Image<Rgb24> resizedAndPaddedImage = ResizeAndPadImage(image, imageSize);
        Tensor<float> modelInputTensor = CreateTensor(resizedAndPaddedImage, imageSize);

        DenseTensor<float> imageShapeTensor = new(dimensions: new[] { 1, 2 }) { [0, 0] = image.Height, [0, 1] = image.Width };

        // Create inputs with correct names as expected by the model
        List<NamedOnnxValue> inputs =
        [
            NamedOnnxValue.CreateFromTensor(name: "input_1", modelInputTensor),
            NamedOnnxValue.CreateFromTensor(name: "image_shape", imageShapeTensor)
        ];

        // Run() is used instead of RunAsync() because the model inference is CPU-bound and doesn't benefit from async execution
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue>? outputs = _session.Run(inputs);

        DetectionResult[] detectionResults = PostprocessResults(outputs, image.Width, image.Height);

        return await Task.FromResult(detectionResults);
    }

    /// <summary>
    /// Resize the image to fit within the target dimensions while maintaining the aspect ratio and add padding to centre the image to the required size.
    /// </summary>
    /// <param name="image">Original image</param>
    /// <param name="targetSize">Target size for width and height</param>
    /// <returns>Processed image with dimensions targetSize x targetSize</returns>
    private Image<Rgb24> ResizeAndPadImage(Image<Rgb24> image, int targetSize)
    {
        _logger.LogDebug("Original image size: {ImageWidth} x {ImageHeight}",
                         image.Width,
                         image.Height);

        float scale = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);
        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);

        _logger.LogDebug("Resizing to: {NewImageWidth}x{NewImageHeight} (scale: {ImageScale:F3})",
                         newWidth,
                         newHeight,
                         scale);

        Image<Rgb24> paddedImage = new(width: targetSize, height: targetSize);
        paddedImage.Mutate(operation: imageProcessingContext => imageProcessingContext.BackgroundColor(Color.Black));

        image.Mutate(operation: imageProcessingContext =>
        {
            ResizeOptions resizeOptions = new() { Size = new Size(newWidth, newHeight), Mode = ResizeMode.Stretch };
            imageProcessingContext.Resize(resizeOptions);
        });

        int xPad = (targetSize - newWidth) / 2;
        int yPad = (targetSize - newHeight) / 2;

        _logger.LogDebug("Padding: X={XPad}, Y={YPad}", xPad, yPad);

        paddedImage.Mutate(operation: imageProcessingContext =>
        {
            Point backgroundLocation = new(xPad, yPad);
            imageProcessingContext.DrawImage(foreground: image, backgroundLocation, opacity: 1f);
        });

        return paddedImage;
    }

    /// <summary>
    /// Creat a tensor suitable for model input using the processed image.
    /// A tensor is a mathematical object that can represent multidimensional arrays of data
    /// </summary>
    /// <param name="image">Image to be used to create tensor</param>
    /// <param name="imageSize">Image size</param>
    /// <returns>Tensor of shape [1,3,imageSize,imageSize] containing normalized RGB values</returns>
    private static Tensor<float> CreateTensor(Image<Rgb24> image, int imageSize)
    {
        const int batchSize = 1;
        const int rgbChannels = 3;
        DenseTensor<float> tensor = new(dimensions: [batchSize, rgbChannels, imageSize, imageSize]);

        // Convert image pixels to normalized tensor values
        // Normalize pixel values to [0,1] range and reorder to RGB
        for (int y = 0; y < imageSize; y++)
        {
            for (int x = 0; x < imageSize; x++)
            {
                Rgb24 pixel = image[x, y];
                // YOLOv3 expects pixels normalized to [0, 1] in RGB order
                tensor[0, 0, y, x] = pixel.R / 255.0f; // R channel
                tensor[0, 1, y, x] = pixel.G / 255.0f; // G channel
                tensor[0, 2, y, x] = pixel.B / 255.0f; // B channel
            }
        }

        return tensor;
    }

    /// <summary>
    /// Processes the model outputs to extract detection results.
    /// Includes filtering by confidence threshold and coordinate transformation.
    /// </summary>
    /// TODO: add what this method is returning and parameters
    private DetectionResult[] PostprocessResults(
        IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs,
        int originalWidth,
        int originalHeight)
    {
        List<DetectionResult> detections = [];

        try
        {
            // Extract bounding boxes and confidence scores from model outputs
            // yolonms_layer_1: contains [y1,x1,y2,x2] coordinates
            Tensor<float>? boxes = outputs.First(disposableNamedOnnxValue => disposableNamedOnnxValue.Name == "yolonms_layer_1").AsTensor<float>();
            // yolonms_layer_1:1: contains class probabilities
            Tensor<float>? scores = outputs.First(disposableNamedOnnxValue => disposableNamedOnnxValue.Name == "yolonms_layer_1:1").AsTensor<float>();

            _logger.LogDebug("Boxes shape: {BoxesShape}", string.Join(",", boxes.Dimensions.ToArray()));
            _logger.LogDebug("Scores shape: {ScoresShape}", string.Join(",", scores.Dimensions.ToArray()));

            // Process each detection from the model output
            for (int i = 0; i < boxes.Dimensions[1]; i++)
            {
                // Find the class with highest confidence score
                float maxScore = float.MinValue;
                const int noValidClass = -1;
                int bestClass = noValidClass; // -1 indicates no valid class found

                for (int c = 0; c < scores.Dimensions[1]; c++)
                {
                    // QUESTION: what is scores[0, c, i]? 
                    float score = scores[0, c, i];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestClass = c;
                    }
                }

                if (maxScore < ConfidenceThreshold || bestClass == noValidClass)
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
                float scaleX = processedWidth / originalWidth;
                float scaleY = processedHeight / originalHeight;

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

    /// <summary>
    /// Applies Non-Maximum Suppression to filter out overlapping detections.
    /// Keeps the detection with highest confidence when multiple detections overlap significantly.
    /// </summary>
    /// TODO: add what this method is returning and parameters
    private static DetectionResult[] ApplyNonMaximumSuppression(DetectionResult[] detections)
    {
        List<DetectionResult> results = [];
        IEnumerable<IGrouping<string, DetectionResult>> detectionsGroups = detections.GroupBy(r => r.Label);

        // Process each group of detections with the same label
        // This prevents removing detections of different objects that happen to overlap
        foreach (IGrouping<string, DetectionResult> group in detectionsGroups)
        {
            List<DetectionResult> sorted = group.OrderByDescending(r => r.Confidence).ToList();

            while (sorted.Count > 0)
            {
                DetectionResult current = sorted[0];
                results.Add(current);
                sorted.RemoveAt(index: 0);

                // Remove all remaining detections that overlap significantly with the current one
                // QUESTION: why do we need to do this?
                sorted.RemoveAll(r => CalculateIntersectionOverUnion(current.Box, r.Box) > IntersectionOverUnionThreshold);
            }
        }

        return results.ToArray();
    }

    /// <summary>
    /// Calculates the Intersection over Union (IoU) between two bounding boxes.
    /// Used to determine the overlap between detections for Non-Maximum Suppression.
    /// </summary>
    /// TODO: add what this method is returning and parameters
    private static float CalculateIntersectionOverUnion(Box box1, Box box2)
    {
        // Calculate the coordinates of the intersection rectangle
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

/// <summary>
/// Represents a single object detection result.
/// IMPORTANT: Property names must match the JavaScript object properties exactly as they are used in camera.js.
/// JavaScript destructuring: const { label, confidence, box } = detection;
/// </summary>
public class DetectionResult
{
    public string Label { get; init; } = "";
    public float Confidence { get; init; }
    public Box Box { get; init; } = new();
}

/// <summary>
/// Represents the bounding box coordinates for a detected object.
/// IMPORTANT: Property names must match the JavaScript object properties exactly as they are used in camera.js.
/// JavaScript destructuring: const { x, y, width, height } = box;
/// All values are normalized (0-1) representing percentage of image dimensions.
/// </summary>
public class Box
{
    public float X { get; init; } // Normalized x-coordinate (0-1)
    public float Y { get; init; } // Normalized y-coordinate (0-1)
    public float Width { get; init; } // Normalized width (0-1)
    public float Height { get; init; } // Normalized height (0-1)
}