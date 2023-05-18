namespace Open.Autom.AI.tion.Console;

/// <summary>
/// The users that are also called colleagues.
/// </summary>
/// <param name="Email"></param>
/// <param name="Name">Attention, check names always fuzzy.</param>
public record User(
    string Email,
    string Name
);

/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Description"></param>
/// <param name="Start">The start of the meeting, inclusive.</param>
/// <param name="End">The end of the meeting, inclusive.</param>
/// <param name="Attendees"></param>
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
    /// Gets all meetings for the user..
    /// </summary>
    /// <returns>The fetched meeting objects.</returns>
    Task<ICollection<Meeting>> GetMeetings();
}