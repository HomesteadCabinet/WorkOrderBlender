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
      this.labelRoot = new System.Windows.Forms.Label();
      this.txtRoot = new System.Windows.Forms.TextBox();
      this.btnScan = new System.Windows.Forms.Button();
      this.listWorkOrders = new System.Windows.Forms.ListView();
      this.colDir = new System.Windows.Forms.ColumnHeader();
      this.colSdf = new System.Windows.Forms.ColumnHeader();
      this.btnConsolidate = new System.Windows.Forms.Button();
      this.btnChooseOutput = new System.Windows.Forms.Button();
      this.txtOutput = new System.Windows.Forms.TextBox();
      this.labelOutput = new System.Windows.Forms.Label();
      this.progress = new System.Windows.Forms.ProgressBar();
      this.labelSearch = new System.Windows.Forms.Label();
      this.txtSearch = new System.Windows.Forms.TextBox();
      this.splitMain = new System.Windows.Forms.SplitContainer();
      this.breakdownList = new System.Windows.Forms.ListView();
      this.colMetric = new System.Windows.Forms.ColumnHeader();
      this.colValue = new System.Windows.Forms.ColumnHeader();
      this.colAction = new System.Windows.Forms.ColumnHeader();
      this.panelSearchLeft = new System.Windows.Forms.Panel();
      this.btnSelectAll = new System.Windows.Forms.Button();
      this.panelMetricsTop = new System.Windows.Forms.Panel();
      this.btnPreviewChanges = new System.Windows.Forms.Button();
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
      this.splitMain.Panel1.SuspendLayout();
      this.splitMain.Panel2.SuspendLayout();
      this.splitMain.SuspendLayout();
      this.SuspendLayout();
      //
      // labelRoot
      //
      this.labelRoot.AutoSize = true;
      this.labelRoot.Location = new System.Drawing.Point(12, 15);
      this.labelRoot.Name = "labelRoot";
      this.labelRoot.Size = new System.Drawing.Size(83, 13);
      this.labelRoot.TabIndex = 0;
      this.labelRoot.Text = "Root Directory:";
      //
      // txtRoot
      //
      this.txtRoot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.txtRoot.Location = new System.Drawing.Point(101, 12);
      this.txtRoot.Name = "txtRoot";
      this.txtRoot.Size = new System.Drawing.Size(546, 20);
      this.txtRoot.TabIndex = 1;
      //
      // btnScan
      //
      this.btnScan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnScan.Location = new System.Drawing.Point(660, 10);
      this.btnScan.Name = "btnScan";
      this.btnScan.Size = new System.Drawing.Size(75, 23);
      this.btnScan.TabIndex = 2;
      this.btnScan.Text = "Scan";
      this.btnScan.UseVisualStyleBackColor = true;
      this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
      //
      // colDir
      //
      this.colDir.Text = "Directory";
      this.colDir.Width = 300;
      //
      // colSdf
      //
      this.colSdf.Text = "SDF Exists";
      this.colSdf.Width = 120;
      //
      // listWorkOrders
      //
      this.listWorkOrders.Dock = System.Windows.Forms.DockStyle.Fill;
      this.listWorkOrders.CheckBoxes = false;
      this.listWorkOrders.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.colDir,
        this.colSdf,
      });
      this.listWorkOrders.FullRowSelect = true;
      this.listWorkOrders.HideSelection = false;
      this.listWorkOrders.Location = new System.Drawing.Point(0, 0);
      this.listWorkOrders.Name = "listWorkOrders";
      this.listWorkOrders.Size = new System.Drawing.Size(120, 292);
      this.listWorkOrders.TabIndex = 3;
      this.listWorkOrders.UseCompatibleStateImageBehavior = false;
      this.listWorkOrders.View = System.Windows.Forms.View.Details;
      this.listWorkOrders.SelectedIndexChanged += new System.EventHandler(this.listWorkOrders_SelectedIndexChanged);
      //
      // btnPreviewChanges
      //
      this.btnPreviewChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnPreviewChanges.Location = new System.Drawing.Point(524, 415);
      this.btnPreviewChanges.Name = "btnPreviewChanges";
      this.btnPreviewChanges.Size = new System.Drawing.Size(130, 23);
      this.btnPreviewChanges.TabIndex = 4;
      this.btnPreviewChanges.Text = "Preview Changes";
      this.btnPreviewChanges.UseVisualStyleBackColor = true;
      this.btnPreviewChanges.Click += new System.EventHandler(this.btnPreviewChanges_Click);
      //
      // btnConsolidate
      //
      this.btnConsolidate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnConsolidate.Location = new System.Drawing.Point(660, 415);
      this.btnConsolidate.Name = "btnConsolidate";
      this.btnConsolidate.Size = new System.Drawing.Size(75, 23);
      this.btnConsolidate.TabIndex = 5;
      this.btnConsolidate.Text = "Run";
      this.btnConsolidate.UseVisualStyleBackColor = true;
      this.btnConsolidate.Click += new System.EventHandler(this.btnConsolidate_Click);
      //
      // btnChooseOutput
      //
      this.btnChooseOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnChooseOutput.Location = new System.Drawing.Point(660, 38);
      this.btnChooseOutput.Name = "btnChooseOutput";
      this.btnChooseOutput.Size = new System.Drawing.Size(75, 23);
      this.btnChooseOutput.TabIndex = 4;
      this.btnChooseOutput.Text = "Browse...";
      this.btnChooseOutput.UseVisualStyleBackColor = true;
      this.btnChooseOutput.Click += new System.EventHandler(this.btnChooseOutput_Click);
      //
      // txtOutput
      //
      this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.txtOutput.Location = new System.Drawing.Point(101, 40);
      this.txtOutput.Name = "txtOutput";
      this.txtOutput.Size = new System.Drawing.Size(546, 20);
      this.txtOutput.TabIndex = 6;
      //
      // labelOutput
      //
      this.labelOutput.AutoSize = true;
      this.labelOutput.Location = new System.Drawing.Point(12, 43);
      this.labelOutput.Name = "labelOutput";
      this.labelOutput.Size = new System.Drawing.Size(79, 13);
      this.labelOutput.TabIndex = 7;
      this.labelOutput.Text = "Output .sdf file:";
      //
      // progress
      //
      this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.progress.Location = new System.Drawing.Point(15, 415);
      this.progress.Name = "progress";
      this.progress.Size = new System.Drawing.Size(500, 23);
      this.progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
      this.progress.TabIndex = 8;
      //
      // (removed legacy chkSelectAll)

      // labelSearch
      //
      this.labelSearch.AutoSize = true;
      this.labelSearch.Location = new System.Drawing.Point(12, 85);
      this.labelSearch.Margin = new System.Windows.Forms.Padding(4, 6, 4, 0);
      this.labelSearch.Name = "labelSearch";
      this.labelSearch.Size = new System.Drawing.Size(44, 13);
      this.labelSearch.TabIndex = 10;
      this.labelSearch.Text = "Search:";

      // txtSearch
      //
      this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.txtSearch.Location = new System.Drawing.Point(101, 90);
      this.txtSearch.Name = "txtSearch";
      this.txtSearch.Size = new System.Drawing.Size(146, 20);
      this.txtSearch.TabIndex = 11;
      this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);

      // splitMain
      //
      this.splitMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
      | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.splitMain.Location = new System.Drawing.Point(15, 90);
      this.splitMain.Name = "splitMain";
      // Panel1
      this.splitMain.Panel1.Controls.Add(this.listWorkOrders);
      this.splitMain.Panel1.Controls.Add(this.panelSearchLeft);
      // Panel2
      this.splitMain.Panel2.Controls.Add(this.breakdownList);
      this.splitMain.Panel2.Controls.Add(this.panelMetricsTop);
      this.splitMain.Size = new System.Drawing.Size(718, 319);
      this.splitMain.SplitterDistance = 420;
      this.splitMain.TabIndex = 12;

      // breakdownList
      //
      this.breakdownList.Dock = System.Windows.Forms.DockStyle.Fill;
      this.breakdownList.FullRowSelect = true;
      this.breakdownList.HideSelection = false;
      this.breakdownList.Location = new System.Drawing.Point(0, 32);
      this.breakdownList.Name = "breakdownList";
      this.breakdownList.Size = new System.Drawing.Size(229, 287);
      this.breakdownList.TabIndex = 13;
      this.breakdownList.UseCompatibleStateImageBehavior = false;
      this.breakdownList.View = System.Windows.Forms.View.Details;
      this.breakdownList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.colMetric,
        this.colValue,
        this.colAction
      });
      this.breakdownList.OwnerDraw = true;
      this.breakdownList.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.breakdownList_DrawColumnHeader);
      this.breakdownList.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.breakdownList_DrawItem);
      this.breakdownList.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.breakdownList_DrawSubItem);
      this.breakdownList.MouseClick += new System.Windows.Forms.MouseEventHandler(this.breakdownList_MouseClick);
      this.breakdownList.MouseMove += new System.Windows.Forms.MouseEventHandler(this.breakdownList_MouseMove);

      // colMetric
      //
      this.colMetric.Text = "Metric";
      this.colMetric.Width = 130;
      // colValue
      //
      this.colValue.Text = "Value";
      this.colValue.Width = 80;
      // colAction
      //
      this.colAction.Text = "Action";
      this.colAction.Width = 80;

      // panelSearchLeft
      //
      this.panelSearchLeft.Dock = System.Windows.Forms.DockStyle.Top;
      this.panelSearchLeft.Height = 32;
      this.panelSearchLeft.Location = new System.Drawing.Point(0, 0);
      this.panelSearchLeft.Name = "panelSearchLeft";
      this.panelSearchLeft.TabIndex = 14;
      // build a table layout to avoid overlap
      var tableSearch = new System.Windows.Forms.TableLayoutPanel();
      tableSearch.ColumnCount = 4;
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
      tableSearch.Controls.Add(this.btnSelectAll, 2, 0);
      var btnSettings = new System.Windows.Forms.Button();
      btnSettings.AutoSize = true;
      btnSettings.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
      btnSettings.Text = "Settings...";
      btnSettings.UseVisualStyleBackColor = true;
      btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
      tableSearch.Controls.Add(btnSettings, 3, 0);

      // panelMetricsTop
      //
      this.panelMetricsTop.Dock = System.Windows.Forms.DockStyle.Top;
      this.panelMetricsTop.Height = 32;
      this.panelMetricsTop.Location = new System.Drawing.Point(0, 0);
      this.panelMetricsTop.Name = "panelMetricsTop";
      this.panelMetricsTop.TabIndex = 16;
      this.panelMetricsTop.Visible = false;
      //
      // MainForm
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(748, 450);
      this.Controls.Add(this.splitMain);
      this.Controls.Add(this.progress);
      this.Controls.Add(this.labelOutput);
      this.Controls.Add(this.txtOutput);
      this.Controls.Add(this.btnChooseOutput);
      this.Controls.Add(this.btnPreviewChanges);
      this.Controls.Add(this.btnConsolidate);
      this.Controls.Add(this.btnScan);
      this.Controls.Add(this.txtRoot);
      this.Controls.Add(this.labelRoot);
      this.MinimumSize = new System.Drawing.Size(700, 350);
      this.Name = "MainForm";
      this.Text = "Work Order SDF Consolidator";
      this.splitMain.Panel1.ResumeLayout(false);
      this.splitMain.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
      this.splitMain.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    private Label labelRoot;
    private TextBox txtRoot;
    private Button btnScan;
    private ListView listWorkOrders;
    private ColumnHeader colDir;
    private ColumnHeader colSdf;
    private Button btnConsolidate;
    private Button btnPreviewChanges;
    private Button btnChooseOutput;
    private TextBox txtOutput;
    private Label labelOutput;
    private ProgressBar progress;
    private Label labelSearch;
    private TextBox txtSearch;
    private SplitContainer splitMain;
    private ListView breakdownList;
    private ColumnHeader colMetric;
    private ColumnHeader colValue;
    private ColumnHeader colAction;
    private Panel panelSearchLeft;
    private Button btnSelectAll;
    private Panel panelMetricsTop;
    // removed legacy chkSelectAll field
  }
}
