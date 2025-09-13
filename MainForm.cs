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
    // Suppress order persistence and related work during heavy programmatic updates/binding
    private bool isSuppressingOrderPersistence = false;
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

    // Cache key for virtual column lookup caches to avoid unnecessary rebuilds
    private string virtualCacheKey = null;

    // Clear virtual column caches when source data changes
    private void ClearVirtualColumnCaches()
    {
      virtualCacheKey = null;
      virtualLookupCacheByColumn.Clear();
      Program.Log("ClearVirtualColumnCaches: cleared all virtual column caches");
    }

    public MainForm()
    {
      InitializeComponent();
      // Wire toolbar selection buttons
      try
      {
        if (btnTableSelectAll != null) btnTableSelectAll.Click += btnTableSelectAll_Click;
        if (btnTableClearAll != null) btnTableClearAll.Click += btnTableClearAll_Click;
      }
      catch { }

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
      this.metricsGrid.DataError += metricsGrid_DataError;
      // Add context menu support and right-click handlers for grid
      this.metricsGrid.MouseDown += MetricsGrid_MouseDown;
      this.metricsGrid.Sorted += MetricsGrid_Sorted;
      this.metricsGrid.CurrentCellChanged += MetricsGrid_CurrentCellChanged;
      this.Shown += (s, e) =>
      {
        try
        {
          // Use synchronous loading instead of async
          ScanWorkOrders();

          // Defer applying saved splitter distance until after initial layout completes
          this.BeginInvoke(new Action(() =>
          {
            try
            {
              var cfg = UserConfig.LoadOrDefault();
              if (cfg.MainSplitterDistance > 0)
              {
                // Clamp desired distance within valid bounds
                int available = splitMain.Width - splitMain.SplitterWidth - splitMain.Panel2MinSize;
                if (available < splitMain.Panel1MinSize) available = splitMain.Panel1MinSize;
                int desired = cfg.MainSplitterDistance;
                int clamped = Math.Max(splitMain.Panel1MinSize, Math.Min(desired, available));
                if (clamped > 0)
                {
                  splitMain.SplitterDistance = clamped;
                }
              }
            }
            catch { }
          }));
        }
        catch (Exception ex)
        {
          Program.Log("Error during initial scan", ex);
          MessageBox.Show("Error during initial scan: " + ex.Message);
        }
      };

      // Restore window size and position from config
      this.Load += (s, e) =>
      {
        try
        {
          var cfg = UserConfig.LoadOrDefault();
          // Apply size
          if (cfg.MainWindowWidth > 0 && cfg.MainWindowHeight > 0)
          {
            int w = Math.Max(this.MinimumSize.Width, cfg.MainWindowWidth);
            int h = Math.Max(this.MinimumSize.Height, cfg.MainWindowHeight);
            this.Size = new System.Drawing.Size(w, h);
          }
          // Apply position if valid
          if (cfg.MainWindowX >= 0 && cfg.MainWindowY >= 0)
          {
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(cfg.MainWindowX, cfg.MainWindowY);
          }

          // Apply splitter distance if valid
          if (cfg.MainSplitterDistance > 0)
          {
            try
            {
              splitMain.SplitterDistance = Math.Max(splitMain.Panel1MinSize, cfg.MainSplitterDistance);
            }
            catch { }
          }
        }
        catch { }
      };

      // Persist window size and position
      this.FormClosing += (s, e) =>
      {
        try
        {
          var cfg = UserConfig.LoadOrDefault();
          cfg.MainWindowWidth = this.Width;
          cfg.MainWindowHeight = this.Height;
          cfg.MainWindowX = this.Location.X;
          cfg.MainWindowY = this.Location.Y;
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
            // Hard-lock: always record the actual splitter distance
            cfg.MainSplitterDistance = Math.Max(splitMain.Panel1MinSize, splitMain.SplitterDistance);
            cfg.Save();
          }
          catch { }
        };
      }
      catch { }

      // Re-apply saved splitter distance after any size/layout changes to keep it locked
      try
      {
        void ReapplySavedSplitterDistance()
        {
          try
          {
            var cfg = UserConfig.LoadOrDefault();
            if (cfg.MainSplitterDistance > 0 && splitMain.Width > 0)
            {
              int available = splitMain.Width - splitMain.SplitterWidth - splitMain.Panel2MinSize;
              if (available < splitMain.Panel1MinSize) available = splitMain.Panel1MinSize;
              int desired = cfg.MainSplitterDistance;
              int clamped = Math.Max(splitMain.Panel1MinSize, Math.Min(desired, available));
              if (clamped > 0 && clamped != splitMain.SplitterDistance)
              {
                splitMain.SplitterDistance = clamped;
              }
            }
          }
          catch { }
        }

        // Apply on initial Shown via BeginInvoke is already wired above.
        // Also apply on first few size changes to counter autosizing/DPI adjustments.
        int remainingApplies = 3; // small number to avoid infinite loops
        this.splitMain.SizeChanged += (s, e) =>
        {
          if (remainingApplies <= 0) return;
          remainingApplies--;
          this.BeginInvoke(new Action(ReapplySavedSplitterDistance));
        };

        // Apply when form finishes user resize
        this.ResizeEnd += (s, e) =>
        {
          this.BeginInvoke(new Action(ReapplySavedSplitterDistance));
        };
      }
      catch { }

      // Setup metrics grid refresh debounce
      metricsRefreshTimer = new System.Windows.Forms.Timer();
      metricsRefreshTimer.Interval = 50; // ms to coalesce repeated requests - reduced from 120ms for more responsive loading indicator
      metricsRefreshTimer.Tick += (s, e2) =>
      {
        if ((DateTime.UtcNow - lastRefreshRequestUtc).TotalMilliseconds >= metricsRefreshTimer.Interval)
        {
          metricsRefreshTimer.Stop();
          Program.Log($"metricsRefreshTimer.Tick: Timer fired, calling RefreshMetricsGrid (isRefreshingMetrics={isRefreshingMetrics})");
          if (!isRefreshingMetrics)
          {
            RefreshMetricsGrid();
          }
        }
      };

      // Subscribe to changes in the edit store
      Program.Edits.ChangesUpdated += (s, e) =>
      {
        UpdatePreviewChangesButton();
      };

      // Initialize button state
      UpdatePreviewChangesButton();
    }

    // Flag to track when we're refreshing the table to prevent loading indicator from being hidden prematurely
    private bool isRefreshingTable = false;

    // Suppress grid binding errors and log them to avoid intrusive dialogs
    private void metricsGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
    {
      try
      {
        Program.Log($"DataGridView DataError at row {e.RowIndex}, column {e.ColumnIndex}", e.Exception);
        e.ThrowException = false;
      }
      catch { }
    }

    // Safely rebind a BindingSource to a new DataTable while keeping the grid stable
    private void RebindBindingSourceSafely(BindingSource bindingSource, DataTable newTable)
    {
      try
      {
        if (metricsGrid == null || bindingSource == null) return;
        Program.Log($"RebindBindingSourceSafely: Rebinding to table '{newTable?.TableName ?? "<null>"}' with {newTable?.Rows.Count ?? 0} rows");
        // Suppress persistence and layout-driven event work during rebind
        var prevApplying = isApplyingLayout; isApplyingLayout = true;
        var prevSuppress = isSuppressingOrderPersistence; isSuppressingOrderPersistence = true;
        try { if (orderPersistTimer != null && orderPersistTimer.Enabled) { orderPersistTimer.Stop(); Program.Log("RebindBindingSourceSafely: stopped pending order persistence timer"); } } catch { }
        metricsGrid.SuspendLayout();
        var prevVisible = metricsGrid.Visible;
        metricsGrid.Visible = false;
        try
        {
          try { metricsGrid.CurrentCell = null; } catch { }
          try { metricsGrid.ClearSelection(); } catch { }

          // Rebind through the BindingSource without detaching the grid to preserve columns/order
          try { bindingSource.SuspendBinding(); } catch { }
          bindingSource.RaiseListChangedEvents = false;
          bindingSource.DataSource = newTable;
          bindingSource.RaiseListChangedEvents = true;
          bindingSource.ResetBindings(false);
          try { bindingSource.ResumeBinding(); } catch { }
        }
        finally
        {
          metricsGrid.Visible = prevVisible;
          metricsGrid.ResumeLayout();
          metricsGrid.Refresh();
          try { if (orderPersistTimer != null && orderPersistTimer.Enabled) { orderPersistTimer.Stop(); Program.Log("RebindBindingSourceSafely: ensured order persistence timer is stopped after rebind"); } } catch { }
          lastOrderChangeUtc = DateTime.UtcNow;
          // Restore flags after rebind
          isApplyingLayout = prevApplying;
          isSuppressingOrderPersistence = prevSuppress;
        }
      }
      catch (Exception ex)
      {
        Program.Log("RebindBindingSourceSafely error", ex);
      }
    }

    // Ensure the filtered table has all columns expected by the grid (including virtuals)
    private void EnsureTableHasGridColumns(DataTable table)
    {
      try
      {
        if (table == null || metricsGrid == null) return;
        // Always ensure built-in _SourceFile exists so bound columns resolve
        if (!table.Columns.Contains("_SourceFile")) table.Columns.Add("_SourceFile", typeof(string));

        // Add any missing bound columns the grid expects
        foreach (DataGridViewColumn gridCol in metricsGrid.Columns)
        {
          var key = !string.IsNullOrEmpty(gridCol.DataPropertyName) ? gridCol.DataPropertyName : gridCol.Name;
          if (string.IsNullOrWhiteSpace(key)) continue;
          if (!table.Columns.Contains(key))
          {
            // Default to string type for missing columns; values can be filled later as needed
            table.Columns.Add(key, typeof(string));
          }
        }

        // Also ensure virtual columns list exist if tracked
        if (virtualColumnNames != null)
        {
          foreach (var name in virtualColumnNames)
          {
            if (!string.IsNullOrWhiteSpace(name) && !table.Columns.Contains(name))
            {
              table.Columns.Add(name, typeof(string));
            }
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("EnsureTableHasGridColumns error", ex);
      }
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
      // Prevent concurrent scans
      if (isScanningWorkOrders)
      {
        Program.Log("Scan already in progress, skipping duplicate request");
        return;
      }

      isScanningWorkOrders = true;
      var scanStartTime = DateTime.Now;
      Program.Log($"=== SCAN STARTED at {scanStartTime:HH:mm:ss.fff} ===");

      ShowLoadingIndicator(true, "Scanning work orders...");
      listWorkOrders.Items.Clear();
      string root = DefaultRoot;
      Program.Log($"Scanning root directory: {root}");

      if (!Directory.Exists(root))
      {
        Program.Log($"Root directory not found: {root}");
        MessageBox.Show("Root directory not found.");
        ShowLoadingIndicator(false);
        return;
      }

      try
      {
        // Get all directories - this is the only operation we need
        var dirScanStart = DateTime.Now;
        Program.Log($"Starting directory enumeration at {dirScanStart:HH:mm:ss.fff}");

        var dirs = Directory.GetDirectories(root);
        var totalDirs = dirs.Length;
        var dirScanEnd = DateTime.Now;
        var dirScanDuration = dirScanEnd - dirScanStart;

        Program.Log($"Directory enumeration completed in {dirScanDuration.TotalMilliseconds:F0}ms - Found {totalDirs} directories");

        // Create work order entries with minimal information
        var results = new List<WorkOrderEntry>();
        foreach (var dir in dirs)
        {
          try
          {
            string sdfPath = Path.Combine(dir, GetSdfFileName());

            var entry = new WorkOrderEntry
            {
              DirectoryPath = dir,
              SdfPath = sdfPath,
              SdfExists = File.Exists(sdfPath), // Simple check, no file info needed
              FileSize = 0 // Not needed, set to 0
            };

            results.Add(entry);
          }
          catch (Exception ex)
          {
            Program.Log($"Error processing directory {dir}", ex);
          }
        }

        var parallelEnd = DateTime.Now;
        var parallelDuration = parallelEnd - dirScanStart;
        Program.Log($"Processing completed in {parallelDuration.TotalMilliseconds:F0}ms");
        Program.Log($"Processed {results.Count} work orders");

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

        ShowLoadingIndicator(false);
        isScanningWorkOrders = false;
        RequestRefreshMetricsGrid();
      }
    }

    private void btnSelectAll_Click(object sender, EventArgs e)
    {
      try
      {
        // Select all work orders by adding them to checkedDirs
        foreach (var wo in filteredWorkOrders)
        {
          checkedDirs.Add(wo.DirectoryPath);
        }
        listWorkOrders.Invalidate(); // Refresh the display
        RequestRefreshMetricsGrid(); // Update metrics if needed
        Program.Log($"Selected all {filteredWorkOrders.Count} work orders");
      }
      catch (Exception ex)
      {
        Program.Log("btnSelectAll_Click error", ex);
      }
    }

    private void btnSelectNone_Click(object sender, EventArgs e)
    {
      try
      {
        // Clear all selections by clearing checkedDirs
        checkedDirs.Clear();
        listWorkOrders.Invalidate(); // Refresh the display
        RequestRefreshMetricsGrid(); // Update metrics if needed
        Program.Log("Cleared all work order selections");
      }
      catch (Exception ex)
      {
        Program.Log("btnSelectNone_Click error", ex);
      }
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

        // Reset progress bar after successful consolidation
        progress.Value = 0;
      }
      catch (Exception ex)
      {
        Program.Log("Consolidation error", ex);
        MessageBox.Show("Error: " + ex.Message);

        // Reset progress bar on error as well
        progress.Value = 0;
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

    private void CheckForUpdates_Click(object sender, EventArgs e)
    {
      try
      {
        // Force update check regardless of rate limiting
        var config = UserConfig.LoadOrDefault();
        config.LastUpdateCheck = DateTime.MinValue; // Reset to allow immediate check
        config.Save();

        Program.CheckForUpdates(silent: false);
      }
      catch (Exception ex)
      {
        Program.Log("CheckForUpdates_Click error", ex);
        MessageBox.Show("Failed to check for updates: " + ex.Message, "Error",
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
          FilterWorkOrders();
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
        Dock = DockStyle.Top,
        ColumnCount = 4,
        RowCount = 4,
        Padding = new Padding(10, 10, 10, 10),
        Height = 150,
      };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

      table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

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

      var lblSdf = new Label { Text = ".sdf File Name:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtSdfLocal = new TextBox
      {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Width = 400,
        Text = GetSdfFileName()
      };

      // Hide Purchasing option
      var cfg = UserConfig.LoadOrDefault();
      var chkHidePurchasing = new CheckBox {
        Text = "Hide Purchasing",
        AutoSize = true,
        Checked = cfg.HidePurchasing,
        Anchor = AnchorStyles.Left,
        Tag = "HidePurchasing"
      };
      // Add tooltip for Hide Purchasing checkbox
      var toolTipHidePurchasing = new ToolTip();
      toolTipHidePurchasing.SetToolTip(chkHidePurchasing, "Hide purchasing work orders from the list.");

      var chkDynamicSheetCosts = new CheckBox {
        Text = "Dynamic Sheet Costs",
        AutoSize = true,
        Checked = cfg.DynamicSheetCosts,
        Anchor = AnchorStyles.Left,
        Tag = "DynamicSheetCosts"
      };
      // Add tooltip for Dynamic Sheet Costs checkbox
      var toolTipDynamicSheetCosts = new ToolTip();
      toolTipDynamicSheetCosts.SetToolTip(
        chkDynamicSheetCosts,
        "If checked, dynamic sheet costs will be calculated based on the width, length, " +
        "and thickness of the sheet. This will replace values we may have added in the database."
      );

      // Layout rows
      table.Controls.Add(lblRoot, 0, 0);
      table.Controls.Add(txtRootLocal, 1, 0);
      table.Controls.Add(btnBrowseRoot, 2, 0);
      table.SetColumnSpan(txtRootLocal, 1);

      table.Controls.Add(lblSdf, 0, 1);
      table.Controls.Add(txtSdfLocal, 1, 1);
      table.SetColumnSpan(txtSdfLocal, 2);

      // Add Hide Purchasing on its own row spanning available columns
      table.Controls.Add(chkHidePurchasing, 0, 3);
      table.SetColumnSpan(chkHidePurchasing, 3);

      // Add Dynamic Sheet Costs on its own row spanning available columns
      table.Controls.Add(chkDynamicSheetCosts, 0, 4);
      table.SetColumnSpan(chkDynamicSheetCosts, 3);

      // Buttons row with Check Updates on left, OK/Cancel on right
      var btnCheckUpdates = new Button
      {
        Text = "Check for Updates",
        AutoSize = true
      };
      btnCheckUpdates.Click += CheckForUpdates_Click;

      var panelButtons = new TableLayoutPanel
      {
        Dock = DockStyle.Bottom,
        ColumnCount = 4,
        RowCount = 1,
        Padding = new Padding(10, 5, 10, 5),
        Height = 40,
      };
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Check Updates
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // spacer
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // OK
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Cancel

      var btnOk = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true };
      panelButtons.Controls.Add(btnCheckUpdates, 0, 0); // Far left
      panelButtons.Controls.Add(btnOk, 2, 0);

      // Add panelButtons to dialog
      panelButtons.Dock = DockStyle.Bottom;
      dlg.Controls.Add(panelButtons);

      if (dlg.ShowDialog(this) == DialogResult.OK)
      {
        try
        {
          var cfgNow = UserConfig.LoadOrDefault();
          var newRoot = (txtRootLocal.Text ?? string.Empty).Trim();
          var newSdf = (txtSdfLocal.Text ?? string.Empty).Trim();
          if (!string.IsNullOrEmpty(newRoot)) cfgNow.DefaultRoot = newRoot;
          if (!string.IsNullOrEmpty(newSdf)) cfgNow.SdfFileName = newSdf;
          cfgNow.HidePurchasing = chkHidePurchasing.Checked; // persist Hide Purchasing
          cfgNow.DynamicSheetCosts = chkDynamicSheetCosts.Checked; // persist Dynamic Sheet Costs
          cfgNow.Save();
          FilterWorkOrders(); // apply filter immediately if changed
        }
        catch (Exception ex)
        {
          Program.Log("Failed to save settings dialog changes", ex);
        }
      }
    }

    private void FilterWorkOrders()
    {
      string query = txtSearch.Text.Trim();

      var cfg = UserConfig.LoadOrDefault();
      bool hidePurchasing = cfg.HidePurchasing;

      var next = allWorkOrders.Where(wo =>
        (string.IsNullOrEmpty(query) || wo.DirectoryPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
        && (!hidePurchasing || wo.DirectoryPath.IndexOf("purchasing", StringComparison.OrdinalIgnoreCase) < 0)
      ).ToList();

      filteredWorkOrders = next;
      listWorkOrders.VirtualListSize = filteredWorkOrders.Count;
      listWorkOrders.Invalidate();
    }

    private void listWorkOrders_SelectedIndexChanged(object sender, EventArgs e)
    {
      try
      {
        // If no work orders are selected at all, clear the metrics grid and return early
        if (checkedDirs.Count == 0 && listWorkOrders.SelectedIndices.Count == 0)
        {
          ClearMetricsGridFast();
          // Also hide the loading indicator as there's nothing to load
          ShowLoadingIndicator(false);
          return; // Exit early to prevent PopulateTableSelector from triggering a refresh
        }

        // Show loading immediately when work order selection changes
        ShowLoadingIndicator(true, "Loading work order data...");
        Application.DoEvents();

        PopulateTableSelector();
        RequestRefreshMetricsGrid();
      }
      catch { }
    }

    private void RequestRefreshMetricsGrid()
    {
      // Start timing to diagnose performance around refresh requests
      var reqStart = DateTime.Now; // capture start time

      lastRefreshRequestUtc = DateTime.UtcNow; // record last request
      Program.Log($"RequestRefreshMetricsGrid: start, timerEnabled={metricsRefreshTimer.Enabled}, isRefreshingMetrics={isRefreshingMetrics}, isRefreshingTable={isRefreshingTable}, intervalMs={metricsRefreshTimer.Interval}");
      if (!metricsRefreshTimer.Enabled)
      {
        metricsRefreshTimer.Start();
      }
      var reqEnd = DateTime.Now; // capture end time
      var reqDuration = reqEnd - reqStart; // compute duration
      Program.Log($"RequestRefreshMetricsGrid: completed in {reqDuration.TotalMilliseconds:F0}ms; timerEnabled={metricsRefreshTimer.Enabled}");
    }

    private void PopulateTableSelector()
    {
      try
      {
        suppressTableSelectorChanged = true;
        try
        {
          // Overall timing for PopulateTableSelector
          var overallStart = DateTime.Now; // start timing

          // Remember the previously selected table name so we can restore it
          string previousTableName = null; // holds previous selection
          if (cmbTableSelector.SelectedItem is TableSelectorItem prev)
          {
            previousTableName = prev.TableName;
          }

          // Log initial state for diagnostics
          Program.Log($"PopulateTableSelector: start, prev='{(string.IsNullOrWhiteSpace(previousTableName) ? "<none>" : previousTableName)}', breakdownMetricsCount={breakdownMetrics.Count()}");

          // Rebuild the items list from breakdownMetrics
          var clearStart = DateTime.Now; // timing clear
          cmbTableSelector.Items.Clear(); // clear items first
          var clearEnd = DateTime.Now; // timing clear end

          // Add table options from breakdownMetrics
          var addStart = DateTime.Now; // timing add loop
          foreach (var (label, table) in breakdownMetrics)
          {
            var item = new TableSelectorItem { Label = label, TableName = table };
            cmbTableSelector.Items.Add(item);
          }
          var addEnd = DateTime.Now; // timing add loop end

          // Try to restore the previously selected table if it still exists
          bool restored = false; // track whether restored
          var restoreStart = DateTime.Now; // timing restore
          if (!string.IsNullOrWhiteSpace(previousTableName))
          {
            for (int i = 0; i < cmbTableSelector.Items.Count; i++)
            {
              if (cmbTableSelector.Items[i] is TableSelectorItem it &&
                  string.Equals(it.TableName, previousTableName, StringComparison.OrdinalIgnoreCase))
              {
                cmbTableSelector.SelectedIndex = i; // restore previous selection
                restored = true;
                break;
              }
            }
          }
          var restoreEnd = DateTime.Now; // timing restore end

          // If nothing restored, ensure we have a valid selection when items exist
          if (!restored && cmbTableSelector.Items.Count > 0 && cmbTableSelector.SelectedIndex < 0)
          {
            cmbTableSelector.SelectedIndex = 0; // default to first item only when needed
          }

          // Log timing details for diagnostics
          var overallEnd = DateTime.Now; // end timing
          Program.Log($"PopulateTableSelector: end, items={cmbTableSelector.Items.Count}, restored={restored}, selectedIndex={cmbTableSelector.SelectedIndex}, clearMs={(clearEnd - clearStart).TotalMilliseconds:F0}, addMs={(addEnd - addStart).TotalMilliseconds:F0}, restoreMs={(restoreEnd - restoreStart).TotalMilliseconds:F0}, totalMs={(overallEnd - overallStart).TotalMilliseconds:F0}");
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

        Program.Log("cmbTableSelector_SelectedIndexChanged: Starting table refresh");

        // Set flag to indicate we're refreshing the table
        isRefreshingTable = true;

        // Show loading indicator immediately when table selector changes
        ShowLoadingIndicator(true, "Loading table data...");

        // Ensure the loading indicator stays visible by forcing a UI update
        Application.DoEvents();

        // Update fronts filter button state immediately
        UpdateFrontsFilterButtonState();

        RequestRefreshMetricsGrid();
      }
      catch (Exception ex)
      {
        Program.Log("Error in table selector changed", ex);
        isRefreshingTable = false;
      }
    }

    private void RefreshMetricsGrid()
    {
      var refreshStart = DateTime.Now;
      var caller = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
      Program.Log($"=== REFRESH METRICS GRID STARTED at {refreshStart:HH:mm:ss.fff} - Called by: {caller} ===");

      // Set refresh flag to prevent concurrent refreshes
      isRefreshingMetrics = true;
      Program.Log($"RefreshMetricsGrid: isRefreshingMetrics set to true, isRefreshingTable={isRefreshingTable}");

      try
      {
        // Loading indicator is already shown by cmbTableSelector_SelectedIndexChanged
        // No need to show it again here

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
            ClearVirtualColumnCaches(); // Clear caches when source data changes

            // No dialog reuse; data is loaded directly in MainForm now
            bool canReuseDialog = false;

            if (canReuseDialog) { }

            if (!canReuseDialog)
            {
              // Build and bind data directly
              var buildStart = DateTime.Now;
              Program.Log($"Building data table for '{selectedTable.TableName}' from {sourcePaths.Count} source(s)");
              var data = BuildDataTableFromSources(selectedTable.TableName, sourcePaths);
              var buildEnd = DateTime.Now;
              Program.Log($"BuildDataTableFromSources completed in {(buildEnd - buildStart).TotalMilliseconds:F0}ms");

              var bindStart = DateTime.Now;
              metricsGrid.SuspendLayout();
              var prevVisible = metricsGrid.Visible;
              metricsGrid.Visible = false;

              // Reduce binding cost by disabling autosizing and autosize calculations during bind
              var prevAutoSizeRows = metricsGrid.AutoSizeRowsMode;
              var prevAutoSizeCols = metricsGrid.AutoSizeColumnsMode;
              var prevRowHeadersWidthSizeMode = metricsGrid.RowHeadersWidthSizeMode;
              var prevColumnHeadersHeightSizeMode = metricsGrid.ColumnHeadersHeightSizeMode;
              metricsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
              metricsGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
              metricsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
              // Note: AutoSizeColumnsMode may be set elsewhere; keep it None during binding
              try { metricsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; } catch { }
              // Also set applying layout to suppress any visual/style work in handlers
              var prevApplyingLayout = isApplyingLayout;
              try
              {
                Program.Log("Setting DataSource and starting virtual column operations");

                // Strong suppression: prevent order persistence while binding
                isSuppressingOrderPersistence = true;
                Program.Log("RefreshMetricsGrid: suppressing order persistence during binding");

                isApplyingLayout = true;

                metricsGrid.AutoGenerateColumns = true;
                metricsGrid.DataSource = new BindingSource { DataSource = data };
                var dataSourceSet = DateTime.Now;
                Program.Log($"DataSource set in {(dataSourceSet - bindStart).TotalMilliseconds:F0}ms");

                var virtualStart = DateTime.Now;
                ApplyVirtualColumnsAndLayout(selectedTable.TableName, data);
                var virtualEnd = DateTime.Now;
                Program.Log($"ApplyVirtualColumnsAndLayout completed in {(virtualEnd - virtualStart).TotalMilliseconds:F0}ms");

                var configStart = DateTime.Now;
                ApplyUserConfigToMetricsGrid(selectedTable.TableName);
                var configEnd = DateTime.Now;
                Program.Log($"ApplyUserConfigToMetricsGrid completed in {(configEnd - configStart).TotalMilliseconds:F0}ms");

                // Ensure events and context menus are wired after binding
                WireUpGridEvents();
                AddGridContextMenu();
                Program.Log("Metrics grid events/context menu wired after binding");
                // Apply disabled style to excluded rows
                ApplyDisabledRowStyling();
              }
              finally
              {
                // Re-enable persistence after binding completes
                isSuppressingOrderPersistence = false;
                Program.Log("RefreshMetricsGrid: re-enabled order persistence after binding");
                // Restore applying layout flag
                isApplyingLayout = prevApplyingLayout;

                // Restore autosize settings
                try { metricsGrid.AutoSizeColumnsMode = prevAutoSizeCols; } catch { }
                metricsGrid.AutoSizeRowsMode = prevAutoSizeRows;
                metricsGrid.RowHeadersWidthSizeMode = prevRowHeadersWidthSizeMode;
                metricsGrid.ColumnHeadersHeightSizeMode = prevColumnHeadersHeightSizeMode;

                metricsGrid.Visible = prevVisible;
                metricsGrid.ResumeLayout();
                metricsGrid.Refresh();
                panelMetricsBorder.Refresh();
              }
              var bindEnd = DateTime.Now;
              Program.Log($"Data bind completed in {(bindEnd - bindStart).TotalMilliseconds:F0}ms (build: {(bindStart - buildStart).TotalMilliseconds:F0}ms)");
            }
            else { }
          }
          else
          {
            // No sources selected – clear the metrics grid
            Program.Log("No work orders selected; clearing metrics grid");
            currentSourcePaths = new List<string>();
            ClearVirtualColumnCaches(); // Clear caches when clearing source data
            metricsGrid.SuspendLayout();
            var prevVisible = metricsGrid.Visible;
            metricsGrid.Visible = false;
            try
            {
              metricsGrid.DataSource = null;
              metricsGrid.Rows.Clear();
            }
            catch { }
            finally
            {
              metricsGrid.Visible = prevVisible;
              metricsGrid.ResumeLayout();
              metricsGrid.Refresh();
              panelMetricsBorder.Refresh();
            }
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

        // Clear table refresh flag first
        isRefreshingTable = false;
        Program.Log($"RefreshMetricsGrid: isRefreshingTable set to false");

        // Hide loading indicator
        ShowLoadingIndicator(false);

        // Clear refresh flag
        isRefreshingMetrics = false;
        Program.Log($"RefreshMetricsGrid: isRefreshingMetrics set to false");
      }
    }

    // Quickly clear the metrics grid when no work orders are selected
    private void ClearMetricsGridFast()
    {
      try
      {
        Program.Log("ClearMetricsGridFast: clearing metrics grid due to no selection");
        currentSourcePaths = new List<string>();
        ClearVirtualColumnCaches(); // Clear caches when clearing source data
        if (metricsGrid == null) return;
        metricsGrid.SuspendLayout();
        var prevVisible = metricsGrid.Visible;
        metricsGrid.Visible = false;
        try
        {
          metricsGrid.DataSource = null;
          metricsGrid.Rows.Clear();
        }
        catch { }
        finally
        {
          metricsGrid.Visible = prevVisible;
          metricsGrid.ResumeLayout();
          metricsGrid.Refresh();
          panelMetricsBorder.Refresh();
        }
      }
      catch { }
    }

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

        // Enable custom header styling (disable visual styles for headers)
        metricsGrid.EnableHeadersVisualStyles = true;

        // Set selection colors for better visibility
        metricsGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(215, 245, 255); // Light blue selection
        metricsGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black; // Black text on selection

        // Wire up any MainForm-specific grid events here
        // Column resizing and reordering persistence
        metricsGrid.ColumnWidthChanged -= MetricsGrid_ColumnWidthChanged;
        metricsGrid.ColumnWidthChanged += MetricsGrid_ColumnWidthChanged;
        metricsGrid.ColumnDisplayIndexChanged -= MetricsGrid_ColumnDisplayIndexChanged;
        metricsGrid.ColumnDisplayIndexChanged += MetricsGrid_ColumnDisplayIndexChanged;
        // Context-aware menus
        metricsGrid.MouseDown -= MetricsGrid_MouseDown;
        metricsGrid.MouseDown += MetricsGrid_MouseDown;

        // Use DoubleClick on the grid (not just CellDoubleClick) so header/blank areas also toggle
        metricsGrid.DoubleClick -= MetricsGrid_DoubleClick;
        metricsGrid.DoubleClick += MetricsGrid_DoubleClick;
        // Also hook the parent panel so double-clicking padding/border toggles too
        try
        {
          if (panelMetricsBorder != null)
          {
            panelMetricsBorder.DoubleClick -= MetricsGrid_DoubleClick;
            panelMetricsBorder.DoubleClick += MetricsGrid_DoubleClick;
          }
        }
        catch { }
        metricsGrid.KeyDown -= MetricsGrid_KeyDown;
        metricsGrid.KeyDown += MetricsGrid_KeyDown;
        // Virtual column action handling
        metricsGrid.CellClick -= Grid_CellClick;
        metricsGrid.CellClick += Grid_CellClick;
        // Disabled-style formatting for excluded rows
        metricsGrid.CellFormatting -= MetricsGrid_CellFormatting_DisabledStyle;
        metricsGrid.CellFormatting += MetricsGrid_CellFormatting_DisabledStyle;

        // Edit-mode related events
        metricsGrid.CellValueChanged -= MetricsGrid_CellValueChanged_Edit;
        metricsGrid.CellValueChanged += MetricsGrid_CellValueChanged_Edit;
        metricsGrid.CurrentCellDirtyStateChanged -= MetricsGrid_CurrentCellDirtyStateChanged_Edit;
        metricsGrid.CurrentCellDirtyStateChanged += MetricsGrid_CurrentCellDirtyStateChanged_Edit;
        metricsGrid.CellEndEdit -= MetricsGrid_CellEndEdit_Edit;
        metricsGrid.CellEndEdit += MetricsGrid_CellEndEdit_Edit;

        // Header styling updates when current cell changes
        metricsGrid.CurrentCellChanged -= MetricsGrid_CurrentCellChanged;
        metricsGrid.CurrentCellChanged += MetricsGrid_CurrentCellChanged;

        // Reapply excluded styling when user sorts or reorders columns
        metricsGrid.Sorted -= MetricsGrid_Sorted;
        metricsGrid.Sorted += MetricsGrid_Sorted;

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
              Program.Log("Order persistence timer fired, calling PersistCurrentOrderSafely");
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

    // Per-cell styling to indicate excluded (non-selected) rows
    private void MetricsGrid_CellFormatting_DisabledStyle(object sender, DataGridViewCellFormattingEventArgs e)
    {
      try
      {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (metricsGrid?.Rows == null) return;
        var row = metricsGrid.Rows[e.RowIndex];
        if (row?.DataBoundItem is DataRowView drv)
        {
          if (string.IsNullOrEmpty(currentSelectedTable)) return;
          var dataRow = drv.Row;
          object keyObj = null;
          if (dataRow.Table.Columns.Contains("LinkID")) keyObj = dataRow["LinkID"]; else if (dataRow.Table.Columns.Contains("ID")) keyObj = dataRow["ID"];
          var linkKey = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
          if (!string.IsNullOrEmpty(linkKey))
          {
            bool include = Program.Edits.ShouldInclude(currentSelectedTable, linkKey);
            if (!include)
            {
              var disabledFore = System.Drawing.Color.FromArgb(150, 150, 150);
              var disabledBack = System.Drawing.Color.FromArgb(240, 240, 240);
              // Only override if column has no explicit color to avoid masking column styling
              if (e.CellStyle.ForeColor == System.Drawing.Color.Empty)
                e.CellStyle.ForeColor = disabledFore;
              if (e.CellStyle.BackColor == System.Drawing.Color.Empty)
                e.CellStyle.BackColor = disabledBack;
              e.CellStyle.SelectionForeColor = disabledFore;
              e.CellStyle.SelectionBackColor = disabledBack;
            }
          }
        }
      }
      catch { }
    }

    private void MetricsGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
    {
      try
      {
        // Suppress persistence while applying layout programmatically or during filtered rebinds
        if (isApplyingLayout || isSuppressingOrderPersistence) return;
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
        // Skip persistence during programmatic layout changes (like initial binding or column reordering)
        if (isApplyingLayout || isSuppressingOrderPersistence)
        {
          return;
        }
        // Debounce persistence to avoid cascades
        lastOrderChangeUtc = DateTime.UtcNow;
        if (orderPersistTimer != null)
        {
          if (!orderPersistTimer.Enabled)
          {
            // Don't start the timer while suppression is active
            if (!isSuppressingOrderPersistence)
            {
              orderPersistTimer.Start();
              Program.Log($"ColumnDisplayIndexChanged: started order persistence timer for column '{e.Column.Name}'");
            }
          }
        }
        // Reapply excluded styling after reordering
        try { ApplyDisabledRowStyling(); } catch { }
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_ColumnDisplayIndexChanged error", ex);
      }
    }

    private void MetricsGrid_Sorted(object sender, EventArgs e)
    {
      try
      {
        // Reapply excluded styling after sort changed
        ApplyDisabledRowStyling();
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_Sorted error", ex);
      }
    }

    private void MetricsGrid_CurrentCellChanged(object sender, EventArgs e)
    {
      try
      {
        // Refresh header styles when current cell changes to update selected header highlighting
        RefreshAllColumnHeaderStyles();
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_CurrentCellChanged error", ex);
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

        // Suppress event-driven persistence while applying layout programmatically
        var prevApplying = isApplyingLayout; isApplyingLayout = true;

        // Stop any pending order persistence timer to prevent saving during programmatic layout
        if (orderPersistTimer != null && orderPersistTimer.Enabled)
        {
          orderPersistTimer.Stop();
          Program.Log("ApplyUserConfig: stopped pending order persistence timer");
        }

        try
        {
          var cfg = UserConfig.LoadOrDefault();
          Program.Log($"ApplyUserConfig: table={tableName}, cols={metricsGrid.Columns.Count}");
          var t0 = DateTime.Now;

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

        var t1 = DateTime.Now; Program.Log($"ApplyUserConfig: widths applied in {(t1 - t0).TotalMilliseconds:F0}ms");

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

          var t2 = DateTime.Now; Program.Log($"ApplyUserConfig: visibility applied in {(t2 - t1).TotalMilliseconds:F0}ms");

          // Apply header overrides and tooltips
          metricsGrid.ShowCellToolTips = true; // enable tooltips generally
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
            var headerText = cfg.TryGetColumnHeaderText(tableName, key);
            if (!string.IsNullOrWhiteSpace(headerText))
            {
              col.HeaderText = headerText;
            }
            var headerTip = cfg.TryGetColumnHeaderToolTip(tableName, key);
            if (!string.IsNullOrWhiteSpace(headerTip))
            {
              // DataGridView does not natively show header tooltips unless set on HeaderCell
              col.HeaderCell.ToolTipText = headerTip;
            }
          }

          var t3 = DateTime.Now; Program.Log($"ApplyUserConfig: headers/tooltips applied in {(t3 - t2).TotalMilliseconds:F0}ms");

          // Apply column colors
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
            var backColor = cfg.TryGetColumnBackColor(tableName, key);
            if (backColor.HasValue)
            {
              col.DefaultCellStyle.BackColor = backColor.Value;
              col.HeaderCell.Style.BackColor = backColor.Value;
            }
            var foreColor = cfg.TryGetColumnForeColor(tableName, key);
            if (foreColor.HasValue)
            {
              col.DefaultCellStyle.ForeColor = foreColor.Value;
              col.HeaderCell.Style.ForeColor = foreColor.Value;
            }

            // Ensure header styles are synchronized
            ApplyHeaderStyles(col);
          }

        var t4 = DateTime.Now; Program.Log($"ApplyUserConfig: colors applied in {(t4 - t3).TotalMilliseconds:F0}ms");

        // Style virtual columns with defaults (only if no custom colors are set)
        foreach (var def in virtualColumnDefs)
        {
          if (string.IsNullOrWhiteSpace(def.ColumnName)) continue;
          if (!metricsGrid.Columns.Contains(def.ColumnName)) continue;
          var vc = metricsGrid.Columns[def.ColumnName];
          vc.ReadOnly = true;

          // Apply default styling only if no custom colors are set
          var key = !string.IsNullOrEmpty(vc.DataPropertyName) ? vc.DataPropertyName : vc.Name;
          var hasCustomBackColor = cfg.TryGetColumnBackColor(tableName, key).HasValue;
          var hasCustomForeColor = cfg.TryGetColumnForeColor(tableName, key).HasValue;

          if (!hasCustomBackColor)
          {
            if (def.IsLookupColumn)
            {
              vc.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 224); // light yellow
              vc.HeaderCell.Style.BackColor = System.Drawing.Color.FromArgb(255, 255, 224);
            }
            else if (def.IsActionColumn)
            {
              vc.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(230, 240, 255); // light blue
              vc.HeaderCell.Style.BackColor = System.Drawing.Color.FromArgb(230, 240, 255);
            }
            else
            {
              vc.DefaultCellStyle.BackColor = System.Drawing.Color.Beige;
              vc.HeaderCell.Style.BackColor = System.Drawing.Color.Beige;
            }
          }

          if (!hasCustomForeColor)
          {
            if (def.IsActionColumn)
            {
              vc.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkBlue;
              vc.HeaderCell.Style.ForeColor = System.Drawing.Color.DarkBlue;
              vc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
              vc.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkSlateGray;
              vc.HeaderCell.Style.ForeColor = System.Drawing.Color.DarkSlateGray;
            }
          }
        }

        var t5 = DateTime.Now; Program.Log($"ApplyUserConfig: virtual column styling applied in {(t5 - t4).TotalMilliseconds:F0}ms");

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
                Program.Log($"ApplyUserConfig: setting DisplayIndex for column '{col.Name}' to {idx}");
                col.DisplayIndex = idx++;
              }
            }
          }
          var t6 = DateTime.Now; Program.Log($"ApplyUserConfig: column order applied in {(t6 - t5).TotalMilliseconds:F0}ms");
        }
        finally
        {
          isApplyingLayout = prevApplying;

          // If we stopped the timer due to programmatic layout, don't restart it
          // The user will need to manually change column order to trigger persistence
          if (prevApplying == false && orderPersistTimer != null && !orderPersistTimer.Enabled)
          {
            Program.Log("ApplyUserConfig: order persistence timer remains stopped after programmatic layout");
          }
        }

        var t6b = DateTime.Now;

        // Style built-in _SourceFile column if present
        if (metricsGrid.Columns.Contains("_SourceFile"))
        {
          var col = metricsGrid.Columns["_SourceFile"];
          col.ReadOnly = true;
          col.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 248, 255);
          col.HeaderCell.Style.BackColor = System.Drawing.Color.FromArgb(248, 248, 255);
          col.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          col.HeaderCell.Style.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          col.HeaderText = "Work Order";
        }

        var t7 = DateTime.Now; Program.Log($"ApplyUserConfig: _SourceFile styling in {(t7 - t6b).TotalMilliseconds:F0}ms");

        // Enable user reordering and resizing
        metricsGrid.AllowUserToOrderColumns = true;
        metricsGrid.AllowUserToResizeColumns = true;

        var t8 = DateTime.Now; Program.Log($"ApplyUserConfig: ordering/resizing toggles set in {(t8 - t7).TotalMilliseconds:F0}ms");

        // Enable custom header styling (disable visual styles for headers)
        metricsGrid.EnableHeadersVisualStyles = false;

        var t9 = DateTime.Now; Program.Log($"ApplyUserConfig: header visual styles applied in {(t9 - t8).TotalMilliseconds:F0}ms");

        // Initialize edit mode visuals/state
        ApplyMainGridEditState();

        var t10 = DateTime.Now; Program.Log($"ApplyUserConfig: edit state applied in {(t10 - t9).TotalMilliseconds:F0}ms");

        // Ensure all column headers have proper styling
        RefreshAllColumnHeaderStyles();
        var t11 = DateTime.Now; Program.Log($"ApplyUserConfig: header styles refreshed in {(t11 - t10).TotalMilliseconds:F0}ms");

        // Update fronts filter button state
        UpdateFrontsFilterButtonState();
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

        // Don't set primary key for metrics grid - we want to show all rows from all work orders
        // Primary key would cause merging of rows with same ID, hiding data from different work orders

        // Load rows from all sources
        int totalRowsLoaded = 0;
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

                int rowsFromThisSource = 0;
                while (reader.Read())
                {
                  var row = dataTable.NewRow();
                  row["_SourceFile"] = GetWorkOrderName(path);
                  foreach (var col in srcCols)
                  {
                    if (!dataTable.Columns.Contains(col)) continue;
                    try { row[col] = reader[col]; } catch { row[col] = DBNull.Value; }
                  }

                  // Apply in-memory overrides by LinkID if present; do not filter here so we can style excluded rows
                  try
                  {
                    object linkVal = DBNull.Value;
                    if (dataTable.Columns.Contains("LinkID")) linkVal = row["LinkID"]; else if (dataTable.Columns.Contains("ID")) linkVal = row["ID"];
                    if (linkVal != DBNull.Value && linkVal != null)
                    {
                      var linkKey = Convert.ToString(linkVal);
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

                  // For metrics grid display, always add rows (don't merge by PK)
                  // This ensures we see all rows from all work orders, even with duplicate IDs
                  dataTable.Rows.Add(row);
                  rowsFromThisSource++;
                  totalRowsLoaded++;
                }

                // Log how many rows were loaded from this source
                string workOrderName = GetWorkOrderName(path);
                Program.Log($"Loaded {rowsFromThisSource} rows from work order '{workOrderName}' for table '{tableName}'");
              }
            }
            catch { }
          }
        }

        dataTable.AcceptChanges();

        // Log summary of data loading
        Program.Log($"BuildDataTableFromSources completed for table '{tableName}': {totalRowsLoaded} total rows loaded from {sourcePaths.Count} source(s)");
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
          var t0 = DateTime.Now;
          var cfg = UserConfig.LoadOrDefault();
          virtualColumnDefs = cfg.GetVirtualColumnsForTable(tableName) ?? new List<UserConfig.VirtualColumnDef>();
          virtualColumnNames.Clear();
          Program.Log($"MainForm: Loading virtual columns for '{tableName}': {virtualColumnDefs.Count} defs");
          foreach (var def in virtualColumnDefs)
          {
            if (!string.IsNullOrWhiteSpace(def.ColumnName)) virtualColumnNames.Add(def.ColumnName);
          }
          var t1 = DateTime.Now;
          Program.Log($"LoadVirtualColumnDefinitions: completed in {(t1 - t0).TotalMilliseconds:F0}ms");
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
          var t0 = DateTime.Now;

          // Create a cache key based on table name and source paths
          var newCacheKey = $"{tableName}:{string.Join("|", currentSourcePaths?.OrderBy(p => p)?.ToArray() ?? new string[0])}";

          // If cache key hasn't changed, we can reuse existing caches
          if (virtualCacheKey == newCacheKey && virtualLookupCacheByColumn.Count > 0)
          {
            Program.Log($"BuildVirtualLookupCaches: reusing existing caches for key '{newCacheKey}' (saved {(DateTime.Now - t0).TotalMilliseconds:F0}ms)");
            return;
          }

          Program.Log($"BuildVirtualLookupCaches: building new caches for key '{newCacheKey}'");
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

          // Update cache key after successful build
          virtualCacheKey = newCacheKey;
          var t1 = DateTime.Now;
          Program.Log($"BuildVirtualLookupCaches: completed in {(t1 - t0).TotalMilliseconds:F0}ms for {virtualLookupCacheByColumn.Count} columns");
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

        // Notify grid of new columns without full rebind to avoid heavy rebuild cost
        if (metricsGrid != null && newlyAdded.Count > 0)
        {
          Program.Log($"MainForm: Refreshing grid after adding virtual columns (lightweight): {string.Join(", ", newlyAdded)}");
          var bs = metricsGrid.DataSource as BindingSource;
          metricsGrid.SuspendLayout();
          try
          {
            metricsGrid.AutoGenerateColumns = true; // ensure autogen picks up new DataTable columns
            if (bs != null)
            {
              bs.RaiseListChangedEvents = false;
              bs.RaiseListChangedEvents = true;
              bs.ResetBindings(false);
            }
            else
            {
              // Fallback: bind through a BindingSource if grid isn't already using one
              metricsGrid.DataSource = new BindingSource { DataSource = data };
            }
          }
          finally
          {
            metricsGrid.ResumeLayout();
            metricsGrid.Invalidate();
          }
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
        hideColumn.Click += (s, e) =>
        {
          col.Visible = false;
          SaveColumnVisibility(col.Name, false);
          panelMetricsBorder.Refresh();
        };
        menu.Items.Add(hideColumn);

        // Only show "Show Columns" submenu if there are hidden columns
        var hiddenColumns = metricsGrid.Columns.Cast<DataGridViewColumn>().Where(gridCol => !gridCol.Visible).ToList();
        if (hiddenColumns.Count > 0)
        {
          var showColumnsMenu = new ToolStripMenuItem("Show Columns");
          foreach (var gridCol in hiddenColumns)
          {
            var showCol = new ToolStripMenuItem(gridCol.HeaderText ?? gridCol.Name);
            showCol.Click += (s, e) =>
            {
              gridCol.Visible = true;
              SaveColumnVisibility(gridCol.Name, true);
              panelMetricsBorder.Refresh();
            };
            showColumnsMenu.DropDownItems.Add(showCol);
          }
          menu.Items.Add(showColumnsMenu);
        }

        // Only show "Show All Columns" if there are hidden columns
        if (hiddenColumns.Count > 0)
        {
          var showAllColumns = new ToolStripMenuItem("Show All Columns");
          showAllColumns.Click += (s, e) =>
          {
            foreach (DataGridViewColumn gridCol in metricsGrid.Columns)
            {
              gridCol.Visible = true;
              SaveColumnVisibility(gridCol.Name, true);
              panelMetricsBorder.Refresh();
            }
          };
          menu.Items.Add(showAllColumns);
        }

        // Header label and tooltip customization
        menu.Items.Add(new ToolStripSeparator());
        var setHeaderLabel = new ToolStripMenuItem("Set Header Label...");
        setHeaderLabel.Click += (s, e) =>
        {
          var text = PromptForText("Header Label", col.HeaderText ?? col.Name);
          if (text != null)
          {
            SaveColumnHeaderText(col, text);
          }
        };
        // Use Unicode X mark for clear actions for clarity and modern UI
        var clearHeaderLabel = new ToolStripMenuItem("  ✗ Clear Header Label");
        clearHeaderLabel.Click += (s, e) => { SaveColumnHeaderText(col, string.Empty); };
        var setHeaderTooltip = new ToolStripMenuItem("Set Header Tooltip...");
        setHeaderTooltip.Click += (s, e) =>
        {
          var text = PromptForText("Header Tooltip", col.HeaderCell.ToolTipText ?? string.Empty);
          if (text != null)
          {
            SaveColumnHeaderToolTip(col, text);
          }
        };
        var clearHeaderTooltip = new ToolStripMenuItem(" ✗ Clear Header Tooltip");
        clearHeaderTooltip.Click += (s, e) => { SaveColumnHeaderToolTip(col, string.Empty); };

        // Show/hide clear actions based on whether values are set in config
        try
        {
          var cfgForMenu = UserConfig.LoadOrDefault();
          var cfgKey = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
          bool hasHeader = !string.IsNullOrWhiteSpace(currentSelectedTable) && !string.IsNullOrWhiteSpace(cfgForMenu.TryGetColumnHeaderText(currentSelectedTable, cfgKey));
          bool hasTip = !string.IsNullOrWhiteSpace(currentSelectedTable) && !string.IsNullOrWhiteSpace(cfgForMenu.TryGetColumnHeaderToolTip(currentSelectedTable, cfgKey));
          clearHeaderLabel.Visible = hasHeader;
          clearHeaderTooltip.Visible = hasTip;
        }
        catch { }
        menu.Items.Add(setHeaderLabel);
        menu.Items.Add(clearHeaderLabel);
        menu.Items.Add(setHeaderTooltip);
        menu.Items.Add(clearHeaderTooltip);

        // Column color customization
        menu.Items.Add(new ToolStripSeparator());
        var setColumnColor = new ToolStripMenuItem("Set Column Color...");
        setColumnColor.Click += (s, e) =>
        {
          var colorDialog = new ColorDialog();
          colorDialog.Color = col.DefaultCellStyle.BackColor;
          if (colorDialog.ShowDialog() == DialogResult.OK)
          {
            SaveColumnBackColor(col, colorDialog.Color);
          }
        };
        var clearColumnColor = new ToolStripMenuItem("  ✗ Clear Column Color");
        clearColumnColor.Click += (s, e) => { SaveColumnBackColor(col, null); };
        var setTextColor = new ToolStripMenuItem("Set Text Color...");
        setTextColor.Click += (s, e) =>
        {
          var colorDialog = new ColorDialog();
          colorDialog.Color = col.DefaultCellStyle.ForeColor;
          if (colorDialog.ShowDialog() == DialogResult.OK)
          {
            SaveColumnForeColor(col, colorDialog.Color);
          }
        };
        var clearTextColor = new ToolStripMenuItem("  ✗ Clear Text Color");
        clearTextColor.Click += (s, e) => { SaveColumnForeColor(col, null); };

        // Show/hide clear color actions based on whether colors are set in config
        try
        {
          var cfgForMenu = UserConfig.LoadOrDefault();
          var cfgKey = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
          bool hasBackColor = !string.IsNullOrWhiteSpace(currentSelectedTable) && cfgForMenu.TryGetColumnBackColor(currentSelectedTable, cfgKey).HasValue;
          bool hasForeColor = !string.IsNullOrWhiteSpace(currentSelectedTable) && cfgForMenu.TryGetColumnForeColor(currentSelectedTable, cfgKey).HasValue;
          clearColumnColor.Visible = hasBackColor;
          clearTextColor.Visible = hasForeColor;
        }
        catch { }
        menu.Items.Add(setColumnColor);
        menu.Items.Add(clearColumnColor);
        menu.Items.Add(setTextColor);
        menu.Items.Add(clearTextColor);

        // Manage virtual columns from header context
        menu.Items.Add(new ToolStripSeparator());
        var manageVirtual = new ToolStripMenuItem("Virtual Columns...");
        manageVirtual.Click += (s, e) => OpenVirtualColumnsDialog();
        menu.Items.Add(manageVirtual);

        // Reset to defaults options
        menu.Items.Add(new ToolStripSeparator());
        var resetThisTable = new ToolStripMenuItem("Reset Columns to Defaults (This Table)");
        resetThisTable.Click += (s, e) =>
        {
          try
          {
            if (string.IsNullOrWhiteSpace(currentSelectedTable)) return;
            var cfg = UserConfig.LoadOrDefault();
            // Reset only column-related preferences for this table
            cfg.ResetColumnsToDefaultsForTable(currentSelectedTable);
            cfg.Save();
            // Reapply layout to reflect defaults
            ApplyUserConfigToMetricsGrid(currentSelectedTable);
            metricsGrid?.Refresh();
            Program.Log($"Columns reset to defaults for table '{currentSelectedTable}'");
          }
          catch (Exception ex)
          {
            Program.Log("Reset columns for table failed", ex);
            MessageBox.Show("Failed to reset columns: " + ex.Message);
          }
        };

        var resetAllTables = new ToolStripMenuItem("Reset Columns to Defaults (All Tables)");
        resetAllTables.Click += (s, e) =>
        {
          try
          {
            var cfg = UserConfig.LoadOrDefault();
            cfg.ResetColumnsToDefaultsAllTables();
            cfg.Save();
            if (!string.IsNullOrWhiteSpace(currentSelectedTable))
            {
              ApplyUserConfigToMetricsGrid(currentSelectedTable);
              metricsGrid?.Refresh();
            }
            Program.Log("Columns reset to defaults for all tables");
          }
          catch (Exception ex)
          {
            Program.Log("Reset columns for all tables failed", ex);
            MessageBox.Show("Failed to reset columns for all tables: " + ex.Message);
          }
        };
        menu.Items.Add(resetThisTable);
        menu.Items.Add(resetAllTables);

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

    private string PromptForText(string title, string initialText)
    {
      try
      {
        var dlg = new Form
        {
          Text = title,
          StartPosition = FormStartPosition.CenterParent,
          FormBorderStyle = FormBorderStyle.FixedDialog,
          MinimizeBox = false,
          MaximizeBox = false,
          Width = 400,
          Height = 150
        };
        var lbl = new Label { Text = title + ":", AutoSize = true, Location = new System.Drawing.Point(12, 15) };
        var txt = new TextBox { Location = new System.Drawing.Point(15, 40), Width = 360 };
        txt.Text = initialText ?? string.Empty;
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new System.Drawing.Point(dlg.ClientSize.Width - 170, 80) };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new System.Drawing.Point(dlg.ClientSize.Width - 90, 80) };
        dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
        dlg.Controls.Add(lbl); dlg.Controls.Add(txt); dlg.Controls.Add(btnOk); dlg.Controls.Add(btnCancel);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
          return txt.Text;
        }
      }
      catch { }
      return null;
    }

    private void SaveColumnHeaderText(DataGridViewColumn column, string headerText)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable) || column == null) return;
        var cfg = UserConfig.LoadOrDefault();
        var key = !string.IsNullOrEmpty(column.DataPropertyName) ? column.DataPropertyName : column.Name;
        cfg.SetColumnHeaderText(currentSelectedTable, key, headerText);
        cfg.Save();
        // Apply immediately
        column.HeaderText = string.IsNullOrEmpty(headerText) ? key : headerText;
      }
      catch (Exception ex)
      {
        Program.Log("SaveColumnHeaderText error", ex);
      }
    }

    private void SaveColumnHeaderToolTip(DataGridViewColumn col, string toolTip)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        var cfg = UserConfig.LoadOrDefault();
        var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
        cfg.SetColumnHeaderToolTip(currentSelectedTable, key, toolTip);
        cfg.Save();
      }
      catch { }
    }

    private void SaveColumnBackColor(DataGridViewColumn col, System.Drawing.Color? backColor)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        var cfg = UserConfig.LoadOrDefault();
        var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
        cfg.SetColumnBackColor(currentSelectedTable, key, backColor);
        cfg.Save();

        // Apply the color immediately
        if (backColor.HasValue)
        {
          col.DefaultCellStyle.BackColor = backColor.Value;
          col.HeaderCell.Style.BackColor = backColor.Value;
        }
        else
        {
          // Reset to default color based on column type
          if (virtualColumnNames.Contains(col.Name))
          {
            col.DefaultCellStyle.BackColor = System.Drawing.Color.Beige;
            col.HeaderCell.Style.BackColor = System.Drawing.Color.Beige;
          }
          else
          {
            col.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            col.HeaderCell.Style.BackColor = System.Drawing.Color.White;
          }
        }

        // Ensure header styles are synchronized
        ApplyHeaderStyles(col);

        // Force refresh to show header changes immediately
        metricsGrid.InvalidateColumn(col.Index);
      }
      catch { }
    }

    private void SaveColumnForeColor(DataGridViewColumn col, System.Drawing.Color? foreColor)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(currentSelectedTable)) return;
        var cfg = UserConfig.LoadOrDefault();
        var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
        cfg.SetColumnForeColor(currentSelectedTable, key, foreColor);
        cfg.Save();

        // Apply the color immediately
        if (foreColor.HasValue)
        {
          col.DefaultCellStyle.ForeColor = foreColor.Value;
          col.HeaderCell.Style.ForeColor = foreColor.Value;
        }
        else
        {
          // Reset to default color based on column type
          if (virtualColumnNames.Contains(col.Name))
          {
            col.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkSlateGray;
            col.HeaderCell.Style.ForeColor = System.Drawing.Color.DarkSlateGray;
          }
          else
          {
            col.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
            col.HeaderCell.Style.ForeColor = System.Drawing.Color.Black;
          }
        }

        // Ensure header styles are synchronized
        ApplyHeaderStyles(col);

        // Force refresh to show header changes immediately
        metricsGrid.InvalidateColumn(col.Index);
      }
      catch { }
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

    private void MetricsGrid_DoubleClick(object sender, EventArgs e)
    {
      try
      {
        // Toggle edit mode regardless of where the double-click occurred within the grid/panel
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
      try
      {
        if (metricsGrid.IsCurrentCellDirty)
        {
          var cell = metricsGrid.CurrentCell;
          if (cell is DataGridViewCheckBoxCell || cell is DataGridViewComboBoxCell)
          {
            // For checkbox/combo, commit immediately and end edit to trigger CellEndEdit
            metricsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            metricsGrid.EndEdit();
          }
          // For text cells, do nothing here; wait for CellEndEdit so typing isn't interrupted
        }
      }
      catch { }
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
      // No-op: we commit on CellEndEdit after CommitEdit to avoid double-processing
    }

    private void ApplyMainGridEditState()
    {
      try
      {
        metricsGrid.ReadOnly = !isEditModeMainGrid;
        metricsGrid.EditMode = isEditModeMainGrid ? DataGridViewEditMode.EditOnKeystroke : DataGridViewEditMode.EditOnKeystrokeOrF2;
        metricsGrid.SelectionMode = isEditModeMainGrid ? DataGridViewSelectionMode.CellSelect : DataGridViewSelectionMode.FullRowSelect;
        RefreshAllColumnHeaderStyles();
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

            var key = (dataRow.Table.Columns.Contains("LinkID") ? dataRow["LinkID"] : (dataRow.Table.Columns.Contains("ID") ? dataRow["ID"] : null));
            string linkKey = key == null || key == DBNull.Value ? null : Convert.ToString(key);

            if (!string.IsNullOrEmpty(linkKey) && !string.IsNullOrEmpty(currentSelectedTable))
            {
              if (!object.Equals(newValue, orig))
              {
                // Value changed from original - add to pending changes
                Program.Edits.UpsertOverride(currentSelectedTable, linkKey, columnName, newValue);
              }
              else
              {
                // Value matches original - remove from pending changes if it exists
                Program.Edits.RemoveOverride(currentSelectedTable, linkKey, columnName);
              }
            }

            // Mark row clean only after the edit is finalized
            if (isEndEdit)
            {
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

    // Add context menu to the grid for easy access to grid features
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

    // Visually indicate excluded rows (non-selected) with a disabled style
    private void ApplyDisabledRowStyling()
    {
      try
      {
        if (metricsGrid == null || metricsGrid.Rows.Count == 0) return;
        if (string.IsNullOrEmpty(currentSelectedTable)) return;
        // Use a subtle gray forecolor and light background for excluded rows
        var disabledFore = System.Drawing.Color.FromArgb(150, 150, 150);
        var disabledBack = System.Drawing.Color.FromArgb(240, 240, 240);
        foreach (DataGridViewRow row in metricsGrid.Rows)
        {
          try
          {
            if (row?.DataBoundItem is DataRowView drv)
            {
              var dataRow = drv.Row;
              object keyObj = null;
              if (dataRow.Table.Columns.Contains("LinkID")) keyObj = dataRow["LinkID"]; else if (dataRow.Table.Columns.Contains("ID")) keyObj = dataRow["ID"];
              var linkKey = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
              bool include = true;
              if (!string.IsNullOrEmpty(linkKey)) include = Program.Edits.ShouldInclude(currentSelectedTable, linkKey);

              if (!include)
              {
                row.DefaultCellStyle.ForeColor = disabledFore;
                row.DefaultCellStyle.BackColor = disabledBack;
                row.DefaultCellStyle.SelectionForeColor = disabledFore;
                row.DefaultCellStyle.SelectionBackColor = disabledBack;
              }
              else
              {
                // Clear row-level overrides so column-level colors apply
                row.DefaultCellStyle.ForeColor = System.Drawing.Color.Empty;
                row.DefaultCellStyle.BackColor = System.Drawing.Color.Empty;
                row.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Empty;
                row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Empty;
              }
            }
          }
          catch { }
        }
      }
      catch { }
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
        var include = new ToolStripMenuItem("Include Selected Rows");
        include.Click += (s, e) => IncludeSelectedRows();
        var delete = new ToolStripMenuItem("Exclude Selected Rows");
        delete.Click += (s, e) => DeleteSelectedRows();
        try
        {
          // Enable/disable based on current selection state
          bool anyExcluded = false;
          bool anyIncluded = false;
          var rows = metricsGrid.SelectedRows.Cast<DataGridViewRow>().ToList();
          if (rows.Count == 0 && metricsGrid.CurrentRow != null) rows.Add(metricsGrid.CurrentRow);
          foreach (var r in rows)
          {
            if (r?.DataBoundItem is DataRowView drv)
            {
              var dataRow = drv.Row;
              object keyObj = null;
              if (dataRow.Table.Columns.Contains("LinkID")) keyObj = dataRow["LinkID"];
              else if (dataRow.Table.Columns.Contains("ID")) keyObj = dataRow["ID"];
              var linkKey = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
              if (!string.IsNullOrEmpty(linkKey) && !string.IsNullOrEmpty(currentSelectedTable))
              {
                bool includeState = Program.Edits.ShouldInclude(currentSelectedTable, linkKey);
                if (includeState) anyIncluded = true; else anyExcluded = true;
              }
            }
          }
          include.Enabled = anyExcluded;
          delete.Enabled = anyIncluded;
        }
        catch { }
        var refreshVirtual = new ToolStripMenuItem("Refresh Virtual Columns");
        refreshVirtual.Click += (s, e) => RefreshVirtualColumns();
        menu.Items.Add(export);
        menu.Items.Add(include);
        menu.Items.Add(delete);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(refreshVirtual);
        menu.Show(metricsGrid, clientLocation);
      }
      catch { }
    }
    // Include all rows for current table
    private void btnTableSelectAll_Click(object sender, EventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(currentSelectedTable)) return;
        Program.Edits.SelectAll(currentSelectedTable);
        // Lightweight restyle only
        if (metricsGrid != null)
        {
          metricsGrid.SuspendLayout();
          ApplyDisabledRowStyling();
          metricsGrid.ResumeLayout();
          metricsGrid.Refresh();
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error selecting all rows", ex);
      }
    }

    // Exclude all rows for current table
    private void btnTableClearAll_Click(object sender, EventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(currentSelectedTable)) return;
        Program.Edits.ClearAll(currentSelectedTable);
        // Lightweight restyle only
        if (metricsGrid != null)
        {
          metricsGrid.SuspendLayout();
          ApplyDisabledRowStyling();
          metricsGrid.ResumeLayout();
          metricsGrid.Refresh();
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error clearing all rows", ex);
      }
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

    // Exclude functionality for selected rows (selection-based include model)
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

        int changed = 0;
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
                // Deselect this row (exclude from destination)
                Program.Edits.DeselectRow(currentSelectedTable, linkKey);
                changed++;
              }
            }
            else
            {
              // If not a DataRow-bound item, just note the change; view will refresh below
            }
          }
          catch { }
        }

        // Lightweight refresh: update styling only; avoid full rebuild
        if (changed > 0)
        {
          try
          {
            if (metricsGrid != null)
            {
              metricsGrid.SuspendLayout();
              ApplyDisabledRowStyling();
              metricsGrid.ResumeLayout();
              metricsGrid.Refresh();
            }
          }
          catch { }
          if (checkedDirs.Count == 0 && listWorkOrders.SelectedIndices.Count == 0)
          {
            RequestRefreshMetricsGrid();
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error excluding selected rows", ex);
        MessageBox.Show("Error excluding selected rows: " + ex.Message, "Selection Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Include functionality for selected rows (undo exclusion)
    private void IncludeSelectedRows()
    {
      try
      {
        if (metricsGrid?.DataSource == null)
        {
          MessageBox.Show("No data loaded. Please select a table first.", "No Data",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        var rows = metricsGrid.SelectedRows.Cast<DataGridViewRow>().ToList();
        if (rows.Count == 0 && metricsGrid.CurrentRow != null) rows.Add(metricsGrid.CurrentRow);
        if (rows.Count == 0) { MessageBox.Show("No rows selected."); return; }

        int changed = 0;
        foreach (var row in rows)
        {
          try
          {
            if (row.IsNewRow) continue;
            if (row?.DataBoundItem is DataRowView drv)
            {
              var dataRow = drv.Row;
              object keyObj = null;
              if (dataRow.Table.Columns.Contains("LinkID")) keyObj = dataRow["LinkID"]; else if (dataRow.Table.Columns.Contains("ID")) keyObj = dataRow["ID"];
              var linkKey = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
              if (!string.IsNullOrEmpty(linkKey) && !string.IsNullOrEmpty(currentSelectedTable))
              {
                Program.Edits.SelectRow(currentSelectedTable, linkKey);
                changed++;
              }
            }
          }
          catch { }
        }

        if (changed > 0)
        {
          try
          {
            if (metricsGrid != null)
            {
              metricsGrid.SuspendLayout();
              ApplyDisabledRowStyling();
              metricsGrid.ResumeLayout();
              metricsGrid.Refresh();
            }
          }
          catch { }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error including selected rows", ex);
        MessageBox.Show("Error including selected rows: " + ex.Message, "Selection Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Fronts filter functionality for Parts table
    private bool isFrontsFilterActive = false;
    private DataTable originalPartsData = null;

    private void btnFilterFronts_Click(object sender, EventArgs e)
    {
      try
      {
        // Only enable when Parts table is selected
        if (string.IsNullOrEmpty(currentSelectedTable) || !string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase))
        {
          return;
        }

        if (isFrontsFilterActive)
        {
          // Reset filter - restore original data
          if (originalPartsData != null && metricsGrid?.DataSource is BindingSource bs)
          {
            Program.Log("Fronts filter: Resetting to show all parts");

            RebindBindingSourceSafely(bs, originalPartsData);

            ApplyDisabledRowStyling();
            isFrontsFilterActive = false;
            originalPartsData = null;
            btnFilterFronts.Text = "Fronts";
            btnFilterFronts.BackColor = System.Drawing.SystemColors.Control;
          }
        }
        else
        {
          // Apply filter - show only front parts
          if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataTable data)
          {
            // Validate data before filtering
            if (data == null || data.Rows.Count == 0)
            {
              Program.Log("Fronts filter: No data to filter");
              return;
            }

            Program.Log("Fronts filter: Filtering to show only front parts");

            // Store original data if not already stored
            if (originalPartsData == null)
            {
              originalPartsData = data.Copy();
            }

            // Create filtered data table with front parts only
            // Start with a copy of the original data to preserve all columns and their data
            var frontParts = data.Copy();
            var frontKeywords = new[] { "door", "slab", "drawer front", "appliance front", "false front", "face frame"};

            // Validate required columns exist
            var requiredColumns = new[] { "Name", "Comments", "Comments1", "Comments2", "Comments3" };
            foreach (var colName in requiredColumns)
            {
              if (!data.Columns.Contains(colName))
              {
                Program.Log($"Fronts filter: Required column '{colName}' not found in data");
                MessageBox.Show($"Required column '{colName}' not found in data. Cannot apply filter.", "Filter Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
              }
            }

            // Filter by removing rows that don't match front criteria
            // Work backwards to avoid index shifting issues
            for (int i = frontParts.Rows.Count - 1; i >= 0; i--)
            {
              var row = frontParts.Rows[i];
              var name = row["Name"]?.ToString() ?? "";
              var comments = row["Comments"]?.ToString() ?? "";
              var comments1 = row["Comments1"]?.ToString() ?? "";
              var comments2 = row["Comments2"]?.ToString() ?? "";
              var comments3 = row["Comments3"]?.ToString() ?? "";

              // Combine all text fields for searching
              var searchText = $"{name} {comments} {comments1} {comments2} {comments3}".ToLowerInvariant();

              // Check if any front keywords are found
              bool isFrontPart = false;
              foreach (var keyword in frontKeywords)
              {
                if (searchText.Contains(keyword.ToLowerInvariant()))
                {
                  isFrontPart = true;
                  break;
                }
              }

              if (!isFrontPart)
              {
                frontParts.Rows.RemoveAt(i);
              }
            }


            // Rebind safely to avoid CurrencyManager index errors
            RebindBindingSourceSafely(bs, frontParts);

            ApplyDisabledRowStyling();

            isFrontsFilterActive = true;
            btnFilterFronts.Text = "Show All";
            btnFilterFronts.BackColor = System.Drawing.Color.LightGreen;

            Program.Log($"Fronts filter: Filtered from {data.Rows.Count} to {frontParts.Rows.Count} parts");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error applying fronts filter", ex);
        MessageBox.Show("Error applying fronts filter: " + ex.Message, "Filter Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void ShowLoadingIndicator(bool show, string message = "Loading data...")
    {
      try
      {
        // If we're trying to hide the loading indicator but we're in the middle of a table refresh,
        // don't hide it yet - wait until the refresh is complete
        if (!show && isRefreshingTable)
        {
          Program.Log($"ShowLoadingIndicator: Skipping hide request while table refresh is in progress (isRefreshingTable={isRefreshingTable})");
          return;
        }

        Program.Log($"ShowLoadingIndicator: show={show}, message='{message}', isRefreshingTable={isRefreshingTable}");

        // Reduce layout thrash while switching views
        bottomLayout.SuspendLayout();
        try
        {
          if (show)
          {
            // Show loading panel, hide actions panel
            panelLoading.Visible = true;
            panelActions.Visible = false;

            // Update loading message
            lblLoading.Text = message;

            // Adjust existing column styles instead of replacing them to avoid flicker
            if (bottomLayout.ColumnStyles.Count >= 2)
            {
              bottomLayout.ColumnStyles[0].SizeType = SizeType.Absolute;
              bottomLayout.ColumnStyles[0].Width = 0F;
              bottomLayout.ColumnStyles[1].SizeType = SizeType.Percent;
              bottomLayout.ColumnStyles[1].Width = 100F;
            }

            // Bring loading panel to front
            panelLoading.BringToFront();
          }
          else
          {
            // Hide loading panel, show actions panel
            panelLoading.Visible = false;
            panelActions.Visible = true;

            // Restore normal column layout
            if (bottomLayout.ColumnStyles.Count >= 2)
            {
              bottomLayout.ColumnStyles[0].SizeType = SizeType.Percent;
              bottomLayout.ColumnStyles[0].Width = 100F;
              bottomLayout.ColumnStyles[1].SizeType = SizeType.Absolute;
              bottomLayout.ColumnStyles[1].Width = 0F;
            }

            // Reset progress bar
            progress.Style = ProgressBarStyle.Continuous;
            progress.Value = 0;
          }
        }
        finally
        {
          bottomLayout.ResumeLayout(true);
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
          // Ensure schema is loaded from resources/wo_schema.xml
          EnsureSchemaLoaded();

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

            // Only process tables present in the schema XML
            if (!SchemaTables.Contains(table)) continue;

            // Always ensure destination table exists (schema conformance), even if we skip data
            EnsureDestinationTableCombined(destConn, srcConn, table);

            // Skip copying data for excluded tables
            if (ExcludedDataTables.Contains(table)) continue;

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

          // Create schema-conformant table: only columns defined in wo_schema.xml
          var allowedCols = GetAllowedSchemaColumns(tableName);
          var srcCols = src.GetSchema("Columns", new[] { null, null, tableName, null });
          var srcColsMap = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
          foreach (DataRow row in srcCols.Rows)
          {
            var name = Convert.ToString(row["COLUMN_NAME"]);
            if (!srcColsMap.ContainsKey(name)) srcColsMap[name] = row;
          }

          var columnDefs = new List<string>();
          foreach (var colName in allowedCols)
          {
            if (virtualColumns.Contains(colName)) continue; // never include virtuals

            string typeSql = "NVARCHAR(255)";
            string nullSql = "NULL";
            if (srcColsMap.TryGetValue(colName, out var col))
            {
              string dataType = Convert.ToString(col["DATA_TYPE"]);
              int length = col.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
              bool nullable = col.Table.Columns.Contains("IS_NULLABLE") && string.Equals(Convert.ToString(col["IS_NULLABLE"]), "YES", StringComparison.OrdinalIgnoreCase);
              typeSql = MapType(dataType, length);
              nullSql = nullable ? "NULL" : "NOT NULL";
            }
            columnDefs.Add($"[{colName}] {typeSql} {nullSql}");
          }
          // No metadata columns in consolidated database
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

            // Add missing schema-defined columns only (schema conformance)
            var allowedCols = new HashSet<string>(GetAllowedSchemaColumns(tableName), StringComparer.OrdinalIgnoreCase);
            var srcCols = src.GetSchema("Columns", new[] { null, null, tableName, null });
            var srcColsMap = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in srcCols.Rows)
            {
              var name = Convert.ToString(row["COLUMN_NAME"]);
              if (!srcColsMap.ContainsKey(name)) srcColsMap[name] = row;
            }

            var destCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var dc = new SqlCeCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", dest))
            {
              dc.Parameters.AddWithValue("@t", tableName);
              using (var r = dc.ExecuteReader())
              {
                while (r.Read()) destCols.Add(r.GetString(0));
              }
            }

            foreach (var colName in allowedCols)
            {
              if (destCols.Contains(colName)) continue;
              if (virtualColumns.Contains(colName)) continue;

              string typeSql = "NVARCHAR(255)";
              if (srcColsMap.TryGetValue(colName, out var col))
              {
                string dataType = Convert.ToString(col["DATA_TYPE"]);
                int length = srcCols.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
                typeSql = MapType(dataType, length);
              }
              using (var alter = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [" + colName + "] " + typeSql + " NULL", dest)) alter.ExecuteNonQuery();
            }
            // No metadata columns in consolidated database
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

        // Helper method to check if a sheet already exists in the destination database
    private static bool SheetExistsInDestination(SqlCeConnection dest, string name, object width, object length, object thickness)
    {
      try
      {
        // Convert values to strings for comparison to avoid SQL CE NULL handling issues
        string nameStr = string.IsNullOrWhiteSpace(name) ? "" : name;
        string widthStr = width == null || width == DBNull.Value ? "" : width.ToString();
        string lengthStr = length == null || length == DBNull.Value ? "" : length.ToString();
        string thicknessStr = thickness == null || thickness == DBNull.Value ? "" : thickness.ToString();

        using (var cmd = new SqlCeCommand(
          "SELECT COUNT(*) FROM [Sheets] WHERE " +
          "COALESCE([Name], '') = @name AND " +
          "COALESCE(CAST([Width] AS NVARCHAR), '') = @width AND " +
          "COALESCE(CAST([Length] AS NVARCHAR), '') = @length AND " +
          "COALESCE(CAST([Thickness] AS NVARCHAR), '') = @thickness",
          dest))
        {
          cmd.Parameters.AddWithValue("@name", nameStr);
          cmd.Parameters.AddWithValue("@width", widthStr);
          cmd.Parameters.AddWithValue("@length", lengthStr);
          cmd.Parameters.AddWithValue("@thickness", thicknessStr);

          var count = Convert.ToInt32(cmd.ExecuteScalar());
          return count > 0;
        }
      }
      catch (Exception ex)
      {
        Program.Log($"Error checking for duplicate sheet: {ex.Message}", ex);
        return false; // If we can't check, allow the insert to proceed
      }
    }

    private static void CopyRowsCombined(SqlCeConnection src, SqlCeConnection dest, string tableName, string sourceTag, string sourcePathDir)
    {
      // Get virtual columns to exclude from consolidation
      var virtualColumns = GetVirtualColumnsForTable(tableName);
      // Load allowed schema columns for this table
      var allowedSchemaCols = new HashSet<string>(GetAllowedSchemaColumns(tableName), StringComparer.OrdinalIgnoreCase);

      using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", src))
      using (var reader = cmd.ExecuteReader())
      {
        var schema = reader.GetSchemaTable();
        var allColNames = schema.Rows.Cast<DataRow>().Select(r => r["ColumnName"].ToString()).ToList();

        // Filter out virtual columns and any columns not present in wo_schema.xml
        var colNames = allColNames
          .Where(name => !virtualColumns.Contains(name))
          .Where(name => allowedSchemaCols.Contains(name))
          .ToList();

        if (virtualColumns.Count > 0)
        {
          var excludedFromData = allColNames.Where(name => virtualColumns.Contains(name)).ToList();
          if (excludedFromData.Count > 0)
          {
            Program.Log($"Excluding {excludedFromData.Count} virtual columns from data copy for table {tableName}: {string.Join(", ", excludedFromData)}");
          }
        }

        // Log any columns skipped due to schema filtering
        var nonSchemaCols = allColNames.Where(name => !allowedSchemaCols.Contains(name)).ToList();
        if (nonSchemaCols.Count > 0)
        {
          Program.Log($"Skipping {nonSchemaCols.Count} non-schema columns for table {tableName}: {string.Join(", ", nonSchemaCols)}");
        }

        var destColumns = new List<string>(colNames.Select(n => "[" + n + "]"));
        var destParams = new List<string>(colNames.Select(n => "@" + n));
        string insertSql = $"INSERT INTO [" + tableName + "] (" + string.Join(", ", destColumns) + ") VALUES (" + string.Join(", ", destParams) + ")";
        using (var insert = new SqlCeCommand(insertSql, dest))
        {
          foreach (var name in colNames)
          {
            insert.Parameters.Add(new SqlCeParameter("@" + name, DBNull.Value));
          }
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

              // Apply in-memory overrides by LinkID if present; do not filter so we can style excluded rows
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

              // Check for duplicate sheets before inserting
              if (tableName.Equals("Sheets", StringComparison.OrdinalIgnoreCase))
              {
                try
                {
                  // Get the values for duplicate checking
                  string name = null;
                  object width = null;
                  object length = null;
                  object thickness = null;

                  // Find the column indices for the duplicate check fields
                  int nameIndex = allColNames.IndexOf("Name");
                  int widthIndex = allColNames.IndexOf("Width");
                  int lengthIndex = allColNames.IndexOf("Length");
                  int thicknessIndex = allColNames.IndexOf("Thickness");

                  if (nameIndex >= 0) name = reader.GetValue(nameIndex)?.ToString();
                  if (widthIndex >= 0) width = reader.GetValue(widthIndex);
                  if (lengthIndex >= 0) length = reader.GetValue(lengthIndex);
                  if (thicknessIndex >= 0) thickness = reader.GetValue(thicknessIndex);

                  // Check if this sheet already exists
                  if (SheetExistsInDestination(dest, name, width, length, thickness))
                  {
                    Program.Log($"Skipping duplicate sheet: Name='{name}', Width={width}, Length={length}, Thickness={thickness}");
                    rowIndex++;
                    continue; // Skip this row
                  }
                }
                catch (Exception ex)
                {
                  Program.Log($"Error checking for duplicate sheet: {ex.Message}", ex);
                  // Continue with insert if duplicate check fails
                }
              }

              // Apply dynamic sheet cost calculation if enabled
              if (tableName.Equals("Sheets", StringComparison.OrdinalIgnoreCase))
              {
                try
                {
                  var userConfig = UserConfig.LoadOrDefault();
                  if (userConfig.DynamicSheetCosts)
                  {
                    // Get the values for cost calculation
                    object width = null;
                    object length = null;
                    object thickness = null;

                    // Find the column indices for the cost calculation fields
                    int widthIndex = allColNames.IndexOf("Width");
                    int lengthIndex = allColNames.IndexOf("Length");
                    int thicknessIndex = allColNames.IndexOf("Thickness");

                    if (widthIndex >= 0) width = reader.GetValue(widthIndex);
                    if (lengthIndex >= 0) length = reader.GetValue(lengthIndex);
                    if (thicknessIndex >= 0) thickness = reader.GetValue(thicknessIndex);

                    // Calculate dynamic cost: (width * length * thickness) / 10
                    if (width != null && width != DBNull.Value &&
                        length != null && length != DBNull.Value &&
                        thickness != null && thickness != DBNull.Value)
                    {
                      try
                      {
                        double widthVal = Convert.ToDouble(width);
                        double lengthVal = Convert.ToDouble(length);
                        double thicknessVal = Convert.ToDouble(thickness);
                        double calculatedCost = (widthVal * lengthVal * thicknessVal) / 10.0;

                        // Find Material Estimate Cost column and update its value
                        int materialCostIndex = allColNames.IndexOf("MaterialCost");
                        if (materialCostIndex >= 0)
                        {
                          var materialCostParam = insert.Parameters["@MaterialCost"];
                          if (materialCostParam != null)
                          {
                            materialCostParam.Value = calculatedCost;
                            Program.Log($"Applied dynamic cost calculation: Width={widthVal}, Length={lengthVal}, Thickness={thicknessVal}, Cost={calculatedCost}");
                          }
                        }

                        // Calculate WeightedSelectionValue: (width * length * thickness) rounded to nearest 100
                        double weightedSelectionValue = Math.Round((widthVal * lengthVal * thicknessVal) / 100.0) * 100.0;

                        // Find WeightedSelectionValue column and update its value
                        int weightedSelectionIndex = allColNames.IndexOf("WeightedSelectionValue");
                        if (weightedSelectionIndex >= 0)
                        {
                          var weightedSelectionParam = insert.Parameters["@WeightedSelectionValue"];
                          if (weightedSelectionParam != null)
                          {
                            weightedSelectionParam.Value = weightedSelectionValue;
                            Program.Log($"Applied weighted selection value calculation: Width={widthVal}, Length={lengthVal}, Thickness={thicknessVal}, WeightedSelectionValue={weightedSelectionValue}");
                          }
                        }
                      }
                      catch (Exception calcEx)
                      {
                        Program.Log($"Error calculating dynamic sheet cost: {calcEx.Message}", calcEx);
                        // Continue with original values if calculation fails
                      }
                    }
                  }
                }
                catch (Exception ex)
                {
                  Program.Log($"Error applying dynamic sheet costs: {ex.Message}", ex);
                  // Continue with original values if dynamic cost application fails
                }
              }

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

    // ----------------------
    // Schema helpers / config
    // ----------------------

    // Tables whose data should not be copied into the consolidated database
    private static readonly HashSet<string> ExcludedDataTables = new HashSet<string>(new[]
    {
      "WorkOrderBatches",
      "OptimizationResults",
      "OptimizationResultAssociates",
      "SawCutLines",
      "PlacedSheets",
      "PlacedSheetsVendors",
      "PartsProcessingStations",
      "SawStacks"
    }, StringComparer.OrdinalIgnoreCase);

    // Loaded from resources/wo_schema.xml
    private static HashSet<string> SchemaTables;
    private static Dictionary<string, HashSet<string>> SchemaTableToColumns;

    private static void EnsureSchemaLoaded()
    {
      if (SchemaTables != null && SchemaTableToColumns != null) return;
      try
      {
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var schemaPath = Path.Combine(exeDir ?? Environment.CurrentDirectory, "resources", "wo_schema.xml");
        if (!File.Exists(schemaPath))
        {
          // Try relative to current working directory as a fallback
          schemaPath = Path.Combine(Environment.CurrentDirectory, "resources", "wo_schema.xml");
        }

        var xdoc = System.Xml.Linq.XDocument.Load(schemaPath);
        var tableToCols = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in xdoc.Root.Elements("Table"))
        {
          var tName = (string)t.Attribute("Name") ?? string.Empty;
          if (string.IsNullOrWhiteSpace(tName)) continue;
          tableNames.Add(tName);
          var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          var colsNode = t.Element("Columns");
          if (colsNode != null)
          {
            foreach (var c in colsNode.Elements("Column"))
            {
              var cName = (string)c.Attribute("Name");
              if (!string.IsNullOrWhiteSpace(cName)) cols.Add(cName);
            }
          }
          tableToCols[tName] = cols;
        }
        SchemaTables = tableNames;
        SchemaTableToColumns = tableToCols;
      }
      catch (Exception ex)
      {
        Program.Log("Failed to load wo_schema.xml; proceeding without strict schema enforcement", ex);
        SchemaTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SchemaTableToColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
      }
    }

    private static IEnumerable<string> GetAllowedSchemaColumns(string tableName)
    {
      EnsureSchemaLoaded();
      if (SchemaTableToColumns.TryGetValue(tableName, out var cols) && cols != null && cols.Count > 0)
      {
        return cols;
      }
      // If schema not found for the table, allow no columns to ensure strict conformance
      return new string[0];
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

    private void ApplyHeaderStyles(DataGridViewColumn col)
    {
      try
      {
        // Ensure header styles are synchronized with cell styles
        // Force creation of header cell style if it doesn't exist
        if (col.HeaderCell.Style == null)
        {
          col.HeaderCell.Style = new DataGridViewCellStyle();
        }

        // Check if this column is currently selected (has the current cell)
        bool isSelected = metricsGrid.CurrentCell != null && metricsGrid.CurrentCell.ColumnIndex == col.Index;

        if (isSelected)
        {
          if (this.isEditModeMainGrid)
          {
            col.HeaderCell.Style.ForeColor = System.Drawing.Color.Green; // Green text on selected header
            col.HeaderCell.Style.BackColor = System.Drawing.SystemColors.Control; // Use default control background
          }
          else
          {
            col.HeaderCell.Style.ForeColor = System.Drawing.Color.White; // White text on selected header
            col.HeaderCell.Style.BackColor = System.Drawing.ColorTranslator.FromHtml("#0078d7"); // Set to #0078d7 (blue)
          }
        }
        else
        {
          // Apply normal header styling
          col.HeaderCell.Style.BackColor = col.DefaultCellStyle.BackColor;
          col.HeaderCell.Style.ForeColor = col.DefaultCellStyle.ForeColor;
        }

        // Apply additional header-specific styling
        col.HeaderCell.Style.Font = col.HeaderCell.Style.Font ?? metricsGrid.Font;
        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
      }
      catch { }
    }

    private void RefreshAllColumnHeaderStyles()
    {
      try
      {
        // Ensure visual styles are disabled for custom header styling
        metricsGrid.EnableHeadersVisualStyles = false;

        foreach (DataGridViewColumn col in metricsGrid.Columns)
        {
          ApplyHeaderStyles(col);
        }

        // Force grid to refresh and redraw headers
        metricsGrid.Invalidate();
        metricsGrid.Refresh();
      }
      catch { }
    }

    // Update fronts filter button state based on selected table
    private void UpdateFrontsFilterButtonState()
    {
      try
      {
        if (btnFilterFronts != null)
        {
          bool isPartsTable = !string.IsNullOrEmpty(currentSelectedTable) &&
                             string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase);

          btnFilterFronts.Enabled = isPartsTable;

          if (!isPartsTable)
          {
            // Reset button state when not on Parts table
            btnFilterFronts.Text = "Fronts";
            btnFilterFronts.BackColor = System.Drawing.SystemColors.Control;
            isFrontsFilterActive = false;
            originalPartsData = null;
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error updating fronts filter button state", ex);
      }
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
