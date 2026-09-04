using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace ATNotifier;
internal sealed class SetupWizard : Form
{
    private readonly string path;
    private readonly TextBox site = new() { Width = 360, PlaceholderText = "https://sharepoint.company.local/sites/Operations" };
    private readonly TextBox list = new() { Width = 360, PlaceholderText = "Daily Operations" };
    private readonly ComboBox day = Combo([DayRange.Today, DayRange.PreviousDay], DayRange.Today);
    private readonly ComboBox notify = Combo([NotificationChannel.Window, NotificationChannel.Email, NotificationChannel.Both], NotificationChannel.Window);
    private readonly RadioButton sso = new() { Text = "Use the current Windows account (SSO)", Checked = true, AutoSize = true };
    private readonly RadioButton other = new() { Text = "Use a different Windows account", AutoSize = true };
    private readonly TextBox user = new() { Width = 250, PlaceholderText = "DOMAIN\\username" };
    private readonly TextBox password = new() { Width = 250, UseSystemPasswordChar = true };
    private readonly TextBox smtp = new() { Width = 250, PlaceholderText = "smtp.company.local" };
    private readonly TextBox from = new() { Width = 250, PlaceholderText = "notifier@company.local" };
    private readonly TextBox to = new() { Width = 250, PlaceholderText = "you@company.local" };
    private SetupWizard(string configurationPath)
    {
        path = configurationPath; Text = "ATNotifier setup"; Width = 560; Height = 600; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16), ColumnCount = 1 };
        panel.Controls.Add(new Label { Text = "Enter these details once. They will be saved for future scheduled runs.", AutoSize = true, MaximumSize = new Size(500, 0) });
        Add(panel, "SharePoint site URL", site); Add(panel, "List title", list); Add(panel, "Check", day); Add(panel, "Notify on failure", notify);
        panel.Controls.Add(new Label { Text = "SharePoint sign-in", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 16, 0, 4) }); panel.Controls.Add(sso); panel.Controls.Add(other); Add(panel, "Different-account username", user); Add(panel, "Different-account password", password);
        panel.Controls.Add(new Label { Text = "Email (required only for Email or Both)", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 16, 0, 4) }); Add(panel, "SMTP server", smtp); Add(panel, "From", from); Add(panel, "To", to);
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 18, 0, 0) }; var save = new Button { Text = "Save and continue", AutoSize = true }; save.Click += (_, _) => Save(); var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel }; buttons.Controls.Add(save); buttons.Controls.Add(cancel); panel.Controls.Add(buttons); Controls.Add(panel); AcceptButton = save; CancelButton = cancel;
    }
    public static bool CreateConfiguration(string path) { Application.EnableVisualStyles(); using var form = new SetupWizard(path); return form.ShowDialog() == DialogResult.OK; }
    private void Save()
    {
        if (!Uri.TryCreate(site.Text.Trim(), UriKind.Absolute, out _)) { Error("Enter a valid SharePoint site URL."); return; } if (string.IsNullOrWhiteSpace(list.Text)) { Error("Enter the SharePoint list title."); return; }
        var channels = (NotificationChannel)notify.SelectedItem!;
        if (other.Checked && (string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(password.Text))) { Error("Enter both credentials or select Windows SSO."); return; }
        if (channels.HasFlag(NotificationChannel.Email) && (string.IsNullOrWhiteSpace(smtp.Text) || string.IsNullOrWhiteSpace(from.Text) || string.IsNullOrWhiteSpace(to.Text))) { Error("Email notification needs SMTP server, From, and To values."); return; }
        const string secret = "sharepoint-password";
        var config = new NotifierConfiguration { Notifications = channels.HasFlag(NotificationChannel.Email) ? new NotificationSettings { Email = new SmtpSettings { Host = smtp.Text.Trim(), From = from.Text.Trim(), To = [to.Text.Trim()] } } : new NotificationSettings(), Checks = [new CheckConfiguration { Name = list.Text.Trim(), Notify = channels, SharePoint = new SharePointListCheckSettings { SiteUrl = site.Text.Trim(), ListTitle = list.Text.Trim(), Day = (DayRange)day.SelectedItem!, Authentication = other.Checked ? new SharePointAuthentication { Mode = AuthenticationMode.ConfiguredCredentials, Username = user.Text.Trim(), PasswordSecretName = secret } : new SharePointAuthentication() } }] };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } }));
        if (other.Checked) new SecretStore(Path.Combine(Path.GetDirectoryName(path)!, config.SecretsFile)).Set(secret, password.Text); DialogResult = DialogResult.OK; Close();
    }
    private static ComboBox Combo<T>(IEnumerable<T> values, T selected) where T : struct, Enum { var box = new ComboBox { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList }; foreach (var value in values) box.Items.Add(value); box.SelectedItem = selected; return box; }
    private static void Add(TableLayoutPanel panel, string label, Control control) { panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 10, 0, 2) }); panel.Controls.Add(control); }
    private void Error(string message) => MessageBox.Show(message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
