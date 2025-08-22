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
  public sealed class VirtualColumnsDialog : Form
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

    private readonly BindingList<UserConfig.VirtualColumnDef> defs;
    private readonly Dictionary<string, List<string>> columnsByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    public VirtualColumnsDialog(string tableName, SqlCeConnection connection, bool showFromConsolidated)
      : this(tableName, connection, showFromConsolidated, null)
    {
    }

    public VirtualColumnsDialog(string tableName, SqlCeConnection connection, bool showFromConsolidated, Dictionary<string, List<string>> preloadedColumns)
    {
      this.tableName = tableName;
      this.connection = connection;
      this.hasDatabaseContext = showFromConsolidated && connection != null && connection.State == ConnectionState.Open;

      Text = "Virtual Columns for " + tableName;
      StartPosition = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.Sizable;
      MinimizeBox = false;
      MaximizeBox = false;
      Width = 800;
      Height = 420;

      defs = new BindingList<UserConfig.VirtualColumnDef>(UserConfig.LoadOrDefault().GetVirtualColumnsForTable(tableName));

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

      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Column Name", DataPropertyName = nameof(UserConfig.VirtualColumnDef.ColumnName), Name = nameof(UserConfig.VirtualColumnDef.ColumnName), Width = 120 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", DataPropertyName = nameof(UserConfig.VirtualColumnDef.ColumnType), Name = nameof(UserConfig.VirtualColumnDef.ColumnType), Width = 80 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action Type", DataPropertyName = nameof(UserConfig.VirtualColumnDef.ActionType), Name = nameof(UserConfig.VirtualColumnDef.ActionType), Width = 100 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Target Table", DataPropertyName = nameof(UserConfig.VirtualColumnDef.TargetTableName), Name = nameof(UserConfig.VirtualColumnDef.TargetTableName), Width = 120 });
      grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Configuration", DataPropertyName = "ConfigurationSummary", Name = "ConfigurationSummary", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

      // Add virtual ConfigurationSummary property to show relevant info based on column type
      grid.CellFormatting += Grid_CellFormatting;
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

    private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
      if (e.ColumnIndex == grid.Columns["ConfigurationSummary"].Index && e.RowIndex >= 0)
      {
        if (grid.Rows[e.RowIndex].DataBoundItem is UserConfig.VirtualColumnDef def)
        {
          if (def.IsActionColumn)
          {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(def.ButtonText)) parts.Add($"Button: '{def.ButtonText}'");
            if (!string.IsNullOrEmpty(def.LocalKeyColumn)) parts.Add($"Key: {def.LocalKeyColumn}");
            e.Value = parts.Count > 0 ? string.Join(", ", parts) : "Action column";
          }
          else if (def.IsLookupColumn)
          {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(def.LocalKeyColumn)) parts.Add($"Local: {def.LocalKeyColumn}");
            if (!string.IsNullOrEmpty(def.TargetKeyColumn)) parts.Add($"Target: {def.TargetKeyColumn}");
            if (!string.IsNullOrEmpty(def.TargetValueColumn)) parts.Add($"Value: {def.TargetValueColumn}");
            e.Value = parts.Count > 0 ? string.Join(" → ", parts) : "Lookup column";
          }
          else
          {
            e.Value = "Unknown type";
          }
          e.FormattingApplied = true;
        }
      }
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
      var def = new UserConfig.VirtualColumnDef
      {
        TableName = tableName,
        ColumnName = "",
        ColumnType = "Lookup", // Default to lookup
        LocalKeyColumn = "",
        TargetTableName = "",
        TargetKeyColumn = "",
        TargetValueColumn = "",
        ActionType = "",
        ButtonText = "",
        ButtonIcon = ""
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
      var copy = new UserConfig.VirtualColumnDef
      {
        TableName = current.TableName,
        ColumnName = current.ColumnName,
        ColumnType = current.ColumnType,
        LocalKeyColumn = current.LocalKeyColumn,
        TargetTableName = current.TargetTableName,
        TargetKeyColumn = current.TargetKeyColumn,
        TargetValueColumn = current.TargetValueColumn,
        ActionType = current.ActionType,
        ButtonText = current.ButtonText,
        ButtonIcon = current.ButtonIcon
      };
      if (EditEntry(copy))
      {
        current.ColumnName = copy.ColumnName;
        current.ColumnType = copy.ColumnType;
        current.LocalKeyColumn = copy.LocalKeyColumn;
        current.TargetTableName = copy.TargetTableName;
        current.TargetKeyColumn = copy.TargetKeyColumn;
        current.TargetValueColumn = copy.TargetValueColumn;
        current.ActionType = copy.ActionType;
        current.ButtonText = copy.ButtonText;
        current.ButtonIcon = copy.ButtonIcon;
        grid.Refresh();
      }
    }

    private void RemoveSelected()
    {
      var current = GetSelected();
      if (current == null) return;
      defs.Remove(current);
    }

    private UserConfig.VirtualColumnDef GetSelected()
    {
      if (grid.CurrentRow?.DataBoundItem is UserConfig.VirtualColumnDef d) return d;
      return null;
    }

    private bool EditEntry(UserConfig.VirtualColumnDef def)
    {
      using (var dlg = new EditVirtualColumnDialog(def, columnsByTable, hasDatabaseContext))
      {
        return dlg.ShowDialog(this) == DialogResult.OK;
      }
    }

    private void SaveAndClose()
    {
      var cfg = UserConfig.LoadOrDefault();
      // Remove all for this table, then add the current list
      cfg.VirtualColumns.RemoveAll(e => string.Equals(e.TableName, tableName, StringComparison.OrdinalIgnoreCase));
      foreach (var d in defs)
      {
        // Basic validation
        if (string.IsNullOrWhiteSpace(d.ColumnName)) continue;
        d.TableName = tableName;
        cfg.UpsertVirtualColumn(d);
      }
      cfg.Save();
      DialogResult = DialogResult.OK;
      Close();
    }
  }

  internal sealed class EditVirtualColumnDialog : Form
  {
    private readonly TextBox txtColumnName;
    private readonly ComboBox cboColumnType;

    // Lookup column controls
    private ComboBox cboLocalKey;
    private ComboBox cboTargetTable;
    private ComboBox cboTargetKey;
    private ComboBox cboTargetValue;
    private readonly Panel pnlLookupFields;

    // Action column controls
    private ComboBox cboActionType;
    private TextBox txtButtonText;
    private ComboBox cboActionLocalKey;
    private readonly Panel pnlActionFields;

    private readonly Button btnOk;
    private readonly Button btnCancel;
    private readonly Dictionary<string, List<string>> columnsByTable;
    private readonly bool hasDb;
    private readonly UserConfig.VirtualColumnDef def;

    public EditVirtualColumnDialog(UserConfig.VirtualColumnDef def, Dictionary<string, List<string>> columnsByTable, bool hasDb)
    {
      this.def = def;
      this.columnsByTable = columnsByTable ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
      this.hasDb = hasDb;

      Text = "Edit Virtual Column";
      StartPosition = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MinimizeBox = false;
      MaximizeBox = false;
      Width = 520;
      Height = 280;

      var mainTable = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 6,
        Padding = new Padding(8),
      };
      mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Column name
      mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Column type
      mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Dynamic content panel
      mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
      Controls.Add(mainTable);

      // Column name
      mainTable.Controls.Add(new Label { Text = "Column name:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
      txtColumnName = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
      mainTable.Controls.Add(txtColumnName, 1, 0);

      // Column type
      mainTable.Controls.Add(new Label { Text = "Column type:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
      cboColumnType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboColumnType.Items.AddRange(new[] { "Lookup", "Action" });
      cboColumnType.SelectedIndexChanged += CboColumnType_SelectedIndexChanged;
      mainTable.Controls.Add(cboColumnType, 1, 1);

      // Create panels for different column types
      var contentPanel = new Panel { Dock = DockStyle.Fill };
      mainTable.Controls.Add(contentPanel, 0, 2);
      mainTable.SetColumnSpan(contentPanel, 2);

      // Lookup fields panel
      pnlLookupFields = CreateLookupPanel();
      contentPanel.Controls.Add(pnlLookupFields);

      // Action fields panel
      pnlActionFields = CreateActionPanel();
      contentPanel.Controls.Add(pnlActionFields);

      // Button panel
      var panelButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
      btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
      btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
      panelButtons.Controls.AddRange(new Control[] { btnOk, btnCancel });
      mainTable.Controls.Add(panelButtons, 0, 3);
      mainTable.SetColumnSpan(panelButtons, 2);

      // Initialize data
      InitializeData();

      AcceptButton = btnOk;
      CancelButton = btnCancel;
    }

    private void InitializeData()
    {
      txtColumnName.Text = def.ColumnName ?? string.Empty;
      cboColumnType.Text = def.ColumnType ?? "Lookup";

      // Initialize action fields
      cboActionType.Text = def.ActionType ?? "";
      txtButtonText.Text = def.ButtonText ?? "";

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
      var localKeyColumns = GetColumnsForTable(def.TableName).Where(c => c.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)).ToList();

      cboLocalKey.Items.Clear();
      cboActionLocalKey.Items.Clear();
      foreach (var c in localKeyColumns)
      {
        cboLocalKey.Items.Add(c);
        cboActionLocalKey.Items.Add(c);
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

      // Wire up action type selection events
      cboActionType.SelectedIndexChanged += (s, e) => SetDefaultButtonText();

      // Initial population
      FillTargetColumns(cboTargetTable.Text);

      // Load existing values
      cboLocalKey.Text = def.LocalKeyColumn ?? string.Empty;
      cboActionLocalKey.Text = def.LocalKeyColumn ?? string.Empty;
      cboTargetKey.Text = def.TargetKeyColumn ?? string.Empty;
      cboTargetValue.Text = def.TargetValueColumn ?? string.Empty;

      // Set initial panel visibility
      CboColumnType_SelectedIndexChanged(cboColumnType, EventArgs.Empty);

      btnOk.Click += (s, e) =>
      {
        if (!ValidateInputs()) { DialogResult = DialogResult.None; return; }
        def.ColumnName = (txtColumnName.Text ?? string.Empty).Trim();
        def.ColumnType = (cboColumnType.Text ?? string.Empty).Trim();

        if (def.IsActionColumn)
        {
          def.ActionType = (cboActionType.Text ?? string.Empty).Trim();
          def.ButtonText = (txtButtonText.Text ?? string.Empty).Trim();
          def.LocalKeyColumn = (cboActionLocalKey.Text ?? string.Empty).Trim();
          // Clear lookup-specific fields for action columns
          def.TargetTableName = "";
          def.TargetKeyColumn = "";
          def.TargetValueColumn = "";
        }
        else
        {
          def.LocalKeyColumn = (cboLocalKey.Text ?? string.Empty).Trim();
          def.TargetTableName = (cboTargetTable.Text ?? string.Empty).Trim();
          def.TargetKeyColumn = (cboTargetKey.Text ?? string.Empty).Trim();
          def.TargetValueColumn = (cboTargetValue.Text ?? string.Empty).Trim();
          // Clear action-specific fields for lookup columns
          def.ActionType = "";
          def.ButtonText = "";
        }
      };
    }

    private bool ValidateInputs()
    {
      if (string.IsNullOrWhiteSpace(txtColumnName.Text)) { MessageBox.Show("Column name is required."); return false; }

      var isAction = cboColumnType.Text == "Action";

      if (isAction)
      {
        // Validate action column
        if (string.IsNullOrWhiteSpace(cboActionType.Text)) { MessageBox.Show("Action type is required."); return false; }
        if (string.IsNullOrWhiteSpace(txtButtonText.Text)) { MessageBox.Show("Button text is required."); return false; }
        if (string.IsNullOrWhiteSpace(cboActionLocalKey.Text)) { MessageBox.Show("Key column is required."); return false; }
        if (!cboActionLocalKey.Text.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Key column must start with 'LinkID'."); return false; }
      }
      else
      {
        // Validate lookup column
        if (string.IsNullOrWhiteSpace(cboLocalKey.Text)) { MessageBox.Show("Local key column is required."); return false; }
        if (!cboLocalKey.Text.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Local key column must start with 'LinkID'."); return false; }
        if (string.IsNullOrWhiteSpace(cboTargetTable.Text)) { MessageBox.Show("Target table is required."); return false; }
        if (string.IsNullOrWhiteSpace(cboTargetKey.Text)) { MessageBox.Show("Target key column is required."); return false; }
        if (!cboTargetKey.Text.StartsWith("LinkID", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Target key column must start with 'LinkID'."); return false; }
        if (string.IsNullOrWhiteSpace(cboTargetValue.Text)) { MessageBox.Show("Target value column is required."); return false; }
      }

      return true;
    }

    private Panel CreateLookupPanel()
    {
      var panel = new Panel { Dock = DockStyle.Fill, Visible = false };

      var table = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 4,
        Padding = new Padding(4),
      };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      for (int i = 0; i < 4; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      panel.Controls.Add(table);

      table.Controls.Add(new Label { Text = "Local key column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
      cboLocalKey = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboLocalKey.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboLocalKey.AutoCompleteSource = AutoCompleteSource.ListItems;
      table.Controls.Add(cboLocalKey, 1, 0);

      table.Controls.Add(new Label { Text = "Target table:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
      cboTargetTable = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboTargetTable.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboTargetTable.AutoCompleteSource = AutoCompleteSource.ListItems;
      table.Controls.Add(cboTargetTable, 1, 1);

      table.Controls.Add(new Label { Text = "Target key column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
      cboTargetKey = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboTargetKey.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboTargetKey.AutoCompleteSource = AutoCompleteSource.ListItems;
      table.Controls.Add(cboTargetKey, 1, 2);

      table.Controls.Add(new Label { Text = "Target value column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
      cboTargetValue = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboTargetValue.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboTargetValue.AutoCompleteSource = AutoCompleteSource.ListItems;
      table.Controls.Add(cboTargetValue, 1, 3);

      return panel;
    }

    private Panel CreateActionPanel()
    {
      var panel = new Panel { Dock = DockStyle.Fill, Visible = false };

      var table = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 3,
        Padding = new Padding(4),
      };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      for (int i = 0; i < 3; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      panel.Controls.Add(table);

      table.Controls.Add(new Label { Text = "Action type:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
      cboActionType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboActionType.Items.AddRange(new[] { "3DViewer", "WebLink", "Export", "Custom" });
      table.Controls.Add(cboActionType, 1, 0);

      table.Controls.Add(new Label { Text = "Button text:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
      txtButtonText = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
      table.Controls.Add(txtButtonText, 1, 1);

      table.Controls.Add(new Label { Text = "Key column:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
      cboActionLocalKey = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      cboActionLocalKey.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
      cboActionLocalKey.AutoCompleteSource = AutoCompleteSource.ListItems;
      table.Controls.Add(cboActionLocalKey, 1, 2);

      return panel;
    }

    private void CboColumnType_SelectedIndexChanged(object sender, EventArgs e)
    {
      var isAction = cboColumnType.SelectedItem?.ToString() == "Action";
      pnlLookupFields.Visible = !isAction;
      pnlActionFields.Visible = isAction;

      // Set helpful defaults for button text based on action type
      if (isAction && cboActionType.SelectedItem != null)
      {
        SetDefaultButtonText();
      }
    }

    private void SetDefaultButtonText()
    {
      if (string.IsNullOrEmpty(txtButtonText.Text))
      {
        switch (cboActionType.SelectedItem?.ToString())
        {
          case "3DViewer":
            txtButtonText.Text = "🎯 3D View";
            break;
          case "WebLink":
            txtButtonText.Text = "🔗 Link";
            break;
          case "Export":
            txtButtonText.Text = "💾 Export";
            break;
          default:
            txtButtonText.Text = "▶ Action";
            break;
        }
      }
    }
  }
}
