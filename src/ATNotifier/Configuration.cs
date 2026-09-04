namespace ATNotifier;
public sealed class NotifierConfiguration { public string SecretsFile { get; init; } = "atnotifier.secrets.json"; public NotificationSettings Notifications { get; init; } = new(); public List<CheckConfiguration> Checks { get; init; } = []; }
public sealed class NotificationSettings { public SmtpSettings? Email { get; init; } }
public sealed class SmtpSettings { public required string Host { get; init; } public int Port { get; init; } = 25; public bool EnableSsl { get; init; } public required string From { get; init; } public List<string> To { get; init; } = []; public string? Username { get; init; } public string? PasswordSecretName { get; init; } }
public sealed class CheckConfiguration { public required string Name { get; init; } public bool Enabled { get; init; } = true; public CheckKind Kind { get; init; } = CheckKind.SharePointListHasItems; public NotificationChannel Notify { get; init; } = NotificationChannel.Both; public required SharePointListCheckSettings SharePoint { get; init; } }
public sealed class SharePointListCheckSettings { public required string SiteUrl { get; init; } public required string ListTitle { get; init; } public string DateField { get; init; } = "Created"; public DayRange Day { get; init; } = DayRange.Today; public SharePointAuthentication Authentication { get; init; } = new(); }
public sealed class SharePointAuthentication { public AuthenticationMode Mode { get; init; } = AuthenticationMode.WindowsSso; public string? Username { get; init; } public string? PasswordSecretName { get; init; } }
public enum CheckKind { SharePointListHasItems }
public enum DayRange { Today, PreviousDay }
public enum AuthenticationMode { WindowsSso, ConfiguredCredentials }
[Flags] public enum NotificationChannel { None = 0, Email = 1, Window = 2, Both = Email | Window }
public enum CheckOutcome { Passed, Failed, Error }
public sealed record CheckResult(string Name, CheckOutcome Outcome, string Message);
