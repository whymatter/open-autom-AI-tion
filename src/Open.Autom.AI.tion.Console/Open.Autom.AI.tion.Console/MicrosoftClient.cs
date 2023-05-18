namespace Open.Autom.AI.tion.Console;

public class MicrosoftClient : IMicrosoftInterface
{
    public async Task<ICollection<User>> GetColleagues()
    {
        return new List<User>
        {
            new(Email: "oliver.seitz@digatus.com",
                Name: "Oliver Seitz"),
            new(Email: "andre.kimmer@digatus.com",
                Name: "Andre Kimmer"),
            new(Email: "florian.bernd@digatus.com",
                Name: "Florian Bernd")
        };
    }

    public async Task<Meeting> CreateMeeting(string name, string description, DateTime start, DateTime end,
        List<string> attendees)
    {
        var colleagues = await GetColleagues();
        var attendeesUsers = colleagues.Where(o => attendees.Contains(o.Email)).ToList();
        var meeting = new Meeting(name, name, description, start, end, attendeesUsers);
        System.Console.WriteLine($"Created a meeting {meeting}");
        return meeting;
    }

    public Task<Meeting> GetMeeting(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<Meeting>> SearchMeetings(DateTime? start = null, DateTime? end = null)
    {
        return new List<Meeting>
        {
            new(
                Id: "748594835",
                Name: "Meeting with Daniel",
                Description: "Want to talk to you",
                Start: new DateTime(2023, 04, 26, 13, 0, 0),
                End: new DateTime(2023, 04, 26, 14, 0, 0),
                Attendees: new List<User>
                {
                    new("oliver.seitz@digatus.com", "Oliver Seitz"),
                    new("florian.bernd@digatus.com", "Florian Bernd")
                })
        };
    }
}