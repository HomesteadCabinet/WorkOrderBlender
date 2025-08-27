using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WorkOrderBlender
{
    public sealed class PendingChangesDialog : Form
    {
        private readonly TabControl tabControl;
        private readonly DataGridView editsGrid;
        private readonly Label statusLabel;

        public PendingChangesDialog()
        {
            InitializeDialog();

            // Create main tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Create edits tab
            var editsTab = new TabPage("Modified Data");
            editsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };
            editsTab.Controls.Add(editsGrid);
            tabControl.TabPages.Add(editsTab);

            // Deletions are no longer shown in pending changes

                        // Create status panel
            var statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10, 8, 10, 8)
            };

            var statusTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            statusTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Loading pending changes...",
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };

            var clearButton = new Button
            {
                Text = "Clear All Changes",
                AutoSize = true,
                MinimumSize = new Size(120, 28),
                Anchor = AnchorStyles.Right,
                BackColor = Color.LightCoral,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.DarkRed }
            };
            clearButton.Click += ClearButton_Click;

            statusTable.Controls.Add(statusLabel, 0, 0);
            statusTable.Controls.Add(clearButton, 1, 0);
            statusPanel.Controls.Add(statusTable);

            // Add controls to form
            Controls.Add(tabControl);
            Controls.Add(statusPanel);

            // Load data when shown
            Load += PendingChangesDialog_Load;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all pending changes? This action cannot be undone.",
                "Clear All Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Program.Edits.ClearAllChanges();
                MessageBox.Show("All pending changes have been cleared.", "Changes Cleared",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
        }

        private void InitializeDialog()
        {
            Text = "Pending Changes Preview";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 300;
            Height = 450;
            MinimumSize = new Size(800, 450);
            Icon = SystemIcons.Information;
            ShowInTaskbar = false;
            MaximizeBox = true;
        }

        private void PendingChangesDialog_Load(object sender, EventArgs e)
        {
            try
            {
                LoadPendingChanges();
            }
            catch (Exception ex)
            {
                Program.Log("PendingChangesDialog load failed", ex);
                MessageBox.Show("Failed to load pending changes: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void LoadPendingChanges()
        {
            // Load modified data
            var editsTable = CreateEditsDataTable();
            var editCount = PopulateEditsTable(editsTable);
            editsGrid.DataSource = editsTable;

            // Deletions are not displayed anymore

            // Update tab titles with counts
            tabControl.TabPages[0].Text = $"Modified Data ({editCount})";

            // Update status
            if (editCount == 0)
            {
                statusLabel.Text = "No pending changes found.";
                statusLabel.ForeColor = Color.Gray;
            }
            else
            {
                statusLabel.Text = $"Found {editCount} modified record(s).";
                statusLabel.ForeColor = Color.DarkGreen;
            }

            // Auto-size columns for better visibility
            AutoSizeGridColumns(editsGrid);
        }

        private DataTable CreateEditsDataTable()
        {
            var table = new DataTable("Edits");
            table.Columns.Add("Table", typeof(string));
            table.Columns.Add("LinkID", typeof(string));
            table.Columns.Add("Column", typeof(string));
            table.Columns.Add("Value", typeof(string));
            return table;
        }

        private int PopulateEditsTable(DataTable table)
        {
            int count = 0;

            // Get all table names that have edits
            var editStore = Program.Edits;
            var allTables = GetTablesWithEdits();

            foreach (var tableName in allTables)
            {
                var tableSnapshot = editStore.SnapshotTable(tableName);
                foreach (var linkEntry in tableSnapshot)
                {
                    var linkId = linkEntry.Key;
                    var columnOverrides = linkEntry.Value;

                    foreach (var columnEntry in columnOverrides)
                    {
                        var columnName = columnEntry.Key;
                        var value = columnEntry.Value;

                        var row = table.NewRow();
                        row["Table"] = tableName;
                        row["LinkID"] = linkId;
                        row["Column"] = columnName;
                        row["Value"] = FormatValue(value);
                        table.Rows.Add(row);
                        count++;
                    }
                }
            }

            return count;
        }

                private HashSet<string> GetTablesWithEdits()
        {
            return Program.Edits.GetAllTablesWithEdits();
        }

        private string FormatValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return "<NULL>";

            if (value is string str)
            {
                if (str.Length > 100)
                    return str.Substring(0, 100) + "...";
                return str;
            }

            if (value is byte[] bytes)
                return $"<Binary data: {bytes.Length} bytes>";

            return Convert.ToString(value) ?? "<NULL>";
        }

                private void AutoSizeGridColumns(DataGridView grid)
        {
            try
            {
                grid.SuspendLayout();
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                }
                grid.AutoResizeColumns();

                // Ensure minimum width and handle very wide columns
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (column.Width < 80)
                        column.Width = 80;
                    else if (column.Width > 300)
                        column.Width = 300;

                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
    }
}
