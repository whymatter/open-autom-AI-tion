using Azure.Identity;
using Microsoft.Graph;

namespace Open.Autom.AI.tion.Console;

internal static class GraphServiceClientFactory
{
    public static async Task<GraphServiceClient> Get(MicrosoftOptions msOptions)
    {
        var scopes = new[]
        {
            "Calendars.Read",
            "Calendars.Read.Shared",
            "Mail.Read",
            "Mail.Read.Shared",
            "User.Read",
            "User.ReadBasic.All"
        };

        // using Azure.Identity;
        var options = new InteractiveBrowserCredentialOptions
        {
            TenantId = msOptions.TenantId,
            ClientId = msOptions.ClientId,
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            RedirectUri = new Uri("http://localhost"),
        };

        var interactiveCredential = new InteractiveBrowserCredential(options);
        await interactiveCredential.AuthenticateAsync();

        var graphClient = new GraphServiceClient(interactiveCredential, scopes);

        return graphClient;
    }
}