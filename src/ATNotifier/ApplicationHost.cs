using System.Text.Json;
using System.Text.Json.Serialization;

namespace ATNotifier;

internal static class ApplicationHost
{
    private const string DefaultConfigFile = "atnotifier.json";
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0].Equals("protect-secret", StringComparison.OrdinalIgnoreCase)) return ProtectSecret(args.Skip(1).ToArray());
            var configPath = GetOption(args, "--config") ?? Path.Combine(AppContext.BaseDirectory, DefaultConfigFile);
            var configuration = await LoadConfigurationAsync(configPath);
            var secrets = new SecretStore(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configPath))!, configuration.SecretsFile));
            var results = await new CheckRunner(secrets, new NotificationDispatcher(configuration.Notifications, secrets)).RunAsync(configuration.Checks.Where(c => c.Enabled));
            return results.Any(r => r.Outcome != CheckOutcome.Passed) ? 1 : 0;
        }
        catch (Exception exception) { Console.Error.WriteLine($"Fatal error: {exception.Message}"); return 2; }
    }
    private static int ProtectSecret(string[] args)
    {
        var name = GetOption(args, "--name"); var secretsFile = GetOption(args, "--secrets") ?? "atnotifier.secrets.json";
        if (string.IsNullOrWhiteSpace(name)) { Console.Error.WriteLine("Usage: protect-secret --name <secret-name> [--secrets <file>]"); return 2; }
        Console.Write($"Enter value for '{name}': "); var value = ReadHiddenLine(); Console.WriteLine();
        if (string.IsNullOrEmpty(value)) { Console.Error.WriteLine("A secret value is required."); return 2; }
        new SecretStore(Path.GetFullPath(secretsFile)).Set(name, value);
        Console.WriteLine($"Encrypted secret '{name}' saved to {Path.GetFullPath(secretsFile)}."); return 0;
    }
    private static async Task<NotifierConfiguration> LoadConfigurationAsync(string path)
    {
        if (!File.Exists(path))
        {
            var fullConfigPath = Path.GetFullPath(path);
            var samplePath = Path.Combine(Path.GetDirectoryName(fullConfigPath)!, "atnotifier.sample.json");
            if (Path.GetFileName(path).Equals(DefaultConfigFile, StringComparison.OrdinalIgnoreCase) && File.Exists(samplePath))
            {
                File.Copy(samplePath, fullConfigPath);
                throw new InvalidOperationException($"Created {fullConfigPath} from the sample. Update its SharePoint and email values, then run again.");
            }
            throw new FileNotFoundException("Configuration file was not found.", fullConfigPath);
        }
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<NotifierConfiguration>(stream, JsonOptions) ?? throw new InvalidOperationException("Configuration file is empty or invalid.");
    }
    private static string? GetOption(IEnumerable<string> args, string option) { var values = args.ToArray(); var index = Array.FindIndex(values, value => value.Equals(option, StringComparison.OrdinalIgnoreCase)); return index >= 0 && index + 1 < values.Length ? values[index + 1] : null; }
    private static string ReadHiddenLine() { var result = new System.Text.StringBuilder(); ConsoleKeyInfo key; while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter) { if (key.Key == ConsoleKey.Backspace && result.Length > 0) result.Length--; else if (!char.IsControl(key.KeyChar)) result.Append(key.KeyChar); } return result.ToString(); }
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
}
