using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Open.Autom.AI.tion.Console;

public class MicrosoftGraphClient : IMicrosoftInterface
{
    private readonly GraphServiceClient _graphServiceClient;

    public MicrosoftGraphClient(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public async Task<ICollection<User>> GetColleagues()
    {
        var response = await _graphServiceClient.Users.GetAsync();
        var graphUsers = response?.Value;
        return graphUsers?
            .Select(o => new User(o.Mail ?? string.Empty, o.DisplayName ?? string.Empty))
            .ToList() ?? new List<User>();
    }

    public Task<Meeting> CreateMeeting(string name, string description, DateTime start, DateTime end,
        List<string> attendees)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<Meeting>> GetMeetings()
    {
        var response = await _graphServiceClient.Me.Calendar.Events.GetAsync();
        return response?.Value?.Select(
            e => new Meeting(
                Id: e.Id ?? string.Empty,
                Name: e.Subject ?? string.Empty,
                Description: e.Body?.Content ?? string.Empty,
                Start: e.Start.ToDateTime(),
                End: e.End.ToDateTime(),
                Attendees: new List<User>())
        ).ToList() ?? new List<Meeting>();
    }
}