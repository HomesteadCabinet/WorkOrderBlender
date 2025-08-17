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
      this.actionsLayout = new System.Windows.Forms.TableLayoutPanel();
      this.lblTableSelector = new System.Windows.Forms.Label();
      this.panelSearchLeft = new System.Windows.Forms.Panel();
      this.btnSelectAll = new System.Windows.Forms.Button();
      this.metricsGrid = new System.Windows.Forms.DataGridView();
      this.panelMetricsBorder = new System.Windows.Forms.Panel();
      this.panelLoading = new System.Windows.Forms.Panel();
      this.progressLoading = new System.Windows.Forms.ProgressBar();
      this.lblLoading = new System.Windows.Forms.Label();
      this.panelTableSelector = new System.Windows.Forms.Panel();
      this.btnPreviewChanges = new System.Windows.Forms.Button();
      this.tableWorkOrder = new System.Windows.Forms.TableLayoutPanel();
      ((System.ComponentModel.ISupportInitialize)(this.metricsGrid)).BeginInit();
      this.mainLayoutTable.SuspendLayout();
      this.actionsLayout.SuspendLayout();
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
      this.btnPreviewChanges.Size = new System.Drawing.Size(130, 23);
      this.btnPreviewChanges.TabIndex = 4;
      this.btnPreviewChanges.Text = "Preview Changes";
      this.btnPreviewChanges.UseVisualStyleBackColor = true;
      this.btnPreviewChanges.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
      this.btnPreviewChanges.Click += new System.EventHandler(this.btnPreviewChanges_Click);
      //
      // btnConsolidate
      //
      this.btnConsolidate.Dock = System.Windows.Forms.DockStyle.Fill;
      this.btnConsolidate.Name = "btnConsolidate";
      this.btnConsolidate.Size = new System.Drawing.Size(75, 23);
      this.btnConsolidate.TabIndex = 5;
      this.btnConsolidate.Text = "Run";
      this.btnConsolidate.UseVisualStyleBackColor = true;
      this.btnConsolidate.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.btnConsolidate.Click += new System.EventHandler(this.btnConsolidate_Click);
      //
      // txtOutput
      //
      this.txtOutput.Name = "txtOutput";
      this.txtOutput.TabIndex = 6;
      this.txtOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtOutput.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      //
      // labelOutput
      //
      this.labelOutput.AutoSize = true;
      this.labelOutput.Name = "labelOutput";
      this.labelOutput.TabIndex = 7;
      this.labelOutput.Text = "Work Order Name:";
      this.labelOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelOutput.Anchor = System.Windows.Forms.AnchorStyles.Left;
      this.labelOutput.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

      this.btnSettings.AutoSize = true;
      this.btnSettings.Text = "Settings...";
      this.btnSettings.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.btnSettings.UseVisualStyleBackColor = true;
      this.btnSettings.Margin = new System.Windows.Forms.Padding(58, 0, 0, 0);
      this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
      this.btnSettings.Anchor = System.Windows.Forms.AnchorStyles.Left;

      //
      // tableWorkOrder
      //
      this.tableWorkOrder.ColumnCount = 4;
      this.tableWorkOrder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      this.tableWorkOrder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableWorkOrder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      this.tableWorkOrder.Controls.Add(this.labelOutput, 0, 0);
      this.tableWorkOrder.Controls.Add(this.txtOutput, 1, 0);
      this.tableWorkOrder.Controls.Add(this.btnSettings, 2, 0);
      this.tableWorkOrder.Location = new System.Drawing.Point(12, 10);
      this.tableWorkOrder.Name = "tableWorkOrder";
      this.tableWorkOrder.RowCount = 1;
      this.tableWorkOrder.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
      this.tableWorkOrder.Size = new System.Drawing.Size(1150, 30);
      this.tableWorkOrder.TabIndex = 8;
      this.tableWorkOrder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));


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
      this.labelSearch.Location = new System.Drawing.Point(12, 80);
      this.labelSearch.Margin = new System.Windows.Forms.Padding(4, 2, 4, 4);
      this.labelSearch.Name = "labelSearch";
      this.labelSearch.Size = new System.Drawing.Size(44, 13);
      this.labelSearch.TabIndex = 10;
      this.labelSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelSearch.Text = "Search:";

      // txtSearch
      //
      this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.txtSearch.Location = new System.Drawing.Point(101, 80);
      this.txtSearch.Margin = new System.Windows.Forms.Padding(4, 0, 4, 5);
      this.txtSearch.Name = "txtSearch";
      this.txtSearch.Size = new System.Drawing.Size(146, 24);
      this.txtSearch.TabIndex = 11;
      this.txtSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);

            // mainLayoutTable - 2x2 grid layout
      //
      this.mainLayoutTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
      | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.mainLayoutTable.Location = new System.Drawing.Point(15, 50);
      this.mainLayoutTable.Name = "mainLayoutTable";
      this.mainLayoutTable.ColumnCount = 2;
      this.mainLayoutTable.RowCount = 2;
      this.mainLayoutTable.Size = new System.Drawing.Size(1170, 540);
      this.mainLayoutTable.TabIndex = 12;

      // Column styles: Left panel fixed 200px, right panel fills remaining space
      this.mainLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F)); // Left panel fixed width
      this.mainLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));   // Right panel fills

      // Row styles: Top row fills space, Bottom row 26px for actions
      this.mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));

      // First create panelTableSelector before adding to layout
      this.panelTableSelector = new System.Windows.Forms.Panel();
      this.panelTableSelector.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelTableSelector.Name = "panelTableSelector";
      this.panelTableSelector.TabIndex = 15;
      this.panelTableSelector.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
      this.panelTableSelector.BackColor = System.Drawing.SystemColors.Control;
      this.panelTableSelector.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

      // splitMain - resizable container for left/right panels
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
      this.splitMain.Panel1.SuspendLayout();
      this.splitMain.Panel2.SuspendLayout();
      this.splitMain.SuspendLayout();
      this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitMain.Name = "splitMain";
      this.splitMain.TabIndex = 16;
      this.splitMain.Orientation = System.Windows.Forms.Orientation.Vertical;
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
      this.rightLayoutTable.ColumnCount = 1;
      this.rightLayoutTable.RowCount = 1;
      this.rightLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.rightLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Metrics grid fills entire area

      // panelMetricsBorder wraps metricsGrid to show a thick border in edit mode
      this.panelMetricsBorder.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelMetricsBorder.Padding = new System.Windows.Forms.Padding(6);
      this.panelMetricsBorder.BackColor = System.Drawing.SystemColors.Control;
      this.panelMetricsBorder.Name = "panelMetricsBorder";
      this.panelMetricsBorder.TabIndex = 99;
      this.panelMetricsBorder.Controls.Add(this.metricsGrid);
      this.rightLayoutTable.Controls.Add(this.panelMetricsBorder, 0, 0);

      // Add left/right layouts into splitMain
      this.splitMain.Panel1.Controls.Add(this.leftLayoutTable);
      this.splitMain.Panel2.Controls.Add(this.rightLayoutTable);

      // Add splitMain to main table top row and span both columns
      this.mainLayoutTable.Controls.Add(this.splitMain, 0, 0);
      this.mainLayoutTable.SetColumnSpan(this.splitMain, 2);
      this.mainLayoutTable.Controls.Add(this.actionsLayout, 0, 1);
      this.mainLayoutTable.SetColumnSpan(this.actionsLayout, 2); // Span both columns


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
      var tableSearch = new System.Windows.Forms.TableLayoutPanel();
      tableSearch.ColumnCount = 3;
      tableSearch.RowCount = 1;
      tableSearch.Dock = System.Windows.Forms.DockStyle.Fill;
      tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      tableSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
      tableSearch.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      tableSearch.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
      this.panelSearchLeft.Controls.Add(tableSearch);

      // labelSearch
      this.labelSearch.AutoSize = true;
      tableSearch.Controls.Add(this.labelSearch, 0, 0);

      // txtSearch
      this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
      tableSearch.Controls.Add(this.txtSearch, 1, 0);

      // btnSelectAll
      this.btnSelectAll.AutoSize = true;
      this.btnSelectAll.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
      this.btnSelectAll.Text = "Select All";
      this.btnSelectAll.UseVisualStyleBackColor = true;
      this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
      this.btnSelectAll.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      tableSearch.Controls.Add(this.btnSelectAll, 2, 0);

      // panelMetricsTop removed - using rightLayoutTable structure instead

      // panelLoading
      //
      // Set a light green background for the loading panel
      this.panelLoading.BackColor = System.Drawing.Color.FromArgb(20, 155, 20); // light green
      this.panelLoading.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelLoading.Name = "panelLoading";
      this.panelLoading.TabIndex = 17;
      this.panelLoading.Visible = false;

            // progressLoading
      //
      // this.progressLoading.Dock = System.Windows.Forms.DockStyle.Fill;
      this.progressLoading.Name = "progressLoading";
      this.progressLoading.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
      this.progressLoading.MarqueeAnimationSpeed = 30;
      this.progressLoading.TabIndex = 0;
      this.progressLoading.Margin = new System.Windows.Forms.Padding(20, 1, 100, 1);

      // Create layout for loading panel (message only)
      var loadingLayout = new System.Windows.Forms.TableLayoutPanel();
      loadingLayout.Dock = System.Windows.Forms.DockStyle.Fill;
      loadingLayout.ColumnCount = 1;
      loadingLayout.RowCount = 1;
      loadingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      loadingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      loadingLayout.Margin = new System.Windows.Forms.Padding(10, 2, 10, 2);

      // lblLoading
      //
      this.lblLoading.AutoSize = true;
      this.lblLoading.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblLoading.Name = "lblLoading";
      this.lblLoading.Text = "Loading data...";
      this.lblLoading.Anchor = System.Windows.Forms.AnchorStyles.Left;
      this.lblLoading.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);

      loadingLayout.Controls.Add(this.lblLoading, 0, 0);
      this.panelLoading.Controls.Add(loadingLayout);

      // actionsLayout - Bottom row spanning both columns (0,1) and (1,1)
      //
      this.actionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
      this.actionsLayout.ColumnCount = 3;
      this.actionsLayout.RowCount = 1;
      this.actionsLayout.Name = "actionsLayout";
      this.actionsLayout.TabIndex = 17;

      // Column styles: Progress bar fills, buttons auto-size
      this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Progress bar
      this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize)); // Preview button
      this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize)); // Run button

      this.actionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

      // Add controls to actionsLayout
      this.actionsLayout.Controls.Add(this.progress, 0, 0);
      this.actionsLayout.Controls.Add(this.btnPreviewChanges, 1, 0);
      this.actionsLayout.Controls.Add(this.btnConsolidate, 2, 0);
      this.actionsLayout.Controls.Add(this.panelLoading, 0, 0);
      this.actionsLayout.SetColumnSpan(this.panelLoading, 3); // Span all 3 columns

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
      this.actionsLayout.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.metricsGrid)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();
    }


    private ListView listWorkOrders;
    private ColumnHeader colDir;
    private Button btnConsolidate;
    private Button btnPreviewChanges;
    private Button btnSettings;
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
    private TableLayoutPanel actionsLayout;
    private Label lblTableSelector;
    private Panel panelTableSelector;
    private DataGridView metricsGrid;
    private Panel panelMetricsBorder;
    private Panel panelSearchLeft;
    private Button btnSelectAll;
    private Panel panelLoading;
    private ProgressBar progressLoading;
    private Label lblLoading;
    private TableLayoutPanel tableWorkOrder;
    // removed legacy chkSelectAll field
  }
}
