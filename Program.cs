using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
// AutoUpdater.NET removed - portable builds only
using System.Threading.Tasks;

namespace WorkOrderBlender
{
  internal static class Program
  {
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "WorkOrderBlender.log");
    public static readonly InMemoryEditStore Edits = new InMemoryEditStore();
    private static bool? cachedAllowFileLogging; // null = unknown; lazily resolved from settings.xml

    public static void Log(string message, Exception ex = null)
    {
      try
      {
        if (!IsFileLoggingEnabled()) return;
        if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);
        File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{(ex != null ? ex + Environment.NewLine : string.Empty)}");
      }
      catch { /* ignore logging errors */ }
    }

    // Determine whether file logging is enabled by reading AllowLogging from settings.xml.
    // Must not call Program.Log (avoid recursion); default is enabled when missing/unknown.
    private static bool IsFileLoggingEnabled()
    {
      try
      {
        if (cachedAllowFileLogging.HasValue) return cachedAllowFileLogging.Value;

        // Default to true for backward compatibility (older settings files won't have AllowLogging).
        var allow = true;

        // Check common config locations: alongside exe and LocalAppData copy.
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exeConfigPath = Path.Combine(baseDir, "settings.xml");
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userConfigPath = Path.Combine(localAppData, "WorkOrderBlender", "settings.xml");

        string xml = null;
        if (File.Exists(userConfigPath))
        {
          xml = SafeReadAllText(userConfigPath);
        }
        else if (File.Exists(exeConfigPath))
        {
          xml = SafeReadAllText(exeConfigPath);
        }

        if (!string.IsNullOrEmpty(xml))
        {
          // Very small/robust parse: look for <AllowLogging>true/false</AllowLogging> (case-insensitive).
          var idx = xml.IndexOf("<AllowLogging>", StringComparison.OrdinalIgnoreCase);
          if (idx >= 0)
          {
            var end = xml.IndexOf("</AllowLogging>", idx, StringComparison.OrdinalIgnoreCase);
            if (end > idx)
            {
              var innerStart = idx + "<AllowLogging>".Length;
              var inner = xml.Substring(innerStart, end - innerStart).Trim();
              if (bool.TryParse(inner, out var parsed))
              {
                allow = parsed;
              }
            }
          }
        }

        cachedAllowFileLogging = allow;
        return allow;
      }
      catch
      {
        cachedAllowFileLogging = true;
        return true;
      }
    }

    private static string SafeReadAllText(string path)
    {
      try { return File.ReadAllText(path); } catch { return null; }
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
        if (IsFileLoggingEnabled())
        {
          if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);
          if (File.Exists(LogFilePath))
          {
            File.Delete(LogFilePath);
          }
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
        // AutoUpdater removed - portable builds only

        // Create main form and defer update check until after UI is shown and idle
        var mainForm = new MainForm();
        mainForm.Shown += (s, e) =>
        {
          // Run after a short delay on a background thread to avoid UI stalls
          Task.Run(async () =>
          {
            await Task.Delay(4000);
            try { CheckForUpdates(silent: false, isStartupCheck: true); } catch { }
          });
        };

        Application.Run(mainForm);
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

        // Prepend bundled Git paths if present for portable updates
        var gitBase = Path.Combine(baseDir, "lib", "git");
        TryPrependToProcessPath(Path.Combine(gitBase, "cmd"));
        TryPrependToProcessPath(Path.Combine(gitBase, "bin"));
        TryPrependToProcessPath(Path.Combine(gitBase, "usr", "bin"));
        TryPrependToProcessPath(Path.Combine(gitBase, "mingw64", "bin"));

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
            // AutoUpdater.NET assembly loading removed - portable builds only
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

    // ConfigureAutoUpdater method removed - portable builds only

    public static async void CheckForUpdates(bool silent = false, bool isStartupCheck = false)
    {
      try
      {
        // Always check on startup (no rate limiting)
        var config = UserConfig.LoadOrDefault();

        Log("Checking for updates using portable update manager...");

        // Use the new portable update manager
        var updateInfo = await PortableUpdateManager.CheckForUpdatesAsync();

        if (updateInfo?.IsUpdateAvailable == true)
        {
          // Check if this version was skipped
          if (config.IsVersionSkipped(updateInfo.AvailableVersion))
          {
            Log($"Update {updateInfo.AvailableVersion} was skipped by user");
            return;
          }

          // Show the portable update dialog
          if (!silent)
          {
            Application.OpenForms[0]?.Invoke(new Action(() =>
            {
              using (var updateDialog = new PortableUpdateDialog(updateInfo))
              {
                updateDialog.ShowDialog();
              }
            }));
          }
        }
        else if (!silent && !isStartupCheck)
        {
          // Only show "no updates available" message for manual checks, not startup checks
          // Ensure we're on the UI thread
          if (Application.OpenForms.Count > 0)
          {
            Application.OpenForms[0]?.Invoke(new Action(() =>
            {
              MessageBox.Show("You are using the latest version.", "No Updates Available",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));
          }
          else
          {
            MessageBox.Show("You are using the latest version.", "No Updates Available",
              MessageBoxButtons.OK, MessageBoxIcon.Information);
          }
        }

        // Update last check time
        config.LastUpdateCheck = DateTime.Now;
        config.Save();
      }
      catch (Exception ex)
      {
        Log("Portable update check failed", ex);
        if (!silent)
        {
          // Ensure we're on the UI thread
          if (Application.OpenForms.Count > 0)
          {
            Application.OpenForms[0]?.Invoke(new Action(() =>
            {
              MessageBox.Show("Failed to check for updates: " + ex.Message + "\n\nPlease check your internet connection and try again.", "Update Check",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }));
          }
          else
          {
            MessageBox.Show("Failed to check for updates: " + ex.Message + "\n\nPlease check your internet connection and try again.", "Update Check",
              MessageBoxButtons.OK, MessageBoxIcon.Warning);
          }
        }
      }
    }
  }
}
