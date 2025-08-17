
using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Drawing;

namespace WorkOrderBlender
{
  public sealed class MetricsDialog : Form
  {
    // Use shared utility for SQL CE connections
    // Removed legacy preferred column order; defaults now come from UserConfig.ColumnOrders

    private readonly string tableName;
    private readonly string databasePath;
    private readonly bool showFromConsolidated;
    private readonly List<string> sourceSdfPaths;

    private readonly SplitContainer splitContainer;
    private readonly TextBox txtSearch;
    private readonly Label lblStatus;
    private readonly DataGridView grid;
    private readonly Panel gridBorderPanel;
    private readonly ContextMenuStrip ctxMenu;
    private bool isApplyingLayout;
    private bool isEditMode = false; // Track edit mode state

    private SqlCeConnection connection;
    private SqlCeDataAdapter adapter;
    private DataTable dataTable;
    private bool canPersistEdits;
    private bool isInitializing;

    // Virtual (linked/display-only) columns support
    private List<UserConfig.VirtualColumnDef> virtualColumnDefs;
    private readonly Dictionary<string, Dictionary<string, object>> virtualLookupCacheByColumn = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> virtualColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Cache of schema columns for breakdown tables to support dialogs without live DB context
    private readonly Dictionary<string, List<string>> breakdownSchemaColumns = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    // Consolidated-mode constructor
    public MetricsDialog(string dialogLabel, string tableName, string consolidatedDatabasePath)
    {
      this.tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
      this.databasePath = consolidatedDatabasePath ?? string.Empty;
      this.showFromConsolidated = true;
      this.sourceSdfPaths = null;

      Text = string.IsNullOrWhiteSpace(dialogLabel) ? $"Data for {tableName}" : $"Data for {dialogLabel}";
      StartPosition = FormStartPosition.CenterScreen;
      Width = 1400;
      Height = 700;

      splitContainer = new SplitContainer
      {
        Dock = DockStyle.Fill,
        Orientation = Orientation.Horizontal,
        FixedPanel = FixedPanel.Panel1,
        IsSplitterFixed = false,
        SplitterDistance = 48
      };
      Controls.Add(splitContainer);

      var panelTop = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
      splitContainer.Panel1.Controls.Add(panelTop);

      var layoutTop = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 3,
        RowCount = 1,
      };
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      panelTop.Controls.Add(layoutTop);

