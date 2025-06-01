namespace UI.Components.Pages.Upload;

/// <summary>
/// Represents a progress value between 0 and 1 (inclusive).
/// </summary>
public readonly struct Progress
{

    /// <summary>
    /// The progress value (between 0 and 1).
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Creates a new Progress instance.
    /// </summary>
    /// <param name="value">The progress value between 0 and 1 (inclusive)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not between 0 and 1</exception>
    public Progress(double value)
    {
        if (value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                                                  value,
                                                  "Progress value must be between 0 and 1 (inclusive)");
        }

        Value = value;
    }

    /// <summary>
    /// Creates a Progress instance representing no progress (0).
    /// </summary>
    public static Progress None => new(0);

    /// <summary>
    /// Creates a Progress instance representing complete progress (1).
    /// </summary>
    public static Progress Complete => new(1);

    public static implicit operator double(Progress progress) => progress.Value;

    public override string ToString() => $"{Value:P0}";

    public double AsPercentage() => Value * 100;
}