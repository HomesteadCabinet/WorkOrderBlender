using System.Windows.Forms;

namespace WorkOrderBlender
{
  public partial class MainForm
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.listWorkOrders = new System.Windows.Forms.ListView();
      this.colDir = new System.Windows.Forms.ColumnHeader();
      this.btnConsolidate = new System.Windows.Forms.Button();
      this.btnSettings = new System.Windows.Forms.Button();
      this.txtOutput = new System.Windows.Forms.TextBox();
      this.labelOutput = new System.Windows.Forms.Label();
      this.progress = new System.Windows.Forms.ProgressBar();
      this.labelSearch = new System.Windows.Forms.Label();
      this.txtSearch = new System.Windows.Forms.TextBox();
      this.mainLayoutTable = new System.Windows.Forms.TableLayoutPanel();
      this.splitMain = new System.Windows.Forms.SplitContainer();
      this.cmbTableSelector = new System.Windows.Forms.ComboBox();
      this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
      this.lblTableSelector = new System.Windows.Forms.Label();
      this.panelSearchLeft = new System.Windows.Forms.Panel();
      this.btnSelectAll = new System.Windows.Forms.Button();
      this.btnSelectNone = new System.Windows.Forms.Button();
      this.metricsGrid = new System.Windows.Forms.DataGridView();
      this.panelMetricsBorder = new System.Windows.Forms.Panel();
      this.panelLoading = new System.Windows.Forms.Panel();
      // progressLoading removed - no longer needed
      this.lblLoading = new System.Windows.Forms.Label();
      this.panelTableSelector = new System.Windows.Forms.Panel();
      this.panelLeftColumn = new System.Windows.Forms.FlowLayoutPanel(); // Fixed: should be FlowLayoutPanel, not TableLayoutPanel
      this.panelToolbar = new System.Windows.Forms.FlowLayoutPanel();
      this.btnPreviewChanges = new System.Windows.Forms.Button();
      this.tableWorkOrder = new System.Windows.Forms.TableLayoutPanel();
      this.loadingLayout = new System.Windows.Forms.TableLayoutPanel();
      this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
      ((System.ComponentModel.ISupportInitialize)(this.metricsGrid)).BeginInit();
      this.mainLayoutTable.SuspendLayout();
      this.bottomLayout.SuspendLayout();
      this.panelLeftColumn.SuspendLayout();
      this.panelToolbar.SuspendLayout();
      this.SuspendLayout();

