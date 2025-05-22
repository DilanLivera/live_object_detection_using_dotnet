namespace UI.Components.Pages.Upload;

/// <summary>
/// Manages status messages for object detection operations
/// </summary>
public sealed class StatusMessages
{
    /// <summary>
    /// Dictionary of status messages for this operation
    /// </summary>
    private readonly Dictionary<ObjectDetectionStatus, StatusMessage> _value = [];

    /// <summary>
    /// Gets a list of status messages ordered by creation time
    /// </summary>
    public IReadOnlyList<StatusMessage> Value => _value.Values
                                                       // issue: when the two messages has the same time upto the second, this reorder the messages
                                                       // .OrderBy(m => m.CreatedAt)
                                                       .ToArray()
                                                       .AsReadOnly();

    /// <summary>
    /// Gets the status messages count
    /// </summary>
    public int Count => _value.Count;

    /// <summary>
    /// Adds or updates a status message for the specified detection status
    /// </summary>
    /// <param name="status">The object detection status to associate with the message</param>
    /// <param name="message">The status message to add</param>
    public void AddMessage(ObjectDetectionStatus status, StatusMessage message) => _value[status] = message;

    /// <summary>
    /// Stops the spinner for the message associated with the specified status
    /// </summary>
    /// <param name="status">The object detection status whose message spinner should be stopped</param>
    public void StopMessageSpinner(ObjectDetectionStatus status) => _value[status].StopSpinner();

    /// <summary>
    /// Clears all status messages
    /// </summary>
    public void Clear() => _value.Clear();
}