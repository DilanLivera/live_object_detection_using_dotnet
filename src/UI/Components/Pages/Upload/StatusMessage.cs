using System.Diagnostics;

namespace UI.Components.Pages.Upload;

/// <summary>
/// Represents a status message with styling and progress information
/// </summary>
public sealed class StatusMessage
{
    /// <summary>
    /// The message text to display
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// CSS class for the message container
    /// </summary>
    public string CssClass { get; }

    /// <summary>
    /// CSS class for the message text
    /// </summary>
    public string TextClass { get; }

    /// <summary>
    /// Whether to show a loading spinner with the message
    /// </summary>
    public bool ShouldShowSpinner { get; private set; }

    /// <summary>
    /// Whether to show a progress bar with the message
    /// </summary>
    public bool ShouldShowProgress { get; }

    /// <summary>
    /// The current progress value (0-1)
    /// </summary>
    public double Progress { get; private set; }

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    /// <summary>
    /// Additional details about the operation
    /// </summary>
    public string Details { get; }

    public StatusMessage(
        string text,
        string cssClass,
        string textClass = "",
        bool shouldShowSpinner = false,
        bool shouldShowProgress = false,
        double progress = 0.0,
        string details = "")
    {
        CssClass = cssClass;
        TextClass = textClass;
        ShouldShowSpinner = shouldShowSpinner;
        ShouldShowProgress = shouldShowProgress;
        Text = text;
        Details = details;

        UpdateProgress(progress);
    }

    /// <summary>
    /// Updates the progress value of this message
    /// </summary>
    /// <param name="progress">Progress value (0-1)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when progress is not between 0 and 1 (inclusive)</exception>
    private void UpdateProgress(double progress)
    {
        Debug.Assert(progress is >= 0 and <= 1, "Progress value must be between 0 and 1 (inclusive)");

        if (progress is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress value must be between 0 and 1 (inclusive)");
        }

        if (ShouldShowProgress)
        {
            Progress = progress;
        }
    }

    /// <summary>
    /// Stops showing the spinner for this message
    /// </summary>
    public void StopSpinner() => ShouldShowSpinner = false;
}