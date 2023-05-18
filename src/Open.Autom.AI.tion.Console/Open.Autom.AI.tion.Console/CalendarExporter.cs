using System.Globalization;
using CsvHelper;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Open.Autom.AI.tion.Console;

public record CsvMeeting(string Title, DateTime Start, DateTime End);

public class CalendarExporter
{
    private readonly GraphServiceClient _graphServiceClient;

    public CalendarExporter(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public async Task ExportAsync()
    {
        var events = _graphServiceClient.Me.Calendar.Events.GetAsync();

        var meetings = await _graphServiceClient.Me.Calendar.Events.GetAsync();

        var csvMeetings = meetings!.Value!.Select(
            m => new CsvMeeting(
                Title: m.Subject ?? throw new Exception("Meeting subject was null"),
                Start: m.Start.ToDateTime(),
                End: m.End.ToDateTime()
            )
        );

        // var writer = new StreamWriter(System.Console.OpenStandardOutput());
        // writer.AutoFlush = true;
        // System.Console.SetOut(writer);
        await using var csv = new CsvWriter(new StreamWriter("text.txt"), CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(csvMeetings);
    }
}