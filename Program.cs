using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
      // Ensure SQL CE native binaries and managed assemblies can be resolved from bundled lib/ folders
      try { ConfigureSqlCeProbing(); } catch { }

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

    // Configure probing for local SQL CE native and managed binaries to support portable (no-install) packaging
    private static void ConfigureSqlCeProbing()
    {
      try
      {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var arch = Environment.Is64BitProcess ? "amd64" : "x86";
        var nativeDir = Path.Combine(baseDir, "lib", arch);
        var managedDir = Path.Combine(baseDir, "lib");

        // Add native directory to DLL search path and process PATH
        TrySetDllDirectory(nativeDir);
        TryPrependToProcessPath(nativeDir);

        // Resolve System.Data.SqlServerCe.dll from local lib folder if not found
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
          try
          {
            if (e?.Name == null) return null;
            if (e.Name.StartsWith("System.Data.SqlServerCe", StringComparison.OrdinalIgnoreCase))
            {
              var path = Path.Combine(managedDir, "System.Data.SqlServerCe.dll");
              if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            if (e.Name.StartsWith("AutoUpdater.NET", StringComparison.OrdinalIgnoreCase))
            {
              var path = Path.Combine(managedDir, "AutoUpdater.NET.dll");
              if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
          }
          catch { }
          return null;
        };
      }
      catch { }
    }

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string lpPathName);

    private static void TrySetDllDirectory(string path)
    {
      try
      {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
          SetDllDirectory(path);
        }
      }
      catch { }
    }

    private static void TryPrependToProcessPath(string path)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        var existing = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var parts = existing.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (!parts.Any(p => string.Equals(p.Trim(), path, StringComparison.OrdinalIgnoreCase)))
        {
          Environment.SetEnvironmentVariable("PATH", path + ";" + existing, EnvironmentVariableTarget.Process);
        }
      }
      catch { }
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
