using System.Net;
using System.Text.Json;

namespace ATNotifier;

public sealed class SharePointListHasItemsCheck(SecretStore secrets)
{
    public async Task<CheckResult> RunAsync(CheckConfiguration check)
    {
        var settings = check.SharePoint;
        var (from, until) = GetPeriod(settings.Day);
        using var handler = CreateHandler(settings.Authentication);
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json;odata=nometadata");

        var title = settings.ListTitle.Replace("'", "''");
        var filter = $"{settings.DateField} ge datetime'{from:yyyy-MM-ddTHH:mm:ss}' and {settings.DateField} lt datetime'{until:yyyy-MM-ddTHH:mm:ss}'";
        var baseUrl = settings.SiteUrl.TrimEnd('/');
        var url = $"{baseUrl}/_api/web/lists/GetByTitle('{title}')/items?$select=Id&$top=1&$filter={Uri.EscapeDataString(filter)}";
        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"SharePoint returned {(int)response.StatusCode} ({response.ReasonPhrase}).");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var found = document.RootElement.TryGetProperty("value", out var value) && value.GetArrayLength() > 0;
        var dayDescription = settings.Day == DayRange.Today ? "today" : "the previous day";
        return found
            ? new CheckResult(check.Name, CheckOutcome.Passed, $"The list contains an item for {dayDescription}.")
            : new CheckResult(check.Name, CheckOutcome.Failed, $"No item was found for {dayDescription}.");
    }

    private HttpClientHandler CreateHandler(SharePointAuthentication authentication)
    {
        if (authentication.Mode == AuthenticationMode.WindowsSso) return new HttpClientHandler { UseDefaultCredentials = true };
        if (string.IsNullOrWhiteSpace(authentication.Username) || string.IsNullOrWhiteSpace(authentication.PasswordSecretName))
            throw new InvalidOperationException("ConfiguredCredentials requires Username and PasswordSecretName.");
        var split = authentication.Username.Split('\\', 2);
        var credential = split.Length == 2 ? new NetworkCredential(split[1], secrets.Get(authentication.PasswordSecretName), split[0]) : new NetworkCredential(authentication.Username, secrets.Get(authentication.PasswordSecretName));
        return new HttpClientHandler { Credentials = credential };
    }

    private static (DateTime From, DateTime Until) GetPeriod(DayRange day)
    {
        var from = DateTime.Today;
        if (day == DayRange.PreviousDay) from = from.AddDays(-1);
        return (from, from.AddDays(1));
    }
}
