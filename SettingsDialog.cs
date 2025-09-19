using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WorkOrderBlender
{
  public partial class SettingsDialog : Form
  {
    public string DefaultRoot { get; set; }
    public string SdfFileName { get; set; }
    public bool HidePurchasing { get; set; }
    public bool DynamicSheetCosts { get; set; }
    public List<string> FrontFilterKeywords { get; set; }
    public List<string> SubassemblyFilterKeywords { get; set; }

    // Event for Check for Updates functionality
    public event EventHandler CheckForUpdatesRequested;

    private bool isInitialized = false;

    public SettingsDialog()
    {
      InitializeComponent();
      LoadSettings();
    }

    private void InitializeComponent()
    {
      this.SuspendLayout();

      this.Text = "Settings";
      this.StartPosition = FormStartPosition.CenterParent;
      this.Width = 700;
      this.Height = 275;
      this.MinimizeBox = false;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.Sizable;

      var table = new TableLayoutPanel
      {
        Dock = DockStyle.Top,
        ColumnCount = 4,
        RowCount = 5,
        Padding = new Padding(10, 10, 10, 10),
        Height = 175,
      };
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

      this.Controls.Add(table);

      // Work Order Directory
      var lblRoot = new Label { Text = "Work Order Directory:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtRootLocal = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 400 };
      var btnBrowseRoot = new Button { Text = "Browse...", AutoSize = true };
      btnBrowseRoot.Click += (s, e) =>
      {
        using (var fbd = new FolderBrowserDialog())
        {
          fbd.SelectedPath = txtRootLocal.Text;
          if (fbd.ShowDialog(this) == DialogResult.OK)
          {
            txtRootLocal.Text = fbd.SelectedPath;
          }
        }
      };

      // .sdf File Name
      var lblSdf = new Label { Text = ".sdf File Name:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtSdfLocal = new TextBox
      {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Width = 400
      };

      // Hide Purchasing option
      var chkHidePurchasing = new CheckBox
      {
        Text = "Hide Purchasing",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Tag = "HidePurchasing"
      };
      var toolTipHidePurchasing = new ToolTip();
      toolTipHidePurchasing.SetToolTip(chkHidePurchasing, "Hide purchasing work orders from the list.");

      // Dynamic Sheet Costs option
      var chkDynamicSheetCosts = new CheckBox
      {
        Text = "Dynamic Sheet Costs",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Tag = "DynamicSheetCosts"
      };
      var toolTipDynamicSheetCosts = new ToolTip();
      toolTipDynamicSheetCosts.SetToolTip(
        chkDynamicSheetCosts,
        "If checked, dynamic sheet costs will be calculated based on the width, length, " +
        "and thickness of the sheet. This will replace values we may have added in the database."
      );

      // Front Filter Keywords
      var lblFrontFilter = new Label { Text = "Parts Front Filter Keywords:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtFrontFilter = new TextBox
      {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Width = 400
      };
      var toolTipFrontFilter = new ToolTip();
      toolTipFrontFilter.SetToolTip(txtFrontFilter, "Comma-separated keywords to filter front parts. Parts with names containing any of these keywords will be shown when the Parts Fronts filter is active.");

      // Subassembly Filter Keywords
      var lblSubassemblyFilter = new Label { Text = "Subassembly Front Filter Keywords:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtSubassemblyFilter = new TextBox
      {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Width = 400
      };
      var toolTipSubassemblyFilter = new ToolTip();
      toolTipSubassemblyFilter.SetToolTip(txtSubassemblyFilter, "Comma-separated keywords to filter front subassemblies. Subassemblies with names containing any of these keywords will be shown when the Subassembly Fronts filter is active.");

      // Layout rows
      table.Controls.Add(lblRoot, 0, 0);
      table.Controls.Add(txtRootLocal, 1, 0);
      table.Controls.Add(btnBrowseRoot, 2, 0);
      table.SetColumnSpan(txtRootLocal, 1);

      table.Controls.Add(lblSdf, 0, 1);
      table.Controls.Add(txtSdfLocal, 1, 1);
      table.SetColumnSpan(txtSdfLocal, 2);

      table.Controls.Add(lblFrontFilter, 0, 2);
      table.Controls.Add(txtFrontFilter, 1, 2);
      table.SetColumnSpan(txtFrontFilter, 2);

      table.Controls.Add(lblSubassemblyFilter, 0, 3);
      table.Controls.Add(txtSubassemblyFilter, 1, 3);
      table.SetColumnSpan(txtSubassemblyFilter, 2);

      // Add Hide Purchasing on its own row spanning available columns
      table.Controls.Add(chkHidePurchasing, 0, 4);
      table.SetColumnSpan(chkHidePurchasing, 3);

      // Add Dynamic Sheet Costs on its own row spanning available columns
      table.Controls.Add(chkDynamicSheetCosts, 0, 5);
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
      this.Controls.Add(panelButtons);

      // Store controls for later access
      this.txtRootLocal = txtRootLocal;
      this.txtSdfLocal = txtSdfLocal;
      this.chkHidePurchasing = chkHidePurchasing;
      this.chkDynamicSheetCosts = chkDynamicSheetCosts;
      this.txtFrontFilter = txtFrontFilter;
      this.txtSubassemblyFilter = txtSubassemblyFilter;

      this.ResumeLayout(false);
    }

    private void LoadSettings()
    {
      if (isInitialized) return; // Prevent multiple loads

      var cfg = UserConfig.LoadOrDefault();
      DefaultRoot = cfg.DefaultRoot ?? string.Empty;
      SdfFileName = cfg.SdfFileName ?? string.Empty;
      HidePurchasing = cfg.HidePurchasing;
      DynamicSheetCosts = cfg.DynamicSheetCosts;

      // Clean up any existing duplicated values in the configuration
      FrontFilterKeywords = (cfg.FrontFilterKeywords ?? new List<string> { "Slab", "Drawer Front" })
        .Distinct()
        .ToList();
      SubassemblyFilterKeywords = (cfg.SubassemblyFilterKeywords ?? new List<string> { "Door", "Drawer Front", "RPE" })
        .Distinct()
        .ToList();

      // Update UI controls - ensure clean values without duplication
      if (txtRootLocal != null) txtRootLocal.Text = DefaultRoot;
      if (txtSdfLocal != null) txtSdfLocal.Text = SdfFileName;
      if (chkHidePurchasing != null) chkHidePurchasing.Checked = HidePurchasing;
      if (chkDynamicSheetCosts != null) chkDynamicSheetCosts.Checked = DynamicSheetCosts;

      // Set filter text fields with proper values, ensuring no duplication
      if (txtFrontFilter != null)
      {
        txtFrontFilter.Text = string.Join(", ", FrontFilterKeywords);
      }
      if (txtSubassemblyFilter != null)
      {
        txtSubassemblyFilter.Text = string.Join(", ", SubassemblyFilterKeywords);
      }

      isInitialized = true;
    }

    public void SaveSettings()
    {
      var cfg = UserConfig.LoadOrDefault();
      cfg.DefaultRoot = (txtRootLocal?.Text ?? string.Empty).Trim();
      cfg.SdfFileName = (txtSdfLocal?.Text ?? string.Empty).Trim();
      cfg.HidePurchasing = chkHidePurchasing?.Checked ?? true;
      cfg.DynamicSheetCosts = chkDynamicSheetCosts?.Checked ?? false;

      // Parse and save front filter keywords - remove duplicates
      var frontFilterText = (txtFrontFilter?.Text ?? string.Empty).Trim();
      if (!string.IsNullOrEmpty(frontFilterText))
      {
        cfg.FrontFilterKeywords = frontFilterText.Split(',')
          .Select(k => k.Trim())
          .Where(k => !string.IsNullOrEmpty(k))
          .Distinct() // Remove duplicates
          .ToList();
      }
      // If empty, don't change the existing saved values - just leave them as they are

      // Parse and save subassembly filter keywords - remove duplicates
      var subassemblyFilterText = (txtSubassemblyFilter?.Text ?? string.Empty).Trim();
      if (!string.IsNullOrEmpty(subassemblyFilterText))
      {
        cfg.SubassemblyFilterKeywords = subassemblyFilterText.Split(',')
          .Select(k => k.Trim())
          .Where(k => !string.IsNullOrEmpty(k))
          .Distinct() // Remove duplicates
          .ToList();
      }
      // If empty, don't change the existing saved values - just leave them as they are

      cfg.Save();
    }

    private void CheckForUpdates_Click(object sender, EventArgs e)
    {
      // Raise the event to let MainForm handle the Check for Updates functionality
      CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
    }

    // Control references for access from methods
    private TextBox txtRootLocal;
    private TextBox txtSdfLocal;
    private CheckBox chkHidePurchasing;
    private CheckBox chkDynamicSheetCosts;
    private TextBox txtFrontFilter;
    private TextBox txtSubassemblyFilter;
  }
}
