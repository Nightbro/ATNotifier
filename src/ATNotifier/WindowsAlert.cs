using System.Windows.Forms;

namespace ATNotifier;

internal static class WindowsAlert
{
    public static void ShowFailure(string title, string message)
    {
        // A scheduled task without an interactive desktop cannot show a window.
        if (!Environment.UserInteractive) return;
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
