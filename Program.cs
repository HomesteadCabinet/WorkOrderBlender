using System;
using System.IO;
using System.Windows.Forms;
using AutoUpdaterDotNET;

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
        // Configure auto-updater
        ConfigureAutoUpdater();

        // Check for updates on startup (optional)
        CheckForUpdates(silent: true);

        Application.Run(new MainForm());
      }
      catch (Exception ex)
      {
        Log("Main crash", ex);
        MessageBox.Show("Startup error: " + ex.Message, "WorkOrderBlender", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

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
        // TODO: Uncomment when HomesteadCabinet/WorkOrderBlender repository is set up
        string updateUrl = "https://raw.githubusercontent.com/HomesteadCabinet/WorkOrderBlender/main/update.xml";

        // TEMPORARY: Using test URL to verify auto-updater functionality
        // string updateUrl = "https://raw.githubusercontent.com/ravibpatel/AutoUpdater.NET/master/AutoUpdaterTest/update.xml";

        // Configure AutoUpdater for this check
        AutoUpdater.Synchronous = true; // Make it synchronous for better error handling

        // Store the silent flag for use in event handlers
        bool isSilentCheck = silent;

        // Set up event handler for update check results
        AutoUpdater.CheckForUpdateEvent += (args) =>
        {
          try
          {
            if (args.IsUpdateAvailable)
            {
              // Update available - show the update dialog
              AutoUpdater.ShowUpdateForm(args);
            }
            else if (!isSilentCheck)
            {
              // Manual check and no updates - inform user
              MessageBox.Show("You are using the latest version.", "No Updates Available",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            // If silent and no updates, do nothing
          }
          catch (Exception ex)
          {
            Log("Error in CheckForUpdateEvent handler", ex);
            if (!isSilentCheck)
            {
              MessageBox.Show("Error checking for updates: " + ex.Message, "Update Check Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
          }
        };

        Log($"Checking for updates from: {updateUrl}");
        AutoUpdater.Start(updateUrl);
      }
      catch (Exception ex)
      {
        Log("Auto-update check failed", ex);
        if (!silent)
        {
          MessageBox.Show("Failed to check for updates: " + ex.Message + "\n\nPlease check your internet connection and try again.", "Update Check",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
    }
  }
}
