using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlServerCe;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WorkOrderBlender
{
  public sealed class SpecialColumnsDialog : Form
  {
    private readonly string tableName;
    private readonly SqlCeConnection connection; // may be null
    private readonly bool hasDatabaseContext;

    private readonly DataGridView grid;
    private readonly Button btnAdd;
    private readonly Button btnEdit;
    private readonly Button btnRemove;
    private readonly Button btnOk;
    private readonly Button btnCancel;

    private readonly BindingList<UserConfig.SpecialColumnDef> defs;
    private readonly Dictionary<string, List<string>> columnsByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    public SpecialColumnsDialog(string tableName, SqlCeConnection connection, bool showFromConsolidated)
      : this(tableName, connection, showFromConsolidated, null)
    {
    }

    public SpecialColumnsDialog(string tableName, SqlCeConnection connection, bool showFromConsolidated, Dictionary<string, List<string>> preloadedColumns)
    {
      this.tableName = tableName;
      this.connection = connection;
      this.hasDatabaseContext = showFromConsolidated && connection != null && connection.State == ConnectionState.Open;

      Text = "Special Columns for " + tableName;
      StartPosition = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.Sizable;
      MinimizeBox = false;
      MaximizeBox = false;
      Width = 800;
      Height = 420;

      defs = new BindingList<UserConfig.SpecialColumnDef>(UserConfig.LoadOrDefault().GetSpecialColumnsForTable(tableName));

      // Merge any preloaded columns into local map
      if (preloadedColumns != null)
      {
        foreach (var kv in preloadedColumns)
        {
          if (!columnsByTable.TryGetValue(kv.Key, out var list))
          {
            list = new List<string>();
            columnsByTable[kv.Key] = list;
          }
          foreach (var c in kv.Value)
          {
            if (!list.Contains(c)) list.Add(c);
          }
          list.Sort(StringComparer.OrdinalIgnoreCase);
        }
      }

      var layout = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 2,
      };
      layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
      layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      Controls.Add(layout);

      grid = new DataGridView
      {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
      };
      layout.Controls.Add(grid, 0, 0);

      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Column Name", DataPropertyName = nameof(UserConfig.SpecialColumnDef.ColumnName), Name = nameof(UserConfig.SpecialColumnDef.ColumnName), Width = 150 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Local Key Column", DataPropertyName = nameof(UserConfig.SpecialColumnDef.LocalKeyColumn), Name = nameof(UserConfig.SpecialColumnDef.LocalKeyColumn), Width = 150 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Target Table", DataPropertyName = nameof(UserConfig.SpecialColumnDef.TargetTableName), Name = nameof(UserConfig.SpecialColumnDef.TargetTableName), Width = 150 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Target Key Column", DataPropertyName = nameof(UserConfig.SpecialColumnDef.TargetKeyColumn), Name = nameof(UserConfig.SpecialColumnDef.TargetKeyColumn), Width = 150 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Target Value Column", DataPropertyName = nameof(UserConfig.SpecialColumnDef.TargetValueColumn), Name = nameof(UserConfig.SpecialColumnDef.TargetValueColumn), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
      grid.DataSource = defs;

      var panelButtons = new FlowLayoutPanel
      {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.RightToLeft,
        Padding = new Padding(8),
        AutoSize = true,
      };
      layout.Controls.Add(panelButtons, 0, 1);

      btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
      btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
      btnAdd = new Button { Text = "Add", Width = 90 };
      btnEdit = new Button { Text = "Edit", Width = 90 };
      btnRemove = new Button { Text = "Remove", Width = 90 };
      panelButtons.Controls.AddRange(new Control[] { btnOk, btnCancel, btnRemove, btnEdit, btnAdd });
      AcceptButton = btnOk;
      CancelButton = btnCancel;

      btnAdd.Click += (s, e) => AddEntry();
      btnEdit.Click += (s, e) => EditSelected();
      btnRemove.Click += (s, e) => RemoveSelected();
      btnOk.Click += (s, e) => SaveAndClose();

      try { if (hasDatabaseContext) LoadSchema(); } catch { }
    }

    private void LoadSchema()
    {
      // Build a map of table -> columns from INFORMATION_SCHEMA
      using (var cmd = new SqlCeCommand("SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS", connection))
      using (var rdr = cmd.ExecuteReader())
      {
        while (rdr.Read())
        {
          var t = rdr.GetString(0);
          var c = rdr.GetString(1);
          if (!columnsByTable.TryGetValue(t, out var list))
          {
            list = new List<string>();
            columnsByTable[t] = list;
          }
          if (!list.Contains(c)) list.Add(c);
        }
      }
      foreach (var kv in columnsByTable) kv.Value.Sort(StringComparer.OrdinalIgnoreCase);
    }

    private void AddEntry()
    {
      var def = new UserConfig.SpecialColumnDef
      {
        TableName = tableName,
        ColumnName = "",
        LocalKeyColumn = "",
        TargetTableName = "",
        TargetKeyColumn = "",
        TargetValueColumn = "",
      };
      if (EditEntry(def))
      {
        defs.Add(def);
      }
    }

    private void EditSelected()
    {
      var current = GetSelected();
      if (current == null) return;
      var copy = new UserConfig.SpecialColumnDef
      {
        TableName = current.TableName,
        ColumnName = current.ColumnName,
        LocalKeyColumn = current.LocalKeyColumn,
        TargetTableName = current.TargetTableName,
        TargetKeyColumn = current.TargetKeyColumn,
        TargetValueColumn = current.TargetValueColumn,
      };
      if (EditEntry(copy))
      {
        current.ColumnName = copy.ColumnName;
        current.LocalKeyColumn = copy.LocalKeyColumn;
        current.TargetTableName = copy.TargetTableName;
        current.TargetKeyColumn = copy.TargetKeyColumn;
        current.TargetValueColumn = copy.TargetValueColumn;
        grid.Refresh();
      }
    }

    private void RemoveSelected()
    {
      var current = GetSelected();
      if (current == null) return;
      defs.Remove(current);
    }

    private UserConfig.SpecialColumnDef GetSelected()
    {
      if (grid.CurrentRow?.DataBoundItem is UserConfig.SpecialColumnDef d) return d;
      return null;
    }

    private bool EditEntry(UserConfig.SpecialColumnDef def)
    {
      using (var dlg = new EditSpecialColumnDialog(def, columnsByTable, hasDatabaseContext))
      {
        return dlg.ShowDialog(this) == DialogResult.OK;
      }
    }

    private void SaveAndClose()
    {
      var cfg = UserConfig.LoadOrDefault();
      // Remove all for this table, then add the current list
      cfg.SpecialColumns.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
      foreach (var d in defs)
      {
        // Basic validation
        if (string.IsNullOrWhiteSpace(d.ColumnName)) continue;
        d.TableName = tableName;
        cfg.UpsertSpecialColumn(d);
      }
      cfg.Save();
      DialogResult = DialogResult.OK;
      Close();
    }
  }

  internal sealed class EditSpecialColumnDialog : Form
  {
    private readonly TextBox txtColumnName;
    private readonly ComboBox cboLocalKey;
    private readonly ComboBox cboTargetTable;
    private readonly ComboBox cboTargetKey;
    private readonly ComboBox cboTargetValue;
    private readonly Button btnOk;
    private readonly Button btnCancel;
    private readonly Dictionary<string, List<string>> columnsByTable;
    private readonly bool hasDb;
    private readonly UserConfig.SpecialColumnDef def;

    public EditSpecialColumnDialog(UserConfig.SpecialColumnDef def, Dictionary<string, List<string>> columnsByTable, bool hasDb)
    {
      this.def = def;
      this.columnsByTable = columnsByTable ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
      this.hasDb = hasDb;

      Text = "Edit Special Column";
      StartPosition = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MinimizeBox = false;
      MaximizeBox = false;
      Width = 520;
      Height = 280;

      var table = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 6,
        Padding = new Padding(8),
      };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      for (int i = 0; i < 5; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      Controls.Add(table);

      table.Controls.Add(new Label { Text = "Column name:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
      txtColumnName = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
      table.Controls.Add(txtColumnName, 1, 0);

      table.Controls.Add(new Label { Text = "Local key column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
      cboLocalKey = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      table.Controls.Add(cboLocalKey, 1, 1);

      table.Controls.Add(new Label { Text = "Target table:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
      cboTargetTable = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      table.Controls.Add(cboTargetTable, 1, 2);

      table.Controls.Add(new Label { Text = "Target key column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
      cboTargetKey = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      table.Controls.Add(cboTargetKey, 1, 3);

      table.Controls.Add(new Label { Text = "Target value column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
      cboTargetValue = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      table.Controls.Add(cboTargetValue, 1, 4);

      // Improve UX with autocomplete on all dropdowns
      cboLocalKey.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboLocalKey.AutoCompleteSource = AutoCompleteSource.ListItems;
      cboTargetTable.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboTargetTable.AutoCompleteSource = AutoCompleteSource.ListItems;
      cboTargetKey.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboTargetKey.AutoCompleteSource = AutoCompleteSource.ListItems;
      cboTargetValue.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboTargetValue.AutoCompleteSource = AutoCompleteSource.ListItems;

      var panelButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
      btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
      btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
      panelButtons.Controls.AddRange(new Control[] { btnOk, btnCancel });
      table.Controls.Add(panelButtons, 0, 5);
      table.SetColumnSpan(panelButtons, 2);

      // Data
      txtColumnName.Text = def.ColumnName ?? string.Empty;

      // Always populate target table choices from breakdown metrics
      cboTargetTable.Items.Clear();
      var breakdownTables = new[] { "Products", "Parts", "Subassemblies", "Sheets" };
      foreach (var t in breakdownTables) cboTargetTable.Items.Add(t);
      cboTargetTable.Text = def.TargetTableName ?? string.Empty;

      // Disable target columns until a target table is set
      cboTargetKey.Enabled = false;
      cboTargetValue.Enabled = false;

      // Helper to get columns for a table: prefer live schema, fallback to user config column order
      List<string> GetColumnsForTable(string t)
      {
        if (string.IsNullOrWhiteSpace(t)) return new List<string>();
        if (columnsByTable != null && columnsByTable.TryGetValue(t, out var colsA) && colsA != null && colsA.Count > 0)
          return new List<string>(colsA);
        try
        {
          var cfgCols = UserConfig.LoadOrDefault().TryGetColumnOrder(t);
          if (cfgCols != null && cfgCols.Count > 0) return cfgCols;
        }
        catch { }
        return new List<string>();
      }

      // Local key options: from current table (schema or fallback), only LinkID*
      cboLocalKey.Items.Clear();
      foreach (var c in GetColumnsForTable(def.TableName))
      {
        if (c.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) cboLocalKey.Items.Add(c);
      }

      // Helper to fill target columns (works with or without DB)
      void FillTargetColumns(string targetTable)
      {
        cboTargetKey.Items.Clear();
        cboTargetValue.Items.Clear();
        var cols = GetColumnsForTable(targetTable);
        if (!string.IsNullOrWhiteSpace(targetTable) && cols.Count > 0)
        {
          foreach (var c in cols)
          {
            if (c.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) cboTargetKey.Items.Add(c);
            cboTargetValue.Items.Add(c);
          }
          cboTargetKey.Enabled = true;
          cboTargetValue.Enabled = true;
        }
        else
        {
          cboTargetKey.Enabled = false;
          cboTargetValue.Enabled = false;
        }
      }

      // Wire up target table selection events
      cboTargetTable.SelectedIndexChanged += (s, e) => FillTargetColumns(cboTargetTable.Text);
      cboTargetTable.TextChanged += (s, e) => FillTargetColumns(cboTargetTable.Text);

      // Initial population
      FillTargetColumns(cboTargetTable.Text);

      cboLocalKey.Text = def.LocalKeyColumn ?? string.Empty;
      cboTargetKey.Text = def.TargetKeyColumn ?? string.Empty;
      cboTargetValue.Text = def.TargetValueColumn ?? string.Empty;

      btnOk.Click += (s, e) =>
      {
        if (!ValidateInputs()) { DialogResult = DialogResult.None; return; }
        def.ColumnName = (txtColumnName.Text ?? string.Empty).Trim();
        def.LocalKeyColumn = (cboLocalKey.Text ?? string.Empty).Trim();
        def.TargetTableName = (cboTargetTable.Text ?? string.Empty).Trim();
        def.TargetKeyColumn = (cboTargetKey.Text ?? string.Empty).Trim();
        def.TargetValueColumn = (cboTargetValue.Text ?? string.Empty).Trim();
      };
    }

    private bool ValidateInputs()
    {
      if (string.IsNullOrWhiteSpace(txtColumnName.Text)) { MessageBox.Show("Column name is required."); return false; }
      if (string.IsNullOrWhiteSpace(cboLocalKey.Text)) { MessageBox.Show("Local key column is required."); return false; }
      if (!cboLocalKey.Text.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Local key column must start with 'LinkID'."); return false; }
      if (string.IsNullOrWhiteSpace(cboTargetTable.Text)) { MessageBox.Show("Target table is required."); return false; }
      if (string.IsNullOrWhiteSpace(cboTargetKey.Text)) { MessageBox.Show("Target key column is required."); return false; }
      if (!cboTargetKey.Text.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Target key column must start with 'LinkID'."); return false; }
      if (string.IsNullOrWhiteSpace(cboTargetValue.Text)) { MessageBox.Show("Target value column is required."); return false; }
      return true;
    }
  }
}
