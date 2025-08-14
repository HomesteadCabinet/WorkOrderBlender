
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

    // Consolidated-mode constructor
    public MetricsDialog(string dialogLabel, string tableName, string consolidatedDatabasePath)
    {
      this.tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
      this.databasePath = consolidatedDatabasePath ?? string.Empty;
      this.showFromConsolidated = true;
      this.sourceSdfPaths = null;

      Text = string.IsNullOrWhiteSpace(dialogLabel) ? $"Data for {tableName}" : $"Data for {dialogLabel}";
      StartPosition = FormStartPosition.CenterScreen;
      Width = 1500;
      Height = 800;

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
      grid.DataBindingComplete += Grid_DataBindingComplete;
      grid.ColumnAdded += Grid_ColumnAdded;
      grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
      grid.MouseDown += Grid_MouseDown;
      grid.MouseUp += Grid_MouseUp;
      grid.MouseClick += Grid_MouseClick;
      grid.DoubleClick += Grid_DoubleClick; // Add double-click handler
      grid.Sorted += (s, e) => { ApplyPersistedColumnWidths(); ApplyPersistedColumnOrder(); };
      grid.SizeChanged += (s, e) => ApplyPersistedColumnWidths();
      grid.ColumnDisplayIndexChanged += Grid_ColumnDisplayIndexChanged;
      grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;

      // Initialize grid state based on edit mode
      InitializeGridEditState();
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

          adapter = new SqlCeDataAdapter($"SELECT * FROM [" + tableName + "]", connection);
          adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
          dataTable = new DataTable(tableName);
          adapter.FillSchema(dataTable, SchemaType.Source);
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
          grid.DataSource = binding;
          ApplyPersistedColumnOrder();
          ApplyPersistedColumnWidths();

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
                    var values = new object[dataTable.Columns.Count];
                    while (reader.Read())
                    {
                      reader.GetValues(values);
                      var row = dataTable.NewRow();
                      row.ItemArray = (object[])values.Clone();
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
          grid.DataSource = binding;
          ApplyPersistedColumnOrder();
          ApplyPersistedColumnWidths();
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
      if (e.Button != MouseButtons.Right || e.ColumnIndex < 0) return;
      var headerPoint = grid.PointToClient(Cursor.Position);
      ShowHeaderContextMenu(e.ColumnIndex, headerPoint);
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
        // Determine a key column for updates/deletes
        string keyCol = preferredKeyColumn;
        if (string.IsNullOrWhiteSpace(keyCol) || !dataTable.Columns.Contains(keyCol))
        {
          var col = FindLinkIdColumn(dataTable.Columns);
          if (col == null) throw new InvalidOperationException("No LinkID or ID column present; cannot persist edits.");
          keyCol = col.ColumnName;
        }

        // Build UPDATE SET clauses for all non-key columns
        var setCols = dataTable.Columns.Cast<DataColumn>()
          .Where(c => !string.Equals(c.ColumnName, keyCol, StringComparison.OrdinalIgnoreCase))
          .Select(c => "[" + c.ColumnName.Replace("]", "]]") + "]=@" + c.ColumnName)
          .ToList();
        var updateSql = "UPDATE [" + tableName + "] SET " + string.Join(", ", setCols) +
                        " WHERE [" + keyCol.Replace("]", "]]") + "]=@__pk";
        var upd = new SqlCeCommand(updateSql, conn);
        foreach (DataColumn c in dataTable.Columns)
        {
          if (string.Equals(c.ColumnName, keyCol, StringComparison.OrdinalIgnoreCase)) continue;
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

    private void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
      // Apply defaults from config (order and widths)
      ApplyPersistedColumnOrder();
      ApplyPersistedColumnWidths();
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
        if (order == null || order.Count == 0) return;
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
      }
      catch { }
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
  }
}
