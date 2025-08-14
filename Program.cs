using System;
using System.IO;
using System.Windows.Forms;
// using AutoUpdaterDotNET; // Uncomment when AutoUpdater.NET is properly referenced

namespace WorkOrderBlender
{
  internal static class Program
  {
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WorkOrderBlender.log");
    public static readonly InMemoryEditStore Edits = new InMemoryEditStore();

    public static void Log(string message, Exception ex = null)
    {
      try
      {
        File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{(ex != null ? ex + Environment.NewLine : string.Empty)}");
      }
      catch { /* ignore logging errors */ }
    }

    [STAThread]
    private static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

      // Clear previous session log on startup
      try
      {
        if (File.Exists(LogFilePath))
        {
          File.Delete(LogFilePath);
        }
      }
      catch { /* ignore log cleanup errors */ }

      Application.ThreadException += (s, e) =>
      {
        Log("ThreadException", e.Exception);
        MessageBox.Show("An error occurred: " + e.Exception.Message, "WorkOrderBlender", MessageBoxButtons.OK, MessageBoxIcon.Error);
      };

      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      {
        var ex = e.ExceptionObject as Exception;
        Log("UnhandledException", ex);
        if (ex != null)
        {
          MessageBox.Show("An error occurred: " + ex.Message, "WorkOrderBlender", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      };

      try
      {
        // TODO: Uncomment when AutoUpdater.NET is properly integrated
        // ConfigureAutoUpdater();
        // CheckForUpdates(silent: true);

        Application.Run(new MainForm());
      }
      catch (Exception ex)
      {
        Log("Main crash", ex);
        MessageBox.Show("Startup error: " + ex.Message, "WorkOrderBlender", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    /* TODO: Uncomment when AutoUpdater.NET is properly integrated
    private static void ConfigureAutoUpdater()
    {
      // Configure AutoUpdater.NET
      AutoUpdater.UpdateMode = Mode.ForcedDownload;
      AutoUpdater.ShowSkipButton = false;
      AutoUpdater.ShowRemindLaterButton = true;
      AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
      AutoUpdater.RemindLaterAt = 3;

      // Set application details
      AutoUpdater.AppTitle = "Work Order SDF Consolidator";
      AutoUpdater.RunUpdateAsAdmin = false;

      // Handle update events
      AutoUpdater.UpdateFormSize = new System.Drawing.Size(800, 600);
      AutoUpdater.ApplicationExitEvent += () => Environment.Exit(0);

      // Optional: Custom update form styling
      AutoUpdater.ReportErrors = true;
    }

    public static void CheckForUpdates(bool silent = false)
    {
      try
      {
        // URL to your update XML file on GitHub
        // This will be: https://raw.githubusercontent.com/HomesteadCabinet/WorkOrderBlender/main/update.xml
        string updateUrl = "https://raw.githubusercontent.com/HomesteadCabinet/WorkOrderBlender/main/update.xml";

        if (silent)
        {
          // Silent check - don't show UI if no updates
          AutoUpdater.CheckForUpdateEvent += (args) =>
          {
            if (!args.IsUpdateAvailable)
            {
              // No updates available, continue silently
              return;
            }

            // Update available - show the update dialog
            AutoUpdater.ShowUpdateForm(args);
          };
        }

        AutoUpdater.Start(updateUrl);
      }
      catch (Exception ex)
      {
        Log("Auto-update check failed", ex);
        if (!silent)
        {
          MessageBox.Show("Failed to check for updates: " + ex.Message, "Update Check",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
    }
    */

    // Temporary placeholder until AutoUpdater.NET is integrated
    public static void CheckForUpdates(bool silent = false)
    {
      if (!silent)
      {
        MessageBox.Show("Auto-update functionality will be available once AutoUpdater.NET is properly integrated.\n\nFor now, check the GitHub releases page for updates.",
          "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
    }
  }
}
