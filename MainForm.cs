using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkOrderBlender
{
    public partial class MainForm : Form
    {
        private const string DefaultRoot = @"M:\Homestead_Library\Work Orders";
        private const string SdfFileName = "MicrovellumWorkOrder.sdf";
        private readonly List<WorkOrderEntry> allWorkOrders = new List<WorkOrderEntry>();
        private List<WorkOrderEntry> filteredWorkOrders = new List<WorkOrderEntry>();
        private readonly HashSet<string> checkedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly (string Label, string Table)[] breakdownMetrics = new (string, string)[]
        {
            ("Products", "Products"),
            ("Parts", "Parts"),
            ("Subassemblies", "Subassemblies"),
            ("Sheets", "Sheets"),
        };

        // Debug switch to log detailed insert failures
        private const bool EnableCopyDebug = true;

        // Preferred column ordering for the details dialog. Columns not listed will follow alphabetically after these.
        private readonly List<string> metricPreferredColumnOrder = new List<string>
        {
            "RoomName",
            "Name",
            "MaterialXData1",
            "LinkID",
            "LinkIDWorkOrder",
            "LinkIDPart",
            "LinkIDProduct",
            "LinkIDSubassembly",
        };

        private CancellationTokenSource scanCts;
        private CancellationTokenSource metricsCts;
        private CancellationTokenSource debounceCts;

        public MainForm()
        {
            InitializeComponent();
            txtRoot.Text = DefaultRoot;
            txtOutput.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ConsolidatedWorkOrders.sdf");
            // Virtualize the big list for performance
            this.listWorkOrders.VirtualMode = true;
            this.listWorkOrders.RetrieveVirtualItem += listWorkOrders_RetrieveVirtualItem;
            // Use custom checkbox via StateImageList (CheckBoxes + VirtualMode toggling is unreliable)
            this.listWorkOrders.CheckBoxes = false;
            this.listWorkOrders.StateImageList = CreateCheckStateImageList();
            this.listWorkOrders.MouseDown += listWorkOrders_MouseDown;
            this.listWorkOrders.KeyDown += listWorkOrders_KeyDown;
            this.Shown += async (s, e) =>
            {
                try
                {
                    await Task.Delay(100); // allow initial paint
                    await ScanAsync();
                }
                catch (Exception ex)
                {
                    Program.Log("Scan on Shown failed", ex);
                    MessageBox.Show("Initial scan failed: " + ex.Message);
                }
            };
        }

        private void listWorkOrders_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            int idx = e.ItemIndex;
            if (idx < 0 || idx >= filteredWorkOrders.Count)
            {
                e.Item = new ListViewItem("");
                return;
            }
            var wo = filteredWorkOrders[idx];
            var item = new ListViewItem(GetDisplayPath(wo.DirectoryPath));
            item.SubItems.Add(wo.SdfExists ? "Yes" : "No");
            item.Tag = wo.SdfPath;
            item.StateImageIndex = checkedDirs.Contains(wo.DirectoryPath) ? 1 : 0; // 1=checked, 0=unchecked
            e.Item = item;
        }

        private void listWorkOrders_MouseDown(object sender, MouseEventArgs e)
        {
            var hit = listWorkOrders.HitTest(e.Location);
            int idx = hit.Item?.Index ?? -1;
            if (idx >= 0 && idx < filteredWorkOrders.Count)
            {
                // Toggle when clicking near the state image or first column
                if (hit.Location == ListViewHitTestLocations.StateImage || hit.SubItem == hit.Item.SubItems[0])
                {
                    var dir = filteredWorkOrders[idx].DirectoryPath;
                    if (checkedDirs.Contains(dir)) checkedDirs.Remove(dir); else checkedDirs.Add(dir);
                    listWorkOrders.Invalidate(hit.Item.Bounds);
                    _ = UpdateConsolidatedBreakdownAsync();
                }
            }
        }

        private void listWorkOrders_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && listWorkOrders.SelectedIndices.Count > 0)
            {
                foreach (int idx in listWorkOrders.SelectedIndices)
                {
                    if (idx < 0 || idx >= filteredWorkOrders.Count) continue;
                    var dir = filteredWorkOrders[idx].DirectoryPath;
                    if (checkedDirs.Contains(dir)) checkedDirs.Remove(dir); else checkedDirs.Add(dir);
                }
                listWorkOrders.Invalidate();
                _ = UpdateConsolidatedBreakdownAsync();
                e.Handled = true;
            }
        }

        private ImageList CreateCheckStateImageList()
        {
            var imgs = new ImageList
            {
                ImageSize = new System.Drawing.Size(16, 16)
            };
            // unchecked
            var bmp0 = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp0))
            {
                g.Clear(System.Drawing.Color.Transparent);
                var rect = new System.Drawing.Rectangle(1, 1, 14, 14);
                g.DrawRectangle(System.Drawing.Pens.Gray, rect);
            }
            imgs.Images.Add(bmp0);
            // checked
            var bmp1 = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp1))
            {
                g.Clear(System.Drawing.Color.Transparent);
                var rect = new System.Drawing.Rectangle(1, 1, 14, 14);
                g.DrawRectangle(System.Drawing.Pens.Gray, rect);
                var oldSmoothing = g.SmoothingMode;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var thickPen = new System.Drawing.Pen(System.Drawing.Color.Green, 3))
                {
                    g.DrawLine(thickPen, 3, 6, 7, 13);
                    g.DrawLine(thickPen, 7, 11, 13, 3);
                }
                g.SmoothingMode = oldSmoothing;
            }
            imgs.Images.Add(bmp1);
            return imgs;
        }

        private string GetDisplayPath(string absoluteDir)
        {
            if (string.IsNullOrEmpty(absoluteDir)) return absoluteDir;
            string root = string.Empty;
            try { root = txtRoot.Text?.Trim() ?? string.Empty; }
            catch { }
            if (string.IsNullOrEmpty(root)) return absoluteDir;
            string absNorm = absoluteDir.Replace('/', '\\');
            string rootNorm = root.Replace('/', '\\').TrimEnd('\\');
            if (absNorm.StartsWith(rootNorm, StringComparison.OrdinalIgnoreCase))
            {
                string rel = absNorm.Substring(rootNorm.Length).TrimStart('\\');
                return rel;
            }
            return absoluteDir;
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            _ = ScanAsync();
        }

        private async Task ScanAsync()
        {
            // Cancel any in-flight scan
            scanCts?.Cancel();
            scanCts = new CancellationTokenSource();
            var token = scanCts.Token;

            SetBusy(true);
            listWorkOrders.Items.Clear();
            string root = txtRoot.Text.Trim();
            if (!Directory.Exists(root))
            {
                MessageBox.Show("Root directory not found.");
                SetBusy(false);
                return;
            }

            try
            {
                var results = await Task.Run(() =>
                {
                    var entries = new List<WorkOrderEntry>();
                    var dirs = Directory.GetDirectories(root);
                    foreach (var dir in dirs)
                    {
                        token.ThrowIfCancellationRequested();
                        string sdfPath = Path.Combine(dir, SdfFileName);
                        bool exists = false;
                        try { exists = File.Exists(sdfPath); }
                        catch (Exception ex) { Program.Log($"File.Exists failed for {sdfPath}", ex); }

                        entries.Add(new WorkOrderEntry
                        {
                            DirectoryPath = dir,
                            SdfPath = sdfPath,
                            SdfExists = exists
                        });
                    }
                    return entries;
                }, token);

                if (token.IsCancellationRequested) return;

                // Update UI / state
                allWorkOrders.Clear();
                allWorkOrders.AddRange(results);
                // Build initial filtered list
                await Task.Yield();
                await FilterAsyncInternal(token);
                if (checkedDirs.Count > 0)
                {
                    await UpdateConsolidatedBreakdownAsync();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Program.Log("ScanAsync error", ex);
                MessageBox.Show("Scan error: " + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // legacy handler no longer used

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (var wo in filteredWorkOrders)
            {
                if (wo.SdfExists)
                {
                    checkedDirs.Add(wo.DirectoryPath);
                }
            }
            listWorkOrders.Invalidate();
            _ = UpdateConsolidatedBreakdownAsync();
        }

        private void btnChooseOutput_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "SQL CE Database (*.sdf)|*.sdf";
                sfd.FileName = Path.GetFileName(txtOutput.Text);
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    txtOutput.Text = sfd.FileName;
                }
            }
        }

        private void btnConsolidate_Click(object sender, EventArgs e)
        {
            var selected = filteredWorkOrders
                .Where(wo => checkedDirs.Contains(wo.DirectoryPath) && File.Exists(wo.SdfPath))
                .Select(wo => new { Directory = wo.DirectoryPath, SdfPath = wo.SdfPath })
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("No work orders selected.");
                return;
            }

            string destPath = txtOutput.Text.Trim();
            if (string.IsNullOrEmpty(destPath))
            {
                MessageBox.Show("Choose an output .sdf file.");
                return;
            }

            try
            {
                RunConsolidation(selected.Select(s => (s.Directory, s.SdfPath)).ToList(), destPath);
                MessageBox.Show("Consolidation complete.");
            }
            catch (Exception ex)
            {
                Program.Log("Consolidation error", ex);
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // debounce to avoid filtering on every keystroke for thousands of items
            debounceCts?.Cancel();
            debounceCts = new CancellationTokenSource();
            var token = debounceCts.Token;
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, token);
                    if (token.IsCancellationRequested) return;
                    await FilterAsyncInternal(CancellationToken.None);
                }
                catch { }
            });
        }

        private async Task FilterAsyncInternal(CancellationToken token)
        {
            string query = string.Empty;
            Invoke(new Action(() => query = txtSearch.Text.Trim()));

            var next = await Task.Run(() =>
            {
                return allWorkOrders.Where(wo => string.IsNullOrEmpty(query) || wo.DirectoryPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                                     .ToList();
            }, token);

            if (token.IsCancellationRequested) return;

            filteredWorkOrders = next;
            listWorkOrders.VirtualListSize = filteredWorkOrders.Count;
            listWorkOrders.Invalidate();
        }

        private void listWorkOrders_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= filteredWorkOrders.Count) return;
            var dir = filteredWorkOrders[e.Index].DirectoryPath;
            if (e.NewValue == CheckState.Checked) checkedDirs.Add(dir); else checkedDirs.Remove(dir);
            // Defer metrics update slightly to avoid multiple recalcs during mass changes
            _ = Task.Run(async () => { await Task.Delay(150); await UpdateConsolidatedBreakdownAsync(); });
        }

        private void listWorkOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            // With VirtualMode selection does not affect metrics; keep behavior minimal
        }

        private void PopulateBreakdown(string sdfPath)
        {
            breakdownList.BeginUpdate();
            breakdownList.Items.Clear();
            if (string.IsNullOrEmpty(sdfPath) || !File.Exists(sdfPath))
            {
                breakdownList.Items.Add(new ListViewItem(new[] { "Status", "No SDF" }));
                breakdownList.EndUpdate();
                return;
            }

            try
            {
                using (var conn = new SqlCeConnection($"Data Source={sdfPath};"))
                {
                    conn.Open();
                    // Basic counts; adjust as needed for your schema
                    AddCount(conn, "Products", "Products");
                    AddCount(conn, "Parts", "Parts");
                    AddCount(conn, "Subassemblies", "Subassemblies");
                    AddCount(conn, "Sheets", "Sheets");
                    AddCount(conn, "PlacedSheets", "PlacedSheets");
                    AddCount(conn, "Routes", "Routes");
                    AddCount(conn, "Drills V", "DrillsVertical");
                    AddCount(conn, "Drills H", "DrillsHorizontal");
                    AddCount(conn, "Vectors", "Vectors");
                }
            }
            catch (Exception ex)
            {
                Program.Log("PopulateBreakdown error", ex);
                breakdownList.Items.Add(new ListViewItem(new[] { "Error", ex.Message }));
            }
            finally
            {
                breakdownList.EndUpdate();
            }
        }

        private void AddCount(SqlCeConnection conn, string label, string table)
        {
            try
            {
                using (var cmd = new SqlCeCommand($"SELECT COUNT(*) FROM [" + table + "]", conn))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    var lvi = new ListViewItem(new[] { label, count.ToString(), "Details" })
                    {
                        Tag = new MetricTag { Label = label, Table = table }
                    };
                    breakdownList.Items.Add(lvi);
                }
            }
            catch
            {
                // table may not exist in some SDFs; ignore
            }
        }

        private async Task UpdateConsolidatedBreakdownAsync()
        {
            // Cancel previous metrics calculation
            metricsCts?.Cancel();
            metricsCts = new CancellationTokenSource();
            var token = metricsCts.Token;

            breakdownList.BeginUpdate();
            breakdownList.Items.Clear();
            breakdownList.Items.Add(new ListViewItem(new[] { "Status", "Calculating..." }));
            breakdownList.EndUpdate();

            // Snapshot selected paths on UI thread
            var paths = filteredWorkOrders.Where(wo => checkedDirs.Contains(wo.DirectoryPath) && File.Exists(wo.SdfPath))
                                          .Select(wo => wo.SdfPath)
                                          .ToList();

            var totals = await Task.Run(() =>
            {
                var localTotals = breakdownMetrics.ToDictionary(m => m.Label, m => 0);
                foreach (var path in paths)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        using (var conn = new SqlCeConnection($"Data Source={path};"))
                        {
                            conn.Open();
                            foreach (var (label, table) in breakdownMetrics)
                            {
                                localTotals[label] += TryCount(conn, table);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Log($"Counting failed for {path}", ex);
                    }
                }
                return localTotals;
            }, token);

            if (token.IsCancellationRequested) return;

            breakdownList.BeginUpdate();
            breakdownList.Items.Clear();
            if (paths.Count == 0)
            {
                breakdownList.Items.Add(new ListViewItem(new[] { "No Selection", "...", string.Empty }));
            }
            else
            {
                foreach (var (label, table) in breakdownMetrics)
                {
                    var lvi = new ListViewItem(new[] { label, totals[label].ToString(), "Details" });
                    lvi.Tag = new MetricTag { Label = label, Table = table };
                    breakdownList.Items.Add(lvi);
                }
            }
            breakdownList.EndUpdate();
        }

        private int TryCount(SqlCeConnection conn, string table)
        {
            try
            {
                using (var cmd = new SqlCeCommand($"SELECT COUNT(*) FROM [" + table + "]", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch
            {
                return 0;
            }
        }

        private void breakdownList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var hit = breakdownList.HitTest(e.Location);
            if (hit.Item == null || hit.SubItem == null) return;
            int colIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (colIndex != 2) return; // Action column

            if (hit.Item.Tag is MetricTag tag)
            {
                OpenMetricDialog(tag);
            }
        }

        private void breakdownList_MouseMove(object sender, MouseEventArgs e)
        {
            var hit = breakdownList.HitTest(e.Location);
            if (hit.Item != null && hit.SubItem != null && hit.Item.SubItems.IndexOf(hit.SubItem) == 2)
            {
                if (breakdownList.Cursor != Cursors.Hand)
                {
                    breakdownList.Cursor = Cursors.Hand;
                }
            }
            else
            {
                if (breakdownList.Cursor != Cursors.Default)
                {
                    breakdownList.Cursor = Cursors.Default;
                }
            }
        }

        // Owner-draw metrics to render a button-like Action cell
        private void breakdownList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void breakdownList_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // handled in subitem drawing
        }

        private void breakdownList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex != 2)
            {
                e.DrawDefault = true;
                return;
            }
            var bounds = e.Bounds;
            var btnRect = new System.Drawing.Rectangle(bounds.X + 4, bounds.Y, 70, bounds.Height);
            System.Windows.Forms.ButtonRenderer.DrawButton(e.Graphics, btnRect, System.Windows.Forms.VisualStyles.PushButtonState.Default);
            using (var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center })
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
            {
                e.Graphics.DrawString("Details", breakdownList.Font, brush, btnRect, sf);
            }
        }

        // removed btnOpenDetails handler (per-row Action buttons used instead)

        private void OpenMetricDialog(MetricTag tag)
        {
            // Collect selected SDF paths
            var paths = filteredWorkOrders.Where(wo => checkedDirs.Contains(wo.DirectoryPath) && File.Exists(wo.SdfPath))
                                          .Select(wo => wo.SdfPath)
                                          .ToList();
            if (paths.Count == 0)
            {
                MessageBox.Show("No work orders selected.");
                return;
            }

            // Simple viewer: show first 200 rows of the selected table across all selected SDFs
            // In a real app, you might build a proper grid dialog; here we use a basic form with a ListView
            var dlg = new Form
            {
                Text = $"Data for {tag.Label}",
                Width = 1500,
                Height = 800,
                StartPosition = FormStartPosition.CenterScreen
            };
            var grid = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                GridLines = true
            };
            grid.AllowColumnReorder = true;
            dlg.Controls.Add(grid);

            try
            {
                // Determine columns from first table that exists
                DataTable schema = null;
                foreach (var path in paths)
                {
                    using (var conn = new SqlCeConnection($"Data Source={path};"))
                    {
                        conn.Open();
                        try
                        {
                            using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tag.Table + "]", conn))
                            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                            {
                                schema = reader.GetSchemaTable();
                                if (schema != null) break;
                            }
                        }
                        catch { }
                    }
                }
                if (schema == null)
                {
                    MessageBox.Show($"Table '{tag.Table}' not found in selected work orders.");
                    return;
                }

                var colNames = schema.Rows.Cast<DataRow>().Select(r => r["ColumnName"].ToString()).ToList();
                // Order columns by preferred order first, then remaining alphabetically
                var orderedColNames = colNames
                    .OrderBy(name =>
                    {
                        int idx = metricPreferredColumnOrder.FindIndex(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
                        return idx < 0 ? int.MaxValue : idx;
                    })
                    .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var col in orderedColNames)
                {
                    grid.Columns.Add(col, 120);
                }

                foreach (var path in paths)
                {
                    using (var conn = new SqlCeConnection($"Data Source={path};"))
                    {
                        conn.Open();
                        try
                        {
                            using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tag.Table + "]", conn))
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var values = new string[orderedColNames.Count];
                                    for (int i = 0; i < orderedColNames.Count; i++)
                                    {
                                        string colName = orderedColNames[i];
                                        int ord;
                                        try { ord = reader.GetOrdinal(colName); }
                                        catch { ord = -1; }
                                        if (ord >= 0)
                                        {
                                            var v = reader.GetValue(ord);
                                            values[i] = v == DBNull.Value ? "" : v.ToString();
                                        }
                                        else
                                        {
                                            values[i] = string.Empty;
                                        }
                                    }
                                    grid.Items.Add(new ListViewItem(values));
                                }
                            }
                        }
                        catch { }
                    }
                }
                // Auto-size after rows added
                AutoSizeListViewColumns(grid);
                // Re-apply autosize when dialog resized
                dlg.ResizeEnd += (s, e2) => AutoSizeListViewColumns(grid);
            }
            catch (Exception ex)
            {
                Program.Log("OpenMetricDialog error", ex);
                MessageBox.Show(ex.Message);
            }

            dlg.ShowDialog(this);
        }

        private sealed class MetricTag
        {
            public string Label { get; set; }
            public string Table { get; set; }
        }

        private static void AutoSizeListViewColumns(ListView view)
        {
            try
            {
                view.BeginUpdate();
                // Fit to content, then ensure headers are visible if wider
                view.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                view.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                foreach (ColumnHeader ch in view.Columns)
                {
                    if (ch.Width < 50) ch.Width = 50;
                }
            }
            finally
            {
                view.EndUpdate();
            }
        }

        private void SetBusy(bool isBusy)
        {
            try
            {
                progress.Style = isBusy ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
                progress.MarqueeAnimationSpeed = isBusy ? 30 : 0;
            }
            catch { }
        }

        private void RunConsolidation(List<(string WorkOrderDir, string SdfPath)> sources, string destinationPath)
        {
            progress.Value = 0;
            progress.Maximum = sources.Count;

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            CreateEmptyDatabase(destinationPath);

            using (var dest = new SqlCeConnection($"Data Source={destinationPath};"))
            {
                dest.Open();

                foreach (var src in sources)
                {
                    // Combined mode with source metadata columns
                    string sourceTag = new DirectoryInfo(src.WorkOrderDir).Name;
                    CopyDatabaseCombined(src.SdfPath, dest, sourceTag, src.WorkOrderDir);
                    progress.Value += 1;
                }
            }
        }

        private static string GeneratePrefixFromDirectory(string directoryPath)
        {
            string name = new DirectoryInfo(directoryPath).Name;
            string alnum = Regex.Replace(name, "[^A-Za-z0-9]+", "_");
            if (string.IsNullOrWhiteSpace(alnum)) alnum = "WO";
            return alnum + "_";
        }

        private static void CreateEmptyDatabase(string path)
        {
            var engine = new SqlCeEngine($"Data Source={path};");
            engine.CreateDatabase();
        }

        private static void CopyDatabase(string sourcePath, SqlCeConnection destConn, string tablePrefix)
        {
            using (var srcConn = new SqlCeConnection($"Data Source={sourcePath};"))
            {
                srcConn.Open();
                var schema = srcConn.GetSchema("Tables");
                foreach (DataRow row in schema.Rows)
                {
                    string table = row["TABLE_NAME"].ToString();
                    if (table.StartsWith("__")) continue; // skip system tables

                    EnsureDestinationTable(destConn, srcConn, table, tablePrefix);
                    CopyRows(srcConn, destConn, table, tablePrefix);
                }
            }
        }

        private static void CopyDatabaseCombined(string sourcePath, SqlCeConnection destConn, string sourceTag, string sourcePathDir)
        {
            using (var srcConn = new SqlCeConnection($"Data Source={sourcePath};"))
            {
                srcConn.Open();
                var schema = srcConn.GetSchema("Tables");
                foreach (DataRow row in schema.Rows)
                {
                    string table = row["TABLE_NAME"].ToString();
                    if (table.StartsWith("__")) continue;

                    EnsureDestinationTableCombined(destConn, srcConn, table);
                    CopyRowsCombined(srcConn, destConn, table, sourceTag, sourcePathDir);
                }
            }
        }

        private static void EnsureDestinationTable(SqlCeConnection dest, SqlCeConnection src, string tableName, string prefix)
        {
            string destTable = prefix + tableName;
            using (var cmd = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", dest))
            {
                cmd.Parameters.AddWithValue("@t", destTable);
                var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                if (exists) return;
            }

            var cols = src.GetSchema("Columns", new[] { null, null, tableName, null });
            var pks = src.GetSchema("IndexColumns", new[] { null, null, null, null, tableName });

            var columnDefs = new List<string>();
            foreach (DataRow col in cols.Rows)
            {
                string colName = col["COLUMN_NAME"].ToString();
                string dataType = col["DATA_TYPE"].ToString();
                int length = col.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
                bool nullable = col.Table.Columns.Contains("IS_NULLABLE") && string.Equals(col["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);

                string typeSql = MapType(dataType, length);
                string nullSql = nullable ? "NULL" : "NOT NULL";
                columnDefs.Add($"[{colName}] {typeSql} {nullSql}");
            }

            string createSql = $"CREATE TABLE [{destTable}] (" + string.Join(", ", columnDefs) + ")";
            using (var create = new SqlCeCommand(createSql, dest))
            {
                create.ExecuteNonQuery();
            }

            try
            {
                var pkCols = new List<string>();
                foreach (DataRow r in pks.Rows)
                {
                    if (string.Equals(r["PRIMARY_KEY"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        pkCols.Add("[" + r["COLUMN_NAME"].ToString() + "]");
                    }
                }

                if (pkCols.Count > 0)
                {
                    string pkSql = $"ALTER TABLE [{destTable}] ADD CONSTRAINT [PK_{destTable}] PRIMARY KEY (" + string.Join(", ", pkCols) + ")";
                    using (var pk = new SqlCeCommand(pkSql, dest))
                    {
                        pk.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void EnsureDestinationTableCombined(SqlCeConnection dest, SqlCeConnection src, string tableName)
        {
            using (var cmd = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", dest))
            {
                cmd.Parameters.AddWithValue("@t", tableName);
                var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                if (!exists)
                {
                    // Create like source
                    var cols = src.GetSchema("Columns", new[] { null, null, tableName, null });
                    var columnDefs = new List<string>();
                    foreach (DataRow col in cols.Rows)
                    {
                        string colName = col["COLUMN_NAME"].ToString();
                        string dataType = col["DATA_TYPE"].ToString();
                        int length = col.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
                        bool nullable = col.Table.Columns.Contains("IS_NULLABLE") && string.Equals(col["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);
                        string typeSql = MapType(dataType, length);
                        string nullSql = nullable ? "NULL" : "NOT NULL";
                        columnDefs.Add($"[{colName}] {typeSql} {nullSql}");
                    }
                    columnDefs.Add("[SourceWorkOrder] NVARCHAR(255) NULL");
                    columnDefs.Add("[SourcePath] NVARCHAR(1024) NULL");
                    string createSql = $"CREATE TABLE [{tableName}] (" + string.Join(", ", columnDefs) + ")";
                    using (var create = new SqlCeCommand(createSql, dest))
                    {
                        create.ExecuteNonQuery();
                    }
                }
                else
                {
                    try
                    {
                        // Add columns if missing in dest (create superset schema)
                        var srcCols = src.GetSchema("Columns", new[] { null, null, tableName, null });
                        var destCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var dc = new SqlCeCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", dest))
                        {
                            dc.Parameters.AddWithValue("@t", tableName);
                            using (var r = dc.ExecuteReader())
                            {
                                while (r.Read()) destCols.Add(r.GetString(0));
                            }
                        }
                        foreach (DataRow col in srcCols.Rows)
                        {
                            string colName = col["COLUMN_NAME"].ToString();
                            if (destCols.Contains(colName)) continue;
                            string dataType = col["DATA_TYPE"].ToString();
                            int length = srcCols.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && col["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]) : -1;
                            string typeSql = MapType(dataType, length);
                            using (var alter = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [" + colName + "] " + typeSql + " NULL", dest)) alter.ExecuteNonQuery();
                        }
                        // Ensure source metadata columns
                        using (var c1 = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME='SourceWorkOrder'", dest))
                        {
                            c1.Parameters.AddWithValue("@t", tableName);
                            if (Convert.ToInt32(c1.ExecuteScalar()) == 0)
                            {
                                using (var a1 = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [SourceWorkOrder] NVARCHAR(255) NULL", dest)) a1.ExecuteNonQuery();
                            }
                        }
                        using (var c2 = new SqlCeCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME='SourcePath'", dest))
                        {
                            c2.Parameters.AddWithValue("@t", tableName);
                            if (Convert.ToInt32(c2.ExecuteScalar()) == 0)
                            {
                                using (var a2 = new SqlCeCommand($"ALTER TABLE [" + tableName + "] ADD [SourcePath] NVARCHAR(1024) NULL", dest)) a2.ExecuteNonQuery();
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private static string MapType(string dataType, int length)
        {
            string dt = dataType.ToLowerInvariant();
            switch (dt)
            {
                case "nvarchar":
                case "nchar":
                case "ntext":
                    if (length <= 0) return "NTEXT";
                    // SQL CE max NVARCHAR length is 4000; longer must be NTEXT
                    return length > 4000 ? "NTEXT" : $"NVARCHAR({Math.Min(length, 4000)})";
                case "varchar":
                case "char":
                case "text":
                    return length > 0 ? $"VARCHAR({length})" : "TEXT";
                case "int":
                case "integer":
                    return "INT";
                case "bigint":
                    return "BIGINT";
                case "smallint":
                    return "SMALLINT";
                case "tinyint":
                    return "TINYINT";
                case "bit":
                    return "BIT";
                case "decimal":
                case "numeric":
                    return "DECIMAL(18,4)";
                case "float":
                    return "FLOAT";
                case "real":
                    return "REAL";
                case "money":
                case "smallmoney":
                    return "MONEY";
                case "datetime":
                case "smalldatetime":
                    return "DATETIME";
                case "uniqueidentifier":
                    return "UNIQUEIDENTIFIER";
                case "image":
                case "varbinary":
                    return "IMAGE";
                default:
                    return "NVARCHAR(255)";
            }
        }

        private static void CopyRows(SqlCeConnection src, SqlCeConnection dest, string tableName, string prefix)
        {
            string destTable = prefix + tableName;
            using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", src))
            using (var reader = cmd.ExecuteReader())
            {
                var schema = reader.GetSchemaTable();
                var colNames = schema.Rows.Cast<DataRow>().Select(r => r["ColumnName"].ToString()).ToList();
                string columnList = string.Join(", ", colNames.Select(n => "[" + n + "]"));
                string paramList = string.Join(", ", colNames.Select(n => "@" + n));

                string insertSql = $"INSERT INTO [" + destTable + "] (" + columnList + ") VALUES (" + paramList + ")";
                using (var insert = new SqlCeCommand(insertSql, dest))
                {
                    foreach (var name in colNames)
                    {
                        // Let provider infer type; Variant is not supported by SQL CE
                        insert.Parameters.Add(new SqlCeParameter("@" + name, DBNull.Value));
                    }

                    int rowIndex = 0;
                    while (reader.Read())
                    {
                        try
                        {
                            for (int i = 0; i < colNames.Count; i++)
                            {
                                object value = reader.GetValue(i);
                                var p = insert.Parameters["@" + colNames[i]];
                                p.Value = value is DBNull ? (object)DBNull.Value : value;
                            }
                            foreach (SqlCeParameter p in insert.Parameters)
                            {
                                if (p.Value == null) p.Value = DBNull.Value;
                            }
                            insert.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            if (EnableCopyDebug)
                            {
                                LogInsertFailure(dest, tableName, destTable, colNames, insert, rowIndex, ex, null, null);
                            }
                            throw;
                        }
                        rowIndex++;
                    }
                }
            }
        }

        private static void CopyRowsCombined(SqlCeConnection src, SqlCeConnection dest, string tableName, string sourceTag, string sourcePathDir)
        {
            using (var cmd = new SqlCeCommand($"SELECT * FROM [" + tableName + "]", src))
            using (var reader = cmd.ExecuteReader())
            {
                var schema = reader.GetSchemaTable();
                var colNames = schema.Rows.Cast<DataRow>().Select(r => r["ColumnName"].ToString()).ToList();
                var destColumns = new List<string>(colNames.Select(n => "[" + n + "]"));
                destColumns.Add("[SourceWorkOrder]");
                destColumns.Add("[SourcePath]");
                var destParams = new List<string>(colNames.Select(n => "@" + n));
                destParams.Add("@__srcWO");
                destParams.Add("@__srcPath");
                string insertSql = $"INSERT INTO [" + tableName + "] (" + string.Join(", ", destColumns) + ") VALUES (" + string.Join(", ", destParams) + ")";
                using (var insert = new SqlCeCommand(insertSql, dest))
                {
                    foreach (var name in colNames)
                    {
                        insert.Parameters.Add(new SqlCeParameter("@" + name, DBNull.Value));
                    }
                    insert.Parameters.Add(new SqlCeParameter("@__srcWO", sourceTag ?? string.Empty));
                    insert.Parameters.Add(new SqlCeParameter("@__srcPath", sourcePathDir ?? string.Empty));
                    int rowIndex = 0;
                    while (reader.Read())
                    {
                        try
                        {
                            for (int i = 0; i < colNames.Count; i++)
                            {
                                object value = reader.GetValue(i);
                                var p = insert.Parameters["@" + colNames[i]];
                                p.Value = value is DBNull ? (object)DBNull.Value : value;
                            }
                            foreach (SqlCeParameter p in insert.Parameters)
                            {
                                if (p.Value == null) p.Value = DBNull.Value;
                            }
                            insert.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            if (EnableCopyDebug)
                            {
                                LogInsertFailure(dest, tableName, tableName, colNames, insert, rowIndex, ex, sourceTag, sourcePathDir);
                            }
                            throw;
                        }
                        rowIndex++;
                    }
                }
            }
        }

        private static void LogInsertFailure(SqlCeConnection dest, string srcTable, string destTable, List<string> colNames, SqlCeCommand insert, int rowIndex, Exception ex, string sourceTag, string sourcePath)
        {
            try
            {
                var destCols = new List<string>();
                using (var dc = new SqlCeCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", dest))
                {
                    dc.Parameters.AddWithValue("@t", destTable);
                    using (var r = dc.ExecuteReader())
                    {
                        while (r.Read()) destCols.Add(r.GetString(0));
                    }
                }
                var paramDump = string.Join(
                    ", ",
                    insert.Parameters.Cast<SqlCeParameter>().Select(p => $"{p.ParameterName}={(p.Value==null?"<null>":(p.Value==DBNull.Value?"<DBNull>":Convert.ToString(p.Value)))}"));
                Program.Log($"INSERT FAILURE\n SrcTable={srcTable}\n DestTable={destTable}\n RowIndex={rowIndex}\n SourceTag={sourceTag}\n SourcePath={sourcePath}\n SrcCols=[{string.Join(", ", colNames)}]\n DestCols=[{string.Join(", ", destCols)}]\n SQL={insert.CommandText}\n Params=[{paramDump}]\n Error={ex}");
            }
            catch (Exception logEx)
            {
                Program.Log("LogInsertFailure error", logEx);
            }
        }
    }

    internal sealed class WorkOrderEntry
    {
        public string DirectoryPath { get; set; }
        public string SdfPath { get; set; }
        public bool SdfExists { get; set; }
    }
}
