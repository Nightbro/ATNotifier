# ATNotifier

Windows .NET notifier for scheduled checks against internal sites. It currently includes a SharePoint Subscription Edition list check: it succeeds when at least one list item exists for today or the previous day, using a selected date field (default: `Created`). A failed check or connection error can send email, show a Windows dialog, or do both.

## Configure

1. Copy `atnotifier.sample.json` to `atnotifier.json` and replace the example site, list, mail server, and recipients.
2. For normal SharePoint SSO, leave `authentication.mode` as `WindowsSso`. The scheduled task must run as the Windows user permitted to access the site.
3. To run a SharePoint check as another account, set `mode` to `ConfiguredCredentials`, add `username` (for example `CONTOSO\\svc-atnotifier`) and `passwordSecretName`, then save the password:

   ```powershell
   .\ATNotifier.exe protect-secret --name sharepoint-service-password
   ```

   Point `passwordSecretName` at that same name. The secret is encrypted with Windows DPAPI for the Windows account that executes this command. Run the scheduled task as that account; copying the secret file to another account or computer will not make it readable.

4. For SMTP using a username/password, set `username` and `passwordSecretName` under `notifications.email`, then use the same `protect-secret` command for the SMTP password. If no SMTP username is given, the application uses the task's Windows credentials for SMTP.

The `notify` setting on each check accepts `Email`, `Window`, `Both`, or `None`. A window notification requires the task to run only while that user is logged on. Email still works without an interactive desktop.

## Build and run

On a workstation with the .NET 8 SDK, publish a portable self-contained Windows build:

```powershell
dotnet publish .\src\ATNotifier\ATNotifier.csproj -c Release -r win-x64 --self-contained true -o .\publish
Copy-Item .\atnotifier.sample.json .\publish\atnotifier.json
Set-Location .\publish
.\ATNotifier.exe
```

Copy the resulting `publish` folder to the destination computer. It does not need the source repository, a repository client, or a separately installed .NET runtime.

Use `--config <path>` when the configuration file is elsewhere. The process returns `0` when every check passes, `1` when a check fails or errors, and `2` for a startup/configuration error.

## Task Scheduler

In **Task Scheduler**, create a task with the account that has SharePoint access. On **General**, choose **Run only when user is logged on** if you want window notifications. On **Triggers**, set the desired daily or repeating schedule. On **Actions**, choose **Start a program**:

- Program/script: `C:\ATNotifier\ATNotifier.exe`
- Start in: `C:\ATNotifier`
- Arguments: `--config C:\ATNotifier\atnotifier.json`

Choose **Run whether user is logged on or not** only for email-only use. In that mode, use Windows SSO only if the task account is authorized in SharePoint.

## Extending checks

Checks are isolated behind `CheckKind` and `CheckRunner`. Add a new settings model and an implementation alongside `SharePointListHasItemsCheck`, then route the new kind in `CheckRunner`. This keeps each custom site integration independent from credentials, scheduling, and notification delivery.
