using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace WorkOrderBlender
{
  public sealed class MetricsDialog : Form
  {
    // Preferred column ordering for the details dialog. Columns not listed will follow alphabetically after these.
    private readonly List<string> metricPreferredColumnOrder = new List<string>
    {
      "RoomName",
      "Name",
      "Quantity",
      "MaterialXData1",
      "LinkID",
      "LinkIDWorkOrder",
      "LinkIDPart",
      "LinkIDProduct",
      "LinkIDSubassembly",
    };

    private readonly string tableName;
    private readonly string databasePath;
    private readonly bool showFromConsolidated;
    private readonly List<string> sourceSdfPaths;

    private readonly SplitContainer splitContainer;
    private readonly TextBox txtSearch;
    private readonly CheckBox chkAllowEdit;
    private readonly Label lblStatus;
    private readonly DataGridView grid;
    private readonly ContextMenuStrip ctxMenu;
    private bool isApplyingLayout;

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
        ColumnCount = 5,
        RowCount = 1,
      };
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      panelTop.Controls.Add(layoutTop);

      var lblSearch = new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left };
      txtSearch = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 600 };
      chkAllowEdit = new CheckBox { Text = "Allow editing", AutoSize = true, Checked = false, Anchor = AnchorStyles.Left };
      lblStatus = new Label { Text = string.Empty, AutoSize = true, Anchor = AnchorStyles.Left };

      layoutTop.Controls.Add(lblSearch, 0, 0);
      layoutTop.Controls.Add(txtSearch, 1, 0);
      layoutTop.Controls.Add(chkAllowEdit, 2, 0);
      layoutTop.Controls.Add(lblStatus, 4, 0);

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
      };
      grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
      grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
      grid.AllowUserToOrderColumns = true;
      splitContainer.Panel2.Controls.Add(grid);

      // Context menu
      ctxMenu = new ContextMenuStrip();
      var miExportJpeg = new ToolStripMenuItem("Export JPEGStream...");
      var miExportWmf = new ToolStripMenuItem("Export WMFStream...");
      var miDeleteRow = new ToolStripMenuItem("Delete Row(s)");
      miExportJpeg.Click += (s, e) => ExportStreamsForSelection("JPEGStream");
      miExportWmf.Click += (s, e) => ExportStreamsForSelection("WMFStream");
      miDeleteRow.Click += (s, e) => DeleteSelectedRows();
      ctxMenu.Items.AddRange(new ToolStripItem[] { miExportJpeg, miExportWmf, new ToolStripSeparator(), miDeleteRow });
      grid.ContextMenuStrip = ctxMenu;

      Load += MetricsDialog_Load;
      FormClosing += MetricsDialog_FormClosing;
      txtSearch.TextChanged += TxtSearch_TextChanged;
      chkAllowEdit.CheckedChanged += ChkAllowEdit_CheckedChanged;
      grid.CellValueChanged += Grid_CellValueChanged;
      grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
      grid.CellEndEdit += Grid_CellEndEdit;
      grid.DataBindingComplete += Grid_DataBindingComplete;
      grid.ColumnAdded += Grid_ColumnAdded;
      grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
      grid.MouseDown += Grid_MouseDown;
      grid.MouseUp += Grid_MouseUp;
      grid.MouseClick += Grid_MouseClick;
            grid.Sorted += (s, e) => { ApplyPersistedColumnWidths(); ApplyPersistedColumnOrder(); };
            grid.SizeChanged += (s, e) => ApplyPersistedColumnWidths();
            grid.ColumnDisplayIndexChanged += Grid_ColumnDisplayIndexChanged;
    }

    // Pre-consolidation constructor: loads from multiple source SDFs into memory
    public MetricsDialog(string dialogLabel, string tableName, List<string> inputSdfPaths, bool allowEditingInMemory)
      : this(dialogLabel, tableName, consolidatedDatabasePath: string.Empty)
    {
      this.showFromConsolidated = false;
      this.sourceSdfPaths = inputSdfPaths ?? new List<string>();
      chkAllowEdit.Checked = allowEditingInMemory;
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
            chkAllowEdit.Enabled = false;
            return;
          }

          connection = new SqlCeConnection($"Data Source={databasePath};");
          connection.Open();

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
          ApplyPreferredColumnOrder();
          ApplyPersistedColumnOrder();
          ApplyPersistedColumnWidths();

          // Prepare manual commands for DB persistence using LinkID as PK
          var linkCol2 = FindLinkIdColumn(dataTable.Columns);
          if (linkCol2 != null)
          {
            ConfigureManualAdapterCommands(connection, linkCol2.ColumnName);
            canPersistEdits = adapter.UpdateCommand != null;
          }
          else
          {
            canPersistEdits = false;
          }

          lblStatus.Text = canPersistEdits ? "Editing updates the consolidated database (LinkID key)." : "Editing disabled: table has no LinkID.";
          chkAllowEdit.Enabled = canPersistEdits;
        }
        else
        {
          // Pre-consolidation: build DataTable by unioning rows from all sources, and applying in-memory edits
          dataTable = new DataTable(tableName);
          DataTable schema = null;
          foreach (var path in sourceSdfPaths)
          {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) continue;
            using (var conn = new SqlCeConnection($"Data Source={path};Mode=Read Only;"))
            {
              conn.Open();
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
            chkAllowEdit.Enabled = false;
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
            using (var conn = new SqlCeConnection($"Data Source={path};Mode=Read Only;"))
            {
              conn.Open();
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

          var binding = new BindingSource { DataSource = dataTable };
          grid.DataSource = binding;
          ApplyPreferredColumnOrder();
          ApplyPersistedColumnOrder();
          ApplyPersistedColumnWidths();
          canPersistEdits = true; // persist to in-memory store only
          chkAllowEdit.Enabled = true;
          lblStatus.Text = "Editing updates the in-memory buffer until consolidation.";
        }
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
      if (columns.Contains("LinkID")) return columns["LinkID"];
      // Some tables may use different casing or types; try case-insensitive lookup
      foreach (DataColumn c in columns)
      {
        if (string.Equals(c.ColumnName, "LinkID", StringComparison.OrdinalIgnoreCase)) return c;
      }
      return null;
    }

    private string GetLinkIdString(DataRow row)
    {
      var linkCol = FindLinkIdColumn(dataTable.Columns);
      if (linkCol == null) return null;
      var val = row[linkCol];
      if (val == null || val == DBNull.Value) return null;
      return Convert.ToString(val);
    }

    private void MetricsDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      try { connection?.Close(); } catch { }
      finally
      {
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
        // Persist after editing completes
        if (!isInitializing && chkAllowEdit.Checked)
        {
          TrySaveChanges();
        }
      }
      catch { }
    }

    private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
      if (isInitializing || !chkAllowEdit.Checked) return;
      TrySaveChanges();
    }

    private void ChkAllowEdit_CheckedChanged(object sender, EventArgs e)
    {
      grid.ReadOnly = !chkAllowEdit.Checked;
      if (!grid.ReadOnly)
      {
      grid.EditMode = DataGridViewEditMode.EditOnKeystroke;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
      }
      else
      {
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
      }
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
        // Persist to in-memory edit store keyed by LinkID (pre-consolidation)
        var linkCol = FindLinkIdColumn(dataTable.Columns);
        if (linkCol == null) return;
        var changes = dataTable.GetChanges();
        var rowsToProcess = changes != null ? changes.Rows.Cast<DataRow>() : dataTable.Rows.Cast<DataRow>();
        foreach (var row in rowsToProcess)
        {
          var linkKey = GetLinkIdString(row);
          if (linkKey == null) continue;
          foreach (DataColumn col in dataTable.Columns)
          {
            if (string.Equals(col.ColumnName, linkCol.ColumnName, StringComparison.OrdinalIgnoreCase)) continue;
            Program.Edits.UpsertOverride(tableName, linkKey, col.ColumnName, row[col] == DBNull.Value ? null : row[col]);
          }
        }
        dataTable.AcceptChanges();
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
          grid.ContextMenuStrip?.Show(grid, new System.Drawing.Point(e.X, e.Y));
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

    private static bool IsStreamColumn(string columnName)
    {
      return string.Equals(columnName, "JPEGStream", StringComparison.OrdinalIgnoreCase)
        || string.Equals(columnName, "WMFStream", StringComparison.OrdinalIgnoreCase);
    }

    private static string SaveStreamCellToTempFile(string columnName, object value)
    {
      byte[] bytes = null;
      if (value is byte[] b)
      {
        bytes = b;
      }
      else if (value is string s)
      {
        try
        {
          bytes = Convert.FromBase64String(s);
        }
        catch
        {
          // Not base64; treat as raw text
          bytes = Encoding.UTF8.GetBytes(s);
        }
      }
      else if (value is System.IO.Stream stream)
      {
        using (var ms = new MemoryStream())
        {
          stream.CopyTo(ms);
          bytes = ms.ToArray();
        }
      }

      if (bytes == null || bytes.Length == 0) return null;

      string ext = string.Equals(columnName, "WMFStream", StringComparison.OrdinalIgnoreCase) ? ".wmf" : ".jpg";
      string path = Path.Combine(Path.GetTempPath(), "WOB_" + Guid.NewGuid().ToString("N") + ext);
      File.WriteAllBytes(path, bytes);
      return path;
    }

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
          }
          grid.Rows.Remove(row);
        }
        TrySaveChanges();
      }
      catch (Exception ex)
      {
        Program.Log("Delete row failed", ex);
        MessageBox.Show("Delete failed: " + ex.Message);
      }
    }

    private void ConfigureManualAdapterCommands(SqlCeConnection conn, string linkIdColumn)
    {
      try
      {
        // Build UPDATE SET clauses for all non-key columns
        var setCols = dataTable.Columns.Cast<DataColumn>()
          .Where(c => !string.Equals(c.ColumnName, linkIdColumn, StringComparison.OrdinalIgnoreCase))
          .Select(c => "[" + c.ColumnName.Replace("]", "]]") + "]=@" + c.ColumnName)
          .ToList();
        var updateSql = "UPDATE [" + tableName + "] SET " + string.Join(", ", setCols) +
                        " WHERE [" + linkIdColumn.Replace("]", "]]") + "]=@__pk";
        var upd = new SqlCeCommand(updateSql, conn);
        foreach (DataColumn c in dataTable.Columns)
        {
          if (string.Equals(c.ColumnName, linkIdColumn, StringComparison.OrdinalIgnoreCase)) continue;
          var p = upd.Parameters.Add("@" + c.ColumnName, System.Data.SqlDbType.Variant);
          p.SourceColumn = c.ColumnName;
          p.SourceVersion = DataRowVersion.Current;
        }
        var pkey = upd.Parameters.Add("@__pk", System.Data.SqlDbType.Variant);
        pkey.SourceColumn = linkIdColumn;
        pkey.SourceVersion = DataRowVersion.Original;
        adapter.UpdateCommand = upd;

        var delSql = "DELETE FROM [" + tableName + "] WHERE [" + linkIdColumn.Replace("]", "]]") + "]=@__pk";
        var del = new SqlCeCommand(delSql, conn);
        var dkey = del.Parameters.Add("@__pk", System.Data.SqlDbType.Variant);
        dkey.SourceColumn = linkIdColumn;
        dkey.SourceVersion = DataRowVersion.Original;
        adapter.DeleteCommand = del;
      }
      catch (Exception ex)
      {
        Program.Log("ConfigureManualAdapterCommands (consolidated) failed", ex);
      }
    }

    private void ApplyPreferredColumnOrder()
    {
      try
      {
        if (grid.Columns.Count == 0) return;
        var prevFlag = isApplyingLayout;
        isApplyingLayout = true;
        var orderedNames = grid.Columns.Cast<DataGridViewColumn>()
          .Select(c => c.Name)
          .OrderBy(name =>
          {
            int idx = metricPreferredColumnOrder.FindIndex(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
            return idx < 0 ? int.MaxValue : idx;
          })
          .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
          .ToList();
        for (int displayIndex = 0; displayIndex < orderedNames.Count; displayIndex++)
        {
          var name = orderedNames[displayIndex];
          var col = grid.Columns[name];
          if (col != null) col.DisplayIndex = displayIndex;
        }
        isApplyingLayout = prevFlag;
      }
      catch { }
    }

    private void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
      // Only read & apply user prefs; do not persist during open
      ApplyPreferredColumnOrder();
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
