namespace Open.Autom.AI.tion.Console;

/// <summary>
/// The users that are also called colleagues.
/// </summary>
/// <param name="Email"></param>
/// <param name="Name"></param>
public record User(
    string Email,
    string Name
);

public record Meeting(
    string Id,
    string Name,
    string Description,
    DateTime Start,
    DateTime End,
    List<User> Attendees
);

public interface IMicrosoftInterface
{
    /// <summary>
    /// Returns a list with all colleagues.
    /// </summary>
    /// <returns>A list with all colleagues.</returns>
    Task<ICollection<User>> GetColleagues();

    /// <summary>
    /// Creates a new meeting.
    /// </summary>
    /// <param name="name">Name of the meeting.</param>
    /// <param name="description">Description of the meeting.</param>
    /// <param name="startDateTime">Start of the meeting. Including Date and Time.</param>
    /// <param name="endDateTime">End of the meeting. Including Date and Time.</param>
    /// <param name="attendees">List of email addresses of the attending Users.</param>
    /// <returns>The meeting that was created.</returns>
    Task<Meeting> CreateMeeting(
        string name,
        string description,
        DateTime startDateTime,
        DateTime endDateTime,
        List<string> attendees
    );

    /// <summary>
    /// Fetches the meeting record for the meeting with the specified id.
    /// </summary>
    /// <param name="id">The id of the meeting to fetch.</param>
    /// <returns>The fetched meeting object.</returns>
    Task<Meeting> GetMeeting(string id);

    /// <summary>
    /// Fetches the meeting record for the meeting with the specified id.
    /// </summary>
    /// <param name="start">
    /// Optional filter for the start time.
    /// If included only meetings that start at or after are returned.
    /// </param>
    /// <param name="end">
    /// Optional filter for the end time.
    /// If included only meetings that end at or before are returned.
    /// </param>
    /// <returns>The fetched meeting objects.</returns>
    Task<ICollection<Meeting>> SearchMeetings(
        DateTime? start = null,
        DateTime? end = null
    );
}