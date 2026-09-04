namespace ATNotifier;
public sealed class CheckRunner(SecretStore secrets, NotificationDispatcher notifier)
{
    public async Task<IReadOnlyList<CheckResult>> RunAsync(IEnumerable<CheckConfiguration> checks)
    {
        var results = new List<CheckResult>();
        foreach (var check in checks)
        {
            CheckResult result;
            try { result = check.Kind switch { CheckKind.SharePointListHasItems => await new SharePointListHasItemsCheck(secrets).RunAsync(check), _ => throw new NotSupportedException($"Unsupported check kind: {check.Kind}") }; }
            catch (Exception exception) { result = new CheckResult(check.Name, CheckOutcome.Error, exception.Message); }
            Console.WriteLine($"[{result.Outcome}] {result.Name}: {result.Message}");
            if (result.Outcome != CheckOutcome.Passed)
            {
                try { await notifier.SendAsync(check.Notify, result); }
                catch (Exception exception) { Console.Error.WriteLine($"Notification for '{check.Name}' failed: {exception.Message}"); }
            }
            results.Add(result);
        }
        return results;
    }
}