      //
      // colDir
      //
      this.colDir.Text = "Directory";
      this.colDir.Width = 320;
      //
      // listWorkOrders
      //
      this.listWorkOrders.Dock = System.Windows.Forms.DockStyle.Fill;
      this.listWorkOrders.CheckBoxes = false;
      this.listWorkOrders.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.colDir,
      });
      this.listWorkOrders.FullRowSelect = true;
      this.listWorkOrders.HideSelection = false;
      this.listWorkOrders.Name = "listWorkOrders";
      this.listWorkOrders.TabIndex = 3;
      this.listWorkOrders.UseCompatibleStateImageBehavior = false;
      this.listWorkOrders.View = System.Windows.Forms.View.Details;
      this.listWorkOrders.SelectedIndexChanged += new System.EventHandler(this.listWorkOrders_SelectedIndexChanged);
      //
      // btnPreviewChanges
      //
      this.btnPreviewChanges.Dock = System.Windows.Forms.DockStyle.Fill;
      this.btnPreviewChanges.Name = "btnPreviewChanges";
      this.btnPreviewChanges.Size = new System.Drawing.Size(130, 40);
      this.btnPreviewChanges.TabIndex = 4;
      this.btnPreviewChanges.Text = "Preview Changes";
      this.btnPreviewChanges.UseVisualStyleBackColor = true;
      this.btnPreviewChanges.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
      this.btnPreviewChanges.Click += new System.EventHandler(this.btnPreviewChanges_Click);
      this.toolTip1.SetToolTip(this.btnPreviewChanges, "Preview changes before consolidation");
      //
      // btnConsolidate
      //
      this.btnConsolidate.Dock = System.Windows.Forms.DockStyle.Fill;
      this.btnConsolidate.Name = "btnConsolidate";
      this.btnConsolidate.Size = new System.Drawing.Size(75, 40);
      this.btnConsolidate.TabIndex = 5;
      this.btnConsolidate.Text = "Run";
      this.btnConsolidate.UseVisualStyleBackColor = true;
      this.btnConsolidate.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.btnConsolidate.Click += new System.EventHandler(this.btnConsolidate_Click);
      this.toolTip1.SetToolTip(this.btnConsolidate, "Run Consolidation");
      //
      // txtOutput
      //
      // Vertically align label and textbox by anchoring both Top and Bottom, and set consistent height
      this.txtOutput.Name = "txtOutput";
      this.txtOutput.TabIndex = 6;
      this.txtOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtOutput.Dock = System.Windows.Forms.DockStyle.None; // No dock, manual placement in panel
      this.txtOutput.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom; // Anchor left, top, and bottom for vertical alignment
      this.txtOutput.Width = 450; // Fixed width
      this.txtOutput.Height = 36; // Consistent height
      this.txtOutput.Location = new System.Drawing.Point(240, 5);

      //
      // labelOutput
      //
      this.labelOutput.AutoSize = false; // Disable AutoSize to control height for vertical alignment
      this.labelOutput.Name = "labelOutput";
      this.labelOutput.TabIndex = 7;
      this.labelOutput.Height = 26; // Consistent height for vertical alignment
      this.labelOutput.Width = 130; // Set a fixed width for better alignment if needed
      this.labelOutput.Text = "Work Order Name:";
      this.labelOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelOutput.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom; // Anchor left, top, and bottom for vertical alignment
      this.labelOutput.TextAlign = System.Drawing.ContentAlignment.MiddleLeft; // Vertically center text
      this.labelOutput.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0); // Right margin for spacing
      // Set the Location property for labelOutput and txtOutput for explicit placement
      this.labelOutput.Location = new System.Drawing.Point(100, 3); // Place label near left edge, vertically aligned

      // Make Settings button fill its cell both horizontally and vertically within its container
      this.btnSettings.Text = "Settings...";
      this.btnSettings.Width = 80;
      this.btnSettings.Height = 26;
      this.btnSettings.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.btnSettings.UseVisualStyleBackColor = true;
      this.btnSettings.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0); // No margin for full fill
      this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
      this.btnSettings.Location = new System.Drawing.Point(2, 3);
      // this.btnSettings.Dock = System.Windows.Forms.DockStyle.Fill; // Fill both horizontally and vertically in container
      this.toolTip1.SetToolTip(this.btnSettings, "Open settings dialog");

      //
      // tableWorkOrder
      //
      // Make tableWorkOrder full width and docked to top
      this.tableWorkOrder.Dock = System.Windows.Forms.DockStyle.Top; // Fill horizontally at the top
      this.tableWorkOrder.Height = 48;
      this.tableWorkOrder.Padding = new System.Windows.Forms.Padding(6,0,6,0);
      this.tableWorkOrder.ColumnCount = 2; // 2 columns: left (label+textbox), right (toolbar)
      this.tableWorkOrder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      // this.tableWorkOrder.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;

      // Set left column to fill available space, right column to auto-size for toolbar
      this.tableWorkOrder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Right column auto-sizes for toolbar
      this.tableWorkOrder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize)); // Right column auto-sizes for toolbar
      // Add top margin of 15 to tableWorkOrder for visual spacing from top of form
      this.tableWorkOrder.Name = "tableWorkOrder";
      this.tableWorkOrder.RowCount = 1;
      // Center content vertically by setting row style to percent and aligning child panels
      this.tableWorkOrder.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableWorkOrder.Size = new System.Drawing.Size(1200, 40); // Match form/client width
      this.tableWorkOrder.TabIndex = 8;

      // Set both panels to fill their respective cells in the table for full container fill
      // Align both panels to fill their cells and stack controls vertically
      this.panelLeftColumn.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelLeftColumn.AutoSize = true;
      this.panelLeftColumn.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
      this.panelLeftColumn.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight; // Horizontal layout for buttons
      this.panelLeftColumn.WrapContents = false; // Prevent wrapping to next line

      this.panelToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
      // Add label and textbox to left column panel
      this.panelLeftColumn.Controls.Add(this.btnSettings);
      this.panelLeftColumn.Controls.Add(this.labelOutput);
      this.panelLeftColumn.Controls.Add(this.txtOutput);
      this.panelToolbar.AutoSize = true;
      this.panelToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
      this.panelToolbar.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft; // Stack toolbar buttons vertically

      // Add controls to tableWorkOrder - left column with label and textbox, right column with toolbar
      this.tableWorkOrder.Controls.Add(this.panelLeftColumn, 0, 0);
      this.tableWorkOrder.Controls.Add(this.panelToolbar, 1, 0);


    // Add buttons to toolbar panel
      // Align toolbar panel's content to the right
      this.panelToolbar.Anchor = System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
      this.panelToolbar.AutoSize = true;
      this.panelToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;


      this.btnFilterFronts = new System.Windows.Forms.Button();
      this.btnFilterFronts.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
      this.btnFilterFronts.Text = "Fronts";
      this.btnFilterFronts.Height = 26;
      this.btnFilterFronts.Width = 90;
      this.btnFilterFronts.UseVisualStyleBackColor = true;
      this.btnFilterFronts.Enabled = false; // Disabled by default
      this.toolTip1.SetToolTip(this.btnFilterFronts, "Show only front parts (doors, drawer fronts, false fronts, applicance fronts, etc.) ");
      this.btnFilterFronts.Click += new System.EventHandler(this.btnFilterFronts_Click);
      this.panelToolbar.Controls.Add(this.btnFilterFronts);


      // Toolbar selection buttons for current table (rows include/exclude)
      this.btnTableSelectAll = new System.Windows.Forms.Button();
      this.btnTableSelectAll.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
      this.btnTableSelectAll.Text = "Select All";
      this.btnTableSelectAll.Height = 26;
      this.btnTableSelectAll.Width = 90;
      this.btnTableSelectAll.UseVisualStyleBackColor = true;
      // click handler wired in constructor
      this.toolTip1.SetToolTip(this.btnTableSelectAll, "Include all rows in current table");
      this.panelToolbar.Controls.Add(this.btnTableSelectAll);

      this.btnTableClearAll = new System.Windows.Forms.Button();
      this.btnTableClearAll.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
      this.btnTableClearAll.Text = "Clear All";
      this.btnTableClearAll.Height = 26;
      this.btnTableClearAll.Width = 90;
      this.btnTableClearAll.UseVisualStyleBackColor = true;
      // click handler wired in constructor
      this.toolTip1.SetToolTip(this.btnTableClearAll, "Exclude all rows in current table");
      this.panelToolbar.Controls.Add(this.btnTableClearAll);



      //
      // progress
      //
      this.progress.Dock = System.Windows.Forms.DockStyle.Fill;
      this.progress.Name = "progress";
      this.progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
      this.progress.TabIndex = 8;
      this.progress.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
      //
      // (removed legacy chkSelectAll)

      // labelSearch
      //
      this.labelSearch.AutoSize = true;
      this.labelSearch.Margin = new System.Windows.Forms.Padding(4, 10, 4, 4);
      this.labelSearch.Name = "labelSearch";
      this.labelSearch.Size = new System.Drawing.Size(24, 16);
      this.labelSearch.TabIndex = 10;
      this.labelSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelSearch.Text = "Search:";

      // txtSearch
      //
      this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.txtSearch.Location = new System.Drawing.Point(101, 80);
      this.txtSearch.Margin = new System.Windows.Forms.Padding(4, 4, 4, 5);
      this.txtSearch.Name = "txtSearch";
      this.txtSearch.Size = new System.Drawing.Size(146, 24);
      this.txtSearch.TabIndex = 11;
      // Set txtSearch width to 200px (200 logical pixels)
      this.txtSearch.Width = 200;
      this.txtSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
      this.toolTip1.SetToolTip(this.txtSearch, "Search work orders by directory name");

            // mainLayoutTable - 2x2 grid layout
      //
      this.mainLayoutTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
      | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.mainLayoutTable.Location = new System.Drawing.Point(15, 50);
      this.mainLayoutTable.Name = "mainLayoutTable";
      this.mainLayoutTable.ColumnCount = 1;
      this.mainLayoutTable.RowCount = 2;
      this.mainLayoutTable.Size = new System.Drawing.Size(1170, 540);
      this.mainLayoutTable.TabIndex = 12;

      // Column styles: Left panel fixed 300px, right panel fills remaining space
      // this.mainLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 500F)); // Left panel fixed width
      this.mainLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));   // Right panel fills

      // Row styles: Top row fills space, Bottom row fixed height for actions
      this.mainLayoutTable.RowStyles.Clear();
      this.mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));

      // First create panelTableSelector before adding to layout
      // Create FlowLayoutPanel for left column to arrange label and textbox horizontally
      this.panelLeftColumn = new System.Windows.Forms.FlowLayoutPanel();
      this.panelLeftColumn.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelLeftColumn.Name = "panelLeftColumn";
      this.panelLeftColumn.TabIndex = 15;
      this.panelLeftColumn.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0); // Add left padding for spacing from left edge
      this.panelLeftColumn.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
      this.panelLeftColumn.AutoSize = false;
      this.panelLeftColumn.WrapContents = false;

      // Create FlowLayoutPanel for right column toolbar (reuse existing instance created above)
      this.panelToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelToolbar.Name = "panelToolbar";
      this.panelToolbar.TabIndex = 16;
      this.panelToolbar.Padding = new System.Windows.Forms.Padding(5);
      this.panelToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
      this.panelToolbar.AutoSize = true;
      this.panelToolbar.WrapContents = false;

      this.panelTableSelector = new System.Windows.Forms.Panel();
      this.panelTableSelector.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelTableSelector.Name = "panelTableSelector";
      this.panelTableSelector.TabIndex = 17;
      this.panelTableSelector.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
      this.panelTableSelector.BackColor = System.Drawing.SystemColors.Control;
      this.panelTableSelector.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

      // splitMain - resizable container for left/right panels
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
      this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitMain.Orientation = System.Windows.Forms.Orientation.Vertical;
      this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
      this.splitMain.Panel1.SuspendLayout();
      this.splitMain.Panel2.SuspendLayout();
      this.splitMain.SuspendLayout();
      this.splitMain.Name = "splitMain";
      this.splitMain.TabIndex = 16;
      this.splitMain.SplitterWidth = 6;
      this.splitMain.Panel1MinSize = 0;
      this.splitMain.Panel2MinSize = 0;
      this.splitMain.SplitterDistance = 0; // safe initial value; real value set at runtime
      // Splitter distance will be applied at runtime after layout

      // leftLayoutTable - placed into splitMain.Panel1
      this.leftLayoutTable = new System.Windows.Forms.TableLayoutPanel();
      this.leftLayoutTable.Dock = System.Windows.Forms.DockStyle.Fill;
      this.leftLayoutTable.ColumnCount = 1;
      this.leftLayoutTable.RowCount = 3;
      this.leftLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.leftLayoutTable.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
      // Set minimum height for the search panel row
      this.leftLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35)); // Search panel min height
      this.leftLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Work orders list fills remaining height
      this.leftLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45)); // Table selector

      this.leftLayoutTable.Controls.Add(this.panelSearchLeft, 0, 0);
      this.leftLayoutTable.Controls.Add(this.listWorkOrders, 0, 1);
      this.leftLayoutTable.Controls.Add(this.panelTableSelector, 0, 2);

      // rightLayoutTable - placed into splitMain.Panel2
      this.rightLayoutTable = new System.Windows.Forms.TableLayoutPanel();
      this.rightLayoutTable.Dock = System.Windows.Forms.DockStyle.Fill;
      // Add a border to the rightLayoutTable for visual separation
      this.rightLayoutTable.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single; // 1px border between cells
      // this.rightLayoutTable.BackColor = System.Drawing.SystemColors.ControlLight; // Subtle background for border effect
      this.rightLayoutTable.ColumnCount = 1;
      this.rightLayoutTable.RowCount = 1;
      this.rightLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.rightLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Metrics grid fills entire area

      // panelMetricsBorder wraps metricsGrid to show a thick border in edit mode
      this.panelMetricsBorder.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelMetricsBorder.Margin = new System.Windows.Forms.Padding(0);
      // this.panelMetricsBorder.BackColor = System.Drawing.SystemColors.Control;
      this.panelMetricsBorder.Name = "panelMetricsBorder";
      this.panelMetricsBorder.TabIndex = 99;
      this.panelMetricsBorder.Controls.Add(this.metricsGrid);
      this.rightLayoutTable.Controls.Add(this.panelMetricsBorder, 0, 0);


      // cmbTableSelector
      //
      this.cmbTableSelector.Dock = System.Windows.Forms.DockStyle.Fill;
      this.cmbTableSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbTableSelector.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.cmbTableSelector.Name = "cmbTableSelector";
      this.cmbTableSelector.TabIndex = 1;
      this.cmbTableSelector.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.cmbTableSelector.BackColor = System.Drawing.Color.White;
      this.cmbTableSelector.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
      this.cmbTableSelector.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
      this.cmbTableSelector.SelectedIndexChanged += new System.EventHandler(this.cmbTableSelector_SelectedIndexChanged);
      this.panelTableSelector.Controls.Add(this.cmbTableSelector);

      // metricsGrid
      //
      this.metricsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
      this.metricsGrid.AllowUserToAddRows = false;
      this.metricsGrid.AllowUserToDeleteRows = false;
      this.metricsGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle; // 1px border for grid

      this.metricsGrid.Margin = new System.Windows.Forms.Padding(4); // 4px margin on all sides
      this.metricsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
      this.metricsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
      this.metricsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.metricsGrid.Location = new System.Drawing.Point(0, 0);
      this.metricsGrid.Name = "metricsGrid";
      this.metricsGrid.RowHeadersWidth = 25;
      this.metricsGrid.RowTemplate.Height = 24;
      this.metricsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;

      this.metricsGrid.Size = new System.Drawing.Size(364, 317);
      this.metricsGrid.TabIndex = 13;

      // panelSearchLeft
      //
      this.panelSearchLeft.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelSearchLeft.Name = "panelSearchLeft";
      this.panelSearchLeft.TabIndex = 14;
      this.panelSearchLeft.MinimumSize = new System.Drawing.Size(0, 32);
      // build a table layout to avoid overlap
      this.tableSearch = new System.Windows.Forms.TableLayoutPanel();
      this.tableSearch.ColumnCount = 4;
      this.tableSearch.RowCount = 1;
      this.tableSearch.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      this.tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      this.tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      this.tableSearch.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableSearch.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
      this.panelSearchLeft.Controls.Add(this.tableSearch);

      // labelSearch
      this.labelSearch.AutoSize = true;
      this.tableSearch.Controls.Add(this.labelSearch, 0, 0);

      // txtSearch
      this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tableSearch.Controls.Add(this.txtSearch, 1, 0);

      // btnSelectAll icon-only
      this.btnSelectAll.AutoSize = true;
      this.btnSelectAll.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
      this.btnSelectAll.Text = "✔"; // icon-like text
      this.btnSelectAll.Width = 28;
      this.btnSelectAll.Height = 24;
      this.btnSelectAll.UseVisualStyleBackColor = true;
      this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
      this.btnSelectAll.Font = new System.Drawing.Font("Segoe UI Symbol", 10F);
      this.toolTip1.SetToolTip(this.btnSelectAll, "Select All Work Orders");
      this.tableSearch.Controls.Add(this.btnSelectAll, 2, 0);

      // btnSelectNone icon-only
      this.btnSelectNone.AutoSize = true;
      this.btnSelectNone.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
      this.btnSelectNone.Text = "✖"; // icon-like text
      this.btnSelectNone.Width = 28;
      this.btnSelectNone.Height = 24;
      this.btnSelectNone.UseVisualStyleBackColor = true;
      this.btnSelectNone.Click += new System.EventHandler(this.btnSelectNone_Click);
      this.btnSelectNone.Font = new System.Drawing.Font("Segoe UI Symbol", 10F);
      this.toolTip1.SetToolTip(this.btnSelectNone, "Clear All Selections");
      this.tableSearch.Controls.Add(this.btnSelectNone, 3, 0);

      // panelMetricsTop removed - using rightLayoutTable structure instead

      // panelLoading
      //
      // Loading panel - no border for cleaner look
      this.panelLoading.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.panelLoading.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelLoading.Name = "panelLoading";
      // Set panelLoading height to 40px (logical pixels)
      this.panelLoading.Height = 40;
      this.panelLoading.TabIndex = 17;
      this.panelLoading.Visible = false;
      this.panelLoading.Padding = new System.Windows.Forms.Padding(0);
      // Custom paint for pink border

      // lblLoading
      //
      this.lblLoading.AutoSize = false;
      this.lblLoading.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblLoading.Name = "lblLoading";
      this.lblLoading.Text = "Loawtwtwetwetwetwetding data...";
      this.lblLoading.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lblLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      this.lblLoading.Margin = new System.Windows.Forms.Padding(0, -10, 0, 0);

      // this.loadingLayout.Controls.Add(this.lblLoading, 0, 0);
      this.panelLoading.Controls.Add(this.lblLoading);

      // bottomLayout - Bottom row spanning both columns (0,1) and (1,1)
      //
      this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
      this.bottomLayout.Name = "bottomLayout";
      this.bottomLayout.BorderStyle = System.Windows.Forms.BorderStyle.None; // Remove border for cleaner look
      this.bottomLayout.Height = 40; // Increase height for better button visibility
      this.bottomLayout.TabIndex = 17;
      this.bottomLayout.Margin = new System.Windows.Forms.Padding(0);

      // Configure TableLayoutPanel for proper column layout
      this.bottomLayout.ColumnCount = 2;
      this.bottomLayout.RowCount = 1;
      this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Loading panel takes no space when hidden
      this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

      // Add controls to bottomLayout
      // Actions panel - left column (0,0)
      this.panelActions = new System.Windows.Forms.Panel();
      this.panelActions.Text = "Actions";
      this.panelActions.Visible = true;
      this.panelActions.BorderStyle = System.Windows.Forms.BorderStyle.None; // Remove border for cleaner look
      this.panelActions.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelActions.Margin = new System.Windows.Forms.Padding(0);
      this.panelActions.Padding = new System.Windows.Forms.Padding(4); // Increase padding for better spacing

      // actionsLayout inside panelActions to position controls
      this.actionsLayout = new System.Windows.Forms.TableLayoutPanel();
      this.actionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
      this.actionsLayout.ColumnCount = 4;
      this.actionsLayout.RowCount = 1;
      this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // progress
      this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize)); // preview
      this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize)); // consolidate
      this.actionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

      // Loading panel - right column (1,0) - will be hidden by default
      this.panelLoading.Visible = false;
      this.panelLoading.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelLoading.Margin = new System.Windows.Forms.Padding(0);
      this.panelLoading.BorderStyle = System.Windows.Forms.BorderStyle.None; // Remove border for consistency

      // Arrange controls within panelActions using proper anchoring
      this.progress.Dock = System.Windows.Forms.DockStyle.Fill;
      // this.progress.Width = 300; // Increase progress bar width
      this.progress.Height = 28; // Increase progress bar height
      this.progress.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0); // Increase right margin

      this.btnPreviewChanges.Anchor = System.Windows.Forms.AnchorStyles.None;
      this.btnPreviewChanges.Height = 36; // Increase button height
      this.btnPreviewChanges.Width = 140; // Set explicit width for consistency
      this.btnPreviewChanges.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0); // Increase right margin

      this.btnConsolidate.Anchor = System.Windows.Forms.AnchorStyles.None;
      this.btnConsolidate.Height = 36; // Increase button height
      this.btnConsolidate.Width = 80; // Set explicit width for consistency

      // Add controls into actionsLayout cells: [0]=progress, [1]=spacer, [2]=preview, [3]=consolidate
      this.actionsLayout.Controls.Add(this.progress, 0, 0);
      // spacer is implicit by percent column at index 1
      this.actionsLayout.Controls.Add(this.btnPreviewChanges, 2, 0);
      this.actionsLayout.Controls.Add(this.btnConsolidate, 3, 0);

      // Add actionsLayout to the actions panel
      this.panelActions.Controls.Add(this.actionsLayout);

      // Add panels to the bottomLayout
      this.bottomLayout.Controls.Add(this.panelActions, 0, 0);
      this.bottomLayout.Controls.Add(this.panelLoading, 1, 0);

      // Add left/right layouts into splitMain
      this.splitMain.Panel1.Controls.Add(this.leftLayoutTable);
      this.splitMain.Panel2.Controls.Add(this.rightLayoutTable);

      // Add splitMain to main table top row and span both columns
      this.mainLayoutTable.Controls.Add(this.splitMain, 0, 0);
      // this.mainLayoutTable.SetColumnSpan(this.splitMain, 2);
      this.mainLayoutTable.Controls.Add(this.bottomLayout, 0, 1);
      // this.mainLayoutTable.SetColumnSpan(this.bottomLayout, 2); // Span both columns


      //
      // MainForm
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1200, 600);
      this.Controls.Add(this.mainLayoutTable);
      this.Controls.Add(this.tableWorkOrder);
      this.MinimumSize = new System.Drawing.Size(1000, 500);
      this.Name = "MainForm";
      this.Text = "Work Order Blender";
      this.mainLayoutTable.ResumeLayout(false);
      this.splitMain.Panel1.ResumeLayout(false);
      this.splitMain.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
      this.splitMain.ResumeLayout(false);
      this.bottomLayout.ResumeLayout(false);
      this.panelLeftColumn.ResumeLayout(false);
      this.panelToolbar.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.metricsGrid)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();
    }


    private ListView listWorkOrders;
    private ColumnHeader colDir;
    private Button btnConsolidate;
    private Button btnPreviewChanges;
    private Button btnSettings;
    private Button btnTableSelectAll;
    private Button btnTableClearAll;
    private Button btnFilterFronts;
    private TextBox txtOutput;
    private Label labelOutput;
    private ProgressBar progress;
    private Label labelSearch;
    private TextBox txtSearch;
    private TableLayoutPanel mainLayoutTable;
    private TableLayoutPanel leftLayoutTable;
    private TableLayoutPanel rightLayoutTable;
    private SplitContainer splitMain;
    private ComboBox cmbTableSelector;
    private TableLayoutPanel bottomLayout;
    private Label lblTableSelector;
    private Panel panelTableSelector;
    private FlowLayoutPanel panelLeftColumn;
    private FlowLayoutPanel panelToolbar;
    private DataGridView metricsGrid;
    private Panel panelMetricsBorder;
    private Panel panelSearchLeft;
    private Button btnSelectAll;
    private Button btnSelectNone;
    private TableLayoutPanel tableSearch;
    private Panel panelLoading;
    // private ProgressBar progressLoading; // Removed - no longer needed
    private Label lblLoading;
    private Panel panelActions;
    private TableLayoutPanel loadingLayout;
    private TableLayoutPanel tableWorkOrder;
    private ToolTip toolTip1;
    // removed legacy chkSelectAll field
    private System.Windows.Forms.TableLayoutPanel actionsLayout;
  }
}
