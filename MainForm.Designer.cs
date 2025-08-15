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
      this.splitMain = new System.Windows.Forms.SplitContainer();
      this.breakdownList = new System.Windows.Forms.ListView();
      this.colMetric = new System.Windows.Forms.ColumnHeader();
      this.colValue = new System.Windows.Forms.ColumnHeader();
      this.colAction = new System.Windows.Forms.ColumnHeader();
      this.panelSearchLeft = new System.Windows.Forms.Panel();
      this.btnSelectAll = new System.Windows.Forms.Button();
      this.panelMetricsTop = new System.Windows.Forms.Panel();
      this.btnPreviewChanges = new System.Windows.Forms.Button();
      this.tableWorkOrder = new System.Windows.Forms.TableLayoutPanel();
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
      this.splitMain.Panel1.SuspendLayout();
      this.splitMain.Panel2.SuspendLayout();
      this.splitMain.SuspendLayout();
      this.SuspendLayout();

      //
      // colDir
      //
      this.colDir.Text = "Directory";
      this.colDir.Width = 400;
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
      this.listWorkOrders.Location = new System.Drawing.Point(0, 0);
      this.listWorkOrders.Name = "listWorkOrders";
      this.listWorkOrders.Size = new System.Drawing.Size(220, 292);
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
      this.tableWorkOrder.Size = new System.Drawing.Size(700, 30);
      this.tableWorkOrder.TabIndex = 8;
      this.tableWorkOrder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));


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

      // splitMain
      //
      this.splitMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
      | System.Windows.Forms.AnchorStyles.Left)
      | System.Windows.Forms.AnchorStyles.Right)));
      this.splitMain.Location = new System.Drawing.Point(15, 50);
      this.splitMain.Name = "splitMain";
      // Panel1
      this.splitMain.Panel1.Controls.Add(this.listWorkOrders);
      this.splitMain.Panel1.Controls.Add(this.panelSearchLeft);
      // Panel2
      this.splitMain.Panel2.Controls.Add(this.breakdownList);
      this.splitMain.Panel2.Controls.Add(this.panelMetricsTop);
      this.splitMain.Size = new System.Drawing.Size(718, 349);
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
      this.ClientSize = new System.Drawing.Size(750, 450);
      this.Controls.Add(this.splitMain);
      this.Controls.Add(this.progress);
      this.Controls.Add(this.tableWorkOrder);
      this.Controls.Add(this.btnPreviewChanges);
      this.Controls.Add(this.btnConsolidate);
      this.MinimumSize = new System.Drawing.Size(750, 450);
      this.Name = "MainForm";
      this.Text = "Work Order Blender";
      this.splitMain.Panel1.ResumeLayout(false);
      this.splitMain.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
      this.splitMain.ResumeLayout(false);
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
    private SplitContainer splitMain;
    private ListView breakdownList;
    private ColumnHeader colMetric;
    private ColumnHeader colValue;
    private ColumnHeader colAction;
    private Panel panelSearchLeft;
    private Button btnSelectAll;
    private Panel panelMetricsTop;
    private TableLayoutPanel tableWorkOrder;
    // removed legacy chkSelectAll field
  }
}
