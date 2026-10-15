using System;
using System.Collections.Generic;
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

    [DllImport("kernel32")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool AttachConsole(int dwProcessId);

    [STAThread]
    private static void Main(string[] args)
    {
      // Ensure SQL CE native binaries and managed assemblies can be resolved from bundled lib/ folders
      try { ConfigureSqlCeProbing(); } catch { }

      // Headless mode: if arguments are provided, run without UI and exit
      if (args.Length > 0)
      {
        // Attach to the parent console (e.g. cmd.exe / PowerShell) so output is visible.
        // Fall back to allocating a new console window if there is no parent.
        if (!AttachConsole(-1))
          AllocConsole();

        // Redirect Console.Out / Console.Error to the now-attached console
        Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new System.IO.StreamWriter(Console.OpenStandardError()) { AutoFlush = true });

        int exitCode = RunHeadless(args);
        Environment.Exit(exitCode);
        return;
      }

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

    // Headless (CLI) consolidation mode.
    // Usage: WorkOrderBlender.exe --output <name> <workorder1> [workorder2 ...]
    //        WorkOrderBlender.exe <workorder1> [workorder2 ...] --output <name>
    // The work order names are resolved as subdirectories under DefaultRoot from settings.xml.
    private static int RunHeadless(string[] args)
    {
      string outputName = null;
      var workOrders = new List<string>();

      for (int i = 0; i < args.Length; i++)
      {
        if ((args[i] == "--output" || args[i] == "-o") && i + 1 < args.Length)
        {
          outputName = args[++i];
        }
        else if (!args[i].StartsWith("-"))
        {
          workOrders.Add(args[i]);
        }
        else
        {
          Console.Error.WriteLine($"Unknown option: {args[i]}");
          PrintHeadlessUsage();
          return 1;
        }
      }

      if (string.IsNullOrWhiteSpace(outputName))
      {
        Console.Error.WriteLine("Error: --output <name> is required.");
        PrintHeadlessUsage();
        return 1;
      }

      if (workOrders.Count == 0)
      {
        Console.Error.WriteLine("Error: at least one work order name is required.");
        PrintHeadlessUsage();
        return 1;
      }

      if (outputName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
      {
        Console.Error.WriteLine("Error: output name contains invalid file system characters.");
        return 1;
      }

      var config = UserConfig.LoadOrDefault();
      string root = config.DefaultRoot ?? @"M:\Homestead_Library\Work Orders";
      string sdfFile = !string.IsNullOrWhiteSpace(config.SdfFileName) ? config.SdfFileName : "MicrovellumWorkOrder.sdf";

      // Resolve source SDF paths
      var sources = new List<(string WorkOrderDir, string SdfPath)>();
      foreach (var wo in workOrders)
      {
        string dir = Path.Combine(root, wo);
        string sdf = Path.Combine(dir, sdfFile);
        if (!File.Exists(sdf))
        {
          Console.Error.WriteLine($"Error: '{sdf}' not found for work order '{wo}'.");
          return 1;
        }
        sources.Add((dir, sdf));
      }

      string destDir = Path.Combine(root, outputName);
      string destPath = Path.Combine(destDir, sdfFile);

      try
      {
        Directory.CreateDirectory(destDir);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error: could not create output directory '{destDir}': {ex.Message}");
        return 1;
      }

      Console.WriteLine($"Consolidating {sources.Count} work order(s) into '{outputName}'...");

      int step = 0;
      try
      {
        MainForm.RunConsolidation(sources, destPath, _ =>
        {
          step++;
          Console.WriteLine($"  [{step}/{sources.Count}] {new DirectoryInfo(sources[step - 1].WorkOrderDir).Name}");
        });
      }
      catch (MainForm.ConsolidationException cex)
      {
        if (cex.IsPartialSuccess)
        {
          Console.Error.WriteLine($"Warning: consolidation completed with errors:\n{cex.Message}");
          Console.WriteLine($"Output (partial): {destPath}");
          return 2; // partial success
        }
        Console.Error.WriteLine($"Error: consolidation failed:\n{cex.Message}");
        return 1;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error: {MainForm.GetDetailedErrorMessage(ex)}");
        return 1;
      }

      Console.WriteLine($"Done. Output: {destPath}");
      return 0;
    }

    private static void PrintHeadlessUsage()
    {
      Console.Error.WriteLine("Usage: WorkOrderBlender.exe --output <name> <workorder1> [workorder2 ...]");
      Console.Error.WriteLine("       WorkOrderBlender.exe <workorder1> [workorder2 ...] --output <name>");
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