      var lblSearch = new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left };
      txtSearch = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 600 };
      lblStatus = new Label { Text = string.Empty, AutoSize = true, Anchor = AnchorStyles.Left };

      layoutTop.Controls.Add(lblSearch, 0, 0);
      layoutTop.Controls.Add(txtSearch, 1, 0);
      layoutTop.Controls.Add(lblStatus, 2, 0);
      lblStatus.MaximumSize = new Size(500, 0);

      grid = new DataGridView
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
        BorderStyle = BorderStyle.None,
      };
      grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
      grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
      grid.AllowUserToOrderColumns = true;
      gridBorderPanel = new Panel
      {
        Dock = DockStyle.Fill,
        Padding = new Padding(4),
        BackColor = SystemColors.Control
      };
      gridBorderPanel.Controls.Add(grid);
      splitContainer.Panel2.Controls.Add(gridBorderPanel);

      // Context menu
      ctxMenu = new ContextMenuStrip();
      var miExportJpeg = new ToolStripMenuItem("Export JPEGStream...");
      var miExportWmf = new ToolStripMenuItem("Export WMFStream...");
      var miDeleteRow = new ToolStripMenuItem("Delete Row(s)");
      miExportJpeg.Click += (s, e) => ExportStreamsForSelection("JPEGStream");
      miExportWmf.Click += (s, e) => ExportStreamsForSelection("WMFStream");
      miDeleteRow.Click += (s, e) => DeleteSelectedRows();
      ctxMenu.Items.AddRange(new ToolStripItem[] { miExportJpeg, miExportWmf, new ToolStripSeparator(), miDeleteRow });

      Load += MetricsDialog_Load;
      FormClosing += MetricsDialog_FormClosing;
      txtSearch.TextChanged += TxtSearch_TextChanged;
      grid.CellValueChanged += Grid_CellValueChanged;
      grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
      grid.CellEndEdit += Grid_CellEndEdit;
              // Removed Grid_DataBindingComplete event handler to prevent performance issues
        // Virtual columns are built during initialization, persisted settings applied during setup
      grid.SortCompare += Grid_SortCompare;
      grid.ColumnAdded += Grid_ColumnAdded;
      grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
      grid.MouseDown += Grid_MouseDown;
      grid.MouseUp += Grid_MouseUp;
      grid.MouseClick += Grid_MouseClick;
      grid.DoubleClick += Grid_DoubleClick; // Add double-click handler
      grid.CellFormatting += Grid_CellFormatting;
      grid.DataError += (s, e) => { e.ThrowException = false; };
      grid.Sorted += (s, e) => { ApplyPersistedColumnWidths(); ApplyPersistedColumnOrder(); };
      grid.SizeChanged += (s, e) => ApplyPersistedColumnWidths();
      grid.ColumnDisplayIndexChanged += Grid_ColumnDisplayIndexChanged;
      grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
      grid.CellClick += Grid_CellClick;
      grid.KeyDown += Grid_KeyDown;

      // Initialize grid state based on edit mode
      InitializeGridEditState();

      // Build schema cache for breakdown tables if possible
      try { BuildBreakdownSchemaColumnsCache(); } catch { }
    }

    // Pre-consolidation constructor: loads from multiple source SDFs into memory
    public MetricsDialog(string dialogLabel, string tableName, List<string> inputSdfPaths, bool allowEditingInMemory)
      : this(dialogLabel, tableName, consolidatedDatabasePath: string.Empty)
    {
      this.showFromConsolidated = false;
      this.sourceSdfPaths = inputSdfPaths ?? new List<string>();
      this.isEditMode = allowEditingInMemory; // Set initial edit mode state
      InitializeGridEditState();
    }

    private void InitializeGridEditState()
    {
      grid.ReadOnly = !isEditMode;
      if (isEditMode)
      {
        grid.EditMode = DataGridViewEditMode.EditOnKeystroke;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        gridBorderPanel.BackColor = Color.LimeGreen;
      }
      else
      {
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridBorderPanel.BackColor = SystemColors.Control;
      }
    }

    // Public method to initialize data without showing the form
    public void InitializeData()
    {
      MetricsDialog_Load(this, EventArgs.Empty);
    }

    private void MetricsDialog_Load(object sender, EventArgs e)
    {
      isInitializing = true;
      try
      {
        if (showFromConsolidated)
        {
          if (string.IsNullOrWhiteSpace(databasePath) || !System.IO.File.Exists(databasePath))
          {
            grid.DataSource = null;
            lblStatus.Text = "No consolidated database found. Run consolidation first.";
            // chkAllowEdit.Enabled = false; // Removed checkbox
            return;
          }

          // Try to open with read-write access first, fallback to read-only if needed
          try
          {
            connection = new SqlCeConnection($"Data Source={databasePath};");
            connection.Open();
          }
          catch (Exception ex)
          {
            // If failed, try read-only access
            Program.Log($"Failed to open consolidated database read-write, trying read-only: {ex.Message}");
            try
            {
              connection?.Close();
              connection?.Dispose();
              connection = SqlCeUtils.CreateReadOnlyConnection(databasePath);
              connection.Open();
              lblStatus.Text = "Database opened in read-only mode. Editing disabled.";
              canPersistEdits = false;
            }
            catch (Exception ex2)
            {
              Program.Log($"Failed to open consolidated database read-only: {ex2.Message}");
              throw new Exception($"Failed to open database: {ex.Message}. Read-only attempt also failed: {ex2.Message}", ex);
            }
          }

          // Add built-in source file tracking column for consolidated database
          adapter = new SqlCeDataAdapter($"SELECT *, '{GetWorkOrderName(databasePath)}' AS _SourceFile FROM [" + tableName + "]", connection);
          adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
          dataTable = new DataTable(tableName);
          adapter.FillSchema(dataTable, SchemaType.Source);

          // Log and fix any duplicate columns that might exist in the schema
          var columnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
          var duplicates = columnNames.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

          if (duplicates.Count > 0)
          {
            Program.Log($"Warning: Duplicate columns detected in schema for table {tableName}: {string.Join(", ", duplicates)}");

            // Remove duplicate columns, keeping only the first occurrence
            var columnsToRemove = new List<DataColumn>();
            var seenColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataColumn col in dataTable.Columns)
            {
              if (seenColumns.Contains(col.ColumnName))
              {
                columnsToRemove.Add(col);
              }
              else
              {
                seenColumns.Add(col.ColumnName);
              }
            }

            foreach (var col in columnsToRemove)
            {
              Program.Log($"Removing duplicate column: {col.ColumnName}");
              dataTable.Columns.Remove(col);
            }
          }
          // Ensure LinkID is treated as key when DB metadata doesn't mark PK
          if ((dataTable.PrimaryKey == null || dataTable.PrimaryKey.Length == 0))
          {
            var linkCol = FindLinkIdColumn(dataTable.Columns);
            if (linkCol != null)
            {
              dataTable.PrimaryKey = new[] { linkCol };
            }
          }
          adapter.Fill(dataTable);

          var binding = new BindingSource { DataSource = dataTable };

          // Ensure AutoGenerateColumns is enabled so new DataTable columns create grid columns
          grid.AutoGenerateColumns = true;
          grid.DataSource = binding;

          Program.Log($"Set DataSource for consolidated table '{tableName}' with {dataTable.Columns.Count} columns");
          LoadVirtualColumnDefinitions();
          BuildVirtualLookupCaches();
          RebuildVirtualColumns();
          ApplyPersistedColumnOrder();
          ApplyPersistedColumnWidths();
          ApplyPersistedColumnVisibilities();

          // Prepare manual commands for DB persistence using LinkID as PK (only if not already set to read-only)
          if (canPersistEdits != false) // Don't override if already set to false due to read-only
          {
            var keyCol = FindLinkIdColumn(dataTable.Columns)?.ColumnName;
            ConfigureManualAdapterCommands(connection, keyCol);
            canPersistEdits = adapter.UpdateCommand != null;
          }

          // Set status message if not already set by read-only fallback
          if (lblStatus.Text.IndexOf("read-only", StringComparison.OrdinalIgnoreCase) < 0)
          {
            lblStatus.Text = canPersistEdits ? "Editing updates the consolidated database. Double-click to enable." : "Editing disabled: no suitable key column found.";
          }
          // chkAllowEdit.Enabled = canPersistEdits; // Removed checkbox

          // Ensure grid state matches current edit mode
          InitializeGridEditState();
        }
        else
        {
          // Pre-consolidation: build DataTable by unioning rows from all sources, and applying in-memory edits
          dataTable = new DataTable(tableName);
          DataTable schema = null;
          var tempFiles = new List<string>();
          try
          {
            foreach (var path in sourceSdfPaths)
            {
              if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) continue;

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
              lblStatus.Text = "Table not found in selected sources.";
              // chkAllowEdit.Enabled = false; // Removed checkbox
              return;
            }

            // Add built-in source file tracking column first
            dataTable.Columns.Add("_SourceFile", typeof(string));

            foreach (DataRow r in schema.Rows)
            {
              var colName = r["ColumnName"].ToString();
              var dataType = (Type)r["DataType"];
              dataTable.Columns.Add(colName, dataType);
            }
            var pkCol = FindLinkIdColumn(dataTable.Columns);
            if (pkCol != null) dataTable.PrimaryKey = new[] { pkCol };

            foreach (var path in sourceSdfPaths)
            {
              if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) continue;

              string tempCopyPath;
              using (var conn = SqlCeUtils.OpenWithFallback(path, out tempCopyPath))
              {
                if (!string.IsNullOrEmpty(tempCopyPath)) tempFiles.Add(tempCopyPath);
                try
                {
                  using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", conn))
                  using (var reader = cmd.ExecuteReader())
                  {
                    var values = new object[dataTable.Columns.Count - 1]; // -1 because we added _SourceFile column
                    while (reader.Read())
                    {
                      reader.GetValues(values);
                      var row = dataTable.NewRow();

                      // Set the source file column first
                      row["_SourceFile"] = GetWorkOrderName(path);

                      // Set the rest of the values (skip the first column which is _SourceFile)
                      for (int i = 0; i < values.Length; i++)
                      {
                        row[i + 1] = values[i]; // +1 to skip _SourceFile column
                      }

                      // Apply pending overrides
                      var linkIdString = GetLinkIdString(row);
                      if (linkIdString != null && Program.Edits.TryGetRowOverrides(tableName, linkIdString, out var overrides))
                      {
                        foreach (var kv in overrides)
                        {
                          if (dataTable.Columns.Contains(kv.Key)) row[kv.Key] = kv.Value ?? DBNull.Value;
                        }
                      }
                      AddOrMergeRowByLinkId(row);
                    }
                  }
                }
                catch { }
              }
            }
          }
          finally
          {
            // Clean up any temporary database copies
            foreach (var tempFile in tempFiles.Distinct())
            {
              try { System.IO.File.Delete(tempFile); } catch { }
            }
          }

          var binding = new BindingSource { DataSource = dataTable };

          // Ensure AutoGenerateColumns is enabled so new DataTable columns create grid columns
          grid.AutoGenerateColumns = true;
          grid.DataSource = binding;

          Program.Log($"Set DataSource for table '{tableName}' with {dataTable.Columns.Count} columns");
          LoadVirtualColumnDefinitions();
          BuildVirtualLookupCaches();
          RebuildVirtualColumns();
          ApplyPersistedColumnOrder();
          ApplyPersistedColumnWidths();
          ApplyPersistedColumnVisibilities();
          // Mark all initially loaded rows as unchanged so only user edits are tracked
          dataTable.AcceptChanges();
          canPersistEdits = true; // persist to in-memory store only
          // chkAllowEdit.Enabled = true; // Removed checkbox
          lblStatus.Text = "Editing updates the in-memory buffer until consolidation. Double-click to enable.";
        }

        // Ensure grid state matches current edit mode
        InitializeGridEditState();
      }
      catch (Exception ex)
      {
        Program.Log("MetricsDialog load failed", ex);
        MessageBox.Show("Failed to load data: " + ex.Message);
        Close();
      }
      finally
      {
        isInitializing = false;
      }
    }

    private void BuildBreakdownSchemaColumnsCache()
    {
      var tables = new[] { "Products", "Parts", "Subassemblies", "Sheets" };

      // Prefer consolidated connection
      if (connection != null)
      {
        try
        {
          using (var cmd = new SqlCeCommand("SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS", connection))
          using (var rdr = cmd.ExecuteReader())
          {
            while (rdr.Read())
            {
              var t = rdr.GetString(0);
              if (!tables.Contains(t, StringComparer.OrdinalIgnoreCase)) continue;
              var c = rdr.GetString(1);
              if (!breakdownSchemaColumns.TryGetValue(t, out var list))
              {
                list = new List<string>();
                breakdownSchemaColumns[t] = list;
              }
              if (!list.Contains(c)) list.Add(c);
            }
          }
        }
        catch { }
      }

      // Fallback: attempt from first available source SDF
      if (breakdownSchemaColumns.Count == 0 && sourceSdfPaths != null)
      {
        foreach (var path in sourceSdfPaths)
        {
          if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
          string tmp = null;
          try
          {
            using (var conn = SqlCeUtils.OpenWithFallback(path, out tmp))
            {
              foreach (var t in tables)
              {
                try
                {
                  using (var cmd = new SqlCeCommand($"SELECT * FROM [" + t + "]", conn))
                  using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                  {
                    var schema = reader.GetSchemaTable();
                    if (schema == null) continue;
                    if (!breakdownSchemaColumns.TryGetValue(t, out var list))
                    {
                      list = new List<string>();
                      breakdownSchemaColumns[t] = list;
                    }
                    foreach (DataRow r in schema.Rows)
                    {
                      var colName = r["ColumnName"].ToString();
                      if (!list.Contains(colName)) list.Add(colName);
                    }
                  }
                }
                catch { }
              }
            }
          }
          catch { }
          finally
          {
            if (!string.IsNullOrEmpty(tmp)) { try { File.Delete(tmp); } catch { } }
          }
          if (breakdownSchemaColumns.Count > 0) break; // we have something
        }
      }

      // Last fallback: defaults from user config order
      if (breakdownSchemaColumns.Count == 0)
      {
        var cfg = UserConfig.LoadOrDefault();
        foreach (var t in tables)
        {
          var cols = cfg.TryGetColumnOrder(t);
          if (cols != null && cols.Count > 0) breakdownSchemaColumns[t] = cols;
        }
      }

      // Sort columns for consistency
      foreach (var kv in breakdownSchemaColumns.ToList())
      {
        kv.Value.Sort(StringComparer.OrdinalIgnoreCase);
      }
    }

    private string GetWorkOrderName(string sdfPath)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(sdfPath)) return "Unknown";

        // Get the directory containing the .sdf file
        string directory = System.IO.Path.GetDirectoryName(sdfPath);
        if (string.IsNullOrWhiteSpace(directory)) return System.IO.Path.GetFileNameWithoutExtension(sdfPath);

        // Get the work order name (directory name)
        return System.IO.Path.GetFileName(directory);
      }
      catch
      {
        // Fallback to filename if there's any error
        return System.IO.Path.GetFileNameWithoutExtension(sdfPath);
      }
    }

    private void AddOrMergeRowByLinkId(DataRow newRow)
    {
      var linkCol = FindLinkIdColumn(dataTable.Columns);
      if (linkCol == null)
      {
        dataTable.Rows.Add(newRow);
        return;
      }
      var key = GetLinkIdString(newRow);
      if (key == null)
      {
        dataTable.Rows.Add(newRow);
        return;
      }
      var existing = dataTable.Rows.Find(new object[] { Convert.ChangeType(key, linkCol.DataType) });
      if (existing == null)
      {
        dataTable.Rows.Add(newRow);
        return;
      }
      // Merge by overwriting non-null values; prefer new row values
      foreach (DataColumn col in dataTable.Columns)
      {
        var v = newRow[col];
        if (v != DBNull.Value && v != null) existing[col] = v;
      }
    }

    private static DataColumn FindLinkIdColumn(DataColumnCollection columns)
    {
      // Prefer LinkID (any casing)
      if (columns.Contains("LinkID")) return columns["LinkID"];
      foreach (DataColumn c in columns)
      {
        if (string.Equals(c.ColumnName, "LinkID", StringComparison.OrdinalIgnoreCase)) return c;
      }
      // Fallback to ID (any casing)
      if (columns.Contains("ID")) return columns["ID"];
      foreach (DataColumn c in columns)
      {
        if (string.Equals(c.ColumnName, "ID", StringComparison.OrdinalIgnoreCase)) return c;
      }
      return null;
    }

    private string GetLinkIdString(DataRow row)
    {
      if (row?.Table == null) return null;

      // Use the row's own table to find the key column, not the dialog's dataTable
      var linkCol = FindLinkIdColumn(row.Table.Columns);
      if (linkCol == null) return null;

      var val = row[linkCol];
      if (val == null || val == DBNull.Value) return null;
      return Convert.ToString(val);
    }

    private void MetricsDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      try
      {
        // Ensure any in-progress cell edit is committed before closing
        try { Validate(); } catch { }
        try { if (grid != null && grid.IsCurrentCellInEditMode) grid.EndEdit(DataGridViewDataErrorContexts.Commit); } catch { }
        try { if (grid != null) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); } catch { }
        try { if (grid?.DataSource is BindingSource bs) bs.EndEdit(); } catch { }

        // Persist any pending changes only for consolidated mode
        // Pre-consolidation mode saves changes immediately to in-memory store
        if (showFromConsolidated && connection != null)
        {
          TrySaveChanges();
        }
      }
      catch { }
      finally
      {
        try { connection?.Close(); } catch { }
        connection?.Dispose();
        adapter?.Dispose();
        dataTable?.Dispose();
      }
    }

    private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
      if (!grid.ReadOnly && grid.IsCurrentCellDirty)
      {
        // Don't commit on every keystroke; let the edit finish naturally
      }
    }

    private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
      try
      {
        // Persist the specific changed cell after editing completes
        if (!isInitializing && isEditMode && e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
          // Ignore display-only special columns
          var colName = grid.Columns[e.ColumnIndex]?.Name;
          if (!string.IsNullOrEmpty(colName) && virtualColumnNames.Contains(colName)) return;
          TrySaveSingleCellChange(e.RowIndex, e.ColumnIndex, true);
        }
      }
      catch { }
    }

    private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
      if (isInitializing || !isEditMode) return;
      if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
      {
        // Ignore display-only special columns
        var colName = grid.Columns[e.ColumnIndex]?.Name;
        if (!string.IsNullOrEmpty(colName) && virtualColumnNames.Contains(colName)) return;
        try { TrySaveSingleCellChange(e.RowIndex, e.ColumnIndex, false); }
        catch (Exception ex) { Program.Log("CellValueChanged save failed", ex); MessageBox.Show("Save failed: " + ex.Message); }
      }
    }

    private void Grid_DoubleClick(object sender, EventArgs e)
    {
      // Toggle edit mode on double-click
      isEditMode = !isEditMode;
      grid.ReadOnly = !isEditMode;
      if (isEditMode)
      {
        grid.EditMode = DataGridViewEditMode.EditOnKeystroke;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
      }
      else
      {
        grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
      }
      UpdateEditModeStatus();
    }

    private void UpdateEditModeStatus()
    {
      if (isEditMode)
      {
        lblStatus.Text = "Editing enabled. Double-click to disable.";
      }
      else
      {
        lblStatus.Text = "Editing disabled. Double-click to enable.";
      }

      // Update grid state to match edit mode
      InitializeGridEditState();
    }

    private void TxtSearch_TextChanged(object sender, EventArgs e)
    {
      ApplyFilter(txtSearch.Text);
    }

    private void ApplyFilter(string rawQuery)
    {
      if (dataTable == null) return;

      var q = (rawQuery ?? string.Empty).Trim();
      var binding = grid.DataSource as BindingSource;
      if (binding == null) return;

      if (q.Length == 0)
      {
        binding.RemoveFilter();
        return;
      }

      var escaped = EscapeLikeValue(q);
      var sb = new StringBuilder();
      for (int i = 0; i < dataTable.Columns.Count; i++)
      {
        var col = dataTable.Columns[i];
        if (i > 0) sb.Append(" OR ");
        // Convert to string to search all types
        sb.Append("CONVERT([").Append(col.ColumnName.Replace("]", "]]"))
          .Append("], 'System.String') LIKE '%").Append(escaped).Append("%'");
      }
      try
      {
        binding.Filter = sb.ToString();
      }
      catch
      {
        // Fallback: if filter expression fails, clear filter
        binding.RemoveFilter();
      }
    }

    private static string EscapeLikeValue(string value)
    {
      if (string.IsNullOrEmpty(value)) return value;
      // Escape special chars for DataColumn LIKE: %, *, [, ] and ' quotes
      return value.Replace("'", "''").Replace("[", "[[]").Replace("]", "]]").Replace("%", "[%]").Replace("*", "[*]");
    }

    private void TrySaveSingleCellChange(int rowIndex, int columnIndex, bool isEndEdit = false)
    {
      if (dataTable == null || rowIndex < 0 || columnIndex < 0) return;

      if (showFromConsolidated)
      {
        // For consolidated database, still use the bulk save approach
        TrySaveChanges();
      }
      else
      {
        // For pre-consolidation, save only the specific changed cell
        var linkCol = FindLinkIdColumn(dataTable.Columns);
        if (linkCol == null) return;

        var row = grid.Rows[rowIndex];
        if (row?.DataBoundItem is DataRowView dataRowView)
        {
          var dataRow = dataRowView.Row;
          var linkKey = GetLinkIdString(dataRow);
          if (linkKey == null) return;

          var column = grid.Columns[columnIndex];
          var columnName = column.DataPropertyName ?? column.Name;

          // Skip if this is the LinkID column
          if (string.Equals(columnName, linkCol.ColumnName, StringComparison.OrdinalIgnoreCase)) return;

          var cellValue = row.Cells[columnIndex].Value;
          var newValue = cellValue == DBNull.Value ? null : cellValue;

          // Get the original value from the DataRow
          var originalValue = dataRow.HasVersion(DataRowVersion.Original) ? dataRow[columnName, DataRowVersion.Original] : dataRow[columnName];
          if (originalValue == DBNull.Value) originalValue = null;

          // Only save if the value actually changed
          if (!object.Equals(newValue, originalValue))
          {
            Program.Edits.UpsertOverride(tableName, linkKey, columnName, newValue);
            lblStatus.Text = $"Edit saved: {columnName} changed for {linkKey}";
          }
          else if (isEndEdit)
          {
            // If no change occurred, just update status
            lblStatus.Text = $"No change detected for {columnName}";
          }

          // Mark the row as clean so we don't later treat all columns as modified
          try { dataRow.AcceptChanges(); } catch { }
        }
      }
    }

    private void TrySaveChanges()
    {
      if (dataTable == null) return;
      if (showFromConsolidated)
      {
        try
        {
          var changes = dataTable.GetChanges();
          if (changes != null && adapter != null && adapter.UpdateCommand != null)
          {
            adapter.Update(dataTable);
            dataTable.AcceptChanges();
            lblStatus.Text = "Edits saved to consolidated database.";
          }
        }
        catch (Exception ex)
        {
          Program.Log("Persist to consolidated failed", ex);
          MessageBox.Show("Failed to save to consolidated DB: " + ex.Message);
        }
      }
      else
      {
        // Pre-consolidation: rely on per-cell saves. Only need to clear change flags.
        try { dataTable.AcceptChanges(); } catch { }
        lblStatus.Text = "Edits saved in memory; will apply on consolidation.";
      }
    }

    private void Grid_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Right)
      {
        var hit = grid.HitTest(e.X, e.Y);
        if (hit.RowIndex >= 0)
        {
          // If the row isn't already selected, select it without clearing other selections
          var row = grid.Rows[hit.RowIndex];
          if (!row.Selected)
          {
            row.Selected = true;
            grid.CurrentCell = grid[hit.ColumnIndex >= 0 ? hit.ColumnIndex : 0, hit.RowIndex];
          }
        }
      }
    }

    private void Grid_MouseUp(object sender, MouseEventArgs e)
    {
      // Ensure context menu shows on mouse up after selection
      if (e.Button == MouseButtons.Right)
      {
        var hit = grid.HitTest(e.X, e.Y);
        if (hit.RowIndex >= 0)
        {
          ctxMenu?.Show(grid, new System.Drawing.Point(e.X, e.Y));
        }
      }
    }

    private void Grid_KeyDown(object sender, KeyEventArgs e)
    {
      // Handle keyboard shortcuts for editing
      if (e.Control)
      {
        switch (e.KeyCode)
        {
          case Keys.C:
            CopySelectedCells();
            e.Handled = true;
            break;
          case Keys.X:
            if (isEditMode && !grid.ReadOnly)
            {
              CutSelectedCells();
              e.Handled = true;
            }
            break;
          case Keys.V:
            if (isEditMode && !grid.ReadOnly)
            {
              PasteToSelectedCells();
              e.Handled = true;
            }
            break;
        }
        return;
      }

      // Handle Delete key to clear cell values when in edit mode
      if (e.KeyCode == Keys.Delete && isEditMode && !grid.ReadOnly)
      {
        try
        {
          ClearSelectedCells();
          e.Handled = true;
          e.SuppressKeyPress = true;
        }
        catch (Exception ex)
        {
          Program.Log("Error clearing cell values", ex);
          lblStatus.Text = "Error clearing cell values";
        }
      }
    }

    private void CopySelectedCells()
    {
      try
      {
        var selectedCells = GetSelectedCellsForClipboard();
        if (selectedCells.Count == 0)
        {
          lblStatus.Text = "No cells selected for copy";
          return;
        }

        var clipboardText = ConvertCellsToClipboardText(selectedCells);
        Clipboard.SetText(clipboardText);
        lblStatus.Text = $"Copied {selectedCells.Count} cell(s) to clipboard";
      }
      catch (Exception ex)
      {
        Program.Log("Error copying cells", ex);
        lblStatus.Text = "Error copying cells";
      }
    }

    private void CutSelectedCells()
    {
      try
      {
        var selectedCells = GetSelectedCellsForClipboard();
        if (selectedCells.Count == 0)
        {
          lblStatus.Text = "No cells selected for cut";
          return;
        }

        var clipboardText = ConvertCellsToClipboardText(selectedCells);
        Clipboard.SetText(clipboardText);

        // Clear the cells after copying
        ClearSelectedCells();

        lblStatus.Text = $"Cut {selectedCells.Count} cell(s) to clipboard";
      }
      catch (Exception ex)
      {
        Program.Log("Error cutting cells", ex);
        lblStatus.Text = "Error cutting cells";
      }
    }

    private void PasteToSelectedCells()
    {
      try
      {
        if (!Clipboard.ContainsText())
        {
          lblStatus.Text = "No text data in clipboard";
          return;
        }

        var clipboardText = Clipboard.GetText();
        var pasteData = ParseClipboardText(clipboardText);

        if (pasteData.Count == 0)
        {
          lblStatus.Text = "No valid data to paste";
          return;
        }

        var targetCells = GetSelectedCellsForPaste();
        if (targetCells.Count == 0)
        {
          lblStatus.Text = "No target cells selected";
          return;
        }

        var pastedCount = ApplyPasteData(targetCells, pasteData);
        lblStatus.Text = $"Pasted data to {pastedCount} cell(s)";
      }
      catch (Exception ex)
      {
        Program.Log("Error pasting cells", ex);
        lblStatus.Text = "Error pasting cells";
      }
    }

    private void ClearSelectedCells()
    {
      // End any current edit first
      if (grid.IsCurrentCellInEditMode)
      {
        grid.EndEdit();
      }

      var clearedCount = 0;
      var linkCol = FindLinkIdColumn(dataTable.Columns);

      if (isEditMode && grid.SelectionMode == DataGridViewSelectionMode.CellSelect)
      {
        // In edit mode, clear all selected cells
        foreach (DataGridViewCell cell in grid.SelectedCells)
        {
          if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
          {
            var column = grid.Columns[cell.ColumnIndex];
            var columnName = column.DataPropertyName ?? column.Name;

            // Skip special columns and LinkID columns
            if (virtualColumnNames.Contains(columnName)) continue;
            if (linkCol != null && string.Equals(columnName, linkCol.ColumnName, StringComparison.OrdinalIgnoreCase)) continue;

            cell.Value = DBNull.Value;
            TrySaveSingleCellChange(cell.RowIndex, cell.ColumnIndex, true);
            clearedCount++;
          }
        }
      }
      else if (grid.CurrentCell != null && grid.CurrentCell.RowIndex >= 0 && grid.CurrentCell.ColumnIndex >= 0)
      {
        // Fallback to single cell if not in cell selection mode
        var currentCell = grid.CurrentCell;
        var column = grid.Columns[currentCell.ColumnIndex];
        var columnName = column.DataPropertyName ?? column.Name;

        // Skip special columns and LinkID columns
        if (!virtualColumnNames.Contains(columnName) &&
            (linkCol == null || !string.Equals(columnName, linkCol.ColumnName, StringComparison.OrdinalIgnoreCase)))
        {
          currentCell.Value = DBNull.Value;
          TrySaveSingleCellChange(currentCell.RowIndex, currentCell.ColumnIndex, true);
          clearedCount = 1;
        }
      }

      lblStatus.Text = clearedCount > 0 ? $"Cleared {clearedCount} cell(s)" : "No editable cells selected";
    }

    private List<DataGridViewCell> GetSelectedCellsForClipboard()
    {
      var cells = new List<DataGridViewCell>();

      if (grid.SelectionMode == DataGridViewSelectionMode.CellSelect && grid.SelectedCells.Count > 0)
      {
        foreach (DataGridViewCell cell in grid.SelectedCells)
        {
          if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
            cells.Add(cell);
        }
      }
      else if (grid.CurrentCell != null && grid.CurrentCell.RowIndex >= 0 && grid.CurrentCell.ColumnIndex >= 0)
      {
        cells.Add(grid.CurrentCell);
      }

      return cells.OrderBy(c => c.RowIndex).ThenBy(c => c.ColumnIndex).ToList();
    }

    private List<DataGridViewCell> GetSelectedCellsForPaste()
    {
      var cells = new List<DataGridViewCell>();
      var linkCol = FindLinkIdColumn(dataTable.Columns);

      if (grid.SelectionMode == DataGridViewSelectionMode.CellSelect && grid.SelectedCells.Count > 0)
      {
        foreach (DataGridViewCell cell in grid.SelectedCells)
        {
          if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
          {
            var column = grid.Columns[cell.ColumnIndex];
            var columnName = column.DataPropertyName ?? column.Name;

            // Skip special columns and LinkID columns
            if (virtualColumnNames.Contains(columnName)) continue;
            if (linkCol != null && string.Equals(columnName, linkCol.ColumnName, StringComparison.OrdinalIgnoreCase)) continue;

            cells.Add(cell);
          }
        }
      }
      else if (grid.CurrentCell != null && grid.CurrentCell.RowIndex >= 0 && grid.CurrentCell.ColumnIndex >= 0)
      {
        var currentCell = grid.CurrentCell;
        var column = grid.Columns[currentCell.ColumnIndex];
        var columnName = column.DataPropertyName ?? column.Name;

        // Only add if it's not a special or LinkID column
        if (!virtualColumnNames.Contains(columnName) &&
            (linkCol == null || !string.Equals(columnName, linkCol.ColumnName, StringComparison.OrdinalIgnoreCase)))
        {
          cells.Add(currentCell);
        }
      }

      return cells.OrderBy(c => c.RowIndex).ThenBy(c => c.ColumnIndex).ToList();
    }

    private string ConvertCellsToClipboardText(List<DataGridViewCell> cells)
    {
      if (cells.Count == 0) return string.Empty;

      var sb = new StringBuilder();
      var rowGroups = cells.GroupBy(c => c.RowIndex).OrderBy(g => g.Key);

      foreach (var rowGroup in rowGroups)
      {
        var rowCells = rowGroup.OrderBy(c => c.ColumnIndex).ToList();
        var values = new List<string>();

        foreach (var cell in rowCells)
        {
          var value = cell.Value?.ToString() ?? string.Empty;
          // Escape tabs and newlines for tab-delimited format
          value = value.Replace("\t", " ").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
          values.Add(value);
        }

        sb.AppendLine(string.Join("\t", values));
      }

      return sb.ToString();
    }

    private List<List<string>> ParseClipboardText(string clipboardText)
    {
      var result = new List<List<string>>();
      if (string.IsNullOrEmpty(clipboardText)) return result;

      var lines = clipboardText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
      foreach (var line in lines)
      {
        if (string.IsNullOrEmpty(line)) continue;
        var values = line.Split('\t').ToList();
        result.Add(values);
      }

      return result;
    }

    private int ApplyPasteData(List<DataGridViewCell> targetCells, List<List<string>> pasteData)
    {
      if (targetCells.Count == 0 || pasteData.Count == 0) return 0;

      var pastedCount = 0;

      // Get the first value from clipboard data to paste to all selected cells
      var valueToApply = pasteData[0].Count > 0 ? pasteData[0][0] : string.Empty;

      foreach (var targetCell in targetCells)
      {
        try
        {
          // Apply the same value to all selected cells
          if (string.IsNullOrEmpty(valueToApply))
          {
            targetCell.Value = DBNull.Value;
          }
          else
          {
            targetCell.Value = valueToApply;
          }

          TrySaveSingleCellChange(targetCell.RowIndex, targetCell.ColumnIndex, true);
          pastedCount++;
        }
        catch (Exception ex)
        {
          Program.Log($"Error pasting to cell [{targetCell.RowIndex}, {targetCell.ColumnIndex}]", ex);
        }
      }

      return pastedCount;
    }

    private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
    {
      if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

      var column = grid.Columns[e.ColumnIndex];
      var columnName = column.DataPropertyName ?? column.Name;

      // Check if this is an action column
      var actionDef = virtualColumnDefs?.FirstOrDefault(def =>
        string.Equals(def.ColumnName, columnName, StringComparison.OrdinalIgnoreCase) && def.IsActionColumn);

      if (actionDef != null)
      {
        HandleActionColumnClick(actionDef, e.RowIndex);
      }
    }

    private void HandleActionColumnClick(UserConfig.VirtualColumnDef actionDef, int rowIndex)
    {
      try
      {
        // Get the row data
        var row = grid.Rows[rowIndex];
        if (row?.DataBoundItem is DataRowView dataRowView)
        {
          var dataRow = dataRowView.Row;

          // Get the key value for this action
          object keyValue = null;
          if (!string.IsNullOrEmpty(actionDef.LocalKeyColumn) && dataTable.Columns.Contains(actionDef.LocalKeyColumn))
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

        if (showFromConsolidated && !string.IsNullOrWhiteSpace(databasePath) && System.IO.File.Exists(databasePath))
        {
          // Use consolidated database path
          dbPathToUse = databasePath;
        }
        else if (sourceSdfPaths != null && sourceSdfPaths.Count > 0)
        {
          // Use first available source SDF path
          dbPathToUse = sourceSdfPaths.FirstOrDefault(path =>
            !string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path));

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

        using (var viewer = new Product3DViewer(keyValue.ToString(), dbPathToUse, tableName))
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

    private void Grid_MouseClick(object sender, MouseEventArgs e)
    {
      // Prevent left-click from jumping focus during edit
      if (e.Button == MouseButtons.Left && grid.IsCurrentCellInEditMode)
      {
        // do nothing; let the edit continue
      }
    }

    private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
      if (e.ColumnIndex < 0) return;

      if (e.Button == MouseButtons.Right)
      {
        var headerPoint = grid.PointToClient(Cursor.Position);
        ShowHeaderContextMenu(e.ColumnIndex, headerPoint);
      }
      // Left click uses native sorting automatically; no custom handler needed for special columns
    }

    private void ShowHeaderContextMenu(int columnIndex, System.Drawing.Point clientLocation)
    {
      try
      {
        var col = grid.Columns[columnIndex];
        if (col == null) return;
        var menu = new ContextMenuStrip();

        var moveToFirst = new ToolStripMenuItem("Move to first");
        moveToFirst.Click += (s, e) => { col.DisplayIndex = 0; };
        var moveToLast = new ToolStripMenuItem("Move to last");
        moveToLast.Click += (s, e) => { col.DisplayIndex = grid.Columns.Count - 1; };
        menu.Items.Add(moveToFirst);
        menu.Items.Add(moveToLast);
        menu.Items.Add(new ToolStripSeparator());

        var moveToIndex = new ToolStripMenuItem("Move to index...");
        moveToIndex.Click += (s, e) =>
        {
          var selected = PromptForDisplayIndex(col.DisplayIndex);
          if (selected.HasValue)
          {
            int target = Math.Max(0, Math.Min(selected.Value, grid.Columns.Count - 1));
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
        };
        menu.Items.Add(hideColumn);

        var showColumnsMenu = new ToolStripMenuItem("Show Columns");
        var hasHiddenColumns = false;
        foreach (DataGridViewColumn gridCol in grid.Columns)
        {
          if (!gridCol.Visible)
          {
            hasHiddenColumns = true;
            var showCol = new ToolStripMenuItem(gridCol.HeaderText ?? gridCol.Name);
            showCol.Click += (s, e) =>
            {
              gridCol.Visible = true;
              SaveColumnVisibility(gridCol.Name, true);
            };
            showColumnsMenu.DropDownItems.Add(showCol);
          }
        }
        showColumnsMenu.Enabled = hasHiddenColumns;
        menu.Items.Add(showColumnsMenu);

        var showAllColumns = new ToolStripMenuItem("Show All Columns");
        showAllColumns.Click += (s, e) =>
        {
          foreach (DataGridViewColumn gridCol in grid.Columns)
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
        manageVirtual.Click += (s, e) => ManageVirtualColumns();
        menu.Items.Add(manageVirtual);

        menu.Show(grid, clientLocation);
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
          Maximum = grid.Columns.Count > 0 ? grid.Columns.Count - 1 : 0,
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
        if (isInitializing) return; // don't save during initial setup
        var cfg = UserConfig.LoadOrDefault();
        cfg.SetColumnVisibility(tableName, columnName, isVisible);
        cfg.Save();
      }
      catch { }
    }

    private void ApplyPersistedColumnVisibilities()
    {
      try
      {
        var cfg = UserConfig.LoadOrDefault();
        foreach (DataGridViewColumn col in grid.Columns)
        {
          var visibility = cfg.TryGetColumnVisibility(tableName, col.Name);
          if (visibility.HasValue)
          {
            col.Visible = visibility.Value;
          }
          else
          {
            // For virtual columns without saved visibility, default to visible
            if (virtualColumnNames.Contains(col.Name))
            {
              col.Visible = true;
              Program.Log($"Defaulting virtual column '{col.Name}' to visible (no saved setting)");
            }
          }
        }
      }
      catch { }
    }

    // Removed heavy custom sorting for special columns; relying on native sorting for performance

    // removed unused IsStreamColumn

    // removed unused SaveStreamCellToTempFile

    private static string SaveStreamCellToFolder(string columnName, object value, string folder, string baseName)
    {
      byte[] bytes = GetStreamBytes(value);
      if (bytes == null || bytes.Length == 0) return null;
      string ext = string.Equals(columnName, "WMFStream", StringComparison.OrdinalIgnoreCase) ? ".wmf" : ".jpg";
      string safeBase = MakeSafeFileName(baseName);
      string target = Path.Combine(folder, safeBase + ext);
      int n = 1;
      while (File.Exists(target))
      {
        target = Path.Combine(folder, safeBase + "_" + n + ext);
        n++;
      }
      File.WriteAllBytes(target, bytes);
      return target;
    }

    private static byte[] GetStreamBytes(object value)
    {
      if (value is byte[] b) return b;
      if (value is string s)
      {
        try { return Convert.FromBase64String(s); } catch { return Encoding.UTF8.GetBytes(s); }
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

    private string BuildExportBaseName(DataGridViewRow row, string columnName)
    {
      string link = null;
      try
      {
        if (row?.DataBoundItem is DataRowView drv) link = GetLinkIdString(drv.Row);
      }
      catch { }
      if (string.IsNullOrEmpty(link)) link = (row?.Index ?? 0).ToString();
      return tableName + "_" + link + "_" + columnName;
    }

    private IEnumerable<DataGridViewRow> GetSelectedRowsOrCurrent()
    {
      var rows = new HashSet<DataGridViewRow>();
      if (grid.SelectedRows != null && grid.SelectedRows.Count > 0)
      {
        foreach (DataGridViewRow r in grid.SelectedRows) rows.Add(r);
      }
      else if (grid.SelectedCells != null && grid.SelectedCells.Count > 0)
      {
        foreach (DataGridViewCell c in grid.SelectedCells) rows.Add(c.OwningRow);
      }
      else if (grid.CurrentRow != null)
      {
        rows.Add(grid.CurrentRow);
      }
      return rows;
    }

    private void ExportStreamsForSelection(string columnName)
    {
      try
      {
        var rows = GetSelectedRowsOrCurrent().ToList();
        if (rows.Count == 0) { MessageBox.Show("No rows selected."); return; }
        if (!grid.Columns.Contains(columnName)) { MessageBox.Show($"Column '{columnName}' not found."); return; }

        string folder = null;
        using (var fbd = new FolderBrowserDialog())
        {
          fbd.Description = "Choose destination folder for exported images";
          if (fbd.ShowDialog(this) != DialogResult.OK) return;
          folder = fbd.SelectedPath;
        }
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;

        int exported = 0;
        foreach (var row in rows)
        {
          var val = row.Cells[columnName].Value;
          if (val == null || val == DBNull.Value) continue;
          var baseName = BuildExportBaseName(row, columnName);
          var filePath = SaveStreamCellToFolder(columnName, val, folder, baseName);
          if (!string.IsNullOrEmpty(filePath)) exported++;
        }
        MessageBox.Show(exported + " file(s) exported.");
      }
      catch (Exception ex)
      {
        Program.Log("Export streams failed", ex);
        MessageBox.Show("Export failed: " + ex.Message);
      }
    }

    private void DeleteSelectedRows()
    {
      try
      {
        var rows = GetSelectedRowsOrCurrent().ToList();
        if (rows.Count == 0) return;

        foreach (var row in rows)
        {
          if (row?.DataBoundItem is DataRowView drv)
          {
            var linkKey = GetLinkIdString(drv.Row);
            if (!showFromConsolidated)
            {
              if (linkKey != null) Program.Edits.MarkDeleted(tableName, linkKey);
            }
            else
            {
              // For consolidated database, delete the underlying data row
              drv.Row.Delete();
            }
          }
          grid.Rows.Remove(row);
        }

        // Only save changes for consolidated database (actual deletion)
        // For pre-consolidation, MarkDeleted already handled it
        if (showFromConsolidated)
        {
          TrySaveChanges();
        }
      }
      catch (Exception ex)
      {
        Program.Log("Delete row failed", ex);
        MessageBox.Show("Delete failed: " + ex.Message);
      }
    }

    private void ConfigureManualAdapterCommands(SqlCeConnection conn, string preferredKeyColumn)
    {
      try
      {
        // Log column information for debugging
        var allColumnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var duplicateColumns = allColumnNames.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
          .Where(g => g.Count() > 1)
          .Select(g => g.Key)
          .ToList();

        if (duplicateColumns.Count > 0)
        {
          Program.Log($"Warning: Found duplicate column names in table {tableName}: {string.Join(", ", duplicateColumns)}");
        }
        // Determine a key column for updates/deletes
        string keyCol = preferredKeyColumn;
        if (string.IsNullOrWhiteSpace(keyCol) || !dataTable.Columns.Contains(keyCol))
        {
          var col = FindLinkIdColumn(dataTable.Columns);
          if (col == null) throw new InvalidOperationException("No LinkID or ID column present; cannot persist edits.");
          keyCol = col.ColumnName;
        }

        // Build UPDATE SET clauses for all non-key columns (ensure no duplicates)
        var uniqueColumns = dataTable.Columns.Cast<DataColumn>()
          .Where(c => !string.Equals(c.ColumnName, keyCol, StringComparison.OrdinalIgnoreCase))
          .GroupBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
          .Select(g => g.First()) // Take first occurrence of any duplicate column names
          .ToList();

        var setCols = uniqueColumns
          .Select(c => "[" + c.ColumnName.Replace("]", "]]") + "]=@" + c.ColumnName)
          .ToList();
        var updateSql = "UPDATE [" + tableName + "] SET " + string.Join(", ", setCols) +
                        " WHERE [" + keyCol.Replace("]", "]]") + "]=@__pk";
        var upd = new SqlCeCommand(updateSql, conn);

        // Add parameters only for unique columns to avoid "column ID occurred more than once" error
        foreach (DataColumn c in uniqueColumns)
        {
          var param = new SqlCeParameter("@" + c.ColumnName, DBNull.Value)
          {
            SourceColumn = c.ColumnName,
            SourceVersion = DataRowVersion.Current
          };
          upd.Parameters.Add(param);
        }
        var pkParam = new SqlCeParameter("@__pk", DBNull.Value)
        {
          SourceColumn = keyCol,
          SourceVersion = DataRowVersion.Original
        };
        upd.Parameters.Add(pkParam);
        adapter.UpdateCommand = upd;

        var delSql = "DELETE FROM [" + tableName + "] WHERE [" + keyCol.Replace("]", "]]") + "]=@__pk";
        var del = new SqlCeCommand(delSql, conn);
        var dkey = new SqlCeParameter("@__pk", DBNull.Value)
        {
          SourceColumn = keyCol,
          SourceVersion = DataRowVersion.Original
        };
        del.Parameters.Add(dkey);
        adapter.DeleteCommand = del;
      }
      catch (Exception ex)
      {
        Program.Log("ConfigureManualAdapterCommands (consolidated) failed", ex);
      }
    }

    // Removed legacy ApplyPreferredColumnOrder; order now comes entirely from UserConfig

        // Removed Grid_DataBindingComplete event handler to prevent performance issues
    // Virtual columns are built during initialization, persisted settings applied during setup

    private void Grid_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
    {
      // Special columns now use actual DataTable columns, so native sorting handles them automatically
      // This handler can be used for other custom sorting needs in the future
    }

    private void Grid_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
    {
      e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
      e.Column.Resizable = DataGridViewTriState.True;
      // Normalize column Name to stable DataPropertyName for persistence
      if (!string.IsNullOrEmpty(e.Column.DataPropertyName))
      {
        e.Column.Name = e.Column.DataPropertyName;
      }
      // Ensure special columns are marked read-only and styled
      if (!string.IsNullOrEmpty(e.Column.Name) && virtualColumnNames.Contains(e.Column.Name))
      {
        e.Column.ReadOnly = true;
        e.Column.DefaultCellStyle.BackColor = Color.Beige;
        e.Column.DefaultCellStyle.ForeColor = Color.DarkSlateGray;
      }
      // Apply persisted width if available (by DataPropertyName when present)
      var cfg = UserConfig.LoadOrDefault();
      var key = !string.IsNullOrEmpty(e.Column.DataPropertyName) ? e.Column.DataPropertyName : e.Column.Name;
      var w = cfg.TryGetColumnWidth(tableName, key);
      if (w.HasValue && w.Value > 0) e.Column.Width = w.Value;
    }

    private void Grid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
    {
      try
      {
        // Persist user-resized width only if not applying layout
        if (isApplyingLayout || isInitializing) return;
        if (e.Column.Width > 0)
        {
          var cfg = UserConfig.LoadOrDefault();
          var key = !string.IsNullOrEmpty(e.Column.DataPropertyName) ? e.Column.DataPropertyName : e.Column.Name;
          cfg.SetColumnWidth(tableName, key, e.Column.Width);
          cfg.Save();
        }
      }
      catch { }
    }

    private void ApplyPersistedColumnWidths()
    {
      try
      {
        var cfg = UserConfig.LoadOrDefault();
        var prev = isApplyingLayout; isApplyingLayout = true;
        try
        {
          foreach (DataGridViewColumn col in grid.Columns)
          {
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            var k = !string.IsNullOrEmpty(col.DataPropertyName) ? col.DataPropertyName : col.Name;
            var w = cfg.TryGetColumnWidth(tableName, k);
            if (w.HasValue && w.Value > 0) col.Width = w.Value;
          }
        }
        finally { isApplyingLayout = prev; }
      }
      catch { }
    }

    private void ApplyPersistedColumnOrder()
    {
      try
      {
        var cfg = UserConfig.LoadOrDefault();
        var order = cfg.TryGetColumnOrder(tableName);

        // If no saved order exists, build a smart default order
        if (order == null || order.Count == 0)
        {
          order = new List<string>();

          // Start with existing grid columns in their current order
          var existingColumns = grid.Columns.Cast<DataGridViewColumn>()
            .OrderBy(c => c.DisplayIndex)
            .Where(c => !virtualColumnNames.Contains(c.Name)) // Exclude virtual columns
            .Select(c => !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name)
            .ToList();

          // Find LinkID column position to insert virtual columns after it
          var linkIdColumn = existingColumns.FirstOrDefault(name =>
            name.Equals("LinkID", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase));

          if (linkIdColumn != null)
          {
            var linkIdIndex = existingColumns.IndexOf(linkIdColumn);
            // Insert virtual columns right after LinkID
            order.AddRange(existingColumns.Take(linkIdIndex + 1));
            order.AddRange(virtualColumnNames);
            order.AddRange(existingColumns.Skip(linkIdIndex + 1));
          }
          else
          {
            // No LinkID found, add virtual columns at the beginning
            order.AddRange(virtualColumnNames);
            order.AddRange(existingColumns);
          }

          Program.Log($"Built default column order with virtual columns positioned after LinkID: {string.Join(", ", order)}");
        }
        else
        {
          // Add any new virtual columns not in the saved order to position 1 (after LinkID)
          var newVirtualColumns = virtualColumnNames.Where(vc =>
            !order.Any(n => string.Equals(n, vc, StringComparison.OrdinalIgnoreCase))).ToList();

          if (newVirtualColumns.Any())
          {
            // Insert new virtual columns after position 0 (typically after LinkID)
            var insertPosition = Math.Min(1, order.Count);
            foreach (var newCol in newVirtualColumns)
            {
              order.Insert(insertPosition++, newCol);
            }
            Program.Log($"Inserted new virtual columns at position 1: {string.Join(", ", newVirtualColumns)}");
          }
        }

        if (order.Count == 0) return;

        isApplyingLayout = true;
        int idx = 0;
        foreach (var name in order)
        {
          DataGridViewColumn col = null;
          // Prefer DataPropertyName match
          foreach (DataGridViewColumn c in grid.Columns)
          {
            var key2 = !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name;
            if (string.Equals(key2, name, StringComparison.OrdinalIgnoreCase)) { col = c; break; }
          }
          if (col != null) col.DisplayIndex = idx++;
        }

        Program.Log($"Applied column order: {string.Join(", ", order)}");
      }
      catch (Exception ex)
      {
        Program.Log("Error in ApplyPersistedColumnOrder", ex);
      }
      finally { isApplyingLayout = false; }
    }

    private void Grid_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
    {
      try
      {
        if (isApplyingLayout || isInitializing) return; // ignore programmatic changes
        var ordered = grid.Columns.Cast<DataGridViewColumn>()
            .OrderBy(c => c.DisplayIndex)
            .Select(c => !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name)
            .ToList();
        var cfg = UserConfig.LoadOrDefault();
        cfg.SetColumnOrder(tableName, ordered);
        cfg.Save();
      }
      catch { }
    }

    private void LoadVirtualColumnDefinitions()
    {
      try
      {
        var cfg = UserConfig.LoadOrDefault();
        virtualColumnDefs = cfg.GetVirtualColumnsForTable(tableName) ?? new List<UserConfig.VirtualColumnDef>();
        virtualColumnNames.Clear();

        Program.Log($"Loading virtual columns for table '{tableName}': found {virtualColumnDefs.Count} definitions");

        foreach (var def in virtualColumnDefs)
        {
          if (!string.IsNullOrWhiteSpace(def.ColumnName))
          {
            virtualColumnNames.Add(def.ColumnName);
            Program.Log($"  Virtual column '{def.ColumnName}': Type={def.ColumnType}, IsAction={def.IsActionColumn}, ActionType={def.ActionType}, ButtonText={def.ButtonText}");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error loading virtual column definitions", ex);
        virtualColumnDefs = new List<UserConfig.VirtualColumnDef>();
        virtualColumnNames.Clear();
      }
    }

    private void BuildVirtualLookupCaches()
    {
      virtualLookupCacheByColumn.Clear();
      if (virtualColumnDefs == null || virtualColumnDefs.Count == 0) return;

      foreach (var def in virtualColumnDefs)
      {
        if (string.IsNullOrWhiteSpace(def.TargetTableName) || string.IsNullOrWhiteSpace(def.LocalKeyColumn) ||
            string.IsNullOrWhiteSpace(def.TargetKeyColumn) || string.IsNullOrWhiteSpace(def.TargetValueColumn)) continue;

        try
        {
          var lookup = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
          string targetKeyCol = def.TargetKeyColumn.Replace("]", "]]");
          string targetValCol = def.TargetValueColumn.Replace("]", "]]");
          string targetTable = def.TargetTableName.Replace("]", "]]");
          string sql = "SELECT [" + targetKeyCol + "], [" + targetValCol + "] FROM [" + targetTable + "]";

          if (showFromConsolidated && connection != null)
          {
            // Use consolidated database connection
            using (var cmd = new SqlCeCommand(sql, connection))
            using (var rdr = cmd.ExecuteReader())
            {
              while (rdr.Read())
              {
                var keyObj = rdr.IsDBNull(0) ? null : rdr.GetValue(0);
                var valObj = rdr.IsDBNull(1) ? null : rdr.GetValue(1);
                var key = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
                if (key == null) continue;
                if (!lookup.ContainsKey(key)) lookup[key] = valObj;
              }
            }
          }
          else if (sourceSdfPaths != null && sourceSdfPaths.Count > 0)
          {
            // Use source SDF files
            var tempFiles = new List<string>();
            try
            {
              foreach (var path in sourceSdfPaths)
              {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                string tempCopyPath;
                using (var conn = SqlCeUtils.OpenWithFallback(path, out tempCopyPath))
                {
                  if (!string.IsNullOrEmpty(tempCopyPath)) tempFiles.Add(tempCopyPath);
                  try
                  {
                    using (var cmd = new SqlCeCommand(sql, conn))
                    using (var rdr = cmd.ExecuteReader())
                    {
                      while (rdr.Read())
                      {
                        var keyObj = rdr.IsDBNull(0) ? null : rdr.GetValue(0);
                        var valObj = rdr.IsDBNull(1) ? null : rdr.GetValue(1);
                        var key = keyObj == null || keyObj == DBNull.Value ? null : Convert.ToString(keyObj);
                        if (key == null) continue;
                        if (!lookup.ContainsKey(key)) lookup[key] = valObj;
                      }
                    }
                  }
                  catch { }
                }
              }
            }
            finally
            {
              // Clean up temporary files
              foreach (var tempFile in tempFiles.Distinct())
              {
                try { File.Delete(tempFile); } catch { }
              }
            }
          }

          virtualLookupCacheByColumn[def.ColumnName] = lookup;
        }
        catch (Exception ex)
        {
          Program.Log("BuildVirtualLookupCaches failed for column " + def.ColumnName, ex);
        }
      }
    }

    private bool isRebuildingVirtualColumns = false;

    private void RebuildVirtualColumns()
    {
      var newlyAddedColumns = new List<string>();
      var columnsToSaveVisibility = new List<string>(); // Collect columns that need visibility saved

      try
      {
        if (dataTable == null) return;
        if (virtualColumnDefs == null || virtualColumnDefs.Count == 0) return;
        if (isRebuildingVirtualColumns)
        {
          Program.Log("RebuildVirtualColumns: Skipping due to already rebuilding");
          return; // Prevent recursive/repeated calls
        }

        Program.Log("RebuildVirtualColumns: Starting rebuild");
        isRebuildingVirtualColumns = true;

        // Add columns to DataTable and handle both lookup and action columns
        foreach (var def in virtualColumnDefs)
        {
          if (string.IsNullOrWhiteSpace(def.ColumnName)) continue;

          Program.Log($"Processing virtual column '{def.ColumnName}' (Type: {def.ColumnType})");

          // Check for potentially problematic column names
          if (def.ColumnName.Length == 1)
          {
            Program.Log($"Warning: Single-character column name '{def.ColumnName}' may cause DataGridView issues. Consider using a longer name like 'Action_{def.ColumnName}' or 'Col_{def.ColumnName}'");
          }

          // Check if column exists with case-insensitive comparison to avoid duplicates
          var existingColumn = dataTable.Columns.Cast<DataColumn>()
            .FirstOrDefault(c => string.Equals(c.ColumnName, def.ColumnName, StringComparison.OrdinalIgnoreCase));

          bool isNewColumn = existingColumn == null;
          Program.Log($"Virtual column '{def.ColumnName}' isNewColumn: {isNewColumn}");

          // Skip adding built-in columns to DataTable as they're already added during data loading
          if (def.IsBuiltInColumn)
          {
            Program.Log($"Built-in column '{def.ColumnName}' already exists in DataTable, skipping add operation");
          }
          else if (isNewColumn)
          {
            dataTable.Columns.Add(def.ColumnName, typeof(string));
            newlyAddedColumns.Add(def.ColumnName);
            Program.Log($"Added new virtual column '{def.ColumnName}' to DataTable. DataTable now has {dataTable.Columns.Count} columns.");

            // Log all DataTable column names for debugging
            var tableColumns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            Program.Log($"DataTable columns: {string.Join(", ", tableColumns)}");
          }
          else
          {
            Program.Log($"Virtual column '{def.ColumnName}' already exists in DataTable");
          }

          if (def.IsLookupColumn)
          {
            // Populate lookup columns with lookup values
            PopulateLookupColumn(def);
          }
          else if (def.IsActionColumn)
          {
            // Action columns just need the button text as placeholder
            foreach (DataRow row in dataTable.Rows)
            {
              row[def.ColumnName] = def.ButtonText ?? "Action";
            }
            Program.Log($"Populated action column '{def.ColumnName}' with button text '{def.ButtonText}'");
          }
        }

        // Force grid to refresh and create columns for new DataTable columns
        if (newlyAddedColumns.Count > 0)
        {
          Program.Log($"Forcing DataGridView to recognize {newlyAddedColumns.Count} new virtual columns");

          // More aggressive approach: temporarily reset and restore the DataSource
          var currentDataSource = grid.DataSource;
          grid.DataSource = null;
          grid.Refresh();
          Application.DoEvents();
          grid.DataSource = currentDataSource;
          grid.Refresh();
          grid.Update();
          Application.DoEvents();

          Program.Log($"Refreshed grid after adding {newlyAddedColumns.Count} new virtual columns");

          // Log what columns the grid actually has after refresh
          var gridColumns = grid.Columns.Cast<DataGridViewColumn>().Select(c => c.Name).ToList();
          Program.Log($"DataGridView columns after refresh: {string.Join(", ", gridColumns)}");
        }

        // Style virtual columns in the grid and ensure visibility
        foreach (var def in virtualColumnDefs)
        {
          if (string.IsNullOrWhiteSpace(def.ColumnName)) continue;

          // Wait a moment for the grid to create the column if it's new
          DataGridViewColumn col = null;
          if (newlyAddedColumns.Contains(def.ColumnName))
          {
            // For new columns, try a few times to find them in the grid
            for (int attempt = 0; attempt < 3 && col == null; attempt++)
            {
              if (grid.Columns.Contains(def.ColumnName))
              {
                col = grid.Columns[def.ColumnName];
                break;
              }
              System.Threading.Thread.Sleep(10); // Brief wait
              grid.Refresh();
            }
          }
          else
          {
            // For existing columns, single check
            if (grid.Columns.Contains(def.ColumnName))
            {
              col = grid.Columns[def.ColumnName];
            }
          }

          if (col != null)
          {
            col.ReadOnly = true;

            // Apply special styling for built-in columns
            if (def.IsBuiltInColumn)
            {
              col.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 248, 255); // Light blue background

              // Fix null reference exception by checking if font exists and providing a default
              if (col.DefaultCellStyle.Font != null)
              {
                col.DefaultCellStyle.Font = new System.Drawing.Font(col.DefaultCellStyle.Font, System.Drawing.FontStyle.Bold);
              }
              else
              {
                // Use a default font if none exists
                col.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
              }

              col.HeaderText = def.ButtonText; // Use "Work Order" as header text
              Program.Log($"Applied built-in column styling to '{def.ColumnName}'");
            }

            // Ensure new virtual columns are visible
            if (newlyAddedColumns.Contains(def.ColumnName))
            {
              col.Visible = true;
              Program.Log($"Made new virtual column '{def.ColumnName}' visible");

              // Collect columns that need visibility saved instead of saving immediately
              columnsToSaveVisibility.Add(def.ColumnName);

              // Let ApplyPersistedColumnOrder handle positioning to preserve existing order
              Program.Log($"New virtual column '{def.ColumnName}' will be positioned by ApplyPersistedColumnOrder()");
            }

            if (def.IsActionColumn)
            {
              // Style action columns differently - they'll be rendered as buttons
              col.DefaultCellStyle.BackColor = Color.LightBlue;
              col.DefaultCellStyle.ForeColor = Color.DarkBlue;
              col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
              Program.Log($"Applied action column styling to '{def.ColumnName}'");
            }
            else
            {
              // Style lookup columns
              col.DefaultCellStyle.BackColor = Color.Beige;
              col.DefaultCellStyle.ForeColor = Color.DarkSlateGray;
            }
          }
          else
          {
            Program.Log($"Warning: Virtual column '{def.ColumnName}' not found in grid columns after refresh attempts");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("Error in RebuildVirtualColumns", ex);
      }
      finally
      {
        // Batch save visibility settings for new columns to reduce file I/O
        if (columnsToSaveVisibility.Count > 0)
        {
          try
          {
            var cfg = UserConfig.LoadOrDefault();
            foreach (var columnName in columnsToSaveVisibility)
            {
              cfg.SetColumnVisibility(tableName, columnName, true);
            }
            cfg.Save();
            Program.Log($"Batch saved visibility settings for {columnsToSaveVisibility.Count} new virtual columns");
          }
          catch (Exception ex)
          {
            Program.Log($"Failed to batch save visibility settings for new virtual columns", ex);
          }
        }

        isRebuildingVirtualColumns = false;
        Program.Log("RebuildVirtualColumns: Completed rebuild");
      }
    }

    private void PopulateLookupColumn(UserConfig.VirtualColumnDef def)
    {
      if (virtualLookupCacheByColumn.TryGetValue(def.ColumnName, out var lookup))
      {
        foreach (DataRow row in dataTable.Rows)
        {
          try
          {
            object localKeyVal = null;
            if (dataTable.Columns.Contains(def.LocalKeyColumn))
              localKeyVal = row[def.LocalKeyColumn];
            var localKey = localKeyVal == null || localKeyVal == DBNull.Value ? null : Convert.ToString(localKeyVal);

            string displayValue = "";
            if (!string.IsNullOrEmpty(localKey) && lookup.TryGetValue(localKey, out var valueObj))
            {
              displayValue = valueObj == null || valueObj == DBNull.Value ? "" : Convert.ToString(valueObj);
            }
            row[def.ColumnName] = displayValue ?? "";
          }
          catch { row[def.ColumnName] = ""; }
        }
      }
    }

    private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
      // Special columns now use actual DataTable columns, so no custom formatting needed
      // This handler can be used for other formatting needs in the future
    }

    private void ManageVirtualColumns()
    {
      try
      {
        using (var dlg = new VirtualColumnsDialog(tableName, connection, showFromConsolidated, breakdownSchemaColumns))
        {
          if (dlg.ShowDialog(this) == DialogResult.OK)
          {
            // Reload definitions and caches, and rebuild columns
            LoadVirtualColumnDefinitions();
            BuildVirtualLookupCaches();
            RebuildVirtualColumns();
            ApplyPersistedColumnOrder();
            ApplyPersistedColumnWidths();
            ApplyPersistedColumnVisibilities();

            // Save the updated column order to preserve any new virtual column positions
            try
            {
              var currentOrder = grid.Columns.Cast<DataGridViewColumn>()
                .OrderBy(c => c.DisplayIndex)
                .Select(c => !string.IsNullOrEmpty(c.DataPropertyName) ? c.DataPropertyName : c.Name)
                .ToList();
              var cfg = UserConfig.LoadOrDefault();
              cfg.SetColumnOrder(tableName, currentOrder);
              cfg.Save();
              Program.Log($"Saved updated column order after virtual column changes: {string.Join(", ", currentOrder)}");
            }
            catch (Exception saveEx)
            {
              Program.Log("Failed to save updated column order after virtual column changes", saveEx);
            }

            grid.Refresh();
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("ManageVirtualColumns failed", ex);
      }
    }
  }
}
