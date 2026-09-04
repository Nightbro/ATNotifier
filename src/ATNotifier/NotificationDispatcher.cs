using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace ATNotifier;

public sealed class NotificationDispatcher(NotificationSettings settings, SecretStore secrets)
{
    public async Task SendAsync(NotificationChannel channels, CheckResult result)
    {
        if (channels.HasFlag(NotificationChannel.Email)) await SendEmailAsync(result);
        if (channels.HasFlag(NotificationChannel.Window)) MessageBox.Show(result.Message, $"ATNotifier: {result.Name}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task SendEmailAsync(CheckResult result)
    {
        var email = settings.Email ?? throw new InvalidOperationException("Email notification is configured but Notifications.Email is missing.");
        if (email.To.Count == 0) throw new InvalidOperationException("Email notification is configured but no recipients were supplied.");
        using var message = new MailMessage { From = new MailAddress(email.From), Subject = $"ATNotifier: {result.Name}", Body = $"Status: {result.Outcome}{Environment.NewLine}{result.Message}" };
        foreach (var recipient in email.To) message.To.Add(recipient);
        using var client = new SmtpClient(email.Host, email.Port) { EnableSsl = email.EnableSsl };
        if (!string.IsNullOrWhiteSpace(email.Username))
        {
            if (string.IsNullOrWhiteSpace(email.PasswordSecretName)) throw new InvalidOperationException("SMTP username requires PasswordSecretName.");
            client.Credentials = new NetworkCredential(email.Username, secrets.Get(email.PasswordSecretName));
        }
        else client.UseDefaultCredentials = true;
        await client.SendMailAsync(message);
    }
}
