using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkOrderBlender
{
  public partial class SettingsDialog : Form
  {
    public string DefaultRoot { get; set; }
    public string SdfFileName { get; set; }
    public bool HidePurchasing { get; set; }
    public bool DynamicSheetCosts { get; set; }
    public bool AllowLogging { get; set; }
    public List<string> FrontFilterKeywords { get; set; }
    public List<string> SubassemblyFilterKeywords { get; set; }

    // MSSQL connection settings
    public string MssqlServer { get; set; }
    public string MssqlDatabase { get; set; }
    public string MssqlUsername { get; set; }
    public string MssqlPassword { get; set; }
    public bool MssqlEnabled { get; set; }

    // Saw Queue directories
    public string StagingDir { get; set; }
    public string ReleaseDir { get; set; }
    public int MaxTrackedFiles { get; set; }

    // Event for Check for Updates functionality
    public event EventHandler CheckForUpdatesRequested;

    private bool isInitialized = false;
    private bool hasUnsavedChanges = false;

    // Backup properties to track original values for discard functionality
    private string originalDefaultRoot;
    private string originalSdfFileName;
    private bool originalHidePurchasing;
    private bool originalDynamicSheetCosts;
    private bool originalAllowLogging;
    private List<string> originalFrontFilterKeywords;
    private List<string> originalSubassemblyFilterKeywords;
    private string originalMssqlServer;
    private string originalMssqlDatabase;
    private string originalMssqlUsername;
    private string originalMssqlPassword;
    private bool originalMssqlEnabled;
    private string originalStagingDir;
    private string originalReleaseDir;
    private int originalMaxTrackedFiles;

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
      this.Height = 550;
      this.MinimizeBox = false;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.Sizable;

      var table = new TableLayoutPanel
      {
        Dock = DockStyle.Top,
        ColumnCount = 4,
        RowCount = 16,
        Padding = new Padding(10, 10, 10, 10),
        Height = this.ClientSize.Height - 80,
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
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Saw Queue Header
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Staging Dir
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Release Dir
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Max Tracked Files

      table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 100% vertical spacer
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // MSSQL Header
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // MSSQL Enable
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // MSSQL Server
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // MSSQL Database
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // MSSQL Username
      table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // MSSQL Password
      table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 100% vertical spacer

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

      // Allow Logging option
      var chkAllowLogging = new CheckBox
      {
        Text = "Allow Logging",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Tag = "AllowLogging"
      };
      var toolTipAllowLogging = new ToolTip();
      toolTipAllowLogging.SetToolTip(
        chkAllowLogging,
        "If unchecked, WorkOrderBlender will not create or write to logs\\WorkOrderBlender.log."
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
      table.SetColumnSpan(chkHidePurchasing, 1);

      // Add Dynamic Sheet Costs on its own row spanning available columns
      table.Controls.Add(chkDynamicSheetCosts, 1, 4);
      table.SetColumnSpan(chkDynamicSheetCosts, 1);

      // Add Allow Logging to the right of Dynamic Sheet Costs
      table.Controls.Add(chkAllowLogging, 2, 4);
      table.SetColumnSpan(chkAllowLogging, 1);

      // Saw Queue Settings Section
      var lblSawQueueHeader = new Label
      {
        Text = "Saw Queue Settings:",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        // Add top margin to visually separate the Saw Queue section from above controls
        Margin = new Padding(0, 16, 0, 0),
        Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
      };
      table.Controls.Add(lblSawQueueHeader, 0, 5);
      table.SetColumnSpan(lblSawQueueHeader, 4);

      // Saw Queue Staging Directory
      var lblStagingDir = new Label { Text = "Saw Queue Staging Dir:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtStagingDir = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 400 };
      var btnBrowseStagingDir = new Button { Text = "Browse...", AutoSize = true };
      btnBrowseStagingDir.Click += (s, e) =>
      {
        using (var fbd = new FolderBrowserDialog())
        {
          fbd.SelectedPath = txtStagingDir.Text;
          if (fbd.ShowDialog(this) == DialogResult.OK)
          {
            txtStagingDir.Text = fbd.SelectedPath;
          }
        }
      };
      table.Controls.Add(lblStagingDir, 0, 6);
      table.Controls.Add(txtStagingDir, 1, 6);
      table.Controls.Add(btnBrowseStagingDir, 2, 6);
      table.SetColumnSpan(txtStagingDir, 1);

      // Saw Queue Release Directory
      var lblReleaseDir = new Label { Text = "Saw Queue Release Dir:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtReleaseDir = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 400 };
      var btnBrowseReleaseDir = new Button { Text = "Browse...", AutoSize = true };
      btnBrowseReleaseDir.Click += (s, e) =>
      {
        using (var fbd = new FolderBrowserDialog())
        {
          fbd.SelectedPath = txtReleaseDir.Text;
          if (fbd.ShowDialog(this) == DialogResult.OK)
          {
            txtReleaseDir.Text = fbd.SelectedPath;
          }
        }
      };
      table.Controls.Add(lblReleaseDir, 0, 7);
      table.Controls.Add(txtReleaseDir, 1, 7);
      table.Controls.Add(btnBrowseReleaseDir, 2, 7);
      table.SetColumnSpan(txtReleaseDir, 1);

      // Saw Queue Max Tracked Files
      var lblMaxTrackedFiles = new Label { Text = "Max Released Files in Saw Queue:", AutoSize = true, Anchor = AnchorStyles.Left };
      var numMaxTrackedFiles = new NumericUpDown
      {
        Minimum = 1,
        Maximum = 10000,
        Value = 200,
        Anchor = AnchorStyles.Left,
        Width = 100
      };
      table.Controls.Add(lblMaxTrackedFiles, 0, 8);
      table.Controls.Add(numMaxTrackedFiles, 1, 8);

      // Empty row for spacing between Saw Queue and MSSQL sections
      // Row 9 is intentionally left empty

      // MSSQL Connection Settings Section
      var lblMssqlHeader = new Label
      {
        Text = "MSSQL Database Connection (Microvellum):",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        // Add top margin to visually separate the MSSQL section from above controls
        Margin = new Padding(0, 16, 0, 0),
        Padding = new Padding(0, 16, 0, 0),
        Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
      };
      table.Controls.Add(lblMssqlHeader, 0, 10);
      table.SetColumnSpan(lblMssqlHeader, 4);

      // MSSQL Enable checkbox
      var chkMssqlEnabled = new CheckBox
      {
        Text = "Enable MSSQL work order name validation",
        AutoSize = true,
        Anchor = AnchorStyles.Left
      };
      table.Controls.Add(chkMssqlEnabled, 0, 11);
      table.SetColumnSpan(chkMssqlEnabled, 4);

      // MSSQL Server
      var lblMssqlServer = new Label { Text = "Server:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtMssqlServer = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 200 };
      table.Controls.Add(lblMssqlServer, 0, 12);
      table.Controls.Add(txtMssqlServer, 1, 12);
      table.SetColumnSpan(txtMssqlServer, 3);

      // MSSQL Database
      var lblMssqlDatabase = new Label { Text = "Database:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtMssqlDatabase = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 200 };
      table.Controls.Add(lblMssqlDatabase, 0, 13);
      table.Controls.Add(txtMssqlDatabase, 1, 13);
      table.SetColumnSpan(txtMssqlDatabase, 3);

      // MSSQL Username
      var lblMssqlUsername = new Label { Text = "Username:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtMssqlUsername = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 200 };
      table.Controls.Add(lblMssqlUsername, 0, 14);
      table.Controls.Add(txtMssqlUsername, 1, 14);
      table.SetColumnSpan(txtMssqlUsername, 3);

      // MSSQL Password
      var lblMssqlPassword = new Label { Text = "Password:", AutoSize = true, Anchor = AnchorStyles.Left };
      var txtMssqlPassword = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 200, UseSystemPasswordChar = true };
      table.Controls.Add(lblMssqlPassword, 0, 15);
      table.Controls.Add(txtMssqlPassword, 1, 15);
      table.SetColumnSpan(txtMssqlPassword, 3);

      // Store control references
      this.chkMssqlEnabled = chkMssqlEnabled;
      this.txtMssqlServer = txtMssqlServer;
      this.txtMssqlDatabase = txtMssqlDatabase;
      this.txtMssqlUsername = txtMssqlUsername;
      this.txtMssqlPassword = txtMssqlPassword;

      // Buttons row with Check Updates and Test MSSQL on left, OK/Cancel on right
      var btnCheckUpdates = new Button
      {
        Text = "Check for Updates",
        AutoSize = true,
        BackColor = System.Drawing.Color.FromArgb(0, 123, 255), // Bootstrap primary blue
        ForeColor = System.Drawing.Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 140
      };

      // Customize the flat button appearance
      btnCheckUpdates.FlatAppearance.BorderSize = 1;
      btnCheckUpdates.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(38, 143, 255); // 15% lighter blue on hover
      btnCheckUpdates.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(77, 163, 255); // 15% lighter than previous click color
      btnCheckUpdates.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 105, 217); // 20% darker than original blue
      btnCheckUpdates.Click += CheckForUpdates_Click;

      var btnTestMssql = new Button
      {
        Text = "Test MSSQL Connection",
        AutoSize = true,
        BackColor = System.Drawing.Color.FromArgb(255, 193, 7), // Bootstrap warning yellow
        ForeColor = System.Drawing.Color.Black,
        FlatStyle = FlatStyle.Flat,
        Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 160
      };

      // Customize the flat button appearance
      btnTestMssql.FlatAppearance.BorderSize = 1;
      btnTestMssql.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(255, 217, 70); // 25% lighter yellow on hover
      btnTestMssql.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(255, 229, 128); // 50% lighter yellow on click
      btnTestMssql.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(216, 163, 6); // 15% darker than original yellow
      btnTestMssql.Click += TestMssqlConnection_Click;

      var panelButtons = new TableLayoutPanel
      {
        Dock = DockStyle.Bottom,
        ColumnCount = 5,
        RowCount = 1,
        Padding = new Padding(10, 5, 10, 5),
        Height = 50,
      };
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Check Updates
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Test MSSQL
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // spacer
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Save
      panelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Discard

      var btnSave = new Button
      {
        Text = "Save and Close",
        AutoSize = true,
        BackColor = System.Drawing.Color.FromArgb(46, 125, 50), // Material Design Green
        ForeColor = System.Drawing.Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
        Visible = false,
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 120
      };

      // Customize the flat button appearance
      btnSave.FlatAppearance.BorderSize = 1;
      btnSave.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(82, 168, 86); // 15% lighter green on hover
      btnSave.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(56, 142, 60); // 15% lighter than previous click color
      btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(37, 100, 40); // 20% darker than original green

      var btnDiscard = new Button
      {
        Text = "Close",
        AutoSize = true,
        BackColor = System.Drawing.Color.FromArgb(173, 179, 184), // 20% lighter than Bootstrap secondary gray
        ForeColor = System.Drawing.Color.Black,
        FlatStyle = FlatStyle.Flat,
        Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 80,
      };

      // Customize the flat button appearance
      btnDiscard.FlatAppearance.BorderSize = 1;
      btnDiscard.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(196, 201, 205); // 15% lighter than backcolor
      btnDiscard.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(217, 221, 224); // 15% lighter than MouseOverBackColor
      btnDiscard.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(138, 143, 147); // 20% darker than backcolor

      // Wire up button events
      btnSave.Click += SaveButton_Click;
      btnDiscard.Click += DiscardButton_Click;

      panelButtons.Controls.Add(btnCheckUpdates, 0, 0); // Far left
      panelButtons.Controls.Add(btnTestMssql, 1, 0); // Second from left
      panelButtons.Controls.Add(btnDiscard, 3, 0); // Far right
      panelButtons.Controls.Add(btnSave, 4, 0); // Second from right

      // Add panelButtons to dialog
      panelButtons.Dock = DockStyle.Bottom;
      this.Controls.Add(panelButtons);

      // Store controls for later access
      this.txtRootLocal = txtRootLocal;
      this.txtSdfLocal = txtSdfLocal;
      this.chkHidePurchasing = chkHidePurchasing;
      this.chkDynamicSheetCosts = chkDynamicSheetCosts;
      this.chkAllowLogging = chkAllowLogging;
      this.txtFrontFilter = txtFrontFilter;
      this.txtSubassemblyFilter = txtSubassemblyFilter;
      this.txtStagingDir = txtStagingDir;
      this.txtReleaseDir = txtReleaseDir;
      this.numMaxTrackedFiles = numMaxTrackedFiles;
      this.btnSave = btnSave;
      this.btnDiscard = btnDiscard;

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
      AllowLogging = cfg.AllowLogging;

      // Load MSSQL settings
      MssqlServer = cfg.MssqlServer ?? "SERVER\\SQL";
      MssqlDatabase = cfg.MssqlDatabase ?? "MicrovellumData";
      MssqlUsername = cfg.MssqlUsername ?? "user";
      MssqlPassword = cfg.MssqlPassword ?? "password";
      MssqlEnabled = cfg.MssqlEnabled;

      // Load Saw Queue directories
      StagingDir = cfg.StagingDir ?? @"P:\CadLinkPTX\staging";
      ReleaseDir = cfg.ReleaseDir ?? @"P:\CadLinkPTX\release";
      MaxTrackedFiles = cfg.MaxTrackedFiles;

      // Clean up any existing duplicated values in the configuration
      FrontFilterKeywords = (cfg.FrontFilterKeywords ?? new List<string> { "Slab", "Drawer Front" })
        .Distinct()
        .ToList();
      SubassemblyFilterKeywords = (cfg.SubassemblyFilterKeywords ?? new List<string> { "Door", "Drawer Front", "RPE" })
        .Distinct()
        .ToList();

      // Clean up the configuration file if it has duplicates
      CleanupConfigurationDuplicates(cfg);

      // Backup original values for discard functionality
      originalDefaultRoot = DefaultRoot;
      originalSdfFileName = SdfFileName;
      originalHidePurchasing = HidePurchasing;
      originalDynamicSheetCosts = DynamicSheetCosts;
      originalAllowLogging = AllowLogging;
      originalFrontFilterKeywords = new List<string>(FrontFilterKeywords);
      originalSubassemblyFilterKeywords = new List<string>(SubassemblyFilterKeywords);
      originalMssqlServer = MssqlServer;
      originalMssqlDatabase = MssqlDatabase;
      originalMssqlUsername = MssqlUsername;
      originalMssqlPassword = MssqlPassword;
      originalMssqlEnabled = MssqlEnabled;
      originalStagingDir = StagingDir;
      originalReleaseDir = ReleaseDir;
      originalMaxTrackedFiles = MaxTrackedFiles;

      // Update UI controls - ensure clean values without duplication
      if (txtRootLocal != null) txtRootLocal.Text = DefaultRoot;
      if (txtSdfLocal != null) txtSdfLocal.Text = SdfFileName;
      if (chkHidePurchasing != null) chkHidePurchasing.Checked = HidePurchasing;
      if (chkDynamicSheetCosts != null) chkDynamicSheetCosts.Checked = DynamicSheetCosts;
      if (chkAllowLogging != null) chkAllowLogging.Checked = AllowLogging;

      // Update MSSQL UI controls
      if (chkMssqlEnabled != null) chkMssqlEnabled.Checked = MssqlEnabled;
      if (txtMssqlServer != null) txtMssqlServer.Text = MssqlServer;
      if (txtMssqlDatabase != null) txtMssqlDatabase.Text = MssqlDatabase;
      if (txtMssqlUsername != null) txtMssqlUsername.Text = MssqlUsername;
      if (txtMssqlPassword != null) txtMssqlPassword.Text = MssqlPassword;

      // Update Saw Queue UI controls
      if (txtStagingDir != null) txtStagingDir.Text = StagingDir;
      if (txtReleaseDir != null) txtReleaseDir.Text = ReleaseDir;
      if (numMaxTrackedFiles != null) numMaxTrackedFiles.Value = MaxTrackedFiles;

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

      // Wire up change detection events
      WireUpChangeDetection();

      // Set initial button states
      UpdateButtonStates();
    }

    private void CleanupConfigurationDuplicates(UserConfig cfg)
    {
      // Check if there are duplicates in the configuration
      bool hasDuplicates = false;

      if (cfg.FrontFilterKeywords != null && cfg.FrontFilterKeywords.Count != cfg.FrontFilterKeywords.Distinct().Count())
      {
        hasDuplicates = true;
        cfg.FrontFilterKeywords = cfg.FrontFilterKeywords.Distinct().ToList();
        Program.Log("SettingsDialog: Cleaned up duplicate FrontFilterKeywords");
      }

      if (cfg.SubassemblyFilterKeywords != null && cfg.SubassemblyFilterKeywords.Count != cfg.SubassemblyFilterKeywords.Distinct().Count())
      {
        hasDuplicates = true;
        cfg.SubassemblyFilterKeywords = cfg.SubassemblyFilterKeywords.Distinct().ToList();
        Program.Log("SettingsDialog: Cleaned up duplicate SubassemblyFilterKeywords");
      }

      // Save the cleaned configuration if duplicates were found
      if (hasDuplicates)
      {
        cfg.Save();
        Program.Log("SettingsDialog: Saved cleaned configuration to remove duplicates");
      }
    }

    public void SaveSettings()
    {
      var cfg = UserConfig.LoadOrDefault();
      cfg.DefaultRoot = (txtRootLocal?.Text ?? string.Empty).Trim();
      cfg.SdfFileName = (txtSdfLocal?.Text ?? string.Empty).Trim();
      cfg.HidePurchasing = chkHidePurchasing?.Checked ?? true;
      cfg.DynamicSheetCosts = chkDynamicSheetCosts?.Checked ?? false;
      cfg.AllowLogging = chkAllowLogging?.Checked ?? true;

      // Save MSSQL settings
      cfg.MssqlServer = (txtMssqlServer?.Text ?? string.Empty).Trim();
      cfg.MssqlDatabase = (txtMssqlDatabase?.Text ?? string.Empty).Trim();
      cfg.MssqlUsername = (txtMssqlUsername?.Text ?? string.Empty).Trim();
      cfg.MssqlPassword = (txtMssqlPassword?.Text ?? string.Empty).Trim();
      cfg.MssqlEnabled = chkMssqlEnabled?.Checked ?? true;

      // Save Saw Queue directories
      cfg.StagingDir = (txtStagingDir?.Text ?? string.Empty).Trim();
      cfg.ReleaseDir = (txtReleaseDir?.Text ?? string.Empty).Trim();
      cfg.MaxTrackedFiles = (int)(numMaxTrackedFiles?.Value ?? 100);

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
      else
      {
        // If empty, set to empty list to clear any existing duplicates
        cfg.FrontFilterKeywords = new List<string>();
      }

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
      else
      {
        // If empty, set to empty list to clear any existing duplicates
        cfg.SubassemblyFilterKeywords = new List<string>();
      }

      cfg.Save();
    }

    private async void CheckForUpdates_Click(object sender, EventArgs e)
    {
      try
      {
        // Disable the button to prevent multiple clicks
        var button = sender as Button;
        if (button != null)
        {
          button.Enabled = false;
          button.Text = "Checking...";
        }

        // Raise the event to let MainForm handle the Check for Updates functionality
        // The event handler will call Program.CheckForUpdates which is async void
        CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);

        // Wait a moment to allow the async update check to start
        // This prevents the button from re-enabling too quickly
        await Task.Delay(1000);
      }
      catch (Exception ex)
      {
        Program.Log("CheckForUpdates_Click error in SettingsDialog", ex);
        MessageBox.Show("Failed to check for updates: " + ex.Message, "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      finally
      {
        // Re-enable the button after allowing time for update check to start
        var button = sender as Button;
        if (button != null)
        {
          button.Enabled = true;
          button.Text = "Check for Updates";
        }
      }
    }

    private void TestMssqlConnection_Click(object sender, EventArgs e)
    {
      try
      {
        // Disable the button to prevent multiple clicks
        var button = sender as Button;
        if (button != null)
        {
          button.Enabled = false;
          button.Text = "Testing...";
        }

        // Test the connection with current dialog settings
        bool isConnected = TestMssqlConnectionWithCurrentSettings();

        if (isConnected)
        {
          var server = (txtMssqlServer?.Text ?? string.Empty).Trim();
          var database = (txtMssqlDatabase?.Text ?? string.Empty).Trim();
          MessageBox.Show(
            "Successfully connected to the Microvellum database!\n\n" +
            $"Server: {server}\n" +
            $"Database: {database}",
            "Connection Test Successful",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        }
        else
        {
          MessageBox.Show(
            "Failed to connect to the Microvellum database.\n\n" +
            "Please check:\n" +
            "• Network connectivity to SERVER2019\\HSSQL\n" +
            "• Database server is running\n" +
            "• Credentials are correct\n" +
            "• Firewall settings allow the connection",
            "Connection Test Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(
          $"Error testing MSSQL connection:\n\n{ex.Message}",
          "Connection Test Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error);
      }
      finally
      {
        // Re-enable the button
        var button = sender as Button;
        if (button != null)
        {
          button.Enabled = true;
          button.Text = "Test MSSQL Connection";
        }
      }
    }

    private bool TestMssqlConnectionWithCurrentSettings()
    {
      try
      {
        var server = (txtMssqlServer?.Text ?? string.Empty).Trim();
        var database = (txtMssqlDatabase?.Text ?? string.Empty).Trim();
        var username = (txtMssqlUsername?.Text ?? string.Empty).Trim();
        var password = (txtMssqlPassword?.Text ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) ||
            string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
          MessageBox.Show(
            "Please fill in all MSSQL connection fields before testing.",
            "Incomplete Settings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
          return false;
        }

        var connectionString = $"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate=true;";
        using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
          connection.Open();
          return true;
        }
      }
      catch (Exception ex)
      {
        Program.Log($"TestMssqlConnectionWithCurrentSettings error: {ex.Message}", ex);
        return false;
      }
    }

    private void WireUpChangeDetection()
    {
      if (txtRootLocal != null) txtRootLocal.TextChanged += OnSettingChanged;
      if (txtSdfLocal != null) txtSdfLocal.TextChanged += OnSettingChanged;
      if (chkHidePurchasing != null) chkHidePurchasing.CheckedChanged += OnSettingChanged;
      if (chkDynamicSheetCosts != null) chkDynamicSheetCosts.CheckedChanged += OnSettingChanged;
      if (chkAllowLogging != null) chkAllowLogging.CheckedChanged += OnSettingChanged;
      if (txtFrontFilter != null) txtFrontFilter.TextChanged += OnSettingChanged;
      if (txtSubassemblyFilter != null) txtSubassemblyFilter.TextChanged += OnSettingChanged;
      if (chkMssqlEnabled != null) chkMssqlEnabled.CheckedChanged += OnSettingChanged;
      if (txtMssqlServer != null) txtMssqlServer.TextChanged += OnSettingChanged;
      if (txtMssqlDatabase != null) txtMssqlDatabase.TextChanged += OnSettingChanged;
      if (txtMssqlUsername != null) txtMssqlUsername.TextChanged += OnSettingChanged;
      if (txtMssqlPassword != null) txtMssqlPassword.TextChanged += OnSettingChanged;
      if (txtStagingDir != null) txtStagingDir.TextChanged += OnSettingChanged;
      if (txtReleaseDir != null) txtReleaseDir.TextChanged += OnSettingChanged;
      if (numMaxTrackedFiles != null) numMaxTrackedFiles.ValueChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object sender, EventArgs e)
    {
      if (!isInitialized) return; // Don't detect changes during initialization

      CheckForChanges();
    }

    private void CheckForChanges()
    {
      bool hasChanges = false;

      // Check basic settings
      hasChanges |= (txtRootLocal?.Text ?? string.Empty).Trim() != originalDefaultRoot;
      hasChanges |= (txtSdfLocal?.Text ?? string.Empty).Trim() != originalSdfFileName;
      hasChanges |= (chkHidePurchasing?.Checked ?? true) != originalHidePurchasing;
      hasChanges |= (chkDynamicSheetCosts?.Checked ?? false) != originalDynamicSheetCosts;
      hasChanges |= (chkAllowLogging?.Checked ?? true) != originalAllowLogging;

      // Check filter keywords
      var currentFrontFilter = (txtFrontFilter?.Text ?? string.Empty).Trim();
      var currentFrontKeywords = string.IsNullOrEmpty(currentFrontFilter)
        ? new List<string>()
        : currentFrontFilter.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();
      hasChanges |= !currentFrontKeywords.SequenceEqual(originalFrontFilterKeywords);

      var currentSubassemblyFilter = (txtSubassemblyFilter?.Text ?? string.Empty).Trim();
      var currentSubassemblyKeywords = string.IsNullOrEmpty(currentSubassemblyFilter)
        ? new List<string>()
        : currentSubassemblyFilter.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();
      hasChanges |= !currentSubassemblyKeywords.SequenceEqual(originalSubassemblyFilterKeywords);

      // Check MSSQL settings
      hasChanges |= (txtMssqlServer?.Text ?? string.Empty).Trim() != originalMssqlServer;
      hasChanges |= (txtMssqlDatabase?.Text ?? string.Empty).Trim() != originalMssqlDatabase;
      hasChanges |= (txtMssqlUsername?.Text ?? string.Empty).Trim() != originalMssqlUsername;
      hasChanges |= (txtMssqlPassword?.Text ?? string.Empty).Trim() != originalMssqlPassword;
      hasChanges |= (chkMssqlEnabled?.Checked ?? true) != originalMssqlEnabled;

      // Check Saw Queue directories
      hasChanges |= (txtStagingDir?.Text ?? string.Empty).Trim() != originalStagingDir;
      hasChanges |= (txtReleaseDir?.Text ?? string.Empty).Trim() != originalReleaseDir;
      hasChanges |= (int)(numMaxTrackedFiles?.Value ?? 100) != originalMaxTrackedFiles;

      hasUnsavedChanges = hasChanges;
      UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
      if (btnSave != null)
      {
        btnSave.Visible = hasUnsavedChanges;
        btnSave.Enabled = hasUnsavedChanges;
      }
      if (btnDiscard != null)
      {
        btnDiscard.Enabled = true; // Always enabled so users can close the dialog
        // Discard button is always visible but changes text based on state
        btnDiscard.Text = hasUnsavedChanges ? "Discard and Close" : "Close";
      }
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
      try
      {
        SaveSettings();

        // Update original values to current values after successful save
        originalDefaultRoot = (txtRootLocal?.Text ?? string.Empty).Trim();
        originalSdfFileName = (txtSdfLocal?.Text ?? string.Empty).Trim();
        originalHidePurchasing = chkHidePurchasing?.Checked ?? true;
        originalDynamicSheetCosts = chkDynamicSheetCosts?.Checked ?? false;
        originalAllowLogging = chkAllowLogging?.Checked ?? true;

        var currentFrontFilter = (txtFrontFilter?.Text ?? string.Empty).Trim();
        originalFrontFilterKeywords = string.IsNullOrEmpty(currentFrontFilter)
          ? new List<string>()
          : currentFrontFilter.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();

        var currentSubassemblyFilter = (txtSubassemblyFilter?.Text ?? string.Empty).Trim();
        originalSubassemblyFilterKeywords = string.IsNullOrEmpty(currentSubassemblyFilter)
          ? new List<string>()
          : currentSubassemblyFilter.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();

        originalMssqlServer = (txtMssqlServer?.Text ?? string.Empty).Trim();
        originalMssqlDatabase = (txtMssqlDatabase?.Text ?? string.Empty).Trim();
        originalMssqlUsername = (txtMssqlUsername?.Text ?? string.Empty).Trim();
        originalMssqlPassword = (txtMssqlPassword?.Text ?? string.Empty).Trim();
        originalMssqlEnabled = chkMssqlEnabled?.Checked ?? true;
        originalStagingDir = (txtStagingDir?.Text ?? string.Empty).Trim();
        originalReleaseDir = (txtReleaseDir?.Text ?? string.Empty).Trim();
        originalMaxTrackedFiles = (int)(numMaxTrackedFiles?.Value ?? 100);

        hasUnsavedChanges = false;
        UpdateButtonStates();

        // Close the dialog after successful save
        this.DialogResult = DialogResult.OK;
        this.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error saving settings:\n\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Program.Log($"SaveButton_Click error: {ex.Message}", ex);
      }
    }

    private void DiscardButton_Click(object sender, EventArgs e)
    {
      if (hasUnsavedChanges)
      {
        var result = MessageBox.Show(
          "Are you sure you want to discard all unsaved changes?",
          "Discard Changes",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
          DiscardChanges();
          // Close the dialog after discarding changes
          this.DialogResult = DialogResult.Cancel;
          this.Close();
        }
      }
      else
      {
        // No unsaved changes, just close the dialog
        this.DialogResult = DialogResult.Cancel;
        this.Close();
      }
    }

    private void DiscardChanges()
    {
      // Restore original values to UI controls
      if (txtRootLocal != null) txtRootLocal.Text = originalDefaultRoot;
      if (txtSdfLocal != null) txtSdfLocal.Text = originalSdfFileName;
      if (chkHidePurchasing != null) chkHidePurchasing.Checked = originalHidePurchasing;
      if (chkDynamicSheetCosts != null) chkDynamicSheetCosts.Checked = originalDynamicSheetCosts;
      if (chkAllowLogging != null) chkAllowLogging.Checked = originalAllowLogging;
      if (txtFrontFilter != null) txtFrontFilter.Text = string.Join(", ", originalFrontFilterKeywords);
      if (txtSubassemblyFilter != null) txtSubassemblyFilter.Text = string.Join(", ", originalSubassemblyFilterKeywords);
      if (chkMssqlEnabled != null) chkMssqlEnabled.Checked = originalMssqlEnabled;
      if (txtMssqlServer != null) txtMssqlServer.Text = originalMssqlServer;
      if (txtMssqlDatabase != null) txtMssqlDatabase.Text = originalMssqlDatabase;
      if (txtMssqlUsername != null) txtMssqlUsername.Text = originalMssqlUsername;
      if (txtMssqlPassword != null) txtMssqlPassword.Text = originalMssqlPassword;
      if (txtStagingDir != null) txtStagingDir.Text = originalStagingDir;
      if (txtReleaseDir != null) txtReleaseDir.Text = originalReleaseDir;
      if (numMaxTrackedFiles != null) numMaxTrackedFiles.Value = originalMaxTrackedFiles;

      hasUnsavedChanges = false;
      UpdateButtonStates();
    }


    // Control references for access from methods
    private TextBox txtRootLocal;
    private TextBox txtSdfLocal;
    private CheckBox chkHidePurchasing;
    private CheckBox chkDynamicSheetCosts;
    private CheckBox chkAllowLogging;
    private TextBox txtFrontFilter;
    private TextBox txtSubassemblyFilter;
    private TextBox txtStagingDir;
    private TextBox txtReleaseDir;
    private NumericUpDown numMaxTrackedFiles;
    private CheckBox chkMssqlEnabled;
    private TextBox txtMssqlServer;
    private TextBox txtMssqlDatabase;
    private TextBox txtMssqlUsername;
    private TextBox txtMssqlPassword;
    private Button btnSave;
    private Button btnDiscard;
  }
}
