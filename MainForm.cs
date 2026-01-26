using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
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
      ("PlacedSheets", "PlacedSheets"),
      ("Edgebanding", "Edgebanding"),
      ("OptimizationResults", "OptimizationResults"),
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
    // Apply layout again after binding completes to ensure DisplayIndex sticks
    private bool applyLayoutAfterBinding = false;

    // Helper: dump current grid columns for diagnostics
    private void LogCurrentGridColumns(string label)
    {
      try
      {
        if (metricsGrid == null) return;
        var cols = metricsGrid.Columns.Cast<DataGridViewColumn>()
          .OrderBy(c => c.DisplayIndex)
          .Select(c =>
          {
            var key = !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name;
            return $"{c.DisplayIndex}:{key} (Name='{c.Name}',DP='{c.DataPropertyName}',Vis={(c.Visible ? "T" : "F")})";
          });
        Program.Log($"GridColumns[{label}]: " + string.Join(" | ", cols));
      }
      catch { }
    }
    private bool isEditModeMainGrid = false; // tracks edit mode for metricsGrid
    private bool isCtrlPressed = false; // tracks Ctrl key state for header coloring
    private System.Windows.Forms.Timer orderPersistTimer;
    private DateTime lastOrderChangeUtc;
    // Virtual columns state
    private List<UserConfig.VirtualColumnDef> virtualColumnDefs = new List<UserConfig.VirtualColumnDef>();
    private readonly Dictionary<string, Dictionary<string, object>> virtualLookupCacheByColumn = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> virtualColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private System.Windows.Forms.Timer metricsFilterTimer; // debounce timer for live filtering
    private DateTime lastFilterRequestUtc;
    private bool isRefreshingMetrics;
    private bool suppressTableSelectorChanged;
    private bool isScanningWorkOrders;
    private bool isClearingGrid = false; // Flag to prevent duplicate clear/refresh calls
    private bool suppressSelectionChangedRefresh = false; // Flag to prevent SelectedIndexChanged from triggering refresh when checkbox is toggled

    // Multi-column sorting state
    private List<SortColumn> sortColumns = new List<SortColumn>();

    private class SortColumn
    {
        public string ColumnName { get; set; }
        public System.Windows.Forms.SortOrder Direction { get; set; }
        public int Priority { get; set; } // 0 = primary, 1 = secondary, etc.
    }

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
      // Set window title with version number
      var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
      this.Text = $"Work Order Blender v{version.Major}.{version.Minor}.{version.Build}";

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

      // Handle form focus changes to reset Ctrl state
      this.LostFocus += (s, e) =>
      {
        try
        {
          if (isCtrlPressed)
          {
            UpdateHeaderColorsForCtrlState(false);
          }
        }
        catch { }
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
      // Note: metricsRefreshTimer removed - calling RefreshMetricsGrid directly to avoid race conditions
      // when items are rapidly selected/deselected

      // Setup metrics filter debounce so we don't filter on every keystroke
      metricsFilterTimer = new System.Windows.Forms.Timer();
      metricsFilterTimer.Interval = 300; // ms debounce for typing
      metricsFilterTimer.Tick += (s, e2) =>
      {
        try
        {
          if ((DateTime.UtcNow - lastFilterRequestUtc).TotalMilliseconds >= metricsFilterTimer.Interval)
          {
            metricsFilterTimer.Stop();
            Program.Log($"metricsFilterTimer.Tick: applying filter '{currentMetricsFilter}'");
            ApplyMetricsFilterLive();
          }
        }
        catch (Exception ex)
        {
          Program.Log("metricsFilterTimer error", ex);
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
          bool wasChecked = checkedDirs.Contains(dir);
          if (wasChecked) checkedDirs.Remove(dir); else checkedDirs.Add(dir);
          listWorkOrders.Invalidate(hit.Item.Bounds);

          // Set flag to prevent SelectedIndexChanged from triggering a refresh
          // We'll handle the refresh/clear here in MouseDown
          suppressSelectionChangedRefresh = true;
          try
          {
            // If no items are checked after this toggle, clear the grid immediately
            if (checkedDirs.Count == 0)
            {
              Program.Log("listWorkOrders_MouseDown: last item unchecked, clearing grid");
              isClearingGrid = true; // Set flag to prevent duplicate clears
              ClearMetricsGridFast();
            }
            else if (!isClearingGrid && !isRefreshingMetrics)
            {
              // Only request refresh if we're not already clearing or refreshing
              Program.Log("listWorkOrders_MouseDown: items checked, requesting refresh");
              RequestRefreshMetricsGrid();
            }

            // Update work order name based on selection
            UpdateWorkOrderNameFromSelection();
          }
          finally
          {
            // Reset flag after event cycle completes (using BeginInvoke)
            this.BeginInvoke(new Action(() =>
            {
              suppressSelectionChangedRefresh = false;
              isClearingGrid = false;
            }));
          }
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

        // Set flag to prevent SelectedIndexChanged from triggering a refresh
        suppressSelectionChangedRefresh = true;
        try
        {
          // If no items are checked after this toggle, clear the grid immediately
          if (checkedDirs.Count == 0)
          {
            Program.Log("listWorkOrders_KeyDown: all items unchecked, clearing grid");
            isClearingGrid = true;
            ClearMetricsGridFast();
          }
          else if (!isClearingGrid && !isRefreshingMetrics)
          {
            Program.Log("listWorkOrders_KeyDown: items checked, requesting refresh");
            RequestRefreshMetricsGrid();
          }

          // Update work order name based on selection
          UpdateWorkOrderNameFromSelection();
        }
        finally
        {
          // Reset flag after event cycle completes (using BeginInvoke)
          this.BeginInvoke(new Action(() =>
          {
            suppressSelectionChangedRefresh = false;
            isClearingGrid = false;
          }));
        }

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

    // Create a list/queue icon for the Saw Queue button
    private System.Drawing.Image CreateListQueueIcon()
    {
      var bmp = new System.Drawing.Bitmap(16, 16);
      using (var g = System.Drawing.Graphics.FromImage(bmp))
      {
        g.Clear(System.Drawing.Color.Transparent);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Draw horizontal lines to represent a list/queue
        using (var pen = new System.Drawing.Pen(System.Drawing.Color.DarkBlue, 2))
        {
          // Draw 4 horizontal lines to represent list items
          g.DrawLine(pen, 2, 3, 14, 3);   // Top line
          g.DrawLine(pen, 2, 6, 14, 6);   // Second line
          g.DrawLine(pen, 2, 9, 14, 9);   // Third line
          g.DrawLine(pen, 2, 12, 14, 12); // Bottom line
        }
      }
      return bmp;
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
        // Only request refresh if there are actually checked work orders
        // Note: We only use CHECKED items, not SELECTED (highlighted) items
        if (checkedDirs.Count > 0)
        {
          Program.Log("ScanWorkOrders: work orders checked, requesting refresh");
          RequestRefreshMetricsGrid();
        }
        else
        {
          Program.Log("ScanWorkOrders: no work orders checked, skipping refresh");
        }
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

        // Update work order name based on selection
        UpdateWorkOrderNameFromSelection();

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

        // Update work order name based on selection (will clear if no selection)
        UpdateWorkOrderNameFromSelection();

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

    private async void btnConsolidate_Click(object sender, EventArgs e)
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

      // Check if work order name already exists in Microvellum database
      try
      {
        Program.Log($"Checking if work order name '{workOrderName}' exists in Microvellum database");

        // Show progress indicator for MSSQL check
        ShowLoadingIndicator(true, "Checking work order name in database...");

        bool nameExists = await MssqlUtils.WorkOrderNameExistsAsync(workOrderName);

        // Hide progress indicator
        ShowLoadingIndicator(false);

        if (nameExists)
        {
          var result = MessageBox.Show(
            $"Work order name '{workOrderName}' already exists in the Microvellum database.\n\n" +
            "Do you want to continue with this name anyway?\n\n" +
            "Click 'Yes' to proceed with the existing name, or 'No' to cancel and enter a different name.",
            "Work Order Name Exists",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

          if (result != DialogResult.Yes)
          {
            Program.Log($"User cancelled consolidation due to existing work order name '{workOrderName}'");
            return; // User chose to cancel and enter a different name
          }

          Program.Log($"User chose to proceed with existing work order name '{workOrderName}'");
        }
        else
        {
          Program.Log($"Work order name '{workOrderName}' is available in Microvellum database");
        }
      }
      catch (Exception ex)
      {
        Program.Log($"Error checking work order name in Microvellum database: {ex.Message}", ex);
        ShowLoadingIndicator(false); // Hide progress indicator on error

        // If we can't check the database, show a warning but allow the user to proceed
        var result = MessageBox.Show(
          $"Unable to verify work order name '{workOrderName}' against the Microvellum database.\n\n" +
          "This may be due to a network or database connection issue.\n\n" +
          "Do you want to continue anyway?\n\n" +
          "Click 'Yes' to proceed, or 'No' to cancel and check your connection.",
          "Database Connection Warning",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Warning,
          MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
          Program.Log("User cancelled consolidation due to database connection issue");
          return; // User chose to cancel due to connection issues
        }

        Program.Log("User chose to proceed despite database connection issue");
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
        MessageBox.Show(
          $"Consolidation complete!\n\n" +
          $"Successfully merged {selected.Count} work order(s).\n\n" +
          $"Output file:\n{destPath}",
          "Consolidation Successful",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information);

        // Reset progress bar after successful consolidation
        progress.Value = 0;
      }
      catch (ConsolidationException cex)
      {
        // Handle consolidation-specific errors with detailed feedback
        Program.Log("Consolidation error (ConsolidationException)", cex);

        if (cex.IsPartialSuccess)
        {
          // Some sources succeeded - show warning but note partial success
          MessageBox.Show(
            $"{cex.Message}\n\n" +
            $"The consolidated database was created but may be incomplete.\n\n" +
            $"Output file:\n{destPath}",
            "Consolidation Completed with Warnings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        }
        else
        {
          // All sources failed
          MessageBox.Show(
            $"{cex.Message}\n\n" +
            "Please check the log file for more details.",
            "Consolidation Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        }

        // Reset progress bar on error
        progress.Value = 0;
      }
      catch (Exception ex)
      {
        // Handle unexpected errors
        Program.Log("Consolidation error (unexpected)", ex);

        string detailedMessage = GetDetailedErrorMessage(ex);
        MessageBox.Show(
          $"An unexpected error occurred during consolidation:\n\n" +
          $"{detailedMessage}\n\n" +
          "Please check the log file for more details.",
          "Consolidation Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error);

        // Reset progress bar on error
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
      try
      {
        using (var dlg = new SettingsDialog())
        {
          // Subscribe to the Check for Updates event
          dlg.CheckForUpdatesRequested += (s, e) => CheckForUpdates_Click(s, e);

          if (dlg.ShowDialog(this) == DialogResult.OK)
          {
            dlg.SaveSettings();
            FilterWorkOrders(); // apply filter immediately if changed
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Failed to open settings dialog", ex);
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

      // If there are no work orders visible and none selected/checked, clear metrics grid
      try
      {
        if ((filteredWorkOrders == null || filteredWorkOrders.Count == 0) && checkedDirs.Count == 0 && listWorkOrders.SelectedIndices.Count == 0)
        {
          Program.Log("FilterWorkOrders: no visible work orders; clearing metrics grid");
          ClearMetricsGridFast();
        }
      }
      catch { }
    }

    private void listWorkOrders_SelectedIndexChanged(object sender, EventArgs e)
    {
      try
      {
        Program.Log($"listWorkOrders_SelectedIndexChanged: checkedDirs.Count={checkedDirs.Count}, selectedIndices.Count={listWorkOrders.SelectedIndices.Count}, suppressSelectionChangedRefresh={suppressSelectionChangedRefresh}, isClearingGrid={isClearingGrid}");

        // If this change was caused by a checkbox toggle in MouseDown, skip refresh logic
        // MouseDown will handle the refresh/clear itself
        if (suppressSelectionChangedRefresh)
        {
          Program.Log("listWorkOrders_SelectedIndexChanged: suppressed (checkbox toggle handled in MouseDown), skipping refresh");
          // Still update work order name, but don't trigger refresh
          UpdateWorkOrderNameFromSelection();
          return;
        }

        // If we're already clearing the grid, skip to avoid duplicate clears
        if (isClearingGrid)
        {
          Program.Log("listWorkOrders_SelectedIndexChanged: grid is already being cleared, skipping");
          return;
        }

        // Update work order name based on selection
        UpdateWorkOrderNameFromSelection();

        // If no work orders are checked, clear the metrics grid and return early
        // Note: We only use CHECKED items, not SELECTED (highlighted) items
        if (checkedDirs.Count == 0)
        {
          Program.Log("listWorkOrders_SelectedIndexChanged: no work orders checked, clearing grid and returning early");
          isClearingGrid = true;
          try
          {
            ClearMetricsGridFast();
            // Also hide the loading indicator as there's nothing to load
            ShowLoadingIndicator(false);
          }
          finally
          {
            // Reset flag after event cycle completes (using BeginInvoke)
            this.BeginInvoke(new Action(() => { isClearingGrid = false; }));
          }
          return; // Exit early to prevent PopulateTableSelector from triggering a refresh
        }

        // Only proceed if we're not already refreshing
        if (isRefreshingMetrics)
        {
          Program.Log("listWorkOrders_SelectedIndexChanged: already refreshing, skipping duplicate refresh");
          return;
        }

        Program.Log("listWorkOrders_SelectedIndexChanged: work orders checked, proceeding with refresh");
        // Show loading immediately when work order selection changes
        ShowLoadingIndicator(true, "Loading work order data...");
        // Note: Removed Application.DoEvents() to prevent event ordering issues

        PopulateTableSelector();
        RequestRefreshMetricsGrid();
      }
      catch { }
    }

    private void RequestRefreshMetricsGrid()
    {
      // If we're already clearing, don't do anything
      if (isClearingGrid)
      {
        Program.Log("RequestRefreshMetricsGrid: grid is being cleared, skipping refresh request");
        return;
      }

      // If nothing is checked, clear immediately and skip refresh
      // Note: We only use CHECKED items, not SELECTED (highlighted) items
      try
      {
        if (checkedDirs.Count == 0)
        {
          Program.Log("RequestRefreshMetricsGrid: no work orders checked; clearing metrics grid and skipping refresh");
          if (!isClearingGrid) // Only clear if not already clearing
          {
            isClearingGrid = true;
            ClearMetricsGridFast();
            // Reset flag after event cycle completes (using BeginInvoke)
            this.BeginInvoke(new Action(() => { isClearingGrid = false; }));
          }
          return;
        }
      }
      catch { }

      // Call RefreshMetricsGrid directly (removed timer-based debouncing to avoid race conditions)
      Program.Log($"RequestRefreshMetricsGrid: calling RefreshMetricsGrid directly, isRefreshingMetrics={isRefreshingMetrics}, isRefreshingTable={isRefreshingTable}");
      if (!isRefreshingMetrics)
      {
        RefreshMetricsGrid();
      }
      else
      {
        Program.Log("RequestRefreshMetricsGrid: skipping refresh - already refreshing");
      }
    }

    private string currentMetricsFilter = string.Empty; // stores active metrics filter text

    private void txtMetricsSearch_TextChanged(object sender, EventArgs e)
    {
      try
      {
        currentMetricsFilter = (sender as TextBox)?.Text ?? string.Empty;
        Program.Log($"txtMetricsSearch_TextChanged: filter='{currentMetricsFilter}'");
        // Debounce: schedule filter apply after short delay
        lastFilterRequestUtc = DateTime.UtcNow;
        if (!metricsFilterTimer.Enabled) metricsFilterTimer.Start();
      }
      catch { }
    }

    // Apply a lightweight filter to the currently bound metrics grid without rebuilding data
    private void ApplyMetricsFilterLive()
    {
      try
      {
        if (!(metricsGrid?.DataSource is BindingSource bs)) return;

        // Resolve current data and view regardless of current binding state
        DataTable data = null;
        DataView view = null;
        if (bs.DataSource is DataView curView)
        {
          view = curView;
          data = curView.Table;
        }
        else if (bs.DataSource is DataTable curTable)
        {
          data = curTable;
          view = curTable.DefaultView;
        }
        else
        {
          Program.Log($"ApplyMetricsFilterLive: unsupported bs.DataSource type {bs.DataSource?.GetType().FullName ?? "<null>"}");
          return;
        }

        var needle = (currentMetricsFilter ?? string.Empty).Trim();
        Program.Log($"ApplyMetricsFilterLive: enter filter='{needle}', dataRows={data?.Rows.Count ?? 0}, viewCount={view?.Count ?? 0}, bsType={bs.DataSource.GetType().Name}");

        if (string.IsNullOrEmpty(needle))
        {
          // Clear filter and keep binding to DataView for consistency
          try { if (view != null) view.RowFilter = string.Empty; } catch { }
          bs.DataSource = view ?? (object)data;
          bs.ResetBindings(false);
          Program.Log("ApplyMetricsFilterLive: cleared filter");
          ApplyDisabledRowStyling();
          metricsGrid?.Invalidate();
          metricsGrid?.Refresh();
          return;
        }

        // Build a RowFilter that searches visible, non-binary columns for the substring
        var safe = needle.Replace("'", "''");

        // Discover a subset of columns to search: limited to string-like columns and those currently visible in the grid
        var visibleCols = new List<string>();
        foreach (DataGridViewColumn gc in metricsGrid.Columns)
        {
          if (!gc.Visible) continue;
          var key = !string.IsNullOrEmpty(gc.DataPropertyName) ? gc.DataPropertyName : gc.Name;
          if (string.IsNullOrWhiteSpace(key)) continue;
          if (!data.Columns.Contains(key)) continue;
          var dt = data.Columns[key].DataType;
          if (dt == typeof(string)) visibleCols.Add(key);
          else if (dt == typeof(int) || dt == typeof(long) || dt == typeof(short) || dt == typeof(decimal) || dt == typeof(double) || dt == typeof(float)) visibleCols.Add(key);
        }

        if (visibleCols.Count == 0)
        {
          foreach (DataColumn c in data.Columns) if (c.DataType == typeof(string)) visibleCols.Add(c.ColumnName);
        }

        // Compose RowFilter using LIKE across columns; use Convert to string for non-string numerics
        var parts = new List<string>();
        foreach (var col in visibleCols)
        {
          var isString = data.Columns[col].DataType == typeof(string);
          var exprCol = isString ? ($"[{col}] LIKE '%{safe}%'") : ($"CONVERT([{col}], 'System.String') LIKE '%{safe}%'");
          parts.Add(exprCol);
        }
        string filterExpr = parts.Count > 0 ? string.Join(" OR ", parts) : string.Empty;
        string filterPreview = filterExpr.Length > 800 ? (filterExpr.Substring(0, 800) + " ...") : filterExpr;
        Program.Log($"ApplyMetricsFilterLive: columnsConsidered={visibleCols.Count}, rowFilterPreview={filterPreview}");

        if (view != null)
        {
          view.RowFilter = filterExpr;
          bs.DataSource = view;
        }
        else
        {
          var dv = data.DefaultView; dv.RowFilter = filterExpr; bs.DataSource = dv; view = dv;
        }
        bs.ResetBindings(false);
        Program.Log("ApplyMetricsFilterLive: applied filter");

        Program.Log($"ApplyMetricsFilterLive: after filter viewCount={view?.Count ?? 0}");

        // Update styling and UI
        ApplyDisabledRowStyling();
        metricsGrid?.Invalidate();
        metricsGrid?.Refresh();
      }
      catch (Exception ex)
      {
        Program.Log("ApplyMetricsFilterLive error", ex);
      }
    }

    private void PopulateTableSelector()
    {
      try
      {
        Program.Log($"PopulateTableSelector: ENTRY - checkedDirs.Count={checkedDirs.Count}, selectedIndices.Count={listWorkOrders.SelectedIndices.Count}");

        // If nothing is checked, clear the table selector and return early
        // Note: We only use CHECKED items, not SELECTED (highlighted) items
        if (checkedDirs.Count == 0)
        {
          Program.Log("PopulateTableSelector: no work orders checked, clearing table selector and returning early");
          suppressTableSelectorChanged = true;
          try
          {
            cmbTableSelector.Items.Clear();
            cmbTableSelector.SelectedIndex = -1;
          }
          finally
          {
            suppressTableSelectorChanged = false;
          }
          return;
        }

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
        Program.Log($"cmbTableSelector_SelectedIndexChanged: ENTRY - suppressTableSelectorChanged={suppressTableSelectorChanged}, checkedDirs.Count={checkedDirs.Count}, selectedIndices.Count={listWorkOrders.SelectedIndices.Count}");

        if (suppressTableSelectorChanged)
        {
          Program.Log("cmbTableSelector_SelectedIndexChanged: suppressed, returning");
          return;
        }

        // If no work orders are checked, don't trigger a refresh
        // Note: We only use CHECKED items, not SELECTED (highlighted) items
        if (checkedDirs.Count == 0)
        {
          Program.Log("cmbTableSelector_SelectedIndexChanged: no work orders checked; skipping refresh");
          return;
        }

        Program.Log("cmbTableSelector_SelectedIndexChanged: Starting table refresh");

        // Set flag to indicate we're refreshing the table
        isRefreshingTable = true;

        // Show loading indicator immediately when table selector changes
        ShowLoadingIndicator(true, "Loading table data...");
        // Note: Removed Application.DoEvents() to prevent event ordering issues

        // Update fronts filter button state immediately
        UpdateFilterButtonStates();

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

      // Early return if no items are checked - prevents repopulating after clearing
      // Note: We only use CHECKED items, not SELECTED (highlighted) items, to avoid confusion
      if (checkedDirs.Count == 0)
      {
        Program.Log("RefreshMetricsGrid: no work orders checked, clearing grid and returning early");
        ClearMetricsGridFast();
        isRefreshingMetrics = false;
        ShowLoadingIndicator(false);
        return;
      }

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

          // Get source paths - ONLY use checked work orders, not selected (highlighted) items
          // This prevents the grid from reverting to "selected" data when unchecking items
          List<string> sourcePaths;
          if (checkedDirs.Count > 0)
          {
            // Use checked work orders only
            sourcePaths = filteredWorkOrders
              .Where(wo => checkedDirs.Contains(wo.DirectoryPath) && File.Exists(wo.SdfPath))
              .Select(wo => wo.SdfPath)
              .ToList();
          }
          else
          {
            // No checked items - clear the grid
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

                // Check if settings.xml has column configuration for this table
                // If it does, we'll still use AutoGenerateColumns but ensure settings.xml is applied after binding
                var cfg = UserConfig.LoadOrDefault();
                var hasColumnConfig = cfg.TryGetColumnOrder(selectedTable.TableName) != null && cfg.TryGetColumnOrder(selectedTable.TableName).Count > 0;
                Program.Log($"RefreshMetricsGrid: Table '{selectedTable.TableName}' has column configuration in settings.xml: {hasColumnConfig}");

                metricsGrid.AutoGenerateColumns = true;

                // Build virtual columns before binding so initial columns reflect settings.xml
                var virtualStart = DateTime.Now;
                ApplyVirtualColumnsAndLayout(selectedTable.TableName, data);
                var virtualEnd = DateTime.Now;
                Program.Log($"ApplyVirtualColumnsAndLayout completed in {(virtualEnd - virtualStart).TotalMilliseconds:F0}ms");
                LogCurrentGridColumns("pre-bind (DataTable only)");

                // Ensure events and context menus are wired before binding to avoid duplicate handlers
                WireUpGridEvents();
                AddGridContextMenu();
                Program.Log("Metrics grid events/context menu wired before binding");

                // DataBindingComplete handler is already attached by WireUpGridEvents()
                applyLayoutAfterBinding = true;

                // Bind to DataView to avoid re-generating columns during filtering
                metricsGrid.DataSource = new BindingSource { DataSource = data.DefaultView };
                var dataSourceSet = DateTime.Now;
                Program.Log($"DataSource set in {(dataSourceSet - bindStart).TotalMilliseconds:F0}ms");

                // Note: DataBindingComplete event will fire after binding completes and will call
                // ApplyUserConfigToMetricsGrid to ensure settings.xml configuration is applied.
                // The hasColumnConfig flag is logged above for debugging purposes.

                // Do not apply layout here; rely on DataBindingComplete to apply once post-bind
                // Apply filter if any text present (log prior to applying)
                Program.Log($"RefreshMetricsGrid: applying live filter '{currentMetricsFilter}'");
                ApplyMetricsFilterLive();
                Program.Log("RefreshMetricsGrid: live filter applied");
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
              // Clear BindingSource data if it exists, then clear DataSource
              if (metricsGrid.DataSource is BindingSource bs)
              {
                bs.DataSource = null;
                bs.ResetBindings(false);
                Program.Log("RefreshMetricsGrid: cleared BindingSource data when no sources");
              }
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

        // Clear all state and caches
        currentSourcePaths = new List<string>();
        currentSelectedTable = null; // Reset selected table to prevent stale references
        ClearVirtualColumnCaches(); // Clear caches when clearing source data

        // Reset filter states
        isFrontsFilterActive = false;
        originalPartsData = null;
        isSubassemblyFilterActive = false;
        originalSubassemblyData = null;

        // Reset the isRefreshingMetrics flag to allow future refreshes
        isRefreshingMetrics = false;
        isRefreshingTable = false;

        // Stop the filter timer if running
        if (metricsFilterTimer != null && metricsFilterTimer.Enabled)
        {
          metricsFilterTimer.Stop();
          Program.Log("ClearMetricsGridFast: stopped filter timer");
        }

        if (metricsGrid == null) return;
        metricsGrid.SuspendLayout();
        var prevVisible = metricsGrid.Visible;
        metricsGrid.Visible = false;
        try
        {
          // Clear BindingSource data if it exists, then clear DataSource
          if (metricsGrid.DataSource is BindingSource bs)
          {
            // Fully disconnect the BindingSource
            bs.SuspendBinding();
            bs.DataSource = null;
            bs.ResetBindings(false);
            bs.ResumeBinding();
            Program.Log("ClearMetricsGridFast: cleared BindingSource data");
          }
          metricsGrid.DataSource = null;
          metricsGrid.Columns.Clear(); // Also clear columns to prevent stale column references
          metricsGrid.Rows.Clear();
          Program.Log($"ClearMetricsGridFast: grid DataSource={metricsGrid.DataSource}, Rows={metricsGrid.Rows.Count}, Cols={metricsGrid.Columns.Count}");
        }
        catch (Exception ex)
        {
          Program.Log($"ClearMetricsGridFast: error during clear - {ex.Message}");
        }
        finally
        {
          metricsGrid.Visible = prevVisible;
          metricsGrid.ResumeLayout();
          metricsGrid.Refresh();
          panelMetricsBorder.Refresh();
        }

        // Clear the table selector
        suppressTableSelectorChanged = true;
        try
        {
          cmbTableSelector.Items.Clear();
          cmbTableSelector.SelectedIndex = -1;
        }
        finally
        {
          suppressTableSelectorChanged = false;
        }

        // Note: Do NOT reset isClearingGrid here - let the caller manage it
        // This allows the flag to persist through the event cycle

        Program.Log("ClearMetricsGridFast: grid cleared successfully");
      }
      catch (Exception ex)
      {
        Program.Log($"ClearMetricsGridFast: outer exception - {ex.Message}");
        // Note: Do NOT reset isClearingGrid here - let the caller manage it
      }
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
        // Re-apply layout after the control reports binding complete
        metricsGrid.DataBindingComplete -= MetricsGrid_DataBindingComplete;
        metricsGrid.DataBindingComplete += MetricsGrid_DataBindingComplete;
        Program.Log("WireUpGridEvents: DataBindingComplete handler attached");

        // Context-aware menus
        metricsGrid.MouseDown -= MetricsGrid_MouseDown;
        metricsGrid.MouseDown += MetricsGrid_MouseDown;

        // Use DoubleClick on the grid (not just CellDoubleClick) so header/blank areas also toggle
        metricsGrid.DoubleClick -= MetricsGrid_DoubleClick;
        metricsGrid.DoubleClick += MetricsGrid_DoubleClick;
        // Also hook the parent panel so double-clicking padding/border toggles too

        // Custom sorting for programmatic sort mode columns
        metricsGrid.ColumnHeaderMouseClick -= MetricsGrid_ColumnHeaderMouseClick;
        metricsGrid.ColumnHeaderMouseClick += MetricsGrid_ColumnHeaderMouseClick;
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
        metricsGrid.KeyUp -= MetricsGrid_KeyUp;
        metricsGrid.KeyUp += MetricsGrid_KeyUp;
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
    private void ApplyUserConfigToMetricsGrid(string tableName, DataTable data = null)
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
              // Set column width
              col.Width = w.Value;
            }
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
          }


          // Apply visibility
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
            var visibility = cfg.TryGetColumnVisibility(tableName, key);
            if (visibility.HasValue)
            {
              // Set column visibility
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

          // Apply header overrides and tooltips
          metricsGrid.ShowCellToolTips = true; // enable tooltips generally
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
            var headerText = cfg.TryGetColumnHeaderText(tableName, key);
            if (!string.IsNullOrWhiteSpace(headerText))
            {
              // Clean any priority numbers from the header text before applying
              var cleanHeaderText = System.Text.RegularExpressions.Regex.Replace(headerText, @"^\d+\.\s*", "");
              col.HeaderText = cleanHeaderText;
            }
            var headerTip = cfg.TryGetColumnHeaderToolTip(tableName, key);
            if (!string.IsNullOrWhiteSpace(headerTip))
            {
              // DataGridView does not natively show header tooltips unless set on HeaderCell
              col.HeaderCell.ToolTipText = headerTip;
            }
          }

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

          // Configure column sorting based on data type
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
            if (!string.IsNullOrWhiteSpace(key) && data != null && data.Columns.Contains(key))
            {
              var dataType = data.Columns[key].DataType;
              ConfigureColumnSorting(col, dataType);
            }
          }

          // Apply order from settings.xml - always use settings.xml if configuration exists
          // Only fall back to auto-generated order if no configuration exists for this table
          var order = cfg.TryGetColumnOrder(tableName);
          if (order == null || order.Count == 0)
          {
            // No configuration exists in settings.xml - build a sensible default from existing columns
            Program.Log($"ApplyUserConfig: No column order configuration found in settings.xml for table '{tableName}', using auto-generated order");
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
            // Do not persist auto-built default order; apply transiently only
            Program.Log($"ApplyUserConfig: built transient default order with virtuals after LinkID: [{string.Join(", ", order)}]");
          }
          else
          {
            // Configuration exists in settings.xml - use it
            Program.Log($"ApplyUserConfig: Found column order configuration in settings.xml for table '{tableName}' with {order.Count} columns: [{string.Join(", ", order)}]");
          }

          if (order != null && order.Count > 0)
          {
            Program.Log($"ApplyUserConfig: applying order [{string.Join(", ", order)}]");
            LogCurrentGridColumns("before-apply-order");
            int idx = 0;
            var matched = new HashSet<DataGridViewColumn>();
            var unmatchedNames = new List<string>();
            foreach (var name in order)
            {
              DataGridViewColumn col = null;
              foreach (DataGridViewColumn c in metricsGrid.Columns)
              {
                var key2 = !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name;
                var keyNorm = (key2 ?? string.Empty).Trim();
                var nameNorm = (name ?? string.Empty).Trim();
                if (string.Equals(keyNorm, nameNorm, StringComparison.OrdinalIgnoreCase)) { col = c; break; }
              }
              if (col != null)
              {
                Program.Log($"ApplyUserConfig: setting DisplayIndex for column '{col.Name}' (DataPropertyName='{col.DataPropertyName}') to {idx}");
                col.DisplayIndex = idx++;
                matched.Add(col);
                Program.Log($"ApplyUserConfig: column '{col.Name}' DisplayIndex is now {col.DisplayIndex}");
              }
              else
              {
                unmatchedNames.Add(name);
              }
            }

            // Place any unspecified columns after the specified ones, preserving their relative order
            var remaining = metricsGrid.Columns
              .Cast<DataGridViewColumn>()
              .Where(c => !matched.Contains(c))
              .OrderBy(c => c.DisplayIndex)
              .ToList();
            foreach (var c in remaining)
            {
              Program.Log($"ApplyUserConfig: appending unspecified column '{c.Name}' at DisplayIndex {idx}");
              c.DisplayIndex = idx++;
            }

            if (unmatchedNames.Count > 0)
            {
              Program.Log($"ApplyUserConfig: unmatched names from settings for table '{tableName}': [{string.Join(", ", unmatchedNames)}]");
            }

            LogCurrentGridColumns("after-apply-order");
          }
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

        // Enable user reordering and resizing
        metricsGrid.AllowUserToOrderColumns = true;
        metricsGrid.AllowUserToResizeColumns = true;

        // Enable custom header styling (disable visual styles for headers)
        metricsGrid.EnableHeadersVisualStyles = false;

        // Initialize edit mode visuals/state
        ApplyMainGridEditState();

        // Ensure all column headers have proper styling
        RefreshAllColumnHeaderStyles();

        // Update fronts filter button state
        UpdateFilterButtonStates();
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

    // Extract work order name from directory path (same as GetDisplayPath but returns just the name)
    private string GetWorkOrderNameFromPath(string directoryPath)
    {
      if (string.IsNullOrEmpty(directoryPath)) return string.Empty;
      string root = string.Empty;
      try { root = DefaultRoot; }
      catch { }
      if (string.IsNullOrEmpty(root)) return Path.GetFileName(directoryPath);
      string absNorm = directoryPath.Replace('/', '\\');
      string rootNorm = root.Replace('/', '\\').TrimEnd('\\');
      if (absNorm.StartsWith(rootNorm, StringComparison.OrdinalIgnoreCase))
      {
        string rel = absNorm.Substring(rootNorm.Length).TrimStart('\\');
        return rel;
      }
      return Path.GetFileName(directoryPath);
    }

    // Generate common prefix from selected work order names
    private void UpdateWorkOrderNameFromSelection()
    {
      try
      {
        // Get all selected work order names
        var selectedNames = new List<string>();

        // Add checked work orders
        foreach (var dir in checkedDirs)
        {
          var wo = filteredWorkOrders.FirstOrDefault(w => w.DirectoryPath == dir);
          if (wo != null)
          {
            string name = GetWorkOrderNameFromPath(wo.DirectoryPath);
            if (!string.IsNullOrWhiteSpace(name))
            {
              selectedNames.Add(name);
            }
          }
        }

        // Add selected work orders (if any and not already checked)
        foreach (int idx in listWorkOrders.SelectedIndices)
        {
          if (idx >= 0 && idx < filteredWorkOrders.Count)
          {
            var wo = filteredWorkOrders[idx];
            if (!checkedDirs.Contains(wo.DirectoryPath))
            {
              string name = GetWorkOrderNameFromPath(wo.DirectoryPath);
              if (!string.IsNullOrWhiteSpace(name))
              {
                selectedNames.Add(name);
              }
            }
          }
        }

        if (selectedNames.Count == 0)
        {
          // No work orders selected, don't change txtOutput
          return;
        }

        if (selectedNames.Count == 1)
        {
          // Single work order selected, use its name as-is
          txtOutput.Text = selectedNames[0];
          return;
        }

        // Multiple work orders selected, find common prefix
        string commonPrefix = FindCommonPrefix(selectedNames);

        // Remove trailing '_' and whitespace characters
        commonPrefix = commonPrefix.TrimEnd('_', ' ', '\t', '\r', '\n');

        // Only update if we found a meaningful common prefix
        if (!string.IsNullOrWhiteSpace(commonPrefix))
        {
          txtOutput.Text = commonPrefix;
        }
      }
      catch (Exception ex)
      {
        Program.Log("UpdateWorkOrderNameFromSelection error", ex);
      }
    }

    // Find the longest common prefix from a list of strings
    private string FindCommonPrefix(List<string> names)
    {
      if (names == null || names.Count == 0) return string.Empty;
      if (names.Count == 1) return names[0];

      // Start with the first name as the initial prefix
      string prefix = names[0];

      // Compare with each subsequent name and shorten prefix as needed
      for (int i = 1; i < names.Count; i++)
      {
        string currentName = names[i];
        int minLength = Math.Min(prefix.Length, currentName.Length);

        // Find the position where they differ
        int commonLength = 0;
        for (int j = 0; j < minLength; j++)
        {
          if (prefix[j] == currentName[j])
          {
            commonLength++;
          }
          else
          {
            break;
          }
        }

        // Update prefix to the common part
        prefix = prefix.Substring(0, commonLength);

        // If no common prefix found, return empty
        if (commonLength == 0)
        {
          return string.Empty;
        }
      }

      return prefix;
    }

    // Build a consolidated DataTable from multiple source SDF paths for the given table
    private DataTable BuildDataTableFromSources(string tableName, List<string> sourcePaths)
    {
      var dataTable = new DataTable(tableName);
      DataTable schema = null;
      var tempFiles = new List<string>();
      try
      {
        // Parts name cleanup counters (avoid per-row logging).
        int partsNamesCleaned = 0; // count of rows whose Name was modified
        bool isPartsTable = string.Equals(tableName, "Parts", StringComparison.OrdinalIgnoreCase); // fast check

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

                  // Clean up Parts.Name as the data is loaded.
                  if (isPartsTable && dataTable.Columns.Contains("Name"))
                  {
                    try
                    {
                      var rawNameObj = row["Name"];
                      var rawName = (rawNameObj == null || rawNameObj == DBNull.Value) ? null : Convert.ToString(rawNameObj);
                      var cleanedName = PartNameUtils.CleanPartName(rawName);
                      if (!string.Equals(rawName, cleanedName, StringComparison.Ordinal))
                      {
                        row["Name"] = cleanedName ?? (object)DBNull.Value;
                        partsNamesCleaned++;
                      }
                    }
                    catch { }
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

        // Convert string columns to numeric types if they contain numeric data
        ConvertNumericStringColumns(dataTable);

        // Log summary of data loading
        Program.Log($"BuildDataTableFromSources completed for table '{tableName}': {totalRowsLoaded} total rows loaded from {sourcePaths.Count} source(s)");

        // Log Parts name cleanup summary once per build.
        if (isPartsTable && partsNamesCleaned > 0)
        {
          Program.Log($"BuildDataTableFromSources: cleaned Parts.Name on {partsNamesCleaned} row(s) (removed markers like [NODRAW])");
        }
      }
      finally
      {
        foreach (var tf in tempFiles.Distinct()) { try { File.Delete(tf); } catch { } }
      }
      return dataTable;
    }

    // Convert string columns to numeric types if they contain numeric data
    private void ConvertNumericStringColumns(DataTable dataTable)
    {
      try
      {
        if (dataTable == null || dataTable.Rows.Count == 0) return;

        var columnsToConvert = new List<DataColumn>();

        // Analyze each string column to see if it should be converted to numeric
        foreach (DataColumn column in dataTable.Columns)
        {
          if (column.DataType != typeof(string)) continue;

          var detectedType = DetectColumnDataType(column.ColumnName, dataTable);
          if (detectedType == typeof(int) || detectedType == typeof(decimal))
          {
            columnsToConvert.Add(column);
            // Will convert column from string to numeric type
          }
        }

        // Convert the identified columns
        foreach (var column in columnsToConvert)
        {
          var detectedType = DetectColumnDataType(column.ColumnName, dataTable);
          ConvertColumnDataType(dataTable, column, detectedType);
        }
      }
      catch (Exception ex)
      {
        Program.Log($"ConvertNumericStringColumns error: {ex.Message}", ex);
      }
    }

    // Convert a single column's data type
    private void ConvertColumnDataType(DataTable dataTable, DataColumn originalColumn, Type newType)
    {
      try
      {
        Program.Log($"ConvertColumnDataType: Converting column '{originalColumn.ColumnName}' from {originalColumn.DataType.Name} to {newType.Name}");

        // Create a new column with the correct data type
        var newColumn = new DataColumn(originalColumn.ColumnName + "_temp", newType);
        dataTable.Columns.Add(newColumn);

        var conversionErrors = 0;
        var totalRows = dataTable.Rows.Count;

        // Copy data with proper type conversion
        foreach (DataRow row in dataTable.Rows)
        {
          var value = row[originalColumn];
          if (value != null && value != DBNull.Value)
          {
            try
            {
              var stringValue = value.ToString();

              if (newType == typeof(int))
              {
                if (int.TryParse(stringValue, out var intVal))
                  row[newColumn] = intVal;
                else
                {
                  row[newColumn] = 0;
                  conversionErrors++;
                  Program.Log($"ConvertColumnDataType: Failed to convert '{stringValue}' to int, using 0");
                }
              }
              else if (newType == typeof(decimal))
              {
                if (decimal.TryParse(stringValue, out var decVal))
                  row[newColumn] = decVal;
                else
                {
                  row[newColumn] = 0m;
                  conversionErrors++;
                  Program.Log($"ConvertColumnDataType: Failed to convert '{stringValue}' to decimal, using 0");
                }
              }
              else
              {
                row[newColumn] = value;
              }
            }
            catch (Exception conversionEx)
            {
              // If conversion fails, use default value
              conversionErrors++;
              Program.Log($"ConvertColumnDataType: Exception converting '{value}' to {newType.Name}: {conversionEx.Message}");

              if (newType == typeof(int))
                row[newColumn] = 0;
              else if (newType == typeof(decimal))
                row[newColumn] = 0m;
              else
                row[newColumn] = value;
            }
          }
          else
          {
            row[newColumn] = DBNull.Value;
          }
        }

        // Remove the original column and rename the new one
        dataTable.Columns.Remove(originalColumn);
        newColumn.ColumnName = originalColumn.ColumnName;

        Program.Log($"ConvertColumnDataType: Successfully converted column '{originalColumn.ColumnName}' to {newType.Name}. {conversionErrors}/{totalRows} conversion errors");
      }
      catch (Exception ex)
      {
        Program.Log($"ConvertColumnDataType error for column '{originalColumn.ColumnName}': {ex.Message}", ex);
      }
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
          Program.Log($"BuildVirtualLookupCaches: completed for {virtualLookupCacheByColumn.Count} columns");
        }
      catch (Exception ex)
      {
        Program.Log("MainForm: BuildVirtualLookupCaches failed", ex);
      }
    }

    // Determine appropriate data type for virtual columns based on their content
    private Type DetermineVirtualColumnDataType(UserConfig.VirtualColumnDef def, DataTable data)
    {
      try
      {
        // For action columns, always use string since they contain button text
        if (def.IsActionColumn)
        {
          return typeof(string);
        }

        // For lookup columns, analyze the target value column data type
        if (def.IsLookupColumn && !string.IsNullOrWhiteSpace(def.TargetValueColumn))
        {
          // Try to find the target table and analyze its column data type
          var targetTableName = def.TargetTableName;
          if (!string.IsNullOrWhiteSpace(targetTableName))
          {
            // Check if we have schema information for the target table
            var schema = GetTableSchema(targetTableName);
            if (schema != null && schema.Rows.Count > 0)
            {
              var targetColumn = schema.Rows.Cast<DataRow>()
                .FirstOrDefault(r => string.Equals(Convert.ToString(r["ColumnName"]), def.TargetValueColumn, StringComparison.OrdinalIgnoreCase));
              if (targetColumn != null)
              {
                var targetDataType = (Type)targetColumn["DataType"];
                Program.Log($"DetermineVirtualColumnDataType: Using target column data type {targetDataType.Name} for virtual column '{def.ColumnName}'");
                return targetDataType;
              }
            }
          }

          // If schema lookup fails, use content-based detection on the virtual column data
          // This will analyze the actual values in the virtual column after it's populated
          Program.Log($"DetermineVirtualColumnDataType: Schema lookup failed for virtual column '{def.ColumnName}', will use content-based detection");
          return typeof(string); // Will be overridden by content-based detection in DetectColumnDataType
        }

        // Default to string for unknown or unanalyzable columns
        return typeof(string);
      }
      catch (Exception ex)
      {
        Program.Log($"DetermineVirtualColumnDataType error for column '{def.ColumnName}': {ex.Message}");
        return typeof(string);
      }
    }

    // Get table schema information (cached for performance)
    private static Dictionary<string, DataTable> _schemaCache = new Dictionary<string, DataTable>();
    private DataTable GetTableSchema(string tableName)
    {
      try
      {
        if (_schemaCache.TryGetValue(tableName, out var cachedSchema))
        {
          return cachedSchema;
        }

        // For now, return null to use default string type
        // In the future, this could be enhanced to analyze actual database schemas
        Program.Log($"GetTableSchema: No schema available for table '{tableName}', using default string type");
        return null;
      }
      catch (Exception ex)
      {
        Program.Log($"GetTableSchema error for table '{tableName}': {ex.Message}");
        return null;
      }
    }

    // Configure DataGridView column sorting based on data type
    private void ConfigureColumnSorting(DataGridViewColumn column, Type dataType)
    {
      try
      {
        if (column == null) return;

        // Enable programmatic sorting for ALL columns to support multi-column sorting
        // This allows Ctrl+click multi-sort to work on all column types
        column.SortMode = DataGridViewColumnSortMode.Programmatic;

        if (dataType == typeof(int) || dataType == typeof(long) || dataType == typeof(short) ||
            dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
        {
          // Numeric columns use programmatic sorting for proper numeric comparison
          Program.Log($"ConfigureColumnSorting: Set programmatic sorting for numeric column '{column.Name}' ({dataType.Name})");
        }
        else if (dataType == typeof(DateTime))
        {
          // DateTime columns use programmatic sorting for proper date comparison
          Program.Log($"ConfigureColumnSorting: Set programmatic sorting for DateTime column '{column.Name}'");
        }
        else
        {
          // String and other types also use programmatic sorting for multi-column support
          Program.Log($"ConfigureColumnSorting: Set programmatic sorting for string column '{column.Name}' ({dataType.Name})");
        }
      }
      catch (Exception ex)
      {
        Program.Log($"ConfigureColumnSorting error for column '{column.Name}': {ex.Message}");
        // Fallback to programmatic sorting to maintain multi-column functionality
        try { column.SortMode = DataGridViewColumnSortMode.Programmatic; } catch { }
      }
    }

    // Handle custom sorting for programmatic sort mode columns
    private void MetricsGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
      try
      {
        if (e.ColumnIndex < 0 || metricsGrid == null) return;

        var column = metricsGrid.Columns[e.ColumnIndex];
        if (column.SortMode != DataGridViewColumnSortMode.Programmatic) return;

        // Get the data source
        if (!(metricsGrid.DataSource is BindingSource bindingSource) ||
            !(bindingSource.DataSource is DataView dataView)) return;

        var dataTable = dataView.Table;
        if (dataTable == null) return;

        // Get the column name
        var columnName = !string.IsNullOrEmpty(column.DataPropertyName) ? column.DataPropertyName : column.Name;
        if (!dataTable.Columns.Contains(columnName)) return;

        var dataType = dataTable.Columns[columnName].DataType;
        var detectedType = DetectColumnDataType(columnName, dataTable);
        Program.Log($"Multi-column sorting column '{columnName}' with data type {dataType.Name}, detected as {detectedType.Name}");

        // Handle multi-column sorting
        HandleMultiColumnSort(columnName, detectedType, column, dataView);
      }
      catch (Exception ex)
      {
        Program.Log($"Multi-column sorting error for column {e.ColumnIndex}: {ex.Message}");
      }
    }

    private void HandleMultiColumnSort(string columnName, Type dataType, DataGridViewColumn column, DataView dataView)
    {
      // Check if Ctrl key is pressed for multi-column sorting
      bool isMultiColumn = Control.ModifierKeys.HasFlag(Keys.Control);

      if (isMultiColumn)
      {
        // Multi-column sorting: add/update this column in the sort list
        var existingSort = sortColumns.FirstOrDefault(s => s.ColumnName == columnName);
        if (existingSort != null)
        {
          // Column already exists, cycle through: ASC -> DESC -> Remove
          if (existingSort.Direction == System.Windows.Forms.SortOrder.Ascending)
          {
            existingSort.Direction = System.Windows.Forms.SortOrder.Descending;
          }
          else
          {
            // Remove from sort list
            sortColumns.Remove(existingSort);
            // Renumber priorities
            for (int i = 0; i < sortColumns.Count; i++)
            {
              sortColumns[i].Priority = i;
            }
          }
        }
        else
        {
          // Add new column to sort list
        sortColumns.Add(new SortColumn
        {
          ColumnName = columnName,
          Direction = System.Windows.Forms.SortOrder.Ascending,
          Priority = sortColumns.Count
        });
        }
      }
      else
      {
        // Single column sorting: clear existing sorts and set this as primary
        // Check if this column is already the primary sort to toggle direction
        var existingPrimary = sortColumns.FirstOrDefault(s => s.Priority == 0 && s.ColumnName == columnName);

        sortColumns.Clear();

        if (existingPrimary != null)
        {
          // Column is already primary, toggle direction
          sortColumns.Add(new SortColumn
          {
            ColumnName = columnName,
            Direction = existingPrimary.Direction == System.Windows.Forms.SortOrder.Ascending
              ? System.Windows.Forms.SortOrder.Descending
              : System.Windows.Forms.SortOrder.Ascending,
            Priority = 0
          });
        }
        else
        {
          // New column, start with ascending
          sortColumns.Add(new SortColumn
          {
            ColumnName = columnName,
            Direction = System.Windows.Forms.SortOrder.Ascending,
            Priority = 0
          });
        }
      }

      // Apply the multi-column sort
      ApplyMultiColumnSort(dataView, dataType);

      // Update column header visual indicators
      UpdateColumnSortIndicators();

      // Update Clear Sort button visibility
      UpdateClearSortButtonVisibility();
    }

    private void ApplyMultiColumnSort(DataView dataView, Type dataType)
    {
      if (sortColumns.Count == 0)
      {
        dataView.Sort = "";
        return;
      }

      var sortExpressions = new List<string>();
      var dataTable = dataView.Table;

      foreach (var sortColumn in sortColumns.OrderBy(s => s.Priority))
      {
        var sortDirection = sortColumn.Direction == System.Windows.Forms.SortOrder.Ascending ? "ASC" : "DESC";
        string sortExpression;

        // Detect the data type for this specific column based on content
        var columnDataType = DetectColumnDataType(sortColumn.ColumnName, dataTable);

        // Create sort expression based on the detected data type
        // SQL CE doesn't support CAST, so we use direct column sorting
        // For numeric types, we rely on the data being properly typed in the DataTable
        if (columnDataType == typeof(int) || columnDataType == typeof(long) || columnDataType == typeof(short) ||
            columnDataType == typeof(decimal) || columnDataType == typeof(double) || columnDataType == typeof(float))
        {
          // For numeric types, use direct column sorting - SQL CE will handle numeric sorting correctly
          sortExpression = $"[{sortColumn.ColumnName}] {sortDirection}";
        }
        else if (columnDataType == typeof(DateTime))
        {
          sortExpression = $"[{sortColumn.ColumnName}] {sortDirection}";
        }
        else
        {
          sortExpression = $"[{sortColumn.ColumnName}] {sortDirection}";
        }

        sortExpressions.Add(sortExpression);
      }

      var finalSortExpression = string.Join(", ", sortExpressions);
      dataView.Sort = finalSortExpression;

      Program.Log($"Applied multi-column sort: {finalSortExpression}");
    }

    private void UpdateColumnSortIndicators()
    {
      // Clear all sort indicators and priority numbers first
      foreach (DataGridViewColumn col in metricsGrid.Columns)
      {
        col.HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None;

        // Always remove priority numbers from all column headers first
        var headerText = col.HeaderText;
        if (!string.IsNullOrWhiteSpace(headerText))
        {
          // Remove pattern like "1. ", "2. ", "3. " etc. from the beginning of header text
          var cleanHeaderText = System.Text.RegularExpressions.Regex.Replace(headerText, @"^\d+\.\s*", "");
          col.HeaderText = cleanHeaderText;
        }
      }

      // Set indicators for active sort columns
      foreach (var sortColumn in sortColumns)
      {
        var column = metricsGrid.Columns.Cast<DataGridViewColumn>()
          .FirstOrDefault(c => (c.DataPropertyName ?? c.Name) == sortColumn.ColumnName);

        if (column != null)
        {
          column.HeaderCell.SortGlyphDirection = sortColumn.Direction;

          // Add priority indicator for multi-column sorts only
          if (sortColumns.Count > 1)
          {
            var priority = sortColumn.Priority + 1;
            // Header text is already clean from the loop above, so just add priority
            column.HeaderText = $"{priority}. {column.HeaderText}";
          }
        }
      }
    }

    private void ClearAllSorts()
    {
      try
      {
        sortColumns.Clear();

        // Clear the data view sort
        if (metricsGrid.DataSource is BindingSource bindingSource &&
            bindingSource.DataSource is DataView dataView)
        {
          dataView.Sort = "";
        }

        // Clear all column indicators
        UpdateColumnSortIndicators();

        // Force refresh of column headers to ensure priority numbers are removed
        RefreshColumnHeadersAfterClearSort();

        // Update Clear Sort button visibility
        UpdateClearSortButtonVisibility();

        Program.Log("Cleared all column sorts");
      }
      catch (Exception ex)
      {
        Program.Log($"Error clearing sorts: {ex.Message}", ex);
      }
    }

    private void RefreshColumnHeadersAfterClearSort()
    {
      try
      {
        // Force refresh of column headers to remove priority numbers
        foreach (DataGridViewColumn col in metricsGrid.Columns)
        {
          var headerText = col.HeaderText;
          if (!string.IsNullOrWhiteSpace(headerText))
          {
            // Remove pattern like "1. ", "2. ", "3. " etc. from the beginning of header text
            var cleanHeaderText = System.Text.RegularExpressions.Regex.Replace(headerText, @"^\d+\.\s*", "");
            col.HeaderText = cleanHeaderText;
          }
        }

        // Force grid refresh to show the changes
        metricsGrid.Invalidate(new System.Drawing.Rectangle(0, 0, metricsGrid.Width, metricsGrid.ColumnHeadersHeight));

        // Refreshed column headers after clearing sort
      }
      catch (Exception ex)
      {
        Program.Log($"RefreshColumnHeadersAfterClearSort error: {ex.Message}", ex);
      }
    }

    private void UpdateClearSortButtonVisibility()
    {
      try
      {
        if (btnClearSort != null)
        {
          // Show Clear Sort button only when multi-sort is enabled (more than 1 column)
          btnClearSort.Visible = sortColumns.Count > 1;
        }
      }
      catch (Exception ex)
      {
        Program.Log($"Error updating Clear Sort button visibility: {ex.Message}", ex);
      }
    }

    private void UpdateVirtualColumnDataTypes(DataTable data)
    {
      try
      {
        if (data == null || virtualColumnDefs == null) return;

        foreach (var def in virtualColumnDefs)
        {
          if (string.IsNullOrWhiteSpace(def.ColumnName) || !data.Columns.Contains(def.ColumnName)) continue;

          // Skip action columns
          if (def.IsActionColumn) continue;

          // Detect the appropriate data type based on content
          var detectedType = DetectColumnDataType(def.ColumnName, data);
          var currentType = data.Columns[def.ColumnName].DataType;

          // If the detected type is different from current type, update it
          if (detectedType != currentType)
          {
            Program.Log($"UpdateVirtualColumnDataTypes: Updating virtual column '{def.ColumnName}' from {currentType.Name} to {detectedType.Name}");

            // Create a new column with the correct data type
            var newColumn = new DataColumn(def.ColumnName + "_temp", detectedType);
            data.Columns.Add(newColumn);

            // Copy data with proper type conversion
            var conversionErrors = 0;
            var totalRows = data.Rows.Count;

            foreach (DataRow row in data.Rows)
            {
              var value = row[def.ColumnName];
              if (value != null && value != DBNull.Value)
              {
                try
                {
                  var stringValue = value.ToString();

                  if (detectedType == typeof(int))
                  {
                    if (int.TryParse(stringValue, out var intVal))
                      row[newColumn] = intVal;
                    else
                    {
                      row[newColumn] = 0;
                      conversionErrors++;
                      Program.Log($"UpdateVirtualColumnDataTypes: Failed to convert '{stringValue}' to int, using 0");
                    }
                  }
                  else if (detectedType == typeof(decimal))
                  {
                    if (decimal.TryParse(stringValue, out var decVal))
                      row[newColumn] = decVal;
                    else
                    {
                      row[newColumn] = 0m;
                      conversionErrors++;
                      Program.Log($"UpdateVirtualColumnDataTypes: Failed to convert '{stringValue}' to decimal, using 0");
                    }
                  }
                  else
                  {
                    row[newColumn] = value;
                  }
                }
                catch (Exception conversionEx)
                {
                  conversionErrors++;
                  Program.Log($"UpdateVirtualColumnDataTypes: Exception converting '{value}' to {detectedType.Name}: {conversionEx.Message}");

                  if (detectedType == typeof(int))
                    row[newColumn] = 0;
                  else if (detectedType == typeof(decimal))
                    row[newColumn] = 0m;
                  else
                    row[newColumn] = value;
                }
              }
              else
              {
                row[newColumn] = DBNull.Value;
              }
            }

            Program.Log($"UpdateVirtualColumnDataTypes: Converted virtual column '{def.ColumnName}' to {detectedType.Name}. {conversionErrors}/{totalRows} conversion errors");

            // Remove old column and rename new one
            var oldOrdinal = data.Columns[def.ColumnName].Ordinal;
            data.Columns.Remove(def.ColumnName);
            newColumn.ColumnName = def.ColumnName;
            newColumn.SetOrdinal(oldOrdinal);
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log($"Error updating virtual column data types: {ex.Message}", ex);
      }
    }

    private void btnClearSort_Click(object sender, EventArgs e)
    {
      ClearAllSorts();
    }

    private Type DetectColumnDataType(string columnName, DataTable dataTable)
    {
      try
      {
        if (!dataTable.Columns.Contains(columnName)) return typeof(string);

        var column = dataTable.Columns[columnName];
        var originalType = column.DataType;

        // If it's already a numeric type, use it
        if (originalType == typeof(int) || originalType == typeof(long) || originalType == typeof(short) ||
            originalType == typeof(decimal) || originalType == typeof(double) || originalType == typeof(float))
        {
          return originalType;
        }

        // If it's already DateTime, use it
        if (originalType == typeof(DateTime))
        {
          return originalType;
        }

        // For string columns, analyze the content to detect numeric data
        if (originalType == typeof(string))
        {
          var sampleSize = Math.Min(20, dataTable.Rows.Count); // Sample first 20 rows
          var integerCount = 0;
          var decimalCount = 0;
          var dateCount = 0;
          var hasDecimalValues = false;

          for (int i = 0; i < sampleSize; i++)
          {
            var value = dataTable.Rows[i][columnName]?.ToString();
            if (string.IsNullOrWhiteSpace(value)) continue;

            // Check for decimal first (this will catch values like "2.00", "3.5", etc.)
            if (decimal.TryParse(value, out var decVal))
            {
              decimalCount++;
              // Check if this decimal value has fractional part or decimal places
              if (decVal != Math.Floor(decVal) || value.Contains("."))
              {
                hasDecimalValues = true;
              }
            }
            // Check for integer (only if it's not already a decimal)
            else if (int.TryParse(value, out _))
            {
              integerCount++;
            }
            // Check for date
            else if (DateTime.TryParse(value, out _))
            {
              dateCount++;
            }
          }

          Program.Log($"DetectColumnDataType for '{columnName}': integerCount={integerCount}, decimalCount={decimalCount}, hasDecimalValues={hasDecimalValues}, dateCount={dateCount}");

          // Determine the best type based on content analysis
          if (hasDecimalValues || (decimalCount > 0 && decimalCount >= integerCount))
          {
            // If we have any decimal values or more decimal values than integers, use decimal
            Program.Log($"DetectColumnDataType: Detected column '{columnName}' as decimal");
            return typeof(decimal);
          }
          else if (integerCount > dateCount && integerCount > sampleSize * 0.7)
          {
            // Detected column as integer based on content analysis
            Program.Log($"DetectColumnDataType: Detected column '{columnName}' as integer");
            return typeof(int);
          }
          else if (dateCount > sampleSize * 0.7)
          {
            // Detected column as DateTime based on content analysis
            Program.Log($"DetectColumnDataType: Detected column '{columnName}' as DateTime");
            return typeof(DateTime);
          }
        }

        // Default to string
        Program.Log($"DetectColumnDataType: Defaulting column '{columnName}' to string");
        return typeof(string);
      }
      catch (Exception ex)
      {
        Program.Log($"Error detecting data type for column '{columnName}': {ex.Message}");
        return typeof(string);
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
            // Determine appropriate data type for virtual column based on its content
            var dataType = DetermineVirtualColumnDataType(def, data);
            data.Columns.Add(def.ColumnName, dataType);
            newlyAdded.Add(def.ColumnName);
            Program.Log($"MainForm: Added virtual column '{def.ColumnName}' with data type {dataType.Name}");
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

        // Update data types for virtual columns after they're populated with data
        UpdateVirtualColumnDataTypes(data);

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
            // Ensure settings.xml configuration is applied after resetting bindings
            // This ensures columns are always configured from settings.xml when available
            if (!string.IsNullOrWhiteSpace(tableName))
            {
              Program.Log($"RebuildVirtualColumns: Applying settings.xml configuration after virtual column refresh");
              ApplyUserConfigToMetricsGrid(tableName, data);
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
          var key = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
          SaveColumnVisibility(key, false);
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
              var key = !string.IsNullOrEmpty(gridCol.DataPropertyName) ? gridCol.DataPropertyName : gridCol.Name;
              SaveColumnVisibility(key, true);
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
              var key = !string.IsNullOrEmpty(gridCol.DataPropertyName) ? gridCol.DataPropertyName : gridCol.Name;
              SaveColumnVisibility(key, true);
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

        // Apply immediately - reset to original column name when clearing
        column.HeaderText = string.IsNullOrWhiteSpace(headerText) ? column.Name : headerText;
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

        // Apply the tooltip immediately - clear when empty
        col.HeaderCell.ToolTipText = string.IsNullOrWhiteSpace(toolTip) ? null : toolTip;
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
        // Handle Ctrl key state changes for header coloring
        if (e.KeyCode == Keys.ControlKey)
        {
          UpdateHeaderColorsForCtrlState(true);
        }

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

    private void MetricsGrid_KeyUp(object sender, KeyEventArgs e)
    {
      try
      {
        // Handle Ctrl key state changes for header coloring
        if (e.KeyCode == Keys.ControlKey)
        {
          UpdateHeaderColorsForCtrlState(false);
        }
      }
      catch { }
    }

    private void UpdateHeaderColorsForCtrlState(bool isCtrlPressed)
    {
      try
      {
        if (metricsGrid == null) return;

        // Check if state has actually changed to avoid unnecessary updates
        if (this.isCtrlPressed == isCtrlPressed) return;

        // Update the Ctrl state tracking
        this.isCtrlPressed = isCtrlPressed;

        // Suspend layout to prevent multiple redraws
        metricsGrid.SuspendLayout();

        try
        {
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            if (col.HeaderCell.Style == null)
            {
              col.HeaderCell.Style = new DataGridViewCellStyle();
            }

            if (isCtrlPressed)
            {
              // Ctrl is pressed - highlight headers to indicate multi-sort mode
              col.HeaderCell.Style.BackColor = System.Drawing.Color.LightBlue;
              col.HeaderCell.Style.ForeColor = System.Drawing.Color.DarkBlue;
            }
            else
            {
              // Ctrl is released - restore normal header colors
              col.HeaderCell.Style.BackColor = System.Drawing.Color.White;
              col.HeaderCell.Style.ForeColor = System.Drawing.Color.Black;
            }
          }

          // Only invalidate the header area, not the entire grid
          metricsGrid.Invalidate(new System.Drawing.Rectangle(0, 0, metricsGrid.Width, metricsGrid.ColumnHeadersHeight));
        }
        finally
        {
          // Resume layout
          metricsGrid.ResumeLayout(false);
        }

        // Updated header colors for Ctrl state
      }
      catch (Exception ex)
      {
        Program.Log($"UpdateHeaderColorsForCtrlState error: {ex.Message}", ex);
      }
    }

    private void MetricsGrid_CurrentCellDirtyStateChanged_Edit(object sender, EventArgs e)
    {
      if (!isEditModeMainGrid || metricsGrid.ReadOnly) return;
      try
      {
        Program.Log($"MetricsGrid_CurrentCellDirtyStateChanged_Edit: IsDirty={metricsGrid.IsCurrentCellDirty}, isEditMode={isEditModeMainGrid}, ReadOnly={metricsGrid.ReadOnly}");
        if (metricsGrid.IsCurrentCellDirty)
        {
          var cell = metricsGrid.CurrentCell;
          Program.Log($"MetricsGrid_CurrentCellDirtyStateChanged_Edit: Cell type={cell?.GetType().Name}");
          if (cell is DataGridViewCheckBoxCell || cell is DataGridViewComboBoxCell)
          {
            // For checkbox/combo, commit immediately and end edit to trigger CellEndEdit
            Program.Log("MetricsGrid_CurrentCellDirtyStateChanged_Edit: Committing and ending edit for checkbox/combo cell");
            metricsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            metricsGrid.EndEdit();
          }
          // For text cells, do nothing here; wait for CellEndEdit so typing isn't interrupted
        }
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_CurrentCellDirtyStateChanged_Edit error", ex);
      }
    }

    private void MetricsGrid_CellEndEdit_Edit(object sender, DataGridViewCellEventArgs e)
    {
      try
      {
        Program.Log($"MetricsGrid_CellEndEdit_Edit: rowIndex={e.RowIndex}, columnIndex={e.ColumnIndex}, isEditMode={isEditModeMainGrid}");
        if (!isEditModeMainGrid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        TrySaveSingleCellChange_MainGrid(e.RowIndex, e.ColumnIndex, true);
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_CellEndEdit_Edit error", ex);
      }
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
        Program.Log($"TrySaveSingleCellChange_MainGrid: rowIndex={rowIndex}, columnIndex={columnIndex}, isEndEdit={isEndEdit}, isEditMode={isEditModeMainGrid}");

        if (metricsGrid?.DataSource is BindingSource bs)
        {
          // Handle both DataTable and DataView as the BindingSource's DataSource
          DataTable data = null;
          if (bs.DataSource is DataTable dataTable)
          {
            data = dataTable;
          }
          else if (bs.DataSource is DataView dataView)
          {
            data = dataView.Table;
          }
          else
          {
            Program.Log($"TrySaveSingleCellChange_MainGrid: Unsupported BindingSource.DataSource type: {bs.DataSource?.GetType().Name ?? "null"}");
            return;
          }

          if (data != null)
          {
            var row = metricsGrid.Rows[rowIndex];
            if (row?.DataBoundItem is DataRowView drv)
            {
              var dataRow = drv.Row;
              var column = metricsGrid.Columns[columnIndex];
              var columnName = column.DataPropertyName ?? column.Name;

              Program.Log($"TrySaveSingleCellChange_MainGrid: columnName={columnName}, currentSelectedTable={currentSelectedTable}");

              // Skip LinkID/ID key columns and virtual/action columns
              if (string.Equals(columnName, "LinkID", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(columnName, "ID", StringComparison.OrdinalIgnoreCase) ||
                  (virtualColumnNames != null && virtualColumnNames.Contains(columnName)))
              {
                Program.Log($"TrySaveSingleCellChange_MainGrid: Skipping column {columnName} (LinkID/ID or virtual column)");
                return;
              }

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

              Program.Log($"TrySaveSingleCellChange_MainGrid: newValue={newValue}, orig={orig}, linkKey={linkKey}");

              if (!string.IsNullOrEmpty(linkKey) && !string.IsNullOrEmpty(currentSelectedTable))
              {
                if (!object.Equals(newValue, orig))
                {
                  // Value changed from original - add to pending changes
                  Program.Log($"TrySaveSingleCellChange_MainGrid: Adding change to InMemoryEditStore: table={currentSelectedTable}, linkKey={linkKey}, column={columnName}, value={newValue}");
                  Program.Edits.UpsertOverride(currentSelectedTable, linkKey, columnName, newValue);
                }
                else
                {
                  // Value matches original - remove from pending changes if it exists
                  Program.Log($"TrySaveSingleCellChange_MainGrid: Removing change from InMemoryEditStore: table={currentSelectedTable}, linkKey={linkKey}, column={columnName}");
                  Program.Edits.RemoveOverride(currentSelectedTable, linkKey, columnName);
                }
              }
              else
              {
                Program.Log($"TrySaveSingleCellChange_MainGrid: Skipping save - linkKey={linkKey}, currentSelectedTable={currentSelectedTable}");
              }

              // Mark row clean only after the edit is finalized
              if (isEndEdit)
              {
                try { dataRow.AcceptChanges(); } catch { }
              }
            }
            else
            {
              Program.Log($"TrySaveSingleCellChange_MainGrid: No DataRowView found for row {rowIndex}");
            }
          }
          else
          {
            Program.Log($"TrySaveSingleCellChange_MainGrid: No DataTable found in BindingSource");
          }
        }
        else
        {
          Program.Log($"TrySaveSingleCellChange_MainGrid: No BindingSource found");
        }
      }
      catch (Exception ex)
      {
        Program.Log("TrySaveSingleCellChange_MainGrid error", ex);
      }
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

    // Exclude row styling removed; rely on filter results only
    private void ApplyDisabledRowStyling() { }

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
        var exportData = new ToolStripMenuItem("Export Data to CSV");
        exportData.Click += (s, e) => ExportData(rowIndex);
        export.DropDownItems.Add(exportData);
        export.DropDownItems.Add(exportJpeg);
        export.DropDownItems.Add(exportWmf);
        // Add sequence menu item for Parts and Subassemblies tables
        ToolStripMenuItem sequenceMenuItem = null;
        if (!string.IsNullOrEmpty(currentSelectedTable) &&
            string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase))
        {
          sequenceMenuItem = new ToolStripMenuItem("Sequence Selected Rows...");
          sequenceMenuItem.Click += (s, e) => SequenceSelectedRows();
        }

        // include/exclude functionality removed; rely on filters
        var refreshVirtual = new ToolStripMenuItem("Refresh Virtual Columns");
        refreshVirtual.Click += (s, e) => RefreshVirtualColumns();

        // Add edit mode toggle
        var editModeToggle = new ToolStripMenuItem(isEditModeMainGrid ? "Disable Edit Mode" : "Enable Edit Mode");
        editModeToggle.Click += (s, e) => {
          isEditModeMainGrid = !isEditModeMainGrid;
          ApplyMainGridEditState();
          Program.Log($"MainForm: Edit mode toggled to {isEditModeMainGrid} via context menu");
        };

        menu.Items.Add(export);
        if (sequenceMenuItem != null)
        {
          menu.Items.Add(new ToolStripSeparator());
          menu.Items.Add(sequenceMenuItem);
        }
        // include/exclude items removed
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(editModeToggle);
        menu.Items.Add(refreshVirtual);
        menu.Show(metricsGrid, clientLocation);
      }
      catch { }
    }

    // Sequence selected rows with custom sequence ID
    private void SequenceSelectedRows()
    {
      try
      {
        Program.Log("SequenceSelectedRows: enter");

        // Check if we have selected rows
        var selectedRows = GetSelectedRowIndices();
        if (selectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more rows to sequence.", "Sequence Error",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        Program.Log($"SequenceSelectedRows: {selectedRows.Count} rows selected");

        // Show dialog to get sequence ID
        using (var dialog = new SequenceDialog("Sequence Selected Rows",
          $"Enter sequence ID for {selectedRows.Count} selected row(s):"))
        {
          if (dialog.ShowDialog(this) == DialogResult.OK)
          {
            string sequenceId = dialog.SequenceId;
            if (!string.IsNullOrWhiteSpace(sequenceId))
            {
              Program.Log($"SequenceSelectedRows: Using sequence ID '{sequenceId}' for {selectedRows.Count} rows");

              // Perform sequencing with custom ID
              PerformAutoSequence(useAutomaticFiltering: false, selectedRowIndices: selectedRows, customSequenceId: sequenceId);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error in SequenceSelectedRows", ex);
        MessageBox.Show("Error during sequence operation: " + ex.Message, "Sequence Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Get currently selected row indices
    private List<int> GetSelectedRowIndices()
    {
      var selectedIndices = new List<int>();

      try
      {
        if (metricsGrid == null) return selectedIndices;

        // Check if we have selected rows
        var selectedRows = metricsGrid.SelectedRows;
        Program.Log($"GetSelectedRowIndices: metricsGrid.SelectedRows.Count = {selectedRows.Count}");

        foreach (DataGridViewRow row in selectedRows)
        {
          if (row.Index >= 0)
          {
            selectedIndices.Add(row.Index);
            Program.Log($"GetSelectedRowIndices: Added row index {row.Index}");
          }
        }

        // If no rows are selected but we have a current cell, use that row
        if (selectedIndices.Count == 0 && metricsGrid.CurrentCell != null)
        {
          int currentRowIndex = metricsGrid.CurrentCell.RowIndex;
          if (currentRowIndex >= 0)
          {
            selectedIndices.Add(currentRowIndex);
            Program.Log($"GetSelectedRowIndices: No selected rows, using current cell row index {currentRowIndex}");
          }
        }

        Program.Log($"GetSelectedRowIndices: Final count = {selectedIndices.Count} selected rows");
      }
      catch (Exception ex)
      {
        Program.Log("Error getting selected row indices", ex);
      }

      return selectedIndices;
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

    // Fronts filter functionality for Parts table
    private bool isFrontsFilterActive = false;
    private string originalPartsData = null;
    private bool isSubassemblyFilterActive = false;
    private string originalSubassemblyData = null;

    private void btnFilterFronts_Click(object sender, EventArgs e) {
      Program.Log("btnFilterFronts_Click: enter");
      try
      {
        // Only enable when Parts table is selected
        Program.Log($"btnFilterFronts_Click: currentSelectedTable='{currentSelectedTable ?? "<null>"}'");
        if (string.IsNullOrEmpty(currentSelectedTable) || !string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase))
        {
          Program.Log($"btnFilterFronts_Click: returning early - table not Parts or null/empty");
          return;
        }

        Program.Log($"btnFilterFronts_Click: isFrontsFilterActive={isFrontsFilterActive}");
        if (isFrontsFilterActive)
        {
          // Reset filter - restore original data
          Program.Log($"btnFilterFronts_Click: originalPartsData is {(originalPartsData != null ? "not null" : "null")}");
          Program.Log($"btnFilterFronts_Click: metricsGrid?.DataSource is {(metricsGrid?.DataSource != null ? "not null" : "null")}");
          if (originalPartsData != null && metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataView dataView)
          {
            Program.Log("Fronts filter: Resetting to show all parts");

            // Restore original filter
            dataView.RowFilter = originalPartsData;
            isFrontsFilterActive = false;
            originalPartsData = null;
            btnFilterFronts.Text = "Show Fronts";
            btnFilterFronts.BackColor = System.Drawing.SystemColors.ButtonHighlight; // Set background to default
          }
        }
        else
        {
          // Apply filter - show only front parts
          Program.Log($"btnFilterFronts_Click: metricsGrid?.DataSource is {(metricsGrid?.DataSource != null ? "not null" : "null")}");
          if (metricsGrid?.DataSource != null)
          {
            Program.Log($"btnFilterFronts_Click: metricsGrid.DataSource type = {metricsGrid.DataSource.GetType().Name}");
            if (metricsGrid.DataSource is BindingSource bindingSource)
            {
              Program.Log($"btnFilterFronts_Click: bindingSource.DataSource type = {bindingSource.DataSource?.GetType().Name ?? "null"}");
            }
          }
          if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataView dataView)
          {
            Program.Log($"btnFilterFronts_Click: DataView has {dataView?.Count ?? 0} rows");
            // Validate data before filtering
            if (dataView == null || dataView.Count == 0)
            {
              Program.Log("Fronts filter: No data to filter");
              return;
            }

            Program.Log("Fronts filter: Filtering to show only front parts");

            // Store original filter if not already stored
            if (originalPartsData == null)
            {
              originalPartsData = dataView.RowFilter; // Store the original filter string
            }

            // Get configurable front filter keywords from settings
            var cfg = UserConfig.LoadOrDefault();
            var frontKeywords = cfg.FrontFilterKeywords ?? new List<string> { "Front", "Drawer Front", "RPE" };

            // Validate required Name column exists
            if (!dataView.Table.Columns.Contains("Name"))
            {
              Program.Log("Fronts filter: Required column 'Name' not found in data");
              MessageBox.Show("Required column 'Name' not found in data. Cannot apply filter.", "Filter Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
              return;
            }

            // Build filter expression for front parts
            var filterParts = new List<string>();
            foreach (var keyword in frontKeywords)
            {
              // Escape single quotes in keyword and create LIKE condition
              var escapedKeyword = keyword.Replace("'", "''");
              filterParts.Add($"Name LIKE '%{escapedKeyword}%'");
            }
            var frontFilter = string.Join(" OR ", filterParts);

            // Apply the filter
            dataView.RowFilter = frontFilter;

            ApplyDisabledRowStyling();

            isFrontsFilterActive = true;
            btnFilterFronts.Text = "Show All";
            btnFilterFronts.BackColor = System.Drawing.Color.LightGreen;

            Program.Log($"Fronts filter: Applied filter '{frontFilter}' - showing {dataView.Count} parts");
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

    private void btnFilterSubassemblies_Click(object sender, EventArgs e) {
      Program.Log("btnFilterSubassemblies_Click: enter");
      try
      {
        // Only enable when Subassemblies table is selected
        Program.Log($"btnFilterSubassemblies_Click: currentSelectedTable='{currentSelectedTable ?? "<null>"}'");
        if (string.IsNullOrEmpty(currentSelectedTable) || !string.Equals(currentSelectedTable, "Subassemblies", StringComparison.OrdinalIgnoreCase))
        {
          Program.Log($"btnFilterSubassemblies_Click: returning early - table not Subassemblies or null/empty");
          return;
        }

        Program.Log($"btnFilterSubassemblies_Click: isSubassemblyFilterActive={isSubassemblyFilterActive}");
        if (isSubassemblyFilterActive)
        {
          // Reset filter - restore original data
          Program.Log($"btnFilterSubassemblies_Click: originalSubassemblyData is {(originalSubassemblyData != null ? "not null" : "null")}");
          Program.Log($"btnFilterSubassemblies_Click: metricsGrid?.DataSource is {(metricsGrid?.DataSource != null ? "not null" : "null")}");
          if (originalSubassemblyData != null && metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataView dataView)
          {
            Program.Log("Subassembly filter: Resetting to show all subassemblies");

            // Restore original filter
            dataView.RowFilter = originalSubassemblyData;
            isSubassemblyFilterActive = false;
            originalSubassemblyData = null;
            btnFilterSubassemblies.Text = "Show Fronts";
            btnFilterSubassemblies.BackColor = System.Drawing.SystemColors.ButtonHighlight; // Set background to default
          }
        }
        else
        {
          // Apply filter - show only front subassemblies
          Program.Log($"btnFilterSubassemblies_Click: metricsGrid?.DataSource is {(metricsGrid?.DataSource != null ? "not null" : "null")}");
          if (metricsGrid?.DataSource != null)
          {
            Program.Log($"btnFilterSubassemblies_Click: metricsGrid.DataSource type = {metricsGrid.DataSource.GetType().Name}");
            if (metricsGrid.DataSource is BindingSource bindingSource)
            {
              Program.Log($"btnFilterSubassemblies_Click: bindingSource.DataSource type = {bindingSource.DataSource?.GetType().Name ?? "null"}");
            }
          }
          if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataView dataView)
          {
            Program.Log($"btnFilterSubassemblies_Click: DataView has {dataView?.Count ?? 0} rows");
            // Validate data before filtering
            if (dataView == null || dataView.Count == 0)
            {
              Program.Log("Subassembly filter: No data to filter");
              return;
            }

            Program.Log("Subassembly filter: Filtering to show only front subassemblies");

            // Store original filter if not already stored
            if (originalSubassemblyData == null)
            {
              originalSubassemblyData = dataView.RowFilter; // Store the original filter string
            }

            // Get configurable subassembly filter keywords from settings
            var cfg = UserConfig.LoadOrDefault();
            var subassemblyKeywords = cfg.SubassemblyFilterKeywords ?? new List<string> { "Front", "Drawer Front", "RPE" };

            // Validate required Name column exists
            if (!dataView.Table.Columns.Contains("Name"))
            {
              Program.Log("Subassembly filter: Required column 'Name' not found in data");
              MessageBox.Show("Required column 'Name' not found in data. Cannot apply filter.", "Filter Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
              return;
            }

            // Build filter expression for front subassemblies
            var filterParts = new List<string>();
            foreach (var keyword in subassemblyKeywords)
            {
              // Escape single quotes in keyword and create LIKE condition
              var escapedKeyword = keyword.Replace("'", "''");
              filterParts.Add($"Name LIKE '%{escapedKeyword}%'");
            }
            var subassemblyFilter = string.Join(" OR ", filterParts);

            // Apply the filter
            dataView.RowFilter = subassemblyFilter;

            ApplyDisabledRowStyling();

            isSubassemblyFilterActive = true;
            btnFilterSubassemblies.Text = "Show All";
            btnFilterSubassemblies.BackColor = System.Drawing.Color.LightGreen;

            Program.Log($"Subassembly filter: Applied filter '{subassemblyFilter}' - showing {dataView.Count} subassemblies");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error applying subassembly filter", ex);
        MessageBox.Show("Error applying subassembly filter: " + ex.Message, "Filter Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void btnAutoSequence_Click(object sender, EventArgs e)
    {
      Program.Log("btnAutoSequence_Click: enter");
      try
      {
        // Only enable when Parts or Subassemblies table is selected
        Program.Log($"btnAutoSequence_Click: currentSelectedTable='{currentSelectedTable ?? "<null>"}'");
        if (string.IsNullOrEmpty(currentSelectedTable) ||
            (!string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(currentSelectedTable, "Subassemblies", StringComparison.OrdinalIgnoreCase)))
        {
          Program.Log($"btnAutoSequence_Click: returning early - table not Parts/Subassemblies or null/empty");
          return;
        }

        // Use automatic filtering for the button click
        PerformAutoSequence(useAutomaticFiltering: true, selectedRowIndices: null, customSequenceId: null);
      }
      catch (Exception ex)
      {
        Program.Log("Error in btnAutoSequence_Click", ex);
        MessageBox.Show("Error during auto sequence: " + ex.Message, "Auto Sequence Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void btnSawQueue_Click(object sender, EventArgs e)
    {
      Program.Log("btnSawQueue_Click: enter");
      try
      {
        // Open the Saw Queue dialog to manage saw cutting patterns
        Program.Log("btnSawQueue_Click: Opening SawQueueDialog");
        using (var dialog = new SawQueueDialog())
        {
          dialog.ShowDialog(this);
        }

        Program.Log("btnSawQueue_Click: completed");
      }
      catch (Exception ex)
      {
        Program.Log("Error in btnSawQueue_Click", ex);
        MessageBox.Show("Error opening saw queue dialog: " + ex.Message, "Saw Queue Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void PerformAutoSequence(bool useAutomaticFiltering, List<int> selectedRowIndices, string customSequenceId)
    {
      try
      {
        Program.Log($"PerformAutoSequence: useAutomaticFiltering={useAutomaticFiltering}, selectedRows={selectedRowIndices?.Count ?? 0}, customSequenceId='{customSequenceId}'");

        // Check if we have data to work with
        if (metricsGrid?.DataSource is BindingSource bs && bs.DataSource is DataView dataView)
        {
          Program.Log($"PerformAutoSequence: DataView has {dataView?.Count ?? 0} rows");
          if (dataView == null || dataView.Count == 0)
          {
            Program.Log("Auto Sequence: No data to process");
            MessageBox.Show("No data available to process.", "Auto Sequence", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
          }

          // Check if required columns exist
          if (!dataView.Table.Columns.Contains("_SourceFile"))
          {
            Program.Log("Auto Sequence: Required column '_SourceFile' not found");
            MessageBox.Show("Required column '_SourceFile' not found in data.", "Auto Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
          }

          if (!dataView.Table.Columns.Contains("#"))
          {
            Program.Log("Auto Sequence: Required column '#' not found");
            MessageBox.Show("Required column '#' not found in data.", "Auto Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
          }

          if (!dataView.Table.Columns.Contains("PerfectGrainCaption"))
          {
            Program.Log("Auto Sequence: Required column 'PerfectGrainCaption' not found");
            MessageBox.Show("Required column 'PerfectGrainCaption' not found in data.", "Auto Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
          }

          // Get appropriate filter keywords based on table type (for both automatic and manual filtering)
          List<string> filterKeywords = null;
          var cfg = UserConfig.LoadOrDefault();
          bool isPartsTable = string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase);

          if (isPartsTable)
          {
            // Use Parts front filter keywords
            filterKeywords = cfg.FrontFilterKeywords ?? new List<string> { "Slab", "Drawer Front" };
          }
          else if (string.Equals(currentSelectedTable, "Subassemblies", StringComparison.OrdinalIgnoreCase))
          {
            // Use Subassembly filter keywords
            filterKeywords = cfg.SubassemblyFilterKeywords ?? new List<string> { "Front", "Drawer Front", "RPE" };
          }

          int processedCount = 0;
          int skippedCount = 0;

          // Determine which rows to process
          IEnumerable<DataRowView> rowsToProcess;
          if (useAutomaticFiltering)
          {
            // Process all rows in current view with automatic filtering
            rowsToProcess = dataView.Cast<DataRowView>();
          }
          else
          {
            // Process only selected rows
            if (selectedRowIndices == null || selectedRowIndices.Count == 0)
            {
              MessageBox.Show("No rows selected for sequencing.", "Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
              return;
            }
            rowsToProcess = selectedRowIndices
              .Where(index => index >= 0 && index < dataView.Count)
              .Select(index => dataView[index]);
          }

          // Process each row
          Program.Log($"PerformAutoSequence: About to process {rowsToProcess.Count()} rows");
          foreach (DataRowView rowView in rowsToProcess)
          {
            var row = rowView.Row;
            string name = row["Name"]?.ToString() ?? "";

            Program.Log($"PerformAutoSequence: Processing row '{name}' (useAutomaticFiltering={useAutomaticFiltering})");

            // Apply front filtering for both automatic and manual modes
            // For manual mode (context menu), we still need to check if rows meet front filter requirements
            if (filterKeywords != null)
            {
              bool matchesFilter = filterKeywords.Any(keyword =>
                name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

              Program.Log($"PerformAutoSequence: Row '{name}' filter check - matchesFilter={matchesFilter}, keywords=[{string.Join(", ", filterKeywords)}]");

              if (!matchesFilter)
              {
                skippedCount++;
                if (!useAutomaticFiltering)
                {
                  // For manual mode, log why the row was skipped
                  Program.Log($"Manual Sequence: Skipping row '{name}' - does not match front filter keywords");
                }
                continue;
              }
            }

            // Process the PerfectGrainCaption
            var result = ProcessPerfectGrainCaption(row, customSequenceId);
            if (result.Processed)
            {
              processedCount++;

              // Register the change in InMemoryEditStore so it gets included in consolidation
              string linkId = row["LinkID"]?.ToString() ?? "";
              if (!string.IsNullOrWhiteSpace(linkId))
              {
                Program.Edits.UpsertOverride(currentSelectedTable, linkId, "PerfectGrainCaption", result.NewValue);
                Program.Log($"Auto Sequence: Registered PerfectGrainCaption change in InMemoryEditStore for LinkID '{linkId}'");
              }
            }
            else
            {
              skippedCount++;
              Program.Log($"PerformAutoSequence: Row '{name}' was not processed");
            }
          }

          // Refresh the grid to show changes
          metricsGrid.Refresh();

          string message = $"Auto Sequence completed.\nProcessed: {processedCount} rows\nSkipped: {skippedCount} rows";
          Program.Log($"Auto Sequence: {message}");
          MessageBox.Show(message, "Auto Sequence Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
          Program.Log("Auto Sequence: No data source available");
          MessageBox.Show("No data source available.", "Auto Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error in PerformAutoSequence", ex);
        MessageBox.Show("Error during auto sequence: " + ex.Message, "Auto Sequence Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private (bool Processed, string NewValue) ProcessPerfectGrainCaption(DataRow row, string customSequenceId)
    {
      try
      {
        string currentGrainCaption = row["PerfectGrainCaption"]?.ToString() ?? "";
        string perfectGrainCaption = "";
        string name = row["Name"]?.ToString() ?? "";

        Program.Log($"ProcessPerfectGrainCaption: Processing row '{name}', currentGrainCaption='{currentGrainCaption}', customSequenceId='{customSequenceId}'");

        if (!string.IsNullOrWhiteSpace(currentGrainCaption))
        {
          // Existing value - check if it needs '#' prefix
          if (!currentGrainCaption.StartsWith("#"))
          {
            perfectGrainCaption = "#" + currentGrainCaption;
            Program.Log($"Auto Sequence: Adding '#' prefix to existing PerfectGrainCaption for '{name}': '{currentGrainCaption}' -> '{perfectGrainCaption}'");
          }
          else
          {
            Program.Log($"Auto Sequence: Skipping row '{name}' - already has '#' prefix");
            return (false, currentGrainCaption); // Already has '#' prefix, skip
          }
        }
        else
        {
          // No existing value - generate new one
          if (!string.IsNullOrWhiteSpace(customSequenceId))
          {
            // Use custom sequence ID
            perfectGrainCaption = "#" + customSequenceId;
            Program.Log($"Auto Sequence: Using custom sequence ID for '{name}': '{perfectGrainCaption}'");
          }
          else
          {
            // Generate based on work order name and # column
            string workOrderName = row["_SourceFile"]?.ToString() ?? "";
            string numberValue = row["#"]?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(workOrderName) || string.IsNullOrWhiteSpace(numberValue))
            {
              Program.Log($"Auto Sequence: Skipping row '{name}' - missing workOrderName or numberValue");
              return (false, "");
            }

            perfectGrainCaption = GeneratePerfectGrainCaption(workOrderName, numberValue);
            if (!string.IsNullOrWhiteSpace(perfectGrainCaption))
            {
              Program.Log($"Auto Sequence: Generated new PerfectGrainCaption for '{name}': '{perfectGrainCaption}'");
            }
            else
            {
              Program.Log($"Auto Sequence: Failed to generate PerfectGrainCaption for '{name}'");
              return (false, "");
            }
          }
        }

        // Update the row if we have a new value
        if (!string.IsNullOrWhiteSpace(perfectGrainCaption))
        {
          row["PerfectGrainCaption"] = perfectGrainCaption;
          Program.Log($"ProcessPerfectGrainCaption: Successfully updated row '{name}' with '{perfectGrainCaption}'");
          return (true, perfectGrainCaption);
        }

        Program.Log($"ProcessPerfectGrainCaption: No update needed for row '{name}'");
        return (false, "");
      }
      catch (Exception ex)
      {
        Program.Log($"Error processing PerfectGrainCaption for row: {ex.Message}", ex);
        return (false, "");
      }
    }

    private string GeneratePerfectGrainCaption(string workOrderName, string numberValue)
    {
      try
      {
        // Example: (03-03) 23079-Reese B1_Primary Tall -> #RBPT4
        // Extract first non-numeric character from each section

        // Remove parentheses and split by common delimiters
        string cleanName = workOrderName.Trim();
        if (cleanName.StartsWith("(") && cleanName.Contains(")"))
        {
          int endParen = cleanName.IndexOf(")");
          if (endParen > 0)
          {
            cleanName = cleanName.Substring(endParen + 1).Trim();
          }
        }

        // Split by common delimiters: space, dash, underscore
        string[] sections = cleanName.Split(new char[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        StringBuilder result = new StringBuilder("#");

        foreach (string section in sections)
        {
          if (string.IsNullOrWhiteSpace(section)) continue;

          // Find first non-numeric character in this section
          for (int i = 0; i < section.Length; i++)
          {
            char c = section[i];
            if (!char.IsDigit(c))
            {
              result.Append(char.ToUpper(c));
              break; // Only take the first non-numeric character
            }
          }
        }

        // If the last part of _SourceFile is numeric, append it to the results
        string lastNumericPart = ExtractLastNumericPart(workOrderName);
        if (!string.IsNullOrEmpty(lastNumericPart))
        {
          result.Append(lastNumericPart);
        }

        result.Append("_");
        // Append the number value
        result.Append(numberValue);

        return result.ToString();
      }
      catch (Exception ex)
      {
        Program.Log($"Error generating PerfectGrainCaption for '{workOrderName}', '{numberValue}': {ex.Message}");
        return "";
      }
    }

    private string ExtractLastNumericPart(string workOrderName)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(workOrderName))
          return "";

        // Remove parentheses and split by common delimiters
        string cleanName = workOrderName.Trim();
        if (cleanName.StartsWith("(") && cleanName.Contains(")"))
        {
          int endParen = cleanName.IndexOf(")");
          if (endParen > 0)
          {
            cleanName = cleanName.Substring(endParen + 1).Trim();
          }
        }

        // Check if the string ends with numeric characters
        // Find the start of the trailing numeric sequence
        int numericStartIndex = -1;
        for (int i = cleanName.Length - 1; i >= 0; i--)
        {
          if (char.IsDigit(cleanName[i]))
          {
            numericStartIndex = i;
          }
          else
          {
            break; // Stop when we hit a non-numeric character
          }
        }

        // If we found numeric characters at the end, extract them
        if (numericStartIndex >= 0)
        {
          return cleanName.Substring(numericStartIndex);
        }

        return "";
      }
      catch (Exception ex)
      {
        Program.Log($"Error extracting last numeric part from '{workOrderName}': {ex.Message}");
        return "";
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

      // Track consolidation results for each source
      var successfulSources = new List<string>();
      var failedSources = new List<(string SourceName, string ErrorMessage)>();

      Program.Log($"Starting consolidation of {sources.Count} source(s) to: {destinationPath}");

      if (File.Exists(destinationPath))
      {
        Program.Log($"Deleting existing destination file: {destinationPath}");
        File.Delete(destinationPath);
      }

      CreateEmptyDatabase(destinationPath);
      Program.Log($"Created empty destination database: {destinationPath}");

      using (var dest = new SqlCeConnection($"Data Source={destinationPath};"))
      {
        dest.Open();

        foreach (var src in sources)
        {
          // Combined mode with source metadata columns
          string sourceTag = new DirectoryInfo(src.WorkOrderDir).Name;
          Program.Log($"Processing source: {sourceTag} ({src.SdfPath})");

          try
          {
            CopyDatabaseCombined(src.SdfPath, dest, sourceTag, src.WorkOrderDir);
            successfulSources.Add(sourceTag);
            Program.Log($"Successfully processed source: {sourceTag}");
          }
          catch (Exception ex)
          {
            // Log the error but continue with other sources
            string errorDetail = GetDetailedErrorMessage(ex);
            Program.Log($"Failed to process source {sourceTag}: {errorDetail}", ex);
            failedSources.Add((sourceTag, errorDetail));
          }

          progress.Value += 1;
        }
      }

      // Log consolidation summary
      Program.Log($"Consolidation summary: {successfulSources.Count} succeeded, {failedSources.Count} failed");

      // If all sources failed, throw an exception
      if (successfulSources.Count == 0 && failedSources.Count > 0)
      {
        var errorSummary = string.Join("\n", failedSources.Select(f => $"  - {f.SourceName}: {f.ErrorMessage}"));
        throw new ConsolidationException(
          $"Consolidation failed. All {failedSources.Count} source(s) encountered errors:\n{errorSummary}",
          failedSources);
      }

      // Compact and verify the database after consolidation to ensure file integrity
      CompactAndVerifyDatabase(destinationPath);

      // If some sources failed, throw an exception with partial success info
      if (failedSources.Count > 0)
      {
        var errorSummary = string.Join("\n", failedSources.Select(f => $"  - {f.SourceName}: {f.ErrorMessage}"));
        throw new ConsolidationException(
          $"Consolidation completed with errors.\n\n" +
          $"Successful: {successfulSources.Count} source(s)\n" +
          $"Failed: {failedSources.Count} source(s):\n{errorSummary}",
          failedSources,
          isPartialSuccess: true);
      }
    }

    // Custom exception to carry consolidation error details
    private class ConsolidationException : Exception
    {
      public List<(string SourceName, string ErrorMessage)> FailedSources { get; }
      public bool IsPartialSuccess { get; }

      public ConsolidationException(string message, List<(string SourceName, string ErrorMessage)> failedSources, bool isPartialSuccess = false)
        : base(message)
      {
        FailedSources = failedSources;
        IsPartialSuccess = isPartialSuccess;
      }
    }

    // Extract detailed error message including inner exceptions
    private static string GetDetailedErrorMessage(Exception ex)
    {
      var messages = new List<string>();
      var current = ex;
      while (current != null)
      {
        messages.Add(current.Message);
        current = current.InnerException;
      }
      return string.Join(" -> ", messages);
    }

    private static void CompactAndVerifyDatabase(string databasePath)
    {
      try
      {
        Program.Log($"Compacting and verifying database: {databasePath}");
        string connectionString = $"Data Source={databasePath};";

        // Compact the database to optimize and ensure data is flushed to disk
        using (var engine = new SqlCeEngine(connectionString))
        {
          engine.Compact(null);
        }

        // Verify the database integrity
        using (var engine = new SqlCeEngine(connectionString))
        {
          if (!engine.Verify())
          {
            Program.Log($"WARNING: Database verification failed for {databasePath}");
            throw new InvalidOperationException("Database verification failed after consolidation. The database may be corrupted.");
          }
        }

        Program.Log($"Database compaction and verification successful: {databasePath}");
      }
      catch (Exception ex)
      {
        Program.Log($"Error during database compaction/verification: {ex.Message}", ex);
        throw; // Re-throw to notify caller of the issue
      }
    }

    // removed unused GeneratePrefixFromDirectory

    private static void CreateEmptyDatabase(string path)
    {
      // Dispose engine properly to ensure database file is fully written
      using (var engine = new SqlCeEngine($"Data Source={path};"))
      {
        engine.CreateDatabase();
      }
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
      string currentTable = null; // Track current table for error reporting
      int tablesProcessed = 0;

      using (var srcConn = SqlCeUtils.OpenWithFallback(sourcePath, out tempCopyPath))
      {
        // Use transaction to ensure atomicity for each source file
        SqlCeTransaction transaction = null;
        try
        {
          transaction = destConn.BeginTransaction();

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

          Program.Log($"Source {sourceTag}: Found {tables.Count} tables to process");

          foreach (string table in tables)
          {
            if (table.StartsWith("__")) continue;

            // Only process tables present in the schema XML
            if (!SchemaTables.Contains(table)) continue;

            currentTable = table; // Track for error reporting

            // Always ensure destination table exists (schema conformance), even if we skip data
            EnsureDestinationTableCombined(destConn, srcConn, table, transaction);

            // Skip copying data for excluded tables
            if (ExcludedDataTables.Contains(table)) continue;

            CopyRowsCombined(srcConn, destConn, table, sourceTag, sourcePathDir, transaction);
            tablesProcessed++;
          }

          // Commit transaction after all tables from this source are copied successfully
          transaction.Commit();
          Program.Log($"Successfully committed data from source: {sourceTag} ({tablesProcessed} tables processed)");
        }
        catch (Exception ex)
        {
          // Build detailed error message with context
          string errorContext = currentTable != null
            ? $"Error copying table '{currentTable}' from source '{sourceTag}'"
            : $"Error copying from source '{sourceTag}'";

          Program.Log($"{errorContext}, rolling back ({tablesProcessed} tables were processed before error): {ex.Message}", ex);

          try
          {
            transaction?.Rollback();
            Program.Log($"Successfully rolled back transaction for source: {sourceTag}");
          }
          catch (Exception rollbackEx)
          {
            Program.Log($"Rollback failed for source {sourceTag}: {rollbackEx.Message}", rollbackEx);
          }

          // Wrap the exception with context about which table failed
          throw new InvalidOperationException(
            $"{errorContext}: {ex.Message}",
            ex);
        }
        finally
        {
          transaction?.Dispose();
          if (!string.IsNullOrEmpty(tempCopyPath))
          {
            try { File.Delete(tempCopyPath); } catch { }
          }
        }
      }
    }

    // removed legacy prefixed consolidation path

    private static void EnsureDestinationTableCombined(SqlCeConnection dest, SqlCeConnection src, string tableName, SqlCeTransaction transaction = null)
    {
      using (var cmd = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", dest))
      {
        cmd.Transaction = transaction;
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
            create.Transaction = transaction;
            create.ExecuteNonQuery();
          }
        }
        else
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
            dc.Transaction = transaction;
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
            using (var alter = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [" + colName + "] " + typeSql + " NULL", dest))
            {
              alter.Transaction = transaction;
              alter.ExecuteNonQuery();
            }
          }
          // No metadata columns in consolidated database
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
    private static bool SheetExistsInDestination(SqlCeConnection dest, string name, object width, object length, object thickness, SqlCeTransaction transaction = null)
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
          cmd.Transaction = transaction;
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

    private static void CopyRowsCombined(SqlCeConnection src, SqlCeConnection dest, string tableName, string sourceTag, string sourcePathDir, SqlCeTransaction transaction = null)
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
          insert.Transaction = transaction; // Associate with transaction for atomicity
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
                  if (SheetExistsInDestination(dest, name, width, length, thickness, transaction))
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
              // Always log row-level insert failures with context
              Program.Log($"Insert failed for table '{tableName}' at row {rowIndex} from source '{sourceTag}': {ex.Message}", ex);

              // Log detailed debug info if enabled
              if (EnableCopyDebug)
              {
                LogInsertFailure(dest, tableName, tableName, colNames, insert, rowIndex, ex, sourceTag, sourcePathDir);
              }

              // Wrap exception with row context for better error reporting
              throw new InvalidOperationException(
                $"Failed to insert row {rowIndex} into table '{tableName}': {ex.Message}",
                ex);
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
      try
      {
        // Export all rows/columns for the currently displayed table (respect current filter/sort).
        if (metricsGrid == null || metricsGrid.DataSource == null)
        {
          MessageBox.Show("No data loaded. Please select a table first.", "No Data",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Build a list of visible columns in display order (lets users hide huge stream columns before exporting).
        var exportCols = metricsGrid.Columns
          .Cast<DataGridViewColumn>()
          .Where(c => c.Visible)
          .OrderBy(c => c.DisplayIndex)
          .ToList();

        if (exportCols.Count == 0)
        {
          MessageBox.Show("No visible columns to export.", "Export",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Count rows to export (skip the new row placeholder).
        int rowCount = metricsGrid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        if (rowCount == 0)
        {
          MessageBox.Show("No rows to export.", "Export",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        var safeTable = string.IsNullOrWhiteSpace(currentSelectedTable) ? "Data" : currentSelectedTable.Trim();
        var defaultName = MakeSafeFileName($"{safeTable}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        using (var sfd = new SaveFileDialog())
        {
          sfd.Title = "Export Data to CSV";
          sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
          sfd.FileName = defaultName;
          sfd.OverwritePrompt = true;
          sfd.AddExtension = true;
          sfd.DefaultExt = "csv";

          if (sfd.ShowDialog(this) != DialogResult.OK) return;

          var path = sfd.FileName;
          Program.Log($"ExportData: exporting table='{safeTable}', rows={rowCount}, cols={exportCols.Count}, path='{path}'");

          // Use UTF-8 BOM for Excel-friendly CSV.
          using (var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(true)))
          {
            // Header row.
            writer.WriteLine(string.Join(",", exportCols.Select(c => ToCsvField(c.HeaderText))));

            // Data rows (use DataBoundItem when available; fall back to cell values).
            foreach (DataGridViewRow gridRow in metricsGrid.Rows)
            {
              if (gridRow == null || gridRow.IsNewRow) continue;

              if (gridRow.DataBoundItem is DataRowView drv)
              {
                var row = drv.Row;
                var values = new List<string>(exportCols.Count);
                foreach (var col in exportCols)
                {
                  var key = !string.IsNullOrWhiteSpace(col.DataPropertyName) ? col.DataPropertyName : col.Name;
                  object val = null;
                  try
                  {
                    if (row.Table.Columns.Contains(key)) val = row[key];
                    else val = gridRow.Cells[col.Index]?.Value;
                  }
                  catch { val = null; }
                  values.Add(ToCsvField(val));
                }
                writer.WriteLine(string.Join(",", values));
              }
              else
              {
                var values = new List<string>(exportCols.Count);
                foreach (var col in exportCols)
                {
                  object val = null;
                  try { val = gridRow.Cells[col.Index]?.Value; } catch { val = null; }
                  values.Add(ToCsvField(val));
                }
                writer.WriteLine(string.Join(",", values));
              }
            }
          }

          MessageBox.Show($"Exported {rowCount} row(s) to:\n\n{path}", "Export Complete",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
      }
      catch (Exception ex)
      {
        Program.Log("ExportData failed", ex);
        MessageBox.Show("Export failed: " + ex.Message, "Export Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Convert a value into a CSV-safe field string.
    private static string ToCsvField(object value)
    {
      try
      {
        if (value == null || value == DBNull.Value) return "";
        var s = Convert.ToString(value);
        return EscapeCsv(s);
      }
      catch { return ""; }
    }

    // Escape CSV field using RFC4180-style quoting.
    private static string EscapeCsv(string s)
    {
      if (s == null) return "";
      bool mustQuote =
        s.Contains(",") ||
        s.Contains("\"") ||
        s.Contains("\r") ||
        s.Contains("\n") ||
        (s.Length > 0 && (s[0] == ' ' || s[s.Length - 1] == ' ')); // preserve leading/trailing spaces
      if (!mustQuote) return s;
      return "\"" + s.Replace("\"", "\"\"") + "\"";
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
            col.HeaderCell.Style.BackColor = System.Drawing.ColorTranslator.FromHtml("#0078d7");
          }
        }
        else
        {
          // Apply normal header styling or Ctrl state styling
          if (isCtrlPressed)
          {
            // Ctrl is pressed - highlight headers to indicate multi-sort mode
            col.HeaderCell.Style.BackColor = System.Drawing.Color.LightBlue;
            col.HeaderCell.Style.ForeColor = System.Drawing.Color.DarkBlue;
          }
          else
          {
            // Normal header styling
            col.HeaderCell.Style.BackColor = col.DefaultCellStyle.BackColor;
            col.HeaderCell.Style.ForeColor = col.DefaultCellStyle.ForeColor;
          }
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

        // Suspend layout to prevent multiple redraws
        metricsGrid.SuspendLayout();

        try
        {
          foreach (DataGridViewColumn col in metricsGrid.Columns)
          {
            ApplyHeaderStyles(col);
          }

          // Only invalidate the header area, not the entire grid
          metricsGrid.Invalidate(new System.Drawing.Rectangle(0, 0, metricsGrid.Width, metricsGrid.ColumnHeadersHeight));
        }
        finally
        {
          // Resume layout
          metricsGrid.ResumeLayout(false);
        }
      }
      catch { }
    }

    // Update filter button states based on selected table
    private void UpdateFilterButtonStates()
    {
      try
      {
        if (btnFilterFronts != null)
        {
          bool isPartsTable = !string.IsNullOrEmpty(currentSelectedTable) &&
                             string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase);

          // Show/hide button based on table selection - only visible for Parts table
          btnFilterFronts.Visible = isPartsTable;

          if (!isPartsTable)
          {
            // Reset button state when not on Parts table
            btnFilterFronts.Text = "Show Fronts";
            isFrontsFilterActive = false;
            originalPartsData = null;
          }
        }

        if (btnFilterSubassemblies != null)
        {
          bool isSubassembliesTable = !string.IsNullOrEmpty(currentSelectedTable) &&
                                     string.Equals(currentSelectedTable, "Subassemblies", StringComparison.OrdinalIgnoreCase);

          // Show/hide button based on table selection - only visible for Subassemblies table
          btnFilterSubassemblies.Visible = isSubassembliesTable;

          if (!isSubassembliesTable)
          {
            // Reset button state when not on Subassemblies table
            btnFilterSubassemblies.Text = "Show Fronts";
            isSubassemblyFilterActive = false;
            originalSubassemblyData = null;
          }
        }

        if (btnAutoSequence != null)
        {
          bool isPartsTable = !string.IsNullOrEmpty(currentSelectedTable) &&
                                           string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase);

          // Show/hide button based on table selection - visible for Parts and Subassemblies tables
          btnAutoSequence.Visible = isPartsTable;
        }

        if (btnSawQueue != null)
        {
          bool isPartsTable = !string.IsNullOrEmpty(currentSelectedTable) &&
                             string.Equals(currentSelectedTable, "Parts", StringComparison.OrdinalIgnoreCase);
          bool isSubassembliesTable = !string.IsNullOrEmpty(currentSelectedTable) &&
                                     string.Equals(currentSelectedTable, "Subassemblies", StringComparison.OrdinalIgnoreCase);
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error updating filter button states", ex);
      }
    }

    private void MetricsGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
      try
      {
      Program.Log("MetricsGrid_DataBindingComplete: fired");

        // Early return if nothing is checked - don't apply layout to empty/clearing grid
        // Note: We only use CHECKED items, not SELECTED (highlighted) items
        if (checkedDirs.Count == 0)
        {
          Program.Log("MetricsGrid_DataBindingComplete: no work orders checked, skipping layout");
          return;
        }

        // Early return if DataSource is null or empty
        if (metricsGrid?.DataSource == null)
        {
          Program.Log("MetricsGrid_DataBindingComplete: DataSource is null, skipping layout");
          return;
        }

      LogCurrentGridColumns("on-DataBindingComplete entry");
        if (!applyLayoutAfterBinding)
        {
          Program.Log("MetricsGrid_DataBindingComplete: skipping layout reapply (applyLayoutAfterBinding=false)");
          return;
        }
        if (string.IsNullOrWhiteSpace(currentSelectedTable))
        {
          Program.Log("MetricsGrid_DataBindingComplete: skipping layout reapply (no selected table)");
          return;
        }
        Program.Log($"MetricsGrid_DataBindingComplete: applying persisted layout for '{currentSelectedTable}' after bind");
        var prev = isApplyingLayout; isApplyingLayout = true;
        try
        {
          // Get the data from the DataSource to pass to ApplyUserConfigToMetricsGrid
          DataTable data = null;
          if (metricsGrid?.DataSource is BindingSource bindingSource && bindingSource.DataSource is DataView dataView)
          {
            data = dataView.Table;
          }
          ApplyUserConfigToMetricsGrid(currentSelectedTable, data);
        }
        finally
        {
          isApplyingLayout = prev;
          applyLayoutAfterBinding = false;
        }
      LogCurrentGridColumns("on-DataBindingComplete exit");
      }
      catch (Exception ex)
      {
        Program.Log("MetricsGrid_DataBindingComplete error", ex);
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
