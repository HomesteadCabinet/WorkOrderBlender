using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkOrderBlender
{
  public partial class MainForm : Form
  {
    private const string DefaultRoot = @"M:\Homestead_Library\Work Orders";
    private const string DefaultOutput = @"M:\Homestead_Library\Work Orders";
    private const string SdfFileName = "MicrovellumWorkOrder.sdf";
    private UserConfig userConfig;
    private readonly List<WorkOrderEntry> allWorkOrders = new List<WorkOrderEntry>();
    private List<WorkOrderEntry> filteredWorkOrders = new List<WorkOrderEntry>();
    private readonly HashSet<string> checkedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly (string Label, string Table)[] breakdownMetrics = new (string, string)[]
    {
      ("Products", "Products"),
      ("Parts", "Parts"),
      ("Subassemblies", "Subassemblies"),
      ("Sheets", "Sheets"),
      ("Materials", "Materials"),
      ("Hardware", "Hardware"),
    };

    // Debug switch to log detailed insert failures
    private const bool EnableCopyDebug = true;

    private CancellationTokenSource debounceCts;

    // Integrated metrics functionality (directly in MainForm)
    private string currentSelectedTable;
    private List<string> currentSourcePaths = new List<string>();
    private bool isApplyingLayout = false;
    private bool gridEventsWired = false;
    private bool isEditModeMainGrid = false; // tracks edit mode for metricsGrid
    private System.Windows.Forms.Timer orderPersistTimer;
    private DateTime lastOrderChangeUtc;
    // Virtual columns state
    private List<UserConfig.VirtualColumnDef> virtualColumnDefs = new List<UserConfig.VirtualColumnDef>();
    private readonly Dictionary<string, Dictionary<string, object>> virtualLookupCacheByColumn = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> virtualColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    // Refresh coalescing
    private System.Windows.Forms.Timer metricsRefreshTimer;
    private DateTime lastRefreshRequestUtc;
    private bool isRefreshingMetrics;
    private bool suppressTableSelectorChanged;
    private bool isScanningWorkOrders;

    public MainForm()
    {
      InitializeComponent();

      // Set window title with version number
      var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
      this.Text = $"Work Order Blender v{version.Major}.{version.Minor}.{version.Build}";

      // // Force the first column width of mainLayoutTable to 300px
      // if (mainLayoutTable.ColumnStyles.Count > 0)
      // {
      //   mainLayoutTable.ColumnStyles[0].Width = 30;
      // }
      // else
      // {
      //   mainLayoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
      // }

      // Set the application icon
      try
      {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "icon.ico");
        if (File.Exists(iconPath))
        {
          this.Icon = new System.Drawing.Icon(iconPath);
        }
        else
        {
          // Try embedded resource as fallback
          var iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WorkOrderBlender.resources.icon.ico");
          if (iconStream != null)
          {
            this.Icon = new System.Drawing.Icon(iconStream);
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Failed to load application icon", ex);
      }

      // Load user config once and cache it
      try
      {
        userConfig = UserConfig.Load(DefaultRoot, DefaultOutput, SdfFileName);
      }
      catch
      {
        userConfig = UserConfig.LoadOrDefault();
        if (string.IsNullOrWhiteSpace(userConfig.DefaultRoot)) userConfig.DefaultRoot = DefaultRoot;
        if (string.IsNullOrWhiteSpace(userConfig.DefaultOutput)) userConfig.DefaultOutput = DefaultOutput;
        if (string.IsNullOrWhiteSpace(userConfig.SdfFileName)) userConfig.SdfFileName = SdfFileName;
      }

      // Show work order name field
      txtOutput.Text = userConfig.WorkOrderName ?? string.Empty;
      // Virtualize the big list for performance
      this.listWorkOrders.VirtualMode = true;
      this.listWorkOrders.RetrieveVirtualItem += listWorkOrders_RetrieveVirtualItem;
      // Use custom checkbox via StateImageList (CheckBoxes + VirtualMode toggling is unreliable)
      this.listWorkOrders.CheckBoxes = false;
      this.listWorkOrders.StateImageList = CreateCheckStateImageList();
      this.listWorkOrders.MouseDown += listWorkOrders_MouseDown;
      this.listWorkOrders.KeyDown += listWorkOrders_KeyDown;
      this.Shown += (s, e) =>
      {
        try
        {
          // Use synchronous loading instead of async
          ScanWorkOrders();
        }
        catch (Exception ex)
        {
          Program.Log("Error during initial scan", ex);
          MessageBox.Show("Error during initial scan: " + ex.Message);
        }
      };

      // Apply saved window size and splitter distance after form is fully loaded and sized
      this.Load += (s, e) =>
      {
        try
        {
          var cfg = UserConfig.LoadOrDefault();
          // Apply window size if saved
          try
          {
            if (cfg.MainWindowWidth > 0 && cfg.MainWindowHeight > 0)
            {
              // Clamp to at least minimum size
              int w = Math.Max(this.MinimumSize.Width, cfg.MainWindowWidth);
              int h = Math.Max(this.MinimumSize.Height, cfg.MainWindowHeight);
              this.Size = new System.Drawing.Size(w, h);
            }
          }
          catch { }
          int desired = cfg.MainSplitterDistance > 0 ? cfg.MainSplitterDistance : 300;

          // Wait for next tick to ensure layout is complete
          this.BeginInvoke(new Action(() =>
          {
            try
            {
              // Clamp to valid range based on current size
              int min = Math.Max(0, splitMain.Panel1MinSize);
              int max = splitMain.Width - splitMain.Panel2MinSize - splitMain.SplitterWidth;
              if (max < min) max = min; // guard for tiny width at startup
              int clamped = Math.Min(Math.Max(desired, min), max);
              if (clamped != splitMain.SplitterDistance) splitMain.SplitterDistance = clamped;
            }
            catch (Exception ex)
            {
              Program.Log("Error setting splitter distance", ex);
            }
          }));
        }
        catch (Exception ex)
        {
          Program.Log("Error loading splitter distance", ex);
        }
      };
      this.FormClosing += MainForm_FormClosing;

      // Persist window size on resize end (avoid spamming saves during drag)
      this.ResizeEnd += (s, e) =>
      {
        try
        {
          var cfg = UserConfig.LoadOrDefault();
          cfg.MainWindowWidth = this.Width;
          cfg.MainWindowHeight = this.Height;
          cfg.Save();
        }
        catch { }
      };

      // Persist splitter distance when user adjusts it
      try
      {
        this.splitMain.SplitterMoved += (s, e) =>
        {
          try
          {
            var cfg = UserConfig.LoadOrDefault();
            cfg.MainSplitterDistance = Math.Max(splitMain.Panel1MinSize, splitMain.SplitterDistance);
            cfg.Save();
          }
          catch { }
        };
      }
      catch { }

      // Setup metrics grid refresh debounce
      metricsRefreshTimer = new System.Windows.Forms.Timer();
      metricsRefreshTimer.Interval = 120; // ms to coalesce repeated requests
      metricsRefreshTimer.Tick += (s, e2) =>
      {
        if ((DateTime.UtcNow - lastRefreshRequestUtc).TotalMilliseconds >= metricsRefreshTimer.Interval)
        {
          metricsRefreshTimer.Stop();
          if (!isRefreshingMetrics)
          {
            RefreshMetricsGrid();
          }
        }
      };

      // Subscribe to changes in the edit store
      Program.Edits.ChangesUpdated += (s, e) =>
      {
        if (InvokeRequired)
          Invoke(new Action(UpdatePreviewChangesButton));
        else
          UpdatePreviewChangesButton();
      };

      // Initialize button state
      UpdatePreviewChangesButton();
    }

    // SQL CE connection helpers are centralized in SqlCeUtils

    private string GetSdfFileName()
    {
      var name = userConfig != null ? (userConfig.SdfFileName ?? string.Empty).Trim() : string.Empty;
      return string.IsNullOrEmpty(name) ? SdfFileName : name;
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      try
      {
        // Reload latest config to avoid overwriting metrics dialog persisted settings
        var cfg = UserConfig.LoadOrDefault();
        cfg.DefaultRoot = DefaultRoot;
        try
        {
          var workOrderName = (txtOutput.Text ?? string.Empty).Trim();
          if (!string.IsNullOrEmpty(workOrderName)) cfg.WorkOrderName = workOrderName;
        }
        catch { }
        // File name is fixed; do not update cfg.SdfFileName here
        try
        {
          // Persist most recent window size and splitter distance on close
          cfg.MainWindowWidth = this.Width;
          cfg.MainWindowHeight = this.Height;
          try { cfg.MainSplitterDistance = Math.Max(0, splitMain?.SplitterDistance ?? cfg.MainSplitterDistance); } catch { }
        }
        catch { }
        cfg.Save();
        userConfig = cfg;
      }
      catch { }
    }

    private void listWorkOrders_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
    {
      int idx = e.ItemIndex;
      if (idx < 0 || idx >= filteredWorkOrders.Count)
      {
        e.Item = new ListViewItem("");
        return;
      }
      var wo = filteredWorkOrders[idx];
      var item = new ListViewItem(GetDisplayPath(wo.DirectoryPath));
      item.Tag = wo.SdfPath;
      item.StateImageIndex = checkedDirs.Contains(wo.DirectoryPath) ? 1 : 0; // 1=checked, 0=unchecked
      e.Item = item;
    }

    private void listWorkOrders_MouseDown(object sender, MouseEventArgs e)
    {
      var hit = listWorkOrders.HitTest(e.Location);
      int idx = hit.Item?.Index ?? -1;
      if (idx >= 0 && idx < filteredWorkOrders.Count)
      {
        // Toggle when clicking near the state image or first column
        if (hit.Location == ListViewHitTestLocations.StateImage || hit.SubItem == hit.Item.SubItems[0])
        {
          var dir = filteredWorkOrders[idx].DirectoryPath;
          if (checkedDirs.Contains(dir)) checkedDirs.Remove(dir); else checkedDirs.Add(dir);
          listWorkOrders.Invalidate(hit.Item.Bounds);
          RequestRefreshMetricsGrid();
        }
      }
    }

    private void listWorkOrders_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Space && listWorkOrders.SelectedIndices.Count > 0)
      {
        foreach (int idx in listWorkOrders.SelectedIndices)
        {
          if (idx < 0 || idx >= filteredWorkOrders.Count) continue;
          var dir = filteredWorkOrders[idx].DirectoryPath;
          if (checkedDirs.Contains(dir)) checkedDirs.Remove(dir); else checkedDirs.Add(dir);
        }
        listWorkOrders.Invalidate();
        RequestRefreshMetricsGrid();
        e.Handled = true;
      }
    }

    private ImageList CreateCheckStateImageList()
    {
      var imgs = new ImageList
      {
        ImageSize = new System.Drawing.Size(16, 16)
      };
      // unchecked
      var bmp0 = new System.Drawing.Bitmap(16, 16);
      using (var g = System.Drawing.Graphics.FromImage(bmp0))
      {
        g.Clear(System.Drawing.Color.Transparent);
        var rect = new System.Drawing.Rectangle(1, 1, 14, 14);
        g.DrawRectangle(System.Drawing.Pens.Gray, rect);
      }
      imgs.Images.Add(bmp0);
      // checked
      var bmp1 = new System.Drawing.Bitmap(16, 16);
      using (var g = System.Drawing.Graphics.FromImage(bmp1))
      {
        g.Clear(System.Drawing.Color.Transparent);
        var rect = new System.Drawing.Rectangle(1, 1, 14, 14);
        g.DrawRectangle(System.Drawing.Pens.Gray, rect);
        var oldSmoothing = g.SmoothingMode;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var thickPen = new System.Drawing.Pen(System.Drawing.Color.Green, 3))
        {
          g.DrawLine(thickPen, 3, 6, 7, 13);
          g.DrawLine(thickPen, 7, 11, 13, 3);
        }
        g.SmoothingMode = oldSmoothing;
      }
      imgs.Images.Add(bmp1);
      return imgs;
    }

    private string GetDisplayPath(string absoluteDir)
    {
      if (string.IsNullOrEmpty(absoluteDir)) return absoluteDir;
      string root = string.Empty;
      try { root = DefaultRoot; }
      catch { }
      if (string.IsNullOrEmpty(root)) return absoluteDir;
      string absNorm = absoluteDir.Replace('/', '\\');
      string rootNorm = root.Replace('/', '\\').TrimEnd('\\');
      if (absNorm.StartsWith(rootNorm, StringComparison.OrdinalIgnoreCase))
      {
        string rel = absNorm.Substring(rootNorm.Length).TrimStart('\\');
        return rel;
      }
      return absoluteDir;
    }



    private void ScanWorkOrders()
    {
      isScanningWorkOrders = true;
      var scanStartTime = DateTime.Now;
      Program.Log($"=== SCAN STARTED at {scanStartTime:HH:mm:ss.fff} ===");

      SetBusy(true);
      listWorkOrders.Items.Clear();
      string root = DefaultRoot;
      Program.Log($"Scanning root directory: {root}");

      if (!Directory.Exists(root))
      {
        Program.Log($"Root directory not found: {root}");
        MessageBox.Show("Root directory not found.");
        SetBusy(false);
        return;
      }

      try
      {
        // Get all directories first
        var dirScanStart = DateTime.Now;
        Program.Log($"Starting directory enumeration at {dirScanStart:HH:mm:ss.fff}");

        var dirs = Directory.GetDirectories(root);
        var totalDirs = dirs.Length;
        var dirScanEnd = DateTime.Now;
        var dirScanDuration = dirScanEnd - dirScanStart;

        Program.Log($"Directory enumeration completed in {dirScanDuration.TotalMilliseconds:F0}ms - Found {totalDirs} directories");

        // Update progress bar for scanning
        progress.Maximum = totalDirs;
        progress.Value = 0;
        progress.Style = ProgressBarStyle.Continuous;

        var results = new List<WorkOrderEntry>();
          var completedCount = 0;
          var startTime = DateTime.Now;
          var totalFileSize = 0L;
          var processedFileSize = 0L;

                 // Simple progress tracking - update UI during processing
            var lastProgressLog = DateTime.Now;

                 foreach (var dir in dirs)
            {
              try
              {
             string sdfPath = Path.Combine(dir, GetSdfFileName());
             bool exists = false;
             long fileSize = 0;

             try
             {
               exists = File.Exists(sdfPath);
               if (exists)
               {
                 var fileInfo = new FileInfo(sdfPath);
                 fileSize = fileInfo.Length;
               }
             }
             catch (Exception ex) { Program.Log($"File.Exists failed for {sdfPath}", ex); }

             var entry = new WorkOrderEntry
             {
               DirectoryPath = dir,
               SdfPath = sdfPath,
               SdfExists = exists,
               FileSize = fileSize
             };

             results.Add(entry);
             completedCount++;
             if (exists && fileSize > 0)
             {
               totalFileSize += fileSize;
               processedFileSize += fileSize;
             }

             // Update progress bar and status
             if (InvokeRequired)
             {
               Invoke(new Action(() =>
               {
                 progress.Value = completedCount;
                 var elapsed = DateTime.Now - startTime;
                 var remaining = totalDirs - completedCount;

                 if (completedCount > 0 && remaining > 0)
                 {
                   // Calculate time estimates
                   var avgTimePerItem = elapsed.TotalMilliseconds / completedCount;
                   var estimatedRemaining = TimeSpan.FromMilliseconds(avgTimePerItem * remaining);
                    var totalEstimated = elapsed + estimatedRemaining;

                    // Format file size information
                    var fileSizeInfo = totalFileSize > 0 ?
                      $" ({FormatFileSize(processedFileSize)}/{FormatFileSize(totalFileSize)})" : "";

                   // Update progress bar text
                    var progressText = $"Scanning work orders... {completedCount}/{totalDirs} ({completedCount * 100 / totalDirs}%){fileSizeInfo} - Est. completion: {totalEstimated:hh\\:mm\\:ss}";
                    progress.Tag = progressText;

                   // Update loading label
                   lblLoading.Text = progressText;

                   // Update tooltip
                      var toolTip = new ToolTip();
                      toolTip.SetToolTip(progress, progressText);
                    }
               }));
                  }

             // Log progress every 2 seconds
                var now = DateTime.Now;
                if ((now - lastProgressLog).TotalSeconds >= 2.0)
                {
                  var elapsed = DateTime.Now - startTime;
                  var remaining = totalDirs - completedCount;
                  var avgTimePerItem = elapsed.TotalMilliseconds / completedCount;
                  var estimatedRemaining = TimeSpan.FromMilliseconds(avgTimePerItem * remaining);

                  Program.Log($"Progress: {completedCount}/{totalDirs} ({completedCount * 100 / totalDirs}%) - Elapsed: {elapsed:hh\\:mm\\:ss} - Est. remaining: {estimatedRemaining:hh\\:mm\\:ss}");
                  lastProgressLog = now;
                }

                    if (exists)
                    {
               Program.Log($"Processed {Path.GetFileName(dir)} - SDF: {FormatFileSize(fileSize)}");
                  }
                  else
                  {
               Program.Log($"Processed {Path.GetFileName(dir)} - No SDF found");
             }
           }
              catch (Exception ex)
              {
             Program.Log($"Error processing directory {dir}", ex);
             completedCount++;
           }
         }

                 // Progress tracking completed

          var parallelEnd = DateTime.Now;
        var parallelDuration = parallelEnd - startTime;
          Program.Log($"Parallel processing completed in {parallelDuration.TotalMilliseconds:F0}ms");
        Program.Log($"Processed {results.Count} work orders, total file size: {FormatFileSize(totalFileSize)}");

        // Update UI / state
        allWorkOrders.Clear();
        allWorkOrders.AddRange(results);
        Program.Log($"Updated work order list with {results.Count} entries");

        // Build initial filtered list
         FilterWorkOrders();

         // Update button state after loading
         UpdatePreviewChangesButton();

        if (checkedDirs.Count > 0)
        {
          var metricsStart = DateTime.Now;
          Program.Log($"Starting metrics grid refresh at {metricsStart:HH:mm:ss.fff}");

          RefreshMetricsGrid();

          var metricsEnd = DateTime.Now;
          var metricsDuration = metricsEnd - metricsStart;
          Program.Log($"Metrics grid refresh completed in {metricsDuration.TotalMilliseconds:F0}ms");
        }
      }
      catch (OperationCanceledException) { }
      catch (Exception ex)
      {
        Program.Log("ScanAsync error", ex);
        MessageBox.Show("Scan error: " + ex.Message);
      }
      finally
      {
        var scanEndTime = DateTime.Now;
        var totalScanDuration = scanEndTime - scanStartTime;
        Program.Log($"=== SCAN COMPLETED at {scanEndTime:HH:mm:ss.fff} - Total duration: {totalScanDuration:hh\\:mm\\:ss\\.fff} ===");

        SetBusy(false);
        progress.Style = ProgressBarStyle.Continuous;
        progress.Value = 0;
        isScanningWorkOrders = false;
        RequestRefreshMetricsGrid();
      }
    }

    // legacy handler no longer used

    private void btnSelectAll_Click(object sender, EventArgs e)
    {
      foreach (var wo in filteredWorkOrders)
      {
        if (wo.SdfExists)
        {
          checkedDirs.Add(wo.DirectoryPath);
        }
      }
      UpdatePreviewChangesButton();
      listWorkOrders.Invalidate();
      RefreshMetricsGrid();
    }

    // Add refresh button functionality
    private void btnRefresh_Click(object sender, EventArgs e)
    {
      try
      {
        ScanWorkOrders();
      }
      catch (Exception ex)
      {
        Program.Log("Error refreshing work orders", ex);
        MessageBox.Show("Error refreshing work orders: " + ex.Message);
      }
    }



    private void btnConsolidate_Click(object sender, EventArgs e)
    {
      var selected = filteredWorkOrders
        .Where(wo => checkedDirs.Contains(wo.DirectoryPath) && File.Exists(wo.SdfPath))
        .Select(wo => new { Directory = wo.DirectoryPath, SdfPath = wo.SdfPath })
        .ToList();

      if (selected.Count == 0)
      {
        MessageBox.Show("No work orders selected.");
        return;
      }

      string workOrderName = txtOutput.Text.Trim();
      if (string.IsNullOrEmpty(workOrderName))
      {
        MessageBox.Show("Please enter a work order name.");
        return;
      }

      // Validate work order name for file system safety
      if (workOrderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
      {
        MessageBox.Show("Work order name contains invalid characters. Please use only letters, numbers, spaces, and basic punctuation.");
        return;
      }

      // Create subfolder in DefaultRoot based on work order name
      string workOrderDir = Path.Combine(userConfig.DefaultRoot ?? DefaultRoot, workOrderName);
      string destPath = Path.Combine(workOrderDir, SdfFileName);

      // Create the work order directory if it doesn't exist
      try
      {
        Directory.CreateDirectory(workOrderDir);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Failed to create work order directory: {ex.Message}");
        return;
      }

      // Check if destination file already exists and prompt user
      if (File.Exists(destPath))
      {
        var result = MessageBox.Show(
          $"The file '{SdfFileName}' already exists in work order '{workOrderName}'.\n\nDo you want to replace it?",
          "File Exists",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question,
          MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
          return; // User chose not to replace
        }
      }

      // Save the work order name to user config
      try
      {
        var cfg = UserConfig.LoadOrDefault();
        cfg.WorkOrderName = workOrderName;
        cfg.Save();
        userConfig = cfg;
      }
      catch { }

      try
      {
        RunConsolidation(selected.Select(s => (s.Directory, s.SdfPath)).ToList(), destPath);
        MessageBox.Show($"Consolidation complete. File created at:\n{destPath}");
      }
      catch (Exception ex)
      {
        Program.Log("Consolidation error", ex);
        MessageBox.Show("Error: " + ex.Message);
      }
    }

    private void UpdatePreviewChangesButton()
    {
      try
      {
        var changeCount = Program.Edits.GetTotalChangeCount();
        if (changeCount == 0)
        {
          btnPreviewChanges.Text = "Pending Changes";
          btnPreviewChanges.Enabled = false;
        }
        else
        {
          btnPreviewChanges.Text = $"Pending Changes ({changeCount})";
          btnPreviewChanges.Enabled = true;
        }
      }
      catch (Exception ex)
      {
        Program.Log("UpdatePreviewChangesButton error", ex);
        btnPreviewChanges.Text = "Pending Changes";
        btnPreviewChanges.Enabled = false;
      }
    }

    private void btnPreviewChanges_Click(object sender, EventArgs e)
    {
      try
      {
        if (!Program.Edits.HasAnyChanges())
        {
          MessageBox.Show("No pending changes found.", "Pending Changes",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        using (var dialog = new PendingChangesDialog())
        {
          dialog.ShowDialog(this);
          // Update button after dialog closes in case changes were cleared
          UpdatePreviewChangesButton();
        }
      }
      catch (Exception ex)
      {
        Program.Log("btnPreviewChanges_Click error", ex);
        MessageBox.Show("Failed to open pending changes preview: " + ex.Message, "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
      // debounce to avoid filtering on every keystroke for thousands of items
      debounceCts?.Cancel();
      debounceCts = new CancellationTokenSource();
      var token = debounceCts.Token;
      Task.Run(async () =>
      {
        try
        {
          await Task.Delay(250, token);
          if (token.IsCancellationRequested) return;

          // Use synchronous filtering for better performance
          if (InvokeRequired)
          {
            Invoke(new Action(FilterWorkOrders));
          }
          else
          {
            FilterWorkOrders();
          }
        }
        catch { }
      });
    }

    private void btnSettings_Click(object sender, EventArgs e)
    {
      OpenSettingsDialog();
    }



    private void OpenSettingsDialog()
    {
      var dlg = new Form
      {
        Text = "Settings",
        StartPosition = FormStartPosition.CenterParent,
        Width = 700,
        Height = 250,
        MinimizeBox = false,
        MaximizeBox = false,
        FormBorderStyle = FormBorderStyle.FixedDialog
      };

      var table = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 4,
        RowCount = 3,
        Padding = new Padding(10)
      };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      dlg.Controls.Add(table);

      var lblRoot = new Label { Text = "Work Order Directory:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtRootLocal = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 400, Text = DefaultRoot };
      var btnBrowseRoot = new Button { Text = "Browse...", AutoSize = true };
      btnBrowseRoot.Click += (s, e) =>
      {
        using (var fbd = new FolderBrowserDialog())
        {
          fbd.SelectedPath = txtRootLocal.Text;
          if (fbd.ShowDialog(dlg) == DialogResult.OK)
          {
            txtRootLocal.Text = fbd.SelectedPath;
          }
        }
      };

      // Output is now based on work order name + DefaultRoot, so no need for separate output folder setting

      var lblSdf = new Label { Text = ".sdf File Name:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtSdfLocal = new TextBox
      {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Width = 400,
        Text = GetSdfFileName(),
        ReadOnly = true
      };

      table.Controls.Add(lblRoot, 0, 0);
      table.Controls.Add(txtRootLocal, 1, 0);
      table.Controls.Add(btnBrowseRoot, 2, 0);

      table.Controls.Add(lblSdf, 0, 1);
      table.Controls.Add(txtSdfLocal, 1, 1);

      // Add Check Updates button
      // var lblUpdates = new Label { Text = "Application:", AutoSize = true, Anchor = AnchorStyles.Left };
      var btnCheckUpdates = new Button
      {
        Text = "Check for Updates",
        AutoSize = true,
        Anchor = AnchorStyles.Left
      };
      btnCheckUpdates.Click += (s, e) =>
      {
        try
        {
          Program.CheckForUpdates(silent: false);
        }
        catch (Exception ex)
        {
          Program.Log("Manual update check failed", ex);
          MessageBox.Show("Failed to check for updates: " + ex.Message, "Update Check",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      };

      // table.Controls.Add(lblUpdates, 0, 3);
      // table.Controls.Add(btnCheckUpdates, 1, 3);

      // Create a table layout for buttons with precise positioning
      var panelButtons = new TableLayoutPanel
      {
        Dock = DockStyle.Bottom,
        Height = 50,
        Padding = new Padding(10),
        ColumnCount = 3,
        RowCount = 1
      };

      // Set up columns: Check Updates (left), spacer (stretch), OK+Cancel (right)
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Check Updates
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Spacer
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // OK+Cancel
      panelButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

      var btnOk = new Button
      {
        Text = "OK",
        DialogResult = DialogResult.OK,
        AutoSize = true
      };
      var btnCancel = new Button
      {
        Text = "Cancel",
        DialogResult = DialogResult.Cancel,
        AutoSize = true
      };

      // Create a panel for OK+Cancel buttons (right side)
      var rightButtonPanel = new FlowLayoutPanel
      {
        FlowDirection = FlowDirection.RightToLeft,
        AutoSize = true,
        Margin = new Padding(0)
      };
      rightButtonPanel.Controls.Add(btnOk);
      rightButtonPanel.Controls.Add(btnCancel);

      // Position the buttons: Check Updates on far left, OK+Cancel on right
      panelButtons.Controls.Add(btnCheckUpdates, 0, 0); // Far left
      panelButtons.Controls.Add(rightButtonPanel, 2, 0); // Far right

      dlg.Controls.Add(panelButtons);
      dlg.AcceptButton = btnOk;
      dlg.CancelButton = btnCancel;

      if (dlg.ShowDialog(this) == DialogResult.OK)
      {
        try
        {
          if (userConfig == null) userConfig = new UserConfig();
          userConfig.DefaultRoot = (txtRootLocal.Text ?? string.Empty).Trim();
          userConfig.SdfFileName = (txtSdfLocal.Text ?? string.Empty).Trim();
          userConfig.Save();

          // txtOutput now shows work order name, not output folder
        }
        catch (Exception ex)
        {
          Program.Log("Saving settings failed", ex);
          MessageBox.Show("Failed to save settings: " + ex.Message);
        }
      }
    }

    private void FilterWorkOrders()
    {
      string query = string.Empty;
      if (InvokeRequired)
      {
        Invoke(new Action(() => query = txtSearch.Text.Trim()));
      }
      else
      {
        query = txtSearch.Text.Trim();
      }

      var next = allWorkOrders.Where(wo => string.IsNullOrEmpty(query) || wo.DirectoryPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                 .ToList();

      filteredWorkOrders = next;
      listWorkOrders.VirtualListSize = filteredWorkOrders.Count;
      listWorkOrders.Invalidate();
    }

    private async Task FilterAsyncInternal(CancellationToken token)
    {
      string query = string.Empty;
      Invoke(new Action(() => query = txtSearch.Text.Trim()));

      var next = await Task.Run(() =>
      {
        return allWorkOrders.Where(wo => string.IsNullOrEmpty(query) || wo.DirectoryPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                   .ToList();
      }, token);

      if (token.IsCancellationRequested) return;

      filteredWorkOrders = next;
      listWorkOrders.VirtualListSize = filteredWorkOrders.Count;
      listWorkOrders.Invalidate();
    }

    // removed legacy ItemCheck handler; custom state image toggling is used instead

    private void listWorkOrders_SelectedIndexChanged(object sender, EventArgs e)
    {
      try
      {
        PopulateTableSelector();
        RequestRefreshMetricsGrid();
      }
      catch { }
    }

    private void RequestRefreshMetricsGrid()
    {
      lastRefreshRequestUtc = DateTime.UtcNow;
      if (!metricsRefreshTimer.Enabled)
      {
        metricsRefreshTimer.Start();
      }
    }

    private void PopulateTableSelector()
    {
      try
      {
        suppressTableSelectorChanged = true;
        try
        {
          cmbTableSelector.Items.Clear();

          // Add table options from breakdownMetrics
          foreach (var (label, table) in breakdownMetrics)
          {
            var item = new TableSelectorItem { Label = label, TableName = table };
            cmbTableSelector.Items.Add(item);
          }

          // Set default selection
          if (cmbTableSelector.Items.Count > 0)
          {
            cmbTableSelector.SelectedIndex = 0;
          }
        }
        finally { suppressTableSelectorChanged = false; }
      }
      catch (Exception ex)
      {
        Program.Log("Error populating table selector", ex);
      }
    }

    private void cmbTableSelector_SelectedIndexChanged(object sender, EventArgs e)
    {
      try
      {
        if (suppressTableSelectorChanged) return;
        RequestRefreshMetricsGrid();
      }
      catch (Exception ex)
      {
        Program.Log("Error in table selector changed", ex);
      }
    }

        private void RefreshMetricsGrid()
    {
      var refreshStart = DateTime.Now;
      var caller = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
      Program.Log($"=== REFRESH METRICS GRID STARTED at {refreshStart:HH:mm:ss.fff} - Called by: {caller} ===");

      try
      {
        // Show loading indicator
        ShowLoadingIndicator(true);

        // Ensure table selector is initialized and has a default selection
        if (cmbTableSelector.Items.Count == 0)
        {
          PopulateTableSelector();
        }
        if (cmbTableSelector.SelectedIndex < 0 && cmbTableSelector.Items.Count > 0)
        {
          cmbTableSelector.SelectedIndex = 0;
        }

        if (cmbTableSelector.SelectedItem is TableSelectorItem selectedTable)
        {
          var sourcePathsStart = DateTime.Now;
          currentSelectedTable = selectedTable.TableName;

          // Get source paths
          List<string> sourcePaths;
          if (checkedDirs.Count > 0)
          {
            // Use checked work orders
            sourcePaths = filteredWorkOrders
              .Where(wo => checkedDirs.Contains(wo.DirectoryPath) && File.Exists(wo.SdfPath))
              .Select(wo => wo.SdfPath)
              .ToList();
          }
          else if (listWorkOrders.SelectedIndices.Count > 0)
          {
            // Use selected work order
            int idx = listWorkOrders.SelectedIndices[0];
            if (idx >= 0 && idx < filteredWorkOrders.Count)
            {
              var wo = filteredWorkOrders[idx];
              if (File.Exists(wo.SdfPath))
              {
                sourcePaths = new List<string> { wo.SdfPath };
              }
              else
              {
                sourcePaths = new List<string>();
              }
            }
            else
            {
              sourcePaths = new List<string>();
            }
          }
          else
          {
            sourcePaths = new List<string>();
          }

          var sourcePathsEnd = DateTime.Now;
          var sourcePathsDuration = sourcePathsEnd - sourcePathsStart;
          Program.Log($"Source paths determination completed in {sourcePathsDuration.TotalMilliseconds:F0}ms - Found {sourcePaths.Count} paths");

          if (sourcePaths.Count > 0)
          {
            Program.Log($"Found {sourcePaths.Count} source paths for table '{selectedTable.TableName}'");
            Program.Log($"Source paths: {string.Join(", ", sourcePaths.Select(p => Path.GetFileName(Path.GetDirectoryName(p))))}");
            currentSourcePaths = sourcePaths.ToList();

            // No dialog reuse; data is loaded directly in MainForm now
            bool canReuseDialog = false;

            if (canReuseDialog) { }

            if (!canReuseDialog)
            {
              // Build and bind data directly
              var buildStart = DateTime.Now;
              Program.Log($"Building data table for '{selectedTable.TableName}' from {sourcePaths.Count} source(s)");
              var data = BuildDataTableFromSources(selectedTable.TableName, sourcePaths);
              var bindStart = DateTime.Now;
              metricsGrid.SuspendLayout();
              var prevVisible = metricsGrid.Visible;
              metricsGrid.Visible = false;
              try
              {
                metricsGrid.AutoGenerateColumns = true;
                metricsGrid.DataSource = new BindingSource { DataSource = data };
                ApplyVirtualColumnsAndLayout(selectedTable.TableName, data);
                ApplyUserConfigToMetricsGrid(selectedTable.TableName);
                // Ensure events and context menus are wired after binding
                WireUpGridEvents();
                AddGridContextMenu();
                Program.Log("Metrics grid events/context menu wired after binding");
              }
              finally { metricsGrid.Visible = prevVisible; metricsGrid.ResumeLayout(); metricsGrid.Refresh(); }
              var bindEnd = DateTime.Now;
              Program.Log($"Data bind completed in {(bindEnd-bindStart).TotalMilliseconds:F0}ms (build: {(bindStart-buildStart).TotalMilliseconds:F0}ms)");
            }
            else { }
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error refreshing metrics grid", ex);
      }
      finally
      {
        var refreshEnd = DateTime.Now;
        var totalRefreshDuration = refreshEnd - refreshStart;
        Program.Log($"=== REFRESH METRICS GRID COMPLETED at {refreshEnd:HH:mm:ss.fff} - Total duration: {totalRefreshDuration:hh\\:mm\\:ss\\.fff} ===");

        // Hide loading indicator
        ShowLoadingIndicator(false);
      }
    }

    // Dialog helpers removed

    private void IntegrateMetricsGrid()
    {
      // No-op after removal of MetricsDialog-based swapping; retained for log stability if called
      try
      {
        WireUpGridEvents();
        AddGridContextMenu();
      }
      catch { }
    }

    // MetricsDialog removed; helper no longer used

    private void WireUpGridEvents()
    {
      try
      {
        if (metricsGrid == null) return;
        if (gridEventsWired)
        {
          Program.Log("WireUpGridEvents: already wired, skipping");
              return;
            }

        // Wire up any MainForm-specific grid events here
        // Column resizing and reordering persistence
        metricsGrid.ColumnWidthChanged -= MetricsGrid_ColumnWidthChanged;
        metricsGrid.ColumnWidthChanged += MetricsGrid_ColumnWidthChanged;
        metricsGrid.ColumnDisplayIndexChanged -= MetricsGrid_ColumnDisplayIndexChanged;
        metricsGrid.ColumnDisplayIndexChanged += MetricsGrid_ColumnDisplayIndexChanged;
        // Context-aware menus
        metricsGrid.MouseDown -= MetricsGrid_MouseDown;
        metricsGrid.MouseDown += MetricsGrid_MouseDown;

        metricsGrid.CellDoubleClick -= MetricsGrid_CellDoubleClick;
        metricsGrid.CellDoubleClick += MetricsGrid_CellDoubleClick;
        metricsGrid.KeyDown -= MetricsGrid_KeyDown;
        metricsGrid.KeyDown += MetricsGrid_KeyDown;
        // Virtual column action handling
        metricsGrid.CellClick -= Grid_CellClick;
        metricsGrid.CellClick += Grid_CellClick;

        // Edit-mode related events
        metricsGrid.CellValueChanged -= MetricsGrid_CellValueChanged_Edit;
        metricsGrid.CellValueChanged += MetricsGrid_CellValueChanged_Edit;
        metricsGrid.CurrentCellDirtyStateChanged -= MetricsGrid_CurrentCellDirtyStateChanged_Edit;
        metricsGrid.CurrentCellDirtyStateChanged += MetricsGrid_CurrentCellDirtyStateChanged_Edit;
        metricsGrid.CellEndEdit -= MetricsGrid_CellEndEdit_Edit;
        metricsGrid.CellEndEdit += MetricsGrid_CellEndEdit_Edit;

        // Debounced order persistence setup
        if (orderPersistTimer == null)
        {
          orderPersistTimer = new System.Windows.Forms.Timer();
          orderPersistTimer.Interval = 200; // ms debounce window
          orderPersistTimer.Tick += (s, e) =>
          {
            // fire only if no order change for at least interval
            if ((DateTime.UtcNow - lastOrderChangeUtc).TotalMilliseconds >= orderPersistTimer.Interval)
            {
              orderPersistTimer.Stop();
              PersistCurrentOrderSafely();
            }
          };
        }

        gridEventsWired = true;
        Program.Log("Wired up MainForm grid events");
      }
      catch (Exception ex)
      {
        Program.Log("Error wiring up grid events", ex);
      }
    }

    private void MetricsGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
    {
      try
      {
        if (e?.Column == null || string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        var key = !string.IsNullOrEmpty(e.Column.DataPropertyName) ? e.Column.DataPropertyName : e.Column.Name;
        Program.Log($"ColumnWidthChanged: table={currentSelectedTable}, column={key}, newWidth={e.Column.Width}");
        if (e.Column.Width > 0)
        {
          var cfg = UserConfig.LoadOrDefault();
          cfg.SetColumnWidth(currentSelectedTable, key, e.Column.Width);
          Program.Log($"Persisting width: table={currentSelectedTable}, column={key}, width={e.Column.Width}");
          cfg.Save();
          Program.Log("Persisted width successfully");
        }
            }
            catch (Exception ex)
            {
        Program.Log("MetricsGrid_ColumnWidthChanged error", ex);
      }
    }

    private void MetricsGrid_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
    {
      try
      {
        if (metricsGrid == null || string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        if (isApplyingLayout)
        {
          return;
        }
        // Debounce persistence to avoid cascades
        lastOrderChangeUtc = DateTime.UtcNow;
        if (orderPersistTimer != null)
        {
          if (!orderPersistTimer.Enabled) orderPersistTimer.Start();
        }
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_ColumnDisplayIndexChanged error", ex);
      }
    }

    private void PersistCurrentOrderSafely()
    {
      try
      {
        if (metricsGrid == null || string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        var ordered = metricsGrid.Columns.Cast<DataGridViewColumn>()
          .OrderBy(c => c.DisplayIndex)
          .Select(c => !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name)
          .ToList();
        Program.Log($"PersistCurrentOrder: table={currentSelectedTable}, order=[{string.Join(", ", ordered)}]");
        var cfg = UserConfig.LoadOrDefault();
        cfg.SetColumnOrder(currentSelectedTable, ordered);
        Program.Log("PersistCurrentOrder: saving to settings.xml");
        cfg.Save();
        Program.Log("PersistCurrentOrder: save complete");
            }
            catch (Exception ex)
            {
        Program.Log("PersistCurrentOrderSafely error", ex);
      }
    }

    private void MetricsGrid_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right || metricsGrid == null) return;
      var hit = metricsGrid.HitTest(e.X, e.Y);
      if (hit.Type == DataGridViewHitTestType.ColumnHeader && hit.ColumnIndex >= 0)
      {
        ShowHeaderContextMenu(hit.ColumnIndex, new System.Drawing.Point(e.X, e.Y));
      }
      else if (hit.RowIndex >= 0 && hit.ColumnIndex >= 0)
      {
        ShowBodyContextMenu(hit.RowIndex, hit.ColumnIndex, new System.Drawing.Point(e.X, e.Y));
      }
    }

    // Apply user-configured layout and built-in column styling to the main metricsGrid
    private void ApplyUserConfigToMetricsGrid(string tableName)
    {
      try
      {
        if (metricsGrid == null) return;

        var cfg = UserConfig.LoadOrDefault();
        Program.Log($"ApplyUserConfig: table={tableName}, cols={metricsGrid.Columns.Count}");

        // Apply column widths
        foreach (DataGridViewColumn col in metricsGrid.Columns)
        {
          var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
          var w = cfg.TryGetColumnWidth(tableName, key);
          if (w.HasValue && w.Value > 0)
          {
            Program.Log($"ApplyUserConfig: set width column={key} width={w.Value}");
            col.Width = w.Value;
          }
          col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }

        // Apply visibility
        foreach (DataGridViewColumn col in metricsGrid.Columns)
        {
          var visibility = cfg.TryGetColumnVisibility(tableName, col.Name);
          if (visibility.HasValue)
          {
            Program.Log($"ApplyUserConfig: set visibility column={col.Name} visible={visibility.Value}");
            col.Visible = visibility.Value;
          }
          else
          {
            // Default new virtual columns to visible
            if (virtualColumnNames.Contains(col.Name))
            {
              col.Visible = true;
            }
          }
        }

        // Style virtual columns distinctly to stand out
        foreach (var def in virtualColumnDefs)
        {
          if (string.IsNullOrWhiteSpace(def.ColumnName)) continue;
          if (!metricsGrid.Columns.Contains(def.ColumnName)) continue;
          var vc = metricsGrid.Columns[def.ColumnName];
          if (def.IsLookupColumn)
          {
            vc.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 224); // light yellow
            vc.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
          }
          else if (def.IsActionColumn)
          {
            vc.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(230, 240, 255); // light blue
            vc.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkBlue;
            vc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
          }
          vc.ReadOnly = true;
        }

        // Apply order (build a sensible default when none is saved)
        var order = cfg.TryGetColumnOrder(tableName);
        if (order == null || order.Count == 0)
        {
          var existing = metricsGrid.Columns.Cast<DataGridViewColumn>()
            .OrderBy(c => c.DisplayIndex)
            .Select(c => !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name)
            .ToList();

          // Place virtual columns after LinkID if present, else at the front
          var nonVirtual = existing.Where(name => !virtualColumnNames.Contains(name)).ToList();
          var linkId = nonVirtual.FirstOrDefault(name =>
            name.Equals("LinkID", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase));

          var built = new List<string>();
          if (!string.IsNullOrEmpty(linkId))
          {
            var idxLink = nonVirtual.IndexOf(linkId);
            built.AddRange(nonVirtual.Take(idxLink + 1));
            built.AddRange(virtualColumnNames);
            built.AddRange(nonVirtual.Skip(idxLink + 1));
          }
          else
          {
            built.AddRange(virtualColumnNames);
            built.AddRange(nonVirtual);
          }
          order = built;
          cfg.SetColumnOrder(tableName, order);
          cfg.Save();
          Program.Log($"ApplyUserConfig: built default order with virtuals after LinkID: [{string.Join(", ", order)}]");
        }

        if (order != null && order.Count > 0)
        {
          Program.Log($"ApplyUserConfig: applying order [{string.Join(", ", order)}]");
          var prev = isApplyingLayout; isApplyingLayout = true;
          int idx = 0;
          foreach (var name in order)
          {
            DataGridViewColumn col = null;
            foreach (DataGridViewColumn c in metricsGrid.Columns)
            {
              var key2 = !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name;
              if (string.Equals(key2, name, StringComparison.OrdinalIgnoreCase)) { col = c; break; }
            }
            if (col != null)
            {
              col.DisplayIndex = idx++;
            }
          }
          isApplyingLayout = prev;
        }

        // Style built-in _SourceFile column if present
        if (metricsGrid.Columns.Contains("_SourceFile"))
        {
          var col = metricsGrid.Columns["_SourceFile"];
          col.ReadOnly = true;
          col.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 248, 255);
          col.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          col.HeaderText = "Work Order";
        }

        // Enable user reordering and resizing
        metricsGrid.AllowUserToOrderColumns = true;
        metricsGrid.AllowUserToResizeColumns = true;

        // Initialize edit mode visuals/state
        ApplyMainGridEditState();
      }
      catch (Exception ex)
      {
        Program.Log("ApplyUserConfigToMetricsGrid error", ex);
      }
    }

    private string GetWorkOrderName(string sdfPath)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(sdfPath)) return "Unknown";
        var directory = Path.GetDirectoryName(sdfPath);
        if (string.IsNullOrWhiteSpace(directory)) return Path.GetFileNameWithoutExtension(sdfPath);
        return Path.GetFileName(directory);
      }
      catch
      {
        return Path.GetFileNameWithoutExtension(sdfPath);
      }
    }

    // Build a consolidated DataTable from multiple source SDF paths for the given table
    private DataTable BuildDataTableFromSources(string tableName, List<string> sourcePaths)
    {
      var dataTable = new DataTable(tableName);
      DataTable schema = null;
      var tempFiles = new List<string>();
      try
      {
        // Derive schema from first available source
        foreach (var path in sourcePaths)
        {
          if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
          string tempCopyPath;
          using (var conn = SqlCeUtils.OpenWithFallback(path, out tempCopyPath))
          {
            if (!string.IsNullOrEmpty(tempCopyPath)) tempFiles.Add(tempCopyPath);
            try
            {
              using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", conn))
              using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
              {
                schema = reader.GetSchemaTable();
                break;
              }
            }
            catch { }
          }
        }

        if (schema == null)
        {
          return dataTable; // empty
        }

        // Add built-in source file tracking column first
        dataTable.Columns.Add("_SourceFile", typeof(string));

        foreach (DataRow r in schema.Rows)
        {
          var colName = Convert.ToString(r["ColumnName"]);
          var dataType = (Type)r["DataType"];
          if (!dataTable.Columns.Contains(colName)) dataTable.Columns.Add(colName, dataType);
        }

        // Set a primary key if LinkID or ID exists
        var pk = dataTable.Columns.Contains("LinkID") ? dataTable.Columns["LinkID"] : (dataTable.Columns.Contains("ID") ? dataTable.Columns["ID"] : null);
        if (pk != null) dataTable.PrimaryKey = new[] { pk };

        // Load rows from all sources
        foreach (var path in sourcePaths)
        {
          if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
          string tempCopyPath;
          using (var conn = SqlCeUtils.OpenWithFallback(path, out tempCopyPath))
          {
            if (!string.IsNullOrEmpty(tempCopyPath)) tempFiles.Add(tempCopyPath);
            try
            {
              using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", conn))
              using (var reader = cmd.ExecuteReader())
              {
                var allCols = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                var srcCols = new List<string>();
                // Build list of source column names from reader schema
                var readerSchema = reader.GetSchemaTable();
                srcCols = readerSchema.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["ColumnName"]).Trim()).ToList();

                while (reader.Read())
                {
                  var row = dataTable.NewRow();
                  row["_SourceFile"] = GetWorkOrderName(path);
                  foreach (var col in srcCols)
                  {
                    if (!dataTable.Columns.Contains(col)) continue;
                    try { row[col] = reader[col]; } catch { row[col] = DBNull.Value; }
                  }

                  // Apply in-memory overrides by LinkID if present and skip deleted rows
                  try
                  {
                    object linkVal = DBNull.Value;
                    if (dataTable.Columns.Contains("LinkID")) linkVal = row["LinkID"]; else if (dataTable.Columns.Contains("ID")) linkVal = row["ID"];
                    if (linkVal != DBNull.Value && linkVal != null)
                    {
                      var linkKey = Convert.ToString(linkVal);
                      if (Program.Edits.IsDeleted(tableName, linkKey)) continue;
                      var overrides = Program.Edits.SnapshotTable(tableName);
                      if (overrides.TryGetValue(linkKey, out var rowOverrides))
                      {
                        foreach (var kv in rowOverrides)
                        {
                          if (dataTable.Columns.Contains(kv.Key)) row[kv.Key] = kv.Value ?? DBNull.Value;
                        }
                      }
                    }
                  }
                  catch { }

                  // Add or merge by PK if set
                  if (dataTable.PrimaryKey != null && dataTable.PrimaryKey.Length == 1)
                  {
                    var keyCol = dataTable.PrimaryKey[0];
                    var keyVal = row[keyCol];
                    if (keyVal != null && keyVal != DBNull.Value)
                    {
                      var existing = dataTable.Rows.Find(new object[] { keyVal });
                      if (existing != null)
                      {
                        foreach (DataColumn c in dataTable.Columns)
                        {
                          var v = row[c]; if (v != DBNull.Value && v != null) existing[c] = v;
                        }
                        continue;
                      }
                    }
                  }
                  dataTable.Rows.Add(row);
                }
              }
            }
            catch { }
          }
        }

        dataTable.AcceptChanges();
      }
      finally
      {
        foreach (var tf in tempFiles.Distinct()) { try { File.Delete(tf); } catch { } }
      }
      return dataTable;
    }

    // Apply virtual columns (built-in) and persisted layout; placeholder for future lookup/action support
    private void ApplyVirtualColumnsAndLayout(string tableName, DataTable data)
    {
      try
      {
        // Ensure built-in _SourceFile column exists and visible in grid
        if (!data.Columns.Contains("_SourceFile")) data.Columns.Add("_SourceFile", typeof(string));

        // Load virtual column definitions from settings
        LoadVirtualColumnDefinitions(tableName);
        BuildVirtualLookupCaches(tableName);
        RebuildVirtualColumns(tableName, data);
      }
      catch { }
    }

    private void LoadVirtualColumnDefinitions(string tableName)
    {
      try
      {
        var cfg = UserConfig.LoadOrDefault();
        virtualColumnDefs = cfg.GetVirtualColumnsForTable(tableName) ?? new List<UserConfig.VirtualColumnDef>();
        virtualColumnNames.Clear();
        Program.Log($"MainForm: Loading virtual columns for '{tableName}': {virtualColumnDefs.Count} defs");
        foreach (var def in virtualColumnDefs)
        {
          if (!string.IsNullOrWhiteSpace(def.ColumnName)) virtualColumnNames.Add(def.ColumnName);
        }
      }
      catch (Exception ex)
      {
        Program.Log("MainForm: LoadVirtualColumnDefinitions failed", ex);
        virtualColumnDefs = new List<UserConfig.VirtualColumnDef>();
        virtualColumnNames.Clear();
      }
    }

    private void BuildVirtualLookupCaches(string tableName)
    {
      try
      {
        virtualLookupCacheByColumn.Clear();
        foreach (var def in virtualColumnDefs)
        {
          if (!def.IsLookupColumn) continue;
          if (string.IsNullOrWhiteSpace(def.TargetTableName) ||
              string.IsNullOrWhiteSpace(def.LocalKeyColumn) ||
              string.IsNullOrWhiteSpace(def.TargetKeyColumn) ||
              string.IsNullOrWhiteSpace(def.TargetValueColumn)) continue;

          var lookup = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
          try
          {
            // Pull from first available SDF for lookup
            foreach (var path in currentSourcePaths)
            {
              if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
              string tmp;
              using (var conn = SqlCeUtils.OpenWithFallback(path, out tmp))
              {
                try
                {
                  string k = def.TargetKeyColumn.Replace("]", "]]"), v = def.TargetValueColumn.Replace("]", "]]"), t = def.TargetTableName.Replace("]", "]]");
                  using (var cmd = new SqlCeCommand($"SELECT [" + k + "],[" + v + "] FROM [" + t + "]", conn))
                  using (var rdr = cmd.ExecuteReader())
                  {
                    while (rdr.Read())
                    {
                      var keyObj = rdr.IsDBNull(0) ? null : rdr.GetValue(0);
                      var valObj = rdr.IsDBNull(1) ? null : rdr.GetValue(1);
                      var key = keyObj == null ? null : Convert.ToString(keyObj);
                      if (string.IsNullOrEmpty(key)) continue;
                      if (!lookup.ContainsKey(key)) lookup[key] = valObj;
                    }
                  }
                }
                catch { }
              }
        }
      }
      catch (Exception ex)
      {
            Program.Log($"MainForm: BuildVirtualLookupCaches failed for {def.ColumnName}", ex);
      }
          virtualLookupCacheByColumn[def.ColumnName] = lookup;
        }
      }
      catch (Exception ex)
      {
        Program.Log("MainForm: BuildVirtualLookupCaches failed", ex);
      }
    }

    private void RebuildVirtualColumns(string tableName, DataTable data)
    {
      try
      {
        if (data == null || virtualColumnDefs == null || virtualColumnDefs.Count == 0) return;
        var newlyAdded = new List<string>();
        foreach (var def in virtualColumnDefs)
        {
          if (string.IsNullOrWhiteSpace(def.ColumnName)) continue;
          bool exists = data.Columns.Cast<DataColumn>().Any(c => string.Equals(c.ColumnName, def.ColumnName, StringComparison.OrdinalIgnoreCase));
          if (def.IsBuiltInColumn)
          {
            // Built-in handled above
          }
          else if (!exists)
          {
            data.Columns.Add(def.ColumnName, typeof(string));
            newlyAdded.Add(def.ColumnName);
            Program.Log($"MainForm: Added virtual column '{def.ColumnName}'");
          }

          if (def.IsLookupColumn && virtualLookupCacheByColumn.TryGetValue(def.ColumnName, out var cache))
          {
            if (data.Columns.Contains(def.LocalKeyColumn))
            {
              foreach (DataRow row in data.Rows)
              {
                var keyObj = row[def.LocalKeyColumn];
                var key = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
                row[def.ColumnName] = (key != null && cache.TryGetValue(key, out var val)) ? (val ?? "") : "";
              }
            }
          }
          else if (def.IsActionColumn)
          {
            foreach (DataRow row in data.Rows) row[def.ColumnName] = def.ButtonText ?? "Action";
          }
        }

        // Force DataGridView to recognize any new columns after DataSource set
        if (metricsGrid != null && newlyAdded.Count > 0)
        {
          Program.Log($"MainForm: Refreshing grid after adding virtual columns: {string.Join(", ", newlyAdded)}");
          var bs = metricsGrid.DataSource as BindingSource;
          var ds = bs?.DataSource;
          metricsGrid.DataSource = null;
          metricsGrid.Refresh();
          Application.DoEvents();
          metricsGrid.DataSource = bs ?? new BindingSource { DataSource = ds ?? data };
          metricsGrid.Refresh();
        }
      }
      catch (Exception ex)
      {
        Program.Log("MainForm: RebuildVirtualColumns failed", ex);
      }
    }

    // Header context menu (ported)
    private void ShowHeaderContextMenu(int columnIndex, System.Drawing.Point clientLocation)
    {
      try
      {
        var col = metricsGrid.Columns[columnIndex];
        if (col == null) return;
        var menu = new ContextMenuStrip();

        var moveToFirst = new ToolStripMenuItem("Move to first");
        moveToFirst.Click += (s, e) => { col.DisplayIndex = 0; };
        var moveToLast = new ToolStripMenuItem("Move to last");
        moveToLast.Click += (s, e) => { col.DisplayIndex = metricsGrid.Columns.Count - 1; };
        menu.Items.Add(moveToFirst);
        menu.Items.Add(moveToLast);
        menu.Items.Add(new ToolStripSeparator());

        var moveToIndex = new ToolStripMenuItem("Move to index...");
        moveToIndex.Click += (s, e) =>
        {
          var selected = PromptForDisplayIndex(col.DisplayIndex);
          if (selected.HasValue)
          {
            int target = Math.Max(0, Math.Min(selected.Value, metricsGrid.Columns.Count - 1));
            if (target != col.DisplayIndex) col.DisplayIndex = target;
          }
        };
        menu.Items.Add(moveToIndex);

        // Hide/Show columns
        menu.Items.Add(new ToolStripSeparator());
        var hideColumn = new ToolStripMenuItem("Hide Column");
        hideColumn.Click += (s, e) => { col.Visible = false; SaveColumnVisibility(col.Name, false); };
        menu.Items.Add(hideColumn);

        var showColumnsMenu = new ToolStripMenuItem("Show Columns");
        var hasHiddenColumns = false;
        foreach (DataGridViewColumn gridCol in metricsGrid.Columns)
        {
          if (!gridCol.Visible)
          {
            hasHiddenColumns = true;
            var showCol = new ToolStripMenuItem(gridCol.HeaderText ?? gridCol.Name);
            showCol.Click += (s, e) => { gridCol.Visible = true; SaveColumnVisibility(gridCol.Name, true); };
            showColumnsMenu.DropDownItems.Add(showCol);
          }
        }
        showColumnsMenu.Enabled = hasHiddenColumns;
        menu.Items.Add(showColumnsMenu);

        var showAllColumns = new ToolStripMenuItem("Show All Columns");
        showAllColumns.Click += (s, e) =>
        {
          foreach (DataGridViewColumn gridCol in metricsGrid.Columns)
          {
            gridCol.Visible = true;
            SaveColumnVisibility(gridCol.Name, true);
          }
        };
        showAllColumns.Enabled = hasHiddenColumns;
        menu.Items.Add(showAllColumns);

        // Manage virtual columns from header context
        menu.Items.Add(new ToolStripSeparator());
        var manageVirtual = new ToolStripMenuItem("Virtual Columns...");
        manageVirtual.Click += (s, e) => OpenVirtualColumnsDialog();
        menu.Items.Add(manageVirtual);

        menu.Show(metricsGrid, clientLocation);
      }
      catch { }
    }

    private int? PromptForDisplayIndex(int currentIndex)
    {
      try
      {
        var dlg = new Form
        {
          Text = "Move Column",
          StartPosition = FormStartPosition.CenterParent,
          FormBorderStyle = FormBorderStyle.FixedDialog,
          MinimizeBox = false,
          MaximizeBox = false,
          Width = 280,
          Height = 140
        };
        var lbl = new Label { Text = "Target index:", AutoSize = true, Location = new System.Drawing.Point(12, 15) };
        var nud = new NumericUpDown
        {
          Minimum = 0,
          Maximum = metricsGrid.Columns.Count > 0 ? metricsGrid.Columns.Count - 1 : 0,
          Location = new System.Drawing.Point(100, 12),
          Width = 150
        };
        int safeCurrent = Math.Max((int)nud.Minimum, Math.Min(currentIndex, (int)nud.Maximum));
        nud.Value = safeCurrent;
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new System.Drawing.Point(dlg.ClientSize.Width - 170, 70) };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new System.Drawing.Point(dlg.ClientSize.Width - 90, 70) };
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;
        dlg.Controls.Add(lbl);
        dlg.Controls.Add(nud);
        dlg.Controls.Add(btnOk);
        dlg.Controls.Add(btnCancel);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
          return (int)nud.Value;
        }
      }
      catch { }
      return null;
    }

    private void SaveColumnVisibility(string columnName, bool isVisible)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        var cfg = UserConfig.LoadOrDefault();
        Program.Log($"SaveColumnVisibility: table={currentSelectedTable}, column={columnName}, visible={isVisible}");
        cfg.SetColumnVisibility(currentSelectedTable, columnName, isVisible);
        cfg.Save();
        Program.Log("Persisted visibility successfully");
      }
      catch (Exception ex)
      {
        Program.Log("SaveColumnVisibility error", ex);
      }
    }

    // Export helpers
    private static string SaveStreamCellToFolder(string columnName, object value, string folder, string baseName)
    {
      byte[] bytes = GetStreamBytes(value);
      if (bytes == null || bytes.Length == 0) return null;
      string ext = string.Equals(columnName, "WMFStream", StringComparison.OrdinalIgnoreCase) ? ".wmf" : ".jpg";
      string safeBase = MakeSafeFileName(baseName);
      string target = Path.Combine(folder, safeBase + ext);
      int n = 1;
      while (File.Exists(target)) { target = Path.Combine(folder, safeBase + "_" + n + ext); n++; }
      File.WriteAllBytes(target, bytes);
      return target;
    }

    private static byte[] GetStreamBytes(object value)
    {
      if (value is byte[] b) return b;
      if (value is string s)
      {
        try { return Convert.FromBase64String(s); } catch { return System.Text.Encoding.UTF8.GetBytes(s); }
      }
      if (value is Stream stream)
      {
        using (var ms = new MemoryStream()) { stream.CopyTo(ms); return ms.ToArray(); }
      }
      return null;
    }

    private static string MakeSafeFileName(string name)
    {
      foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
      return name;
    }

    private void MetricsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
      try
      {
        // Toggle edit mode
        isEditModeMainGrid = !isEditModeMainGrid;
        ApplyMainGridEditState();
        Program.Log($"MainForm: Edit mode toggled to {isEditModeMainGrid}");
      }
      catch { }
    }

    private void MetricsGrid_KeyDown(object sender, KeyEventArgs e)
    {
      try
      {
        if (e.Control)
        {
          if (e.KeyCode == Keys.C)
          {
            CopySelectedCells_MainGrid();
            e.Handled = true; return;
          }
          if (isEditModeMainGrid && !metricsGrid.ReadOnly)
          {
            if (e.KeyCode == Keys.X) { CutSelectedCells_MainGrid(); e.Handled = true; return; }
            if (e.KeyCode == Keys.V) { PasteToSelectedCells_MainGrid(); e.Handled = true; return; }
          }
        }
        if (e.KeyCode == Keys.Delete && isEditModeMainGrid && !metricsGrid.ReadOnly)
        {
          ClearSelectedCells_MainGrid();
          e.Handled = true; e.SuppressKeyPress = true; return;
        }
      }
      catch { }
    }

    private void MetricsGrid_CurrentCellDirtyStateChanged_Edit(object sender, EventArgs e)
    {
      if (!isEditModeMainGrid || metricsGrid.ReadOnly) return;
      // defer commit to CellEndEdit
    }

    private void MetricsGrid_CellEndEdit_Edit(object sender, DataGridViewCellEventArgs e)
    {
      try
      {
        if (!isEditModeMainGrid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        TrySaveSingleCellChange_MainGrid(e.RowIndex, e.ColumnIndex, true);
      }
      catch { }
    }

    private void MetricsGrid_CellValueChanged_Edit(object sender, DataGridViewCellEventArgs e)
    {
      if (!isEditModeMainGrid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
      try { TrySaveSingleCellChange_MainGrid(e.RowIndex, e.ColumnIndex, false); } catch { }
    }

    private void ApplyMainGridEditState()
    {
      try
      {
        metricsGrid.ReadOnly = !isEditModeMainGrid;
        metricsGrid.EditMode = isEditModeMainGrid ? DataGridViewEditMode.EditOnKeystroke : DataGridViewEditMode.EditOnKeystrokeOrF2;
        metricsGrid.SelectionMode = isEditModeMainGrid ? DataGridViewSelectionMode.CellSelect : DataGridViewSelectionMode.FullRowSelect;
        // Toggle thick green border using wrapper panel if available
        try
        {
          if (panelMetricsBorder != null)
          {
            panelMetricsBorder.Padding = isEditModeMainGrid ? new Padding(6) : new Padding(6);
            panelMetricsBorder.BackColor = isEditModeMainGrid ? System.Drawing.Color.LimeGreen : System.Drawing.SystemColors.Control;
          }
        }
        catch { }
        metricsGrid.BorderStyle = BorderStyle.None;
      }
      catch { }
    }

    private void CopySelectedCells_MainGrid()
    {
      try
      {
        var cells = GetSelectedCellsForClipboard_MainGrid();
        if (cells.Count == 0) return;
        var text = ConvertCellsToClipboardText_MainGrid(cells);
        Clipboard.SetText(text);
      }
      catch { }
    }

    private void CutSelectedCells_MainGrid()
    {
      try
      {
        var cells = GetSelectedCellsForClipboard_MainGrid();
        if (cells.Count == 0) return;
        var text = ConvertCellsToClipboardText_MainGrid(cells);
        Clipboard.SetText(text);
        ClearSelectedCells_MainGrid();
      }
      catch { }
    }

    private void PasteToSelectedCells_MainGrid()
    {
      try
      {
        if (!Clipboard.ContainsText()) return;
        var pasteText = Clipboard.GetText();
        var pasteData = ParseClipboardText_MainGrid(pasteText);
        if (pasteData.Count == 0) return;
        var targets = GetSelectedCellsForPaste_MainGrid();
        if (targets.Count == 0) return;
        foreach (var cell in targets)
        {
          var valueToApply = pasteData[0].Count > 0 ? pasteData[0][0] : string.Empty;
          cell.Value = string.IsNullOrEmpty(valueToApply) ? (object)DBNull.Value : valueToApply;
          TrySaveSingleCellChange_MainGrid(cell.RowIndex, cell.ColumnIndex, true);
        }
      }
      catch { }
    }

    private void ClearSelectedCells_MainGrid()
    {
      try
      {
        if (metricsGrid.IsCurrentCellInEditMode) metricsGrid.EndEdit();
        int cleared = 0;
        foreach (DataGridViewCell cell in metricsGrid.SelectedCells)
        {
          if (cell.RowIndex < 0 || cell.ColumnIndex < 0) continue;
          var col = metricsGrid.Columns[cell.ColumnIndex];
          var colName = col.DataPropertyName ?? col.Name;
          if (string.Equals(colName, "LinkID", StringComparison.OrdinalIgnoreCase)) continue;

          cell.Value = DBNull.Value;
          TrySaveSingleCellChange_MainGrid(cell.RowIndex, cell.ColumnIndex, true);
          cleared++;
        }
        Program.Log($"MainForm: Cleared {cleared} cell(s)");
      }
      catch { }
    }

    private List<DataGridViewCell> GetSelectedCellsForClipboard_MainGrid()
    {
      var cells = new List<DataGridViewCell>();
      if (metricsGrid.SelectionMode == DataGridViewSelectionMode.CellSelect && metricsGrid.SelectedCells.Count > 0)
      {
        foreach (DataGridViewCell c in metricsGrid.SelectedCells)
        {
          if (c.RowIndex >= 0 && c.ColumnIndex >= 0) cells.Add(c);
        }
      }
      else if (metricsGrid.CurrentCell != null && metricsGrid.CurrentCell.RowIndex >= 0 && metricsGrid.CurrentCell.ColumnIndex >= 0)
      {
        cells.Add(metricsGrid.CurrentCell);
      }
      return cells.OrderBy(c => c.RowIndex).ThenBy(c => c.ColumnIndex).ToList();
    }

    private List<List<string>> ParseClipboardText_MainGrid(string text)
    {
      var result = new List<List<string>>();
      if (string.IsNullOrEmpty(text)) return result;
      var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
      foreach (var line in lines)
      {
        if (string.IsNullOrEmpty(line)) continue;
        result.Add(line.Split('\t').ToList());
      }
      return result;
    }

    private List<DataGridViewCell> GetSelectedCellsForPaste_MainGrid()
    {
      var cells = new List<DataGridViewCell>();
      if (metricsGrid.SelectionMode == DataGridViewSelectionMode.CellSelect && metricsGrid.SelectedCells.Count > 0)
      {
        foreach (DataGridViewCell c in metricsGrid.SelectedCells)
        {
          if (c.RowIndex >= 0 && c.ColumnIndex >= 0) cells.Add(c);
        }
      }
      else if (metricsGrid.CurrentCell != null && metricsGrid.CurrentCell.RowIndex >= 0 && metricsGrid.CurrentCell.ColumnIndex >= 0)
      {
        cells.Add(metricsGrid.CurrentCell);
      }
      return cells.OrderBy(c => c.RowIndex).ThenBy(c => c.ColumnIndex).ToList();
    }

    private string ConvertCellsToClipboardText_MainGrid(List<DataGridViewCell> cells)
    {
      if (cells.Count == 0) return string.Empty;
      var sb = new StringBuilder();
      var groups = cells.GroupBy(c => c.RowIndex).OrderBy(g => g.Key);
      foreach (var g in groups)
      {
        var rowCells = g.OrderBy(c => c.ColumnIndex).ToList();
        var values = new List<string>();
        foreach (var cell in rowCells)
        {
          var v = cell.Value?.ToString() ?? string.Empty;
          v = v.Replace("\t", " ").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
          values.Add(v);
        }
        sb.AppendLine(string.Join("\t", values));
      }
      return sb.ToString();
    }

    private void TrySaveSingleCellChange_MainGrid(int rowIndex, int columnIndex, bool isEndEdit)
    {
      try
      {
        if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataTable data)
        {
          var row = metricsGrid.Rows[rowIndex];
          if (row?.DataBoundItem is DataRowView drv)
          {
            var dataRow = drv.Row;
            var column = metricsGrid.Columns[columnIndex];
            var columnName = column.DataPropertyName ?? column.Name;
            // Skip LinkID/ID key columns and virtual/action columns
            if (string.Equals(columnName, "LinkID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(columnName, "ID", StringComparison.OrdinalIgnoreCase) ||
                (virtualColumnNames != null && virtualColumnNames.Contains(columnName)))
              return;
            var cellValue = row.Cells[columnIndex].Value;
            var newValue = cellValue == DBNull.Value ? null : cellValue;
            object orig = null;
            if (dataRow.Table.Columns.Contains(columnName))
            {
              try { orig = dataRow.HasVersion(DataRowVersion.Original) ? dataRow[columnName, DataRowVersion.Original] : dataRow[columnName]; }
              catch { orig = dataRow[columnName]; }
            }
            if (orig == DBNull.Value) orig = null;
            if (!object.Equals(newValue, orig))
            {
              var key = (dataRow.Table.Columns.Contains("LinkID") ? dataRow["LinkID"] : (dataRow.Table.Columns.Contains("ID") ? dataRow["ID"] : null));
              string linkKey = key == null || key == DBNull.Value ? null : Convert.ToString(key);
              if (!string.IsNullOrEmpty(linkKey) && !string.IsNullOrEmpty(currentSelectedTable))
              {
                Program.Edits.UpsertOverride(currentSelectedTable, linkKey, columnName, newValue);
              }
              // Mark row clean so subsequent edits diff against new baseline
              try { dataRow.AcceptChanges(); } catch { }
            }
          }
        }
      }
      catch { }
    }

    // Open virtual columns dialog implemented directly in MainForm
    public void OpenVirtualColumnsDialog()
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable) || metricsGrid?.DataSource == null)
        {
          MessageBox.Show("No data loaded. Please select a table first.", "No Data",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }
        using (var dlg = new VirtualColumnsDialog(currentSelectedTable, null, false, new Dictionary<string, List<string>>()))
        {
          if (dlg.ShowDialog(this) == DialogResult.OK)
          {
            // Reload virtual column definitions and rebuild the grid
            LoadVirtualColumnDefinitions(currentSelectedTable);
            BuildVirtualLookupCaches(currentSelectedTable);

            // Get current data and rebuild virtual columns
            if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataTable data)
            {
              RebuildVirtualColumns(currentSelectedTable, data);
            }

            // Apply persisted layout after changes
            ApplyUserConfigToMetricsGrid(currentSelectedTable);

            // Refresh the grid to show changes
            metricsGrid?.Refresh();

            Program.Log("Virtual columns modified and grid refreshed successfully");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error opening virtual columns dialog", ex);
        MessageBox.Show("Error opening virtual columns dialog: " + ex.Message, "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    public void RefreshVirtualColumns()
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable)) return;

        // Reload virtual column definitions and rebuild
        LoadVirtualColumnDefinitions(currentSelectedTable);
        BuildVirtualLookupCaches(currentSelectedTable);

        // Get current data and rebuild virtual columns
        if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataTable data)
        {
          RebuildVirtualColumns(currentSelectedTable, data);
        }

        // Apply persisted layout
        ApplyUserConfigToMetricsGrid(currentSelectedTable);

        // Refresh the grid
        metricsGrid?.Refresh();

        Program.Log("Virtual columns/layout refreshed successfully");
      }
      catch (Exception ex)
      {
        Program.Log("Error refreshing virtual columns", ex);
      }
    }

    // Add context menu to the grid for easy access to MetricsDialog features
    private void AddGridContextMenu()
    {
      try
      {
        if (metricsGrid == null) return;

        var contextMenu = new ContextMenuStrip();
        metricsGrid.ContextMenuStrip = contextMenu; // will be populated dynamically in MouseDown

        Program.Log("Added context menu to metrics grid");

        // Add edit mode toggle in header context later when menu is built
      }
      catch (Exception ex)
      {
        Program.Log("Error adding grid context menu", ex);
      }
    }

    private void ShowBodyContextMenu(int rowIndex, int columnIndex, System.Drawing.Point clientLocation)
    {
      try
      {
        var menu = new ContextMenuStrip();
        var export = new ToolStripMenuItem("Export...");
        var exportJpeg = new ToolStripMenuItem("Export JPEG Streams");
        exportJpeg.Click += (s, e) => ExportStreamsForSelection("JPEGStream");
        var exportWmf = new ToolStripMenuItem("Export WMF Streams");
        exportWmf.Click += (s, e) => ExportStreamsForSelection("WMFStream");
        export.DropDownItems.Add(exportJpeg);
        export.DropDownItems.Add(exportWmf);
        var delete = new ToolStripMenuItem("Delete Selected Rows");
        delete.Click += (s, e) => DeleteSelectedRows();
        var refreshVirtual = new ToolStripMenuItem("Refresh Virtual Columns");
        refreshVirtual.Click += (s, e) => RefreshVirtualColumns();
        menu.Items.Add(export);
        menu.Items.Add(delete);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(refreshVirtual);
        menu.Show(metricsGrid, clientLocation);
      }
      catch { }
    }

    // Export functionality for stream columns
    private void ExportStreamsForSelection(string columnName)
    {
      try
      {
        if (metricsGrid?.DataSource == null)
        {
          MessageBox.Show("No data loaded. Please select a table first.", "No Data",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }
        // Implement export against current grid
        try
        {
          var rows = metricsGrid.SelectedRows.Cast<DataGridViewRow>().ToList();
          if (rows.Count == 0 && metricsGrid.CurrentRow != null) rows.Add(metricsGrid.CurrentRow);
          if (rows.Count == 0) { MessageBox.Show("No rows selected."); return; }
          if (!metricsGrid.Columns.Contains(columnName)) { MessageBox.Show($"Column '{columnName}' not found."); return; }
          using (var fbd = new FolderBrowserDialog())
          {
            fbd.Description = "Choose destination folder for exported images";
            if (fbd.ShowDialog(this) != DialogResult.OK) return;
            var folder = fbd.SelectedPath;
            int exported = 0;
            foreach (var row in rows)
            {
              var val = row.Cells[columnName].Value;
              if (val == null || val == DBNull.Value) continue;
              var name = currentSelectedTable + "_" + (row.Index + 1) + "_" + columnName;
              var path = SaveStreamCellToFolder(columnName, val, folder, name);
              if (!string.IsNullOrEmpty(path)) exported++;
            }
            MessageBox.Show(exported + " file(s) exported.");
          }
        }
        catch (Exception ex)
        {
          Program.Log("Export streams failed", ex);
          MessageBox.Show("Export failed: " + ex.Message);
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error exporting streams", ex);
        MessageBox.Show("Error exporting streams: " + ex.Message, "Export Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Delete functionality for selected rows
    private void DeleteSelectedRows()
    {
      try
      {
        if (metricsGrid?.DataSource == null)
        {
          MessageBox.Show("No data loaded. Please select a table first.", "No Data",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Collect rows to delete (selected, or current if none)
        var rows = metricsGrid.SelectedRows.Cast<DataGridViewRow>().ToList();
        if (rows.Count == 0 && metricsGrid.CurrentRow != null) rows.Add(metricsGrid.CurrentRow);
        if (rows.Count == 0) { MessageBox.Show("No rows selected."); return; }

        int deleted = 0;
        foreach (var row in rows)
        {
          try
          {
            if (row.IsNewRow) continue;
            if (row?.DataBoundItem is DataRowView drv)
            {
              var dataRow = drv.Row;
              // Determine key
              object keyObj = null;
              if (dataRow.Table.Columns.Contains("LinkID")) keyObj = dataRow["LinkID"]; else if (dataRow.Table.Columns.Contains("ID")) keyObj = dataRow["ID"];
              var linkKey = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
              if (!string.IsNullOrEmpty(linkKey) && !string.IsNullOrEmpty(currentSelectedTable))
              {
                Program.Edits.MarkDeleted(currentSelectedTable, linkKey);
                deleted++;
              }

              // Remove from current view immediately
              try { dataRow.Delete(); } catch { }
            }
            else
            {
              // Fallback: remove the grid row
              metricsGrid.Rows.Remove(row);
            }
          }
          catch { }
        }

        // Refresh grid to reflect deletions
        if (deleted > 0)
        {
          try { RefreshMetricsGrid(); } catch { }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error deleting selected rows", ex);
        MessageBox.Show("Error deleting selected rows: " + ex.Message, "Delete Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void ShowLoadingIndicator(bool show)
    {
      try
      {
        if (InvokeRequired)
        {
          Invoke(new Action<bool>(ShowLoadingIndicator), show);
          return;
        }

        panelLoading.Visible = show;
        if (show)
        {
          // Position the message where the progress bar lives
          panelLoading.Bounds = progress.Bounds;
          panelLoading.Anchor = progress.Anchor;
          lblLoading.Dock = DockStyle.Fill;
          lblLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
          lblLoading.Text = "Loading data...";
          panelLoading.BringToFront();
          progress.Visible = false;
        }
        else
        {
          panelLoading.Visible = false;
          progress.Visible = true;
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error showing loading indicator", ex);
      }
    }

    private class TableSelectorItem
    {
      public string Label { get; set; }
      public string TableName { get; set; }

      public override string ToString()
      {
        return Label;
      }
    }


    private static void AutoSizeListViewColumns(ListView view)
    {
      try
      {
        view.BeginUpdate();
        // Fit to content, then ensure headers are visible if wider
        view.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        view.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        foreach (ColumnHeader ch in view.Columns)
        {
          if (ch.Width < 50) ch.Width = 50;
        }
      }
      finally
      {
        view.EndUpdate();
      }
    }

    private void SetBusy(bool isBusy)
    {
      try
      {
        if (isBusy)
        {
          // Show loading panel at the progress bar location
          panelLoading.Bounds = progress.Bounds;
          panelLoading.Anchor = progress.Anchor;
          panelLoading.Visible = true;
          lblLoading.Dock = DockStyle.Fill;
          lblLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
          lblLoading.Text = "Loading work orders...";
          panelLoading.BringToFront();
          progress.Visible = false;
        }
        else
        {
          // Hide loading panel and show progress bar
          panelLoading.Visible = false;
          progress.Visible = true;
          progress.Style = ProgressBarStyle.Continuous;
          progress.Value = 0;
        }
      }
      catch { }
    }

    // Thread-safe UI update helper
    private async Task InvokeAsync(Action action)
    {
      if (InvokeRequired)
      {
        await Task.Run(() => Invoke(action));
      }
      else
      {
        action();
      }
    }

    // Format file size for display
    private string FormatFileSize(long bytes)
    {
      if (bytes < 1024) return $"{bytes} B";
      if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
      if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
      return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    // Update scan status in the UI
    private void UpdateScanStatus(string status)
    {
      try
      {
        // Update the progress bar tooltip with detailed status
        if (progress.Tag != null)
        {
          var toolTip = new ToolTip();
          toolTip.SetToolTip(progress, status);
        }

        // Log the status for debugging
        Program.Log($"Scan Status: {status}");
      }
      catch (Exception ex)
      {
        Program.Log("Error updating scan status", ex);
      }
    }

    private void RunConsolidation(List<(string WorkOrderDir, string SdfPath)> sources, string destinationPath)
    {
      progress.Value = 0;
      progress.Maximum = sources.Count;

      if (File.Exists(destinationPath))
      {
        File.Delete(destinationPath);
      }

      CreateEmptyDatabase(destinationPath);

      using (var dest = new SqlCeConnection($"Data Source={destinationPath};"))
      {
        dest.Open();

        foreach (var src in sources)
        {
          // Combined mode with source metadata columns
          string sourceTag = new DirectoryInfo(src.WorkOrderDir).Name;
          CopyDatabaseCombined(src.SdfPath, dest, sourceTag, src.WorkOrderDir);
          progress.Value += 1;
        }
      }
    }

    // removed unused GeneratePrefixFromDirectory

    private static void CreateEmptyDatabase(string path)
    {
      var engine = new SqlCeEngine($"Data Source={path};");
      engine.CreateDatabase();
    }

    // removed legacy prefixed consolidation path

    private static HashSet<string> GetVirtualColumnsForTable(string tableName)
    {
      try
      {
        var userConfig = UserConfig.LoadOrDefault();
        var virtualColumns = userConfig.GetVirtualColumnsForTable(tableName);
        return new HashSet<string>(
          virtualColumns.Select(vc => vc.ColumnName).Where(name => !string.IsNullOrWhiteSpace(name)),
          StringComparer.OrdinalIgnoreCase
        );
      }
      catch (Exception ex)
      {
        Program.Log($"Error loading virtual columns for table {tableName}", ex);
        return new HashSet<string>();
      }
    }

    private static void CopyDatabaseCombined(string sourcePath, SqlCeConnection destConn, string sourceTag, string sourcePathDir)
    {
      string tempCopyPath;
      using (var srcConn = SqlCeUtils.OpenWithFallback(sourcePath, out tempCopyPath))
      {
        try
        {
          var tables = new List<string>();
          using (var cmd = new SqlCeCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'TABLE'", srcConn))
          {
            using (var r = cmd.ExecuteReader())
            {
              while (r.Read()) tables.Add(r.GetString(0));
            }
          }
          foreach (string table in tables)
          {
            if (table.StartsWith("__")) continue;

            EnsureDestinationTableCombined(destConn, srcConn, table);
            CopyRowsCombined(srcConn, destConn, table, sourceTag, sourcePathDir);
          }
        }
        finally
        {
          if (!string.IsNullOrEmpty(tempCopyPath))
          {
            try { File.Delete(tempCopyPath); } catch { }
          }
        }
      }
    }

    // removed legacy prefixed consolidation path

    private static void EnsureDestinationTableCombined(SqlCeConnection dest, SqlCeConnection src, string tableName)
    {
      using (var cmd = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", dest))
      {
        cmd.Parameters.AddWithValue("@t", tableName);
        var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        if (!exists)
        {
                    // Get virtual columns to exclude from consolidation
          var virtualColumns = GetVirtualColumnsForTable(tableName);
          if (virtualColumns.Count > 0)
          {
            Program.Log($"Excluding {virtualColumns.Count} virtual columns from table {tableName}: {string.Join(", ", virtualColumns)}");
          }

          // Create like source
          var cols = src.GetSchema("Columns", new[] { null, null, tableName, null });
          var columnDefs = new List<string>();
          foreach (DataRow col in cols.Rows)
          {
            string colName = col["COLUMN_NAME"].ToString();

            // Skip virtual columns - they shouldn't be in the consolidated database
            if (virtualColumns.Contains(colName))
            {
              continue;
            }

            string dataType = col["DATA_TYPE"].ToString();
            int length = col.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
            bool nullable = col.Table.Columns.Contains("IS_NULLABLE") && string.Equals(col["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);
            string typeSql = MapType(dataType, length);
            string nullSql = nullable ? "NULL" : "NOT NULL";
            columnDefs.Add($"[{colName}] {typeSql} {nullSql}");
          }
          columnDefs.Add("[SourceWorkOrder] NVARCHAR(255) NULL");
          columnDefs.Add("[SourcePath] NVARCHAR(1024) NULL");
          string createSql = $"CREATE TABLE [{tableName}] (" + string.Join(", ", columnDefs) + ")";
          using (var create = new SqlCeCommand(createSql, dest))
          {
            create.ExecuteNonQuery();
          }
        }
        else
        {
                    try
          {
            // Get virtual columns to exclude from consolidation
            var virtualColumns = GetVirtualColumnsForTable(tableName);

            // Add columns if missing in dest (create superset schema)
            var srcCols = src.GetSchema("Columns", new[] { null, null, tableName, null });
            var destCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var dc = new SqlCeCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", dest))
            {
              dc.Parameters.AddWithValue("@t", tableName);
              using (var r = dc.ExecuteReader())
              {
                while (r.Read()) destCols.Add(r.GetString(0));
              }
            }
            foreach (DataRow col in srcCols.Rows)
            {
              string colName = col["COLUMN_NAME"].ToString();
              if (destCols.Contains(colName)) continue;

              // Skip virtual columns - they shouldn't be in the consolidated database
              if (virtualColumns.Contains(colName)) continue;

              string dataType = col["DATA_TYPE"].ToString();
              int length = srcCols.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
              string typeSql = MapType(dataType, length);
              using (var alter = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [" + colName + "] " + typeSql + " NULL", dest)) alter.ExecuteNonQuery();
            }
            // Ensure source metadata columns
            using (var c1 = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME='SourceWorkOrder'", dest))
            {
              c1.Parameters.AddWithValue("@t", tableName);
              if (Convert.ToInt32(c1.ExecuteScalar()) == 0)
              {
                using (var a1 = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [SourceWorkOrder] NVARCHAR(255) NULL", dest)) a1.ExecuteNonQuery();
              }
            }
            using (var c2 = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME='SourcePath'", dest))
            {
              c2.Parameters.AddWithValue("@t", tableName);
              if (Convert.ToInt32(c2.ExecuteScalar()) == 0)
              {
                using (var a2 = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [SourcePath] NVARCHAR(1024) NULL", dest)) a2.ExecuteNonQuery();
              }
            }
          }
          catch { }
        }
      }
    }

    private static string MapType(string dataType, int length)
    {
      string dt = dataType.ToLowerInvariant();
      switch (dt)
      {
        case "nvarchar":
        case "nchar":
        case "ntext":
          if (length <= 0) return "NTEXT";
          // SQL CE max NVARCHAR length is 4000; longer must be NTEXT
          return length > 4000 ? "NTEXT" : $"NVARCHAR({Math.Min(length, 4000)})";
        case "varchar":
        case "char":
        case "text":
          return length > 0 ? $"VARCHAR({length})" : "TEXT";
        case "int":
        case "integer":
          return "INT";
        case "bigint":
          return "BIGINT";
        case "smallint":
          return "SMALLINT";
        case "tinyint":
          return "TINYINT";
        case "bit":
          return "BIT";
        case "decimal":
        case "numeric":
          return "DECIMAL(18,4)";
        case "float":
          return "FLOAT";
        case "real":
          return "REAL";
        case "money":
        case "smallmoney":
          return "MONEY";
        case "datetime":
        case "smalldatetime":
          return "DATETIME";
        case "uniqueidentifier":
          return "UNIQUEIDENTIFIER";
        case "image":
        case "varbinary":
          return "IMAGE";
        default:
          return "NVARCHAR(255)";
      }
    }

    // removed legacy prefixed consolidation path

        private static void CopyRowsCombined(SqlCeConnection src, SqlCeConnection dest, string tableName, string sourceTag, string sourcePathDir)
    {
      // Get virtual columns to exclude from consolidation
      var virtualColumns = GetVirtualColumnsForTable(tableName);

      using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", src))
      using (var reader = cmd.ExecuteReader())
      {
        var schema = reader.GetSchemaTable();
        var allColNames = schema.Rows.Cast<DataRow>().Select(r => r["ColumnName"].ToString()).ToList();

        // Filter out virtual columns from data copying
        var colNames = allColNames.Where(name => !virtualColumns.Contains(name)).ToList();

        if (virtualColumns.Count > 0)
        {
          var excludedFromData = allColNames.Where(name => virtualColumns.Contains(name)).ToList();
          if (excludedFromData.Count > 0)
          {
            Program.Log($"Excluding {excludedFromData.Count} virtual columns from data copy for table {tableName}: {string.Join(", ", excludedFromData)}");
          }
        }

        var destColumns = new List<string>(colNames.Select(n => "[" + n + "]"));
        destColumns.Add("[SourceWorkOrder]");
        destColumns.Add("[SourcePath]");
        var destParams = new List<string>(colNames.Select(n => "@" + n));
        destParams.Add("@__srcWO");
        destParams.Add("@__srcPath");
        string insertSql = $"INSERT INTO [" + tableName + "] (" + string.Join(", ", destColumns) + ") VALUES (" + string.Join(", ", destParams) + ")";
        using (var insert = new SqlCeCommand(insertSql, dest))
        {
          foreach (var name in colNames)
          {
            insert.Parameters.Add(new SqlCeParameter("@" + name, DBNull.Value));
          }
          insert.Parameters.Add(new SqlCeParameter("@__srcWO", sourceTag ?? string.Empty));
          insert.Parameters.Add(new SqlCeParameter("@__srcPath", sourcePathDir ?? string.Empty));
          int rowIndex = 0;
          while (reader.Read())
          {
            try
            {
              // Map filtered column names to their original positions in the reader
              for (int i = 0; i < colNames.Count; i++)
              {
                var colName = colNames[i];
                var originalIndex = allColNames.IndexOf(colName);
                object value = reader.GetValue(originalIndex);
                var p = insert.Parameters["@" + colName];
                p.Value = value is DBNull ? (object)DBNull.Value : value;
              }
              foreach (SqlCeParameter p in insert.Parameters)
              {
                if (p.Value == null) p.Value = DBNull.Value;
              }

              // Apply in-memory overrides by LinkID if present and skip deleted rows
              try
              {
                int linkOrdinal = -1;
                try { linkOrdinal = reader.GetOrdinal("LinkID"); } catch { linkOrdinal = -1; }
                if (linkOrdinal >= 0)
                {
                  var linkVal = reader.GetValue(linkOrdinal);
                  if (linkVal != DBNull.Value && linkVal != null)
                  {
                    var linkKey = Convert.ToString(linkVal);
                    if (Program.Edits.IsDeleted(tableName, linkKey)) { rowIndex++; continue; }
                    var overrides = Program.Edits.SnapshotTable(tableName);
                    if (overrides.TryGetValue(linkKey, out var rowOverrides))
                    {
                      foreach (var kv in rowOverrides)
                      {
                        var paramName = "@" + kv.Key;
                        if (insert.Parameters.Contains(paramName))
                        {
                          insert.Parameters[paramName].Value = kv.Value ?? (object)DBNull.Value;
                        }
                      }
                    }
                  }
                }
              }
              catch { }

              insert.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
              if (EnableCopyDebug)
              {
                LogInsertFailure(dest, tableName, tableName, colNames, insert, rowIndex, ex, sourceTag, sourcePathDir);
              }
              throw;
            }
            rowIndex++;
          }
        }
      }
    }

    private static void LogInsertFailure(SqlCeConnection dest, string srcTable, string destTable, List<string> colNames, SqlCeCommand insert, int rowIndex, Exception ex, string sourceTag, string sourcePath)
    {
      try
      {
        var destCols = new List<string>();
        using (var dc = new SqlCeCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", dest))
        {
          dc.Parameters.AddWithValue("@t", destTable);
          using (var r = dc.ExecuteReader())
          {
            while (r.Read()) destCols.Add(r.GetString(0));
          }
        }
        var paramDump = string.Join(
          ", ",
          insert.Parameters.Cast<SqlCeParameter>().Select(p => $"{p.ParameterName}={(p.Value == null ? "<null>" : (p.Value == DBNull.Value ? "<DBNull>" : Convert.ToString(p.Value)))}"));
        Program.Log($"INSERT FAILURE\n SrcTable={srcTable}\n DestTable={destTable}\n RowIndex={rowIndex}\n SourceTag={sourceTag}\n SourcePath={sourcePath}\n SrcCols=[{string.Join(", ", colNames)}]\n DestCols=[{string.Join(", ", destCols)}]\n SQL={insert.CommandText}\n Params=[{paramDump}]\n Error={ex}");
      }
      catch (Exception logEx)
      {
        Program.Log("LogInsertFailure error", logEx);
      }
    }

    private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
    {
      if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

      var column = metricsGrid.Columns[e.ColumnIndex];
      var columnName = column.DataPropertyName ?? column.Name;

      // Check if this is an action column
      var actionDef = virtualColumnDefs?.FirstOrDefault(def =>
        string.Equals(def.ColumnName, columnName, StringComparison.OrdinalIgnoreCase) && def.IsActionColumn);

      if (actionDef != null)
      {
        HandleActionColumnClick(actionDef, e.RowIndex);
      }
      else if (isEditModeMainGrid)
      {
        try
        {
          if (!metricsGrid.ReadOnly && metricsGrid.SelectionMode != DataGridViewSelectionMode.CellSelect)
          {
            metricsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
          }
          if (!metricsGrid.IsCurrentCellInEditMode)
          {
            metricsGrid.BeginEdit(true);
          }
        }
        catch { }
      }
    }

    private void HandleActionColumnClick(UserConfig.VirtualColumnDef actionDef, int rowIndex)
    {
      try
      {
        // Get the row data
        var row = metricsGrid.Rows[rowIndex];
        if (row?.DataBoundItem is DataRowView dataRowView)
        {
          var dataRow = dataRowView.Row;

          // Get the key value for this action
          object keyValue = null;
          if (!string.IsNullOrEmpty(actionDef.LocalKeyColumn) && dataRow.Table.Columns.Contains(actionDef.LocalKeyColumn))
          {
            keyValue = dataRow[actionDef.LocalKeyColumn];
          }

          // Execute the action based on type
          switch (actionDef.ActionType?.ToLowerInvariant())
          {
            case "3dviewer":
              Open3DViewer(keyValue);
              break;
            case "weblink":
              OpenWebLink(keyValue);
              break;
            case "export":
              ExportData(keyValue);
              break;
            default:
              MessageBox.Show($"Action '{actionDef.ActionType}' is not yet implemented.", "Action Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
              break;
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log($"Error executing action {actionDef.ActionType}", ex);
        MessageBox.Show($"Error executing action: {ex.Message}", "Action Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void Open3DViewer(object keyValue)
    {
      if (keyValue == null || keyValue == DBNull.Value)
      {
        MessageBox.Show("No valid ID found for 3D viewer.", "Cannot Open 3D Viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      try
      {
        string dbPathToUse;

        if (currentSourcePaths != null && currentSourcePaths.Count > 0)
        {
          // Use first available source SDF path
          dbPathToUse = currentSourcePaths.FirstOrDefault(path =>
            !string.IsNullOrWhiteSpace(path) && File.Exists(path));

          if (string.IsNullOrWhiteSpace(dbPathToUse))
          {
            MessageBox.Show("No valid database file found for 3D viewer.", "Database Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
          }
        }
        else
        {
          MessageBox.Show("No database available for 3D viewer.", "Database Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        using (var viewer = new Product3DViewer(keyValue.ToString(), dbPathToUse, currentSelectedTable))
        {
          viewer.ShowDialog(this);
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error opening 3D viewer", ex);
        MessageBox.Show($"Error opening 3D viewer: {ex.Message}", "3D Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void OpenWebLink(object keyValue)
    {
      // Placeholder for web link functionality
      MessageBox.Show($"Web link action for ID: {keyValue}", "Web Link", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportData(object keyValue)
    {
      // Placeholder for export functionality
      MessageBox.Show($"Export action for ID: {keyValue}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
  }

  internal sealed class WorkOrderEntry
  {
    public string DirectoryPath { get; set; }
    public string SdfPath { get; set; }
    public bool SdfExists { get; set; }
    public long FileSize { get; set; }
  }
}
