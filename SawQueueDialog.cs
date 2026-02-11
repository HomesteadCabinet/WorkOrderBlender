using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WorkOrderBlender
{
  public sealed class SawQueueDialog : Form
  {
    private readonly SplitContainer mainSplitContainer;
    private readonly DataGridView stagingDataGrid;
    private readonly DataGridView releaseDataGrid;
    private readonly Label stagingLabel;
    private readonly Label releaseLabel;
    private readonly Label stagingStatusLabel;
    private readonly Label releaseStatusLabel;
    private readonly TextBox stagingFilterTextBox;
    private readonly TextBox releaseFilterTextBox;
    private readonly string stagingDir;
    private readonly string releaseDir;
    private readonly ReleaseFileTracker releaseTracker;
    private FileSystemWatcher releaseDirWatcher;
    private FileSystemWatcher stagingDirWatcher;
    private List<FileDisplayItem> allStagingFiles; // Store original staging files for filtering
    private List<TrackedReleaseFile> allReleaseFiles; // Store original release files for filtering
    private System.Windows.Forms.Timer stagingFilterTimer; // Debounce staging filter typing
    private System.Windows.Forms.Timer stagingRefreshTimer; // Debounce staging list refresh from watcher
    private int stagingSortColumnIndex = -1;
    private bool stagingSortAscending = true;
    private string stagingSortColumnName = null; // Column name for re-applying sort after filter
    private int releaseSortColumnIndex = -1;
    private bool releaseSortAscending = true;
    private string releaseSortColumnName = null;

    private const int StagingFileCountWarningThreshold = 200;

    public SawQueueDialog()
    {
      // Load directories from configuration
      var cfg = UserConfig.LoadOrDefault();
      stagingDir = cfg.StagingDir ?? @"P:\CadLinkPTX\staging";
      releaseDir = cfg.ReleaseDir ?? @"P:\CadLinkPTX\release";

      // Initialize release file tracker with release directory for shared CSV file
      var maxTrackedFiles = cfg.MaxTrackedFiles;
      releaseTracker = new ReleaseFileTracker(releaseDir, maxTrackedFiles);

      // Subscribe to CSV file changes to synchronize across instances
      releaseTracker.CsvFileChanged += ReleaseTracker_CsvFileChanged;

      // Initialize filter lists
      allStagingFiles = new List<FileDisplayItem>();
      allReleaseFiles = new List<TrackedReleaseFile>();

      InitializeDialog();

      // Create main split container for two vertical panes
      mainSplitContainer = new SplitContainer
      {
        Dock = DockStyle.Fill,
        Orientation = Orientation.Vertical,
        SplitterDistance = (int)(this.ClientSize.Width * 0.50), // Left panel gets 50%, right panel gets 50%
        BorderStyle = BorderStyle.Fixed3D
      };

      // Create left panel (Staging)
      var leftPanel = new Panel { Dock = DockStyle.Fill };
      // Create panel for staging label with Open Folder button
      var stagingHeaderPanel = new Panel
      {
        Dock = DockStyle.Top,
        Height = 30,
        BackColor = Color.FromArgb(240, 240, 240),
        Padding = new Padding(5, 0, 5, 0),
      };

      // Calculate vertical center position accounting for padding
      var panelPadding = stagingHeaderPanel.Padding.Top;
      var labelHeight = 20; // Standard label height
      var centeredTop = panelPadding + (stagingHeaderPanel.Height - panelPadding * 2 - labelHeight) / 2;

      stagingLabel = new Label
      {
        Text = $"Staging Directory: {stagingDir}",
        Left = 10,
        Top = stagingHeaderPanel.Height - labelHeight,
        AutoSize = true,
        Height = labelHeight,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.Hand,
        ForeColor = Color.FromArgb(0, 102, 204) // Blue color to indicate clickable
      };
      stagingLabel.Click += StagingLabel_Click;

      // Create container panel for search controls
      var stagingSearchPanel = new Panel
      {
        Dock = DockStyle.Right,
        Width = 250,
        BackColor = Color.Transparent
      };

      var stagingSearchLabel = new Label
      {
        Text = "Search:",
        Left = 5,
        Top = centeredTop,
        Width = 50,
        Height = labelHeight,
        Font = new Font("Segoe UI", 8F, FontStyle.Regular),
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleLeft
      };

      stagingFilterTextBox = new TextBox
      {
        Left = 60,
        Top = centeredTop,
        Width = 180,
        Height = labelHeight,
        Font = new Font("Segoe UI", 8F, FontStyle.Regular),
        Anchor = AnchorStyles.Right | AnchorStyles.Top
      };
      stagingFilterTextBox.TextChanged += StagingFilterTextBox_TextChanged;

      // Debounce staging filter so we don't re-apply on every keystroke when many files
      stagingFilterTimer = new System.Windows.Forms.Timer();
      stagingFilterTimer.Interval = 300;
      stagingFilterTimer.Tick += StagingFilterTimer_Tick;

      stagingSearchPanel.Controls.Add(stagingSearchLabel);
      stagingSearchPanel.Controls.Add(stagingFilterTextBox);

      stagingHeaderPanel.Controls.Add(stagingLabel);
      stagingHeaderPanel.Controls.Add(stagingSearchPanel);

      stagingStatusLabel = new Label
      {
        Text = "Loading files...",
        Dock = DockStyle.Bottom,
        Height = 30,
        Padding = new Padding(10, 5, 10, 5),
        BackColor = Color.FromArgb(250, 250, 250),
        ForeColor = Color.DarkBlue,
        TextAlign = ContentAlignment.MiddleLeft
      };

      stagingDataGrid = new DataGridView
      {
        Dock = DockStyle.Fill,
        Font = GetNonMonospaceFont(9F),
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.None
      };

      // Set up columns for staging DataGridView
      stagingDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "JobName",
        HeaderText = "Job Name",
        DataPropertyName = "JobName",
        FillWeight = 50
      });
      stagingDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "FileSize",
        HeaderText = "Size",
        DataPropertyName = "FileSize",
        FillWeight = 20
      });
      stagingDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "LastModified",
        HeaderText = "Last Modified",
        DataPropertyName = "LastModified",
        FillWeight = 30
      });

      // Create context menu for staging DataGridView
      var stagingContextMenu = new ContextMenuStrip();
      var moveMenuItem = new ToolStripMenuItem("Move to Release");
      moveMenuItem.Click += StagingContextMenu_Move_Click;

      var editJobNameMenuItem = new ToolStripMenuItem("Edit Job Name");
      editJobNameMenuItem.Click += StagingContextMenu_EditJobName_Click;

      var showPartsMenuItem = new ToolStripMenuItem("Show Parts");
      showPartsMenuItem.Click += StagingContextMenu_ShowParts_Click;

      var compareFilesMenuItem = new ToolStripMenuItem("Compare Files");
      compareFilesMenuItem.Click += StagingContextMenu_CompareFiles_Click;

      var deleteMenuItem = new ToolStripMenuItem("Delete");
      deleteMenuItem.Click += StagingContextMenu_Delete_Click;

      stagingContextMenu.Items.Add(editJobNameMenuItem);
      stagingContextMenu.Items.Add(showPartsMenuItem);
      stagingContextMenu.Items.Add(compareFilesMenuItem);
      stagingContextMenu.Items.Add(moveMenuItem);
      stagingContextMenu.Items.Add(new ToolStripSeparator());
      stagingContextMenu.Items.Add(deleteMenuItem);

      // Add Opening event handler to enable/disable menu items based on selection
      stagingContextMenu.Opening += StagingContextMenu_Opening;

      stagingDataGrid.ContextMenuStrip = stagingContextMenu;

      // Add double-click handler to edit job name when clicking on JobName column
      stagingDataGrid.CellDoubleClick += StagingDataGrid_CellDoubleClick;

      stagingDataGrid.ColumnHeaderMouseClick += StagingDataGrid_ColumnHeaderMouseClick;

      // Reduce flicker and improve paint performance with many rows
      try
      {
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
          ?.SetValue(stagingDataGrid, true, null);
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Could not set DoubleBuffered on staging grid: {ex.Message}");
      }

      leftPanel.Controls.Add(stagingDataGrid);
      leftPanel.Controls.Add(stagingHeaderPanel);
      leftPanel.Controls.Add(stagingStatusLabel);

      // Create right panel (Release)
      var rightPanel = new Panel { Dock = DockStyle.Fill };

      // Create panel for release label with Open Folder button
      var releaseHeaderPanel = new Panel
      {
        Dock = DockStyle.Top,
        Height = 30,
        BackColor = Color.FromArgb(240, 240, 240),
        Padding = new Padding(5, 0, 5, 0),
      };

      // Calculate vertical center position accounting for padding
      var releasePanelPadding = releaseHeaderPanel.Padding.Top;
      var releaseLabelHeight = 20; // Standard label height
      var releaseCenteredTop = releasePanelPadding + (releaseHeaderPanel.Height - releasePanelPadding * 2 - releaseLabelHeight) / 2;

      releaseLabel = new Label
      {
        Text = $"Release Directory: {releaseDir}",
        Left = 10,
        Top = releaseHeaderPanel.Height - releaseLabelHeight,
        AutoSize = true,
        Height = releaseLabelHeight,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.Hand,
        ForeColor = Color.FromArgb(0, 102, 204) // Blue color to indicate clickable
      };
      releaseLabel.Click += ReleaseLabel_Click;

      // Create container panel for search controls
      var releaseSearchPanel = new Panel
      {
        Dock = DockStyle.Right,
        Width = 250,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 0, 0, 5),
        Margin = new Padding(0, 0, 0, 5),
      };

      // Use same centered top position for search controls
      var releaseSearchLabel = new Label
      {
        Text = "Search:",
        Left = 5,
        Top = releaseCenteredTop,
        Width = 50,
        Height = releaseLabelHeight,
        Font = new Font("Segoe UI", 8F, FontStyle.Regular),
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleLeft
      };

      releaseFilterTextBox = new TextBox
      {
        Left = 60,
        Top = releaseCenteredTop,
        Width = 180,
        Height = releaseLabelHeight,
        Font = new Font("Segoe UI", 8F, FontStyle.Regular),
        Anchor = AnchorStyles.Right | AnchorStyles.Top
      };
      releaseFilterTextBox.TextChanged += ReleaseFilterTextBox_TextChanged;

      releaseSearchPanel.Controls.Add(releaseSearchLabel);
      releaseSearchPanel.Controls.Add(releaseFilterTextBox);

      releaseStatusLabel = new Label
      {
        Text = "Loading files...",
        Dock = DockStyle.Bottom,
        Height = 30,
        Padding = new Padding(10, 5, 10, 5),
        BackColor = Color.FromArgb(250, 250, 250),
        ForeColor = Color.DarkBlue,
        TextAlign = ContentAlignment.MiddleLeft
      };

      releaseDataGrid = new DataGridView
      {
        Dock = DockStyle.Fill,
        Font = GetNonMonospaceFont(9F),
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.None
      };

      // Set up columns for release DataGridView
      releaseDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "JobName",
        HeaderText = "Job Name",
        DataPropertyName = "JobName",
        FillWeight = 40
      });
      releaseDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "Status",
        HeaderText = "Status",
        DataPropertyName = "Status",
        FillWeight = 15
      });
      releaseDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "ReleaseDate",
        HeaderText = "Release Date",
        DataPropertyName = "ReleaseDate",
        FillWeight = 25
      });
      releaseDataGrid.Columns.Add(new DataGridViewTextBoxColumn
      {
        Name = "SentToSawDate",
        HeaderText = "Sent to Saw",
        DataPropertyName = "SentToSawDate",
        FillWeight = 20
      });

      // Add CellFormatting event to color the Status column
      releaseDataGrid.CellFormatting += ReleaseDataGrid_CellFormatting;

      // Create context menu for release DataGridView
      var releaseContextMenu = new ContextMenuStrip();

      var moveToStagingMenuItem = new ToolStripMenuItem("Move back to Staging");
      moveToStagingMenuItem.Click += ReleaseContextMenu_MoveToStaging_Click;
      releaseContextMenu.Items.Add(moveToStagingMenuItem);

      var archiveMenuItem = new ToolStripMenuItem("Archive");
      archiveMenuItem.Click += ReleaseContextMenu_Archive_Click;
      releaseContextMenu.Items.Add(archiveMenuItem);

      // Add Opening event handler to enable/disable menu items based on selection
      releaseContextMenu.Opening += ReleaseContextMenu_Opening;

      releaseDataGrid.ContextMenuStrip = releaseContextMenu;

      releaseDataGrid.ColumnHeaderMouseClick += ReleaseDataGrid_ColumnHeaderMouseClick;

      // Reduce flicker and improve paint performance with many rows
      try
      {
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
          ?.SetValue(releaseDataGrid, true, null);
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Could not set DoubleBuffered on release grid: {ex.Message}");
      }

      releaseHeaderPanel.Controls.Add(releaseLabel);
      releaseHeaderPanel.Controls.Add(releaseSearchPanel);

      rightPanel.Controls.Add(releaseDataGrid);
      rightPanel.Controls.Add(releaseHeaderPanel);
      rightPanel.Controls.Add(releaseStatusLabel);

      // Add panels to split container
      mainSplitContainer.Panel1.Controls.Add(leftPanel);
      mainSplitContainer.Panel2.Controls.Add(rightPanel);

      // Create button panel at the bottom
      var buttonPanel = CreateButtonPanel();

      // Add controls to form
      Controls.Add(mainSplitContainer);
      Controls.Add(buttonPanel);

      // Load data when shown
      Load += SawQueueDialog_Load;

      // Set up form closing to dispose watcher
      FormClosing += SawQueueDialog_FormClosing;

      // Debounce staging list refresh when watcher fires (e.g. many files dropped at once)
      stagingRefreshTimer = new System.Windows.Forms.Timer();
      stagingRefreshTimer.Interval = 400;
      stagingRefreshTimer.Tick += StagingRefreshTimer_Tick;
    }

    private void StagingRefreshTimer_Tick(object sender, EventArgs e)
    {
      try
      {
        if (stagingRefreshTimer != null) stagingRefreshTimer.Stop();
        RefreshStagingList();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in staging refresh timer", ex);
      }
    }

    private void InitializeDialog()
    {
      Text = "Saw Queue - Saw Cutting Pattern Manager";
      StartPosition = FormStartPosition.CenterScreen;
      Width = 1500;
      Height = 700;
      MinimumSize = new Size(800, 500);
      Icon = SystemIcons.Application;
      ShowInTaskbar = false;
      MaximizeBox = true;
    }

    // Helper method to get a non-monospace font, preferring Noto Sans
    private Font GetNonMonospaceFont(float size)
    {
      // Try Noto Sans first, fall back to Segoe UI if not available
      try
      {
        var testFont = new Font("Noto Sans", size);
        // If we get here, the font exists - dispose test and return new instance
        testFont.Dispose();
        return new Font("Noto Sans", size);
      }
      catch
      {
        // Noto Sans not available, use Segoe UI
        return new Font("Segoe UI", size);
      }
    }

    private Panel CreateButtonPanel()
    {
      var panel = new TableLayoutPanel
      {
        Dock = DockStyle.Bottom,
        ColumnCount = 4,
        RowCount = 1,
        Padding = new Padding(10, 5, 10, 10),
        Height = 50,
      };

      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Close
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // spacer
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Refresh
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Move to Release

      var btnClose = new Button
      {
        Text = "Close",
        AutoSize = true,
        BackColor = Color.FromArgb(215, 218, 222),
        ForeColor = Color.Black,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 80
      };
      btnClose.FlatAppearance.BorderSize = 1;
      btnClose.Click += (s, e) => Close();

      var btnRefresh = new Button
      {
        Text = "Refresh",
        AutoSize = true,
        BackColor = Color.FromArgb(0, 123, 255),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 100
      };
      btnRefresh.FlatAppearance.BorderSize = 1;
      btnRefresh.Click += BtnRefresh_Click;

      var btnMoveToRelease = new Button
      {
        Text = "Move to Release →",
        AutoSize = true,
        BackColor = Color.FromArgb(40, 167, 69),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 140
      };
      btnMoveToRelease.FlatAppearance.BorderSize = 1;
      btnMoveToRelease.Click += BtnMoveToRelease_Click;

      panel.Controls.Add(btnClose, 0, 0);
      panel.Controls.Add(btnRefresh, 2, 0);
      panel.Controls.Add(btnMoveToRelease, 3, 0);

      return panel;
    }

    private void SawQueueDialog_Load(object sender, EventArgs e)
    {
      try
      {
        Program.Log("SawQueueDialog: Loading dialog");

        // Set splitter distance to give more width to right panel (60% left, 40% right)
        if (mainSplitContainer != null && this.ClientSize.Width > 0)
        {
          mainSplitContainer.SplitterDistance = (int)(this.ClientSize.Width * 0.50);
        }

        // Clean up any stale lock files on startup
        CleanupStaleLockFiles();

        // Scan release directory on startup to add any existing files to tracking
        ScanReleaseDirectoryOnStartup();

        LoadFiles();
        SetupReleaseDirectoryWatcher();
        SetupStagingDirectoryWatcher();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog load failed", ex);
        MessageBox.Show($"Failed to load saw queue dialog: {ex.Message}", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Clean up stale lock files that may have been left behind
    private void CleanupStaleLockFiles()
    {
      try
      {
        if (!Directory.Exists(stagingDir))
          return;

        var lockFiles = Directory.GetFiles(stagingDir, "*.lock", SearchOption.TopDirectoryOnly);
        var cleanedCount = 0;

        foreach (var lockFilePath in lockFiles)
        {
          try
          {
            // Check if the corresponding PTX file exists
            var ptxFilePath = lockFilePath.Substring(0, lockFilePath.Length - 5); // Remove ".lock"

            // If PTX file doesn't exist, or lock file is older than 5 minutes, consider it stale
            var lockFileInfo = new FileInfo(lockFilePath);
            var isStale = !File.Exists(ptxFilePath) ||
                         (DateTime.Now - lockFileInfo.LastWriteTime).TotalMinutes > 5;

            if (isStale)
            {
              // Try to delete the stale lock file
              try
              {
                // Wait a moment in case file is still being closed
                System.Threading.Thread.Sleep(100);
                File.Delete(lockFilePath);
                cleanedCount++;
                Program.Log($"SawQueueDialog: Cleaned up stale lock file: {Path.GetFileName(lockFilePath)}");
              }
              catch (Exception ex)
              {
                // Lock file might be in use, skip it
                Program.Log($"SawQueueDialog: Could not delete lock file {Path.GetFileName(lockFilePath)}: {ex.Message}");
              }
            }
          }
          catch (Exception ex)
          {
            Program.Log($"SawQueueDialog: Error checking lock file {Path.GetFileName(lockFilePath)}", ex);
          }
        }

        if (cleanedCount > 0)
        {
          Program.Log($"SawQueueDialog: Cleaned up {cleanedCount} stale lock file(s) on startup");
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error cleaning up stale lock files", ex);
      }
    }

    private void SawQueueDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      // Unsubscribe from CSV file change events
      if (releaseTracker != null)
      {
        releaseTracker.CsvFileChanged -= ReleaseTracker_CsvFileChanged;
        releaseTracker.Dispose();
        Program.Log("SawQueueDialog: Released release file tracker");
      }

      // Dispose the file system watchers when form closes
      if (releaseDirWatcher != null)
      {
        releaseDirWatcher.EnableRaisingEvents = false;
        releaseDirWatcher.Dispose();
        releaseDirWatcher = null;
        Program.Log("SawQueueDialog: Released release directory watcher");
      }

      if (stagingDirWatcher != null)
      {
        stagingDirWatcher.EnableRaisingEvents = false;
        stagingDirWatcher.Dispose();
        stagingDirWatcher = null;
        Program.Log("SawQueueDialog: Released staging directory watcher");
      }

      stagingFilterTimer?.Stop();
      stagingFilterTimer?.Dispose();
      stagingFilterTimer = null;
      stagingRefreshTimer?.Stop();
      stagingRefreshTimer?.Dispose();
      stagingRefreshTimer = null;
    }

    // Handle CSV file changes from other instances
    private void ReleaseTracker_CsvFileChanged(object sender, EventArgs e)
    {
      try
      {
        // Refresh the release list on UI thread when CSV changes
        if (InvokeRequired)
        {
          Invoke(new Action(RefreshReleaseList));
        }
        else
        {
          RefreshReleaseList();
        }
        Program.Log("SawQueueDialog: Refreshed release list due to CSV file change from another instance");
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error refreshing release list after CSV change", ex);
      }
    }

    // Set up FileSystemWatcher to monitor release directory for file deletions
    private void SetupReleaseDirectoryWatcher()
    {
      try
      {
        if (!Directory.Exists(releaseDir))
        {
          Program.Log($"SawQueueDialog: Release directory does not exist, cannot set up watcher: {releaseDir}");
          return;
        }

        releaseDirWatcher = new FileSystemWatcher
        {
          Path = releaseDir,
          Filter = "*.PTX",
          NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
          EnableRaisingEvents = true
        };

        // Watch for file deletions (when files are processed and removed)
        releaseDirWatcher.Deleted += ReleaseDirWatcher_Deleted;

        // Watch for new files added to release directory
        releaseDirWatcher.Created += ReleaseDirWatcher_Created;

        // Also watch for errors
        releaseDirWatcher.Error += ReleaseDirWatcher_Error;

        Program.Log($"SawQueueDialog: Set up directory watcher for {releaseDir}");
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error setting up release directory watcher", ex);
        // Don't show error to user, just log it - watcher is optional
      }
    }

    // Scan release directory on startup to add any existing files to tracking
    private void ScanReleaseDirectoryOnStartup()
    {
      try
      {
        if (!Directory.Exists(releaseDir))
        {
          Program.Log($"SawQueueDialog: Release directory does not exist, cannot scan: {releaseDir}");
          return;
        }

        var files = Directory.GetFiles(releaseDir, "*.PTX", SearchOption.TopDirectoryOnly);
        var addedCount = 0;

        foreach (var filePath in files)
        {
          try
          {
            var fileName = Path.GetFileName(filePath);

            // Check if file is already tracked
            var trackedFiles = releaseTracker.GetTrackedFiles();
            if (!trackedFiles.Any(f => string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase)))
            {
              // File not tracked, add it with "pending" status
              var jobName = ExtractJobNameFromPtx(filePath);
              releaseTracker.AddFile(fileName, filePath, jobName ?? fileName, "pending");
              addedCount++;
              Program.Log($"SawQueueDialog: Added existing file to tracking: {fileName}");
            }
          }
          catch (Exception ex)
          {
            Program.Log($"SawQueueDialog: Error processing file {filePath} during startup scan", ex);
          }
        }

        if (addedCount > 0)
        {
          Program.Log($"SawQueueDialog: Added {addedCount} existing file(s) from release directory to tracking");
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error scanning release directory on startup", ex);
      }
    }

    // Set up FileSystemWatcher to monitor staging directory for new files
    private void SetupStagingDirectoryWatcher()
    {
      try
      {
        if (!Directory.Exists(stagingDir))
        {
          Program.Log($"SawQueueDialog: Staging directory does not exist, cannot set up watcher: {stagingDir}");
          return;
        }

        stagingDirWatcher = new FileSystemWatcher
        {
          Path = stagingDir,
          Filter = "*.PTX",
          NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
          EnableRaisingEvents = true
        };

        // Watch for new files added to staging directory
        stagingDirWatcher.Created += StagingDirWatcher_Created;
        stagingDirWatcher.Changed += StagingDirWatcher_Changed;

        // Also watch for errors
        stagingDirWatcher.Error += StagingDirWatcher_Error;

        Program.Log($"SawQueueDialog: Set up directory watcher for {stagingDir}");
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error setting up staging directory watcher", ex);
        // Don't show error to user, just log it - watcher is optional
      }
    }

    // Handle file creation in release directory
    private void ReleaseDirWatcher_Created(object sender, FileSystemEventArgs e)
    {
      try
      {
        var fileName = Path.GetFileName(e.FullPath);
        Program.Log($"SawQueueDialog: File created in release directory detected by watcher: {fileName}");

        // Wait a moment for file to be fully written
        System.Threading.Thread.Sleep(500);

        // Check if file is already tracked
        var trackedFiles = releaseTracker.GetTrackedFiles();
        if (!trackedFiles.Any(f => string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase)))
        {
          // Add to tracking with "pending" status
          if (InvokeRequired)
          {
            Invoke(new Action<string>(AddFileToReleaseTracking), fileName);
          }
          else
          {
            AddFileToReleaseTracking(fileName);
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling file creation event in release directory", ex);
      }
    }

    // Add file to release tracking
    private void AddFileToReleaseTracking(string fileName)
    {
      try
      {
        var filePath = Path.Combine(releaseDir, fileName);
        if (File.Exists(filePath))
        {
          var jobName = ExtractJobNameFromPtx(filePath);
          releaseTracker.AddFile(fileName, filePath, jobName ?? fileName, "pending");
          RefreshReleaseList();
          Program.Log($"SawQueueDialog: Added file to release tracking: {fileName}");
        }
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error adding file to release tracking: {fileName}", ex);
      }
    }

    // Handle file deletion events from the watcher
    private void ReleaseDirWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
      try
      {
        var fileName = Path.GetFileName(e.FullPath);
        Program.Log($"SawQueueDialog: File deleted detected by watcher: {fileName}");

        // Update the status on the UI thread
        if (InvokeRequired)
        {
          Invoke(new Action<string>(UpdateFileStatusToSentToSaw), fileName);
        }
        else
        {
          UpdateFileStatusToSentToSaw(fileName);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling file deletion event", ex);
      }
    }

    // Update file status to "sent to saw" when file is deleted
    private void UpdateFileStatusToSentToSaw(string fileName)
    {
      try
      {
        // Update the tracker
        releaseTracker.UpdateFileStatusToSentToSaw(fileName);

        // Refresh the release list display
        RefreshReleaseList();

        Program.Log($"SawQueueDialog: Updated status to 'sent to saw' for '{fileName}'");
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error updating file status for '{fileName}'", ex);
      }
    }

    // Handle file creation in staging directory
    private void StagingDirWatcher_Created(object sender, FileSystemEventArgs e)
    {
      try
      {
        var fileName = Path.GetFileName(e.FullPath);
        var filePath = e.FullPath;
        Program.Log($"SawQueueDialog: File created in staging directory detected by watcher: {fileName}");

        // Wait a moment for file to be fully written
        System.Threading.Thread.Sleep(500);

        // Try to update job names with batch ID (only if file is not locked by another instance)
        if (File.Exists(filePath))
        {
          try
          {
            UpdateJobNamesWithBatchId(filePath);
          }
          catch (Exception updateEx)
          {
            // Log but don't fail - file might be locked by another instance
            Program.Log($"SawQueueDialog: Could not update batch ID for {fileName} (may be processed by another instance): {updateEx.Message}");
          }
        }

        // Debounce refresh on UI thread (avoids N refreshes when many files are dropped)
        ScheduleStagingRefresh();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling file creation event in staging directory", ex);
      }
    }

    // Handle file changes in staging directory
    private void StagingDirWatcher_Changed(object sender, FileSystemEventArgs e)
    {
      try
      {
        // Only refresh if it's a file change (not just attribute changes)
        if (e.ChangeType == WatcherChangeTypes.Changed)
        {
          var fileName = Path.GetFileName(e.FullPath);
          var filePath = e.FullPath;
          Program.Log($"SawQueueDialog: File changed in staging directory: {fileName}");

          // Check if this file needs batch ID update (may have been modified by another instance)
          if (File.Exists(filePath) && fileName.EndsWith(".PTX", StringComparison.OrdinalIgnoreCase))
          {
            try
            {
              // Only try to update if file is not currently locked
              UpdateJobNamesWithBatchId(filePath);
            }
            catch (Exception updateEx)
            {
              // Log but don't fail - file might be locked or already updated
              Program.Log($"SawQueueDialog: Could not check/update batch ID for {fileName}: {updateEx.Message}");
            }
          }

          // Debounce refresh on UI thread (avoids N refreshes when many files change)
          ScheduleStagingRefresh();
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling file change event in staging directory", ex);
      }
    }

    private void ScheduleStagingRefresh()
    {
      if (stagingRefreshTimer == null) return;
      if (InvokeRequired)
      {
        Invoke(new Action(() =>
        {
          stagingRefreshTimer.Stop();
          stagingRefreshTimer.Start();
        }));
      }
      else
      {
        stagingRefreshTimer.Stop();
        stagingRefreshTimer.Start();
      }
    }

    // Handle staging directory watcher errors
    private void StagingDirWatcher_Error(object sender, ErrorEventArgs e)
    {
      try
      {
        Program.Log("SawQueueDialog: Staging directory watcher error occurred", e.GetException());

        // Try to restart the watcher
        if (stagingDirWatcher != null)
        {
          stagingDirWatcher.EnableRaisingEvents = false;
          System.Threading.Thread.Sleep(1000); // Wait a bit before restarting

          if (Directory.Exists(stagingDir))
          {
            stagingDirWatcher.EnableRaisingEvents = true;
            Program.Log("SawQueueDialog: Staging directory watcher restarted after error");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling staging watcher error", ex);
      }
    }

    // Refresh the staging list display
    private void RefreshStagingList()
    {
      try
      {
        if (Directory.Exists(stagingDir))
        {
          // Store original files for filtering
          allStagingFiles = GetFilesWithMetadata(stagingDir);

          // Apply current filter
          ApplyStagingFilter();
        }
        else
        {
          allStagingFiles = new List<FileDisplayItem>();
          stagingDataGrid.DataSource = null;
          stagingStatusLabel.Text = "Staging directory not found";
          stagingStatusLabel.ForeColor = Color.DarkRed;
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error refreshing staging list", ex);
      }
    }

    // Handle watcher errors
    private void ReleaseDirWatcher_Error(object sender, ErrorEventArgs e)
    {
      try
      {
        Program.Log("SawQueueDialog: Release directory watcher error occurred", e.GetException());

        // Try to restart the watcher
        if (releaseDirWatcher != null)
        {
          releaseDirWatcher.EnableRaisingEvents = false;
          System.Threading.Thread.Sleep(1000); // Wait a bit before restarting

          if (Directory.Exists(releaseDir))
          {
            releaseDirWatcher.EnableRaisingEvents = true;
            Program.Log("SawQueueDialog: Release directory watcher restarted after error");
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling release watcher error", ex);
      }
    }

    // Refresh the release list display
    private void RefreshReleaseList()
    {
      try
      {
        // Store original files for filtering
        allReleaseFiles = releaseTracker.GetTrackedFiles();

        // Apply current filter
        ApplyReleaseFilter();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error refreshing release list", ex);
      }
    }

    // CellFormatting event handler to color the Status column
    private void ReleaseDataGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
      try
      {
        // Only format the Status column
        if (releaseDataGrid.Columns[e.ColumnIndex].Name == "Status")
        {
          var statusValue = e.Value?.ToString();

          if (string.Equals(statusValue, "Pending", StringComparison.OrdinalIgnoreCase))
          {
            // Orange background for Pending
            e.CellStyle.BackColor = Color.FromArgb(255, 193, 7); // Orange
            e.CellStyle.ForeColor = Color.Black;
          }
          else if (string.Equals(statusValue, "Sent to Saw", StringComparison.OrdinalIgnoreCase))
          {
            // Green background for Sent to Saw
            e.CellStyle.BackColor = Color.FromArgb(40, 167, 69); // Green
            e.CellStyle.ForeColor = Color.White;
          }
          else
          {
            // Default styling for other values
            e.CellStyle.BackColor = releaseDataGrid.DefaultCellStyle.BackColor;
            e.CellStyle.ForeColor = releaseDataGrid.DefaultCellStyle.ForeColor;
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error formatting Status column cell", ex);
      }
    }

    private void LoadFiles()
    {
      try
      {
        Program.Log($"SawQueueDialog: Loading files from staging='{stagingDir}' and release='{releaseDir}'");

        // Load staging files - RefreshStagingList will handle the display and filtering
        RefreshStagingList();

        // Prompt user to clean up staging if too many files (improve system speed)
        if (allStagingFiles != null && allStagingFiles.Count > StagingFileCountWarningThreshold)
        {
          MessageBox.Show(
            $"Staging has {allStagingFiles.Count} file(s). Consider deleting old files to improve system speed.\n\nKeeping fewer than {StagingFileCountWarningThreshold} files in staging is recommended.",
            "Staging File Count",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        }

        // Load release tracking history instead of current files

        // Check for files that have been removed (processed) and update their status
        // This is a fallback check in case the watcher missed something
        releaseTracker.UpdateStatusForRemovedFiles(releaseDir);

        // Get tracked release files (last 100) - RefreshReleaseList will handle the display
        RefreshReleaseList();

        Program.Log("SawQueueDialog: Files loaded successfully");
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error loading files", ex);
        MessageBox.Show($"Error loading files: {ex.Message}", "Load Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private List<FileDisplayItem> GetFilesWithMetadata(string directory)
    {
      var items = new List<FileDisplayItem>();

      try
      {
        // Get all files in the directory
        var files = Directory.GetFiles(directory, "*.PTX", SearchOption.TopDirectoryOnly);

        foreach (var filePath in files)
        {
          try
          {
            var fileInfo = new FileInfo(filePath);
            // Batch ID updates happen only for the single file in watcher Created/Changed handlers
            // to avoid N file writes on every refresh when many files are in staging

            // Extract job name from PTX file
            var jobName = ExtractJobNameFromPtx(filePath);
            var item = new FileDisplayItem
            {
              FileName = fileInfo.Name,
              FilePath = filePath,
              FileSize = fileInfo.Length,
              LastModified = fileInfo.LastWriteTime,
              JobName = jobName ?? fileInfo.Name // Fallback to filename if job name not found
            };
            items.Add(item);
          }
          catch (Exception ex)
          {
            Program.Log($"SawQueueDialog: Error reading file metadata for {filePath}", ex);
          }
        }

        // Sort by last modified date (newest first)
        items = items.OrderByDescending(f => f.LastModified).ToList();
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error enumerating files in {directory}", ex);
      }

      return items;
    }

    // Extract job name from PTX file by reading the JOBS line
    private string ExtractJobNameFromPtx(string filePath)
    {
      try
      {
        // Read the file line by line to find the JOBS line
        using (var reader = new StreamReader(filePath))
        {
          string line;
          while ((line = reader.ReadLine()) != null)
          {
            // Check if this is a JOBS line (starts with "JOBS,")
            if (line.StartsWith("JOBS,", StringComparison.OrdinalIgnoreCase))
            {
              // Split by comma and get the third field (index 2) which contains the job name
              var fields = line.Split(',');
              if (fields.Length >= 3 && !string.IsNullOrWhiteSpace(fields[2]))
              {
                return fields[2].Trim();
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error extracting job name from {filePath}", ex);
      }

      return null; // Return null if job name not found
    }

    // Validate both job name fields in PTX file
    // Returns a tuple: (isValid, jobName1, jobName2, errorMessage)
    private (bool isValid, string jobName1, string jobName2, string errorMessage) ValidateJobNameFields(string filePath)
    {
      try
      {
        // Read the file line by line to find the JOBS line
        using (var reader = new StreamReader(filePath))
        {
          string line;
          while ((line = reader.ReadLine()) != null)
          {
            // Check if this is a JOBS line (starts with "JOBS,")
            if (line.StartsWith("JOBS,", StringComparison.OrdinalIgnoreCase))
            {
              // Split by comma and get both job name fields (index 2 and 3)
              var fields = line.Split(',');
              if (fields.Length < 4)
              {
                return (false, null, null, "JOBS line does not have enough fields");
              }

              var jobName1 = fields.Length > 2 ? fields[2].Trim() : string.Empty;
              var jobName2 = fields.Length > 3 ? fields[3].Trim() : string.Empty;

              // Check if both fields are empty
              if (string.IsNullOrWhiteSpace(jobName1) && string.IsNullOrWhiteSpace(jobName2))
              {
                return (false, jobName1, jobName2, "Both job name fields are empty");
              }

              // Check if fields are identical
              if (!string.Equals(jobName1, jobName2, StringComparison.Ordinal))
              {
                return (false, jobName1, jobName2, $"Job name fields are not identical. Field 1: '{jobName1}', Field 2: '{jobName2}'");
              }

              // Check if both are within 50 character limit
              if (jobName1.Length > 50)
              {
                return (false, jobName1, jobName2, $"Job name is {jobName1.Length} characters long (maximum 50 allowed)");
              }

              if (jobName2.Length > 50)
              {
                return (false, jobName1, jobName2, $"Second job name field is {jobName2.Length} characters long (maximum 50 allowed)");
              }

              // Both fields are valid
              return (true, jobName1, jobName2, null);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error validating job name fields from {filePath}", ex);
        return (false, null, null, $"Error reading file: {ex.Message}");
      }

      return (false, null, null, "JOBS line not found in file");
    }

    private string CondenseName(string name)
    {
      if (string.IsNullOrEmpty(name))
        return name;

      // Dictionary of words to their condensed replacements
      var shortNames = new Dictionary<string, string>
      {
        { "Kitchen", "Kitn" },
        { "Island", "Isl" },
        { "Bedroom", "Bedrm" },
        { "Bathroom", "Bathrm" },
        { "Living", "Liv" },
        { "Dining", "Din" },
        { "Family", "Fam" },
        { "Office", "Off" },
        { "Room", "RM" },
      };

      // Remove extra spaces
      var condensed = name.Replace("  ", " ").Replace(" - ", "-").Replace(" -", "-").Replace("- ", "-").Trim();

      // Apply case-insensitive find-replace for each entry in the dictionary
      // Process longer words first to avoid substring replacement issues (e.g., "Room" in "Bedroom")
      var sortedEntries = shortNames.OrderByDescending(kvp => kvp.Key.Length);

      foreach (var kvp in sortedEntries)
      {
        var searchText = kvp.Key;
        var replacement = kvp.Value;

        // Find all occurrences case-insensitively and replace
        int index = 0;
        while ((index = condensed.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
          // Replace the found occurrence
          condensed = condensed.Substring(0, index) + replacement + condensed.Substring(index + searchText.Length);
          index += replacement.Length; // Move past the replacement
        }
      }

      // Trim any extra spaces that might have been created
      condensed = condensed.Replace("  ", " ").Trim();

      return condensed;
    }

    private void BtnRefresh_Click(object sender, EventArgs e)
    {
      try
      {
        Program.Log("SawQueueDialog: Refresh button clicked");

        // Re-save all staging files in ASCII format to remove any BOM
        ReSaveStagingFilesAsAscii();

        LoadFiles();
        MessageBox.Show("File lists refreshed successfully.", "Refresh Complete",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error refreshing files", ex);
        MessageBox.Show($"Error refreshing files: {ex.Message}", "Refresh Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Re-save all staging PTX files in ASCII format to remove any BOM
    private void ReSaveStagingFilesAsAscii()
    {
      try
      {
        if (!Directory.Exists(stagingDir))
        {
          Program.Log("SawQueueDialog: Staging directory does not exist, skipping ASCII conversion");
          return;
        }

        var ptxFiles = Directory.GetFiles(stagingDir, "*.ptx", SearchOption.TopDirectoryOnly);
        if (ptxFiles.Length == 0)
        {
          Program.Log("SawQueueDialog: No PTX files found in staging directory");
          return;
        }

        Program.Log($"SawQueueDialog: Re-saving {ptxFiles.Length} PTX file(s) in ASCII format");

        int successCount = 0;
        int errorCount = 0;

        foreach (var filePath in ptxFiles)
        {
          try
          {
            // Preserve the original file timestamps
            var fileInfo = new FileInfo(filePath);
            var originalLastWriteTime = fileInfo.LastWriteTime;
            var originalLastAccessTime = fileInfo.LastAccessTime;
            var originalCreationTime = fileInfo.CreationTime;

            // Read all lines from the file (UTF-8 handles BOM automatically)
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            // Write the lines back to the file in ASCII format (no BOM)
            File.WriteAllLines(filePath, lines, Encoding.ASCII);

            // Restore the original file timestamps
            fileInfo.Refresh();
            fileInfo.LastWriteTime = originalLastWriteTime;
            fileInfo.LastAccessTime = originalLastAccessTime;
            fileInfo.CreationTime = originalCreationTime;

            successCount++;
          }
          catch (Exception ex)
          {
            errorCount++;
            Program.Log($"SawQueueDialog: Error re-saving {Path.GetFileName(filePath)} in ASCII format", ex);
          }
        }

        Program.Log($"SawQueueDialog: Re-saved {successCount} file(s) successfully, {errorCount} error(s)");
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in ReSaveStagingFilesAsAscii", ex);
        // Don't throw - allow refresh to continue even if ASCII conversion fails
      }
    }

    private void BtnMoveToRelease_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if any rows are selected
        if (stagingDataGrid.SelectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more files from the staging list to move to release.",
            "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Confirm the move operation
        // var result = MessageBox.Show(
        //   $"Are you sure you want to move {stagingDataGrid.SelectedRows.Count} file(s) from staging to release?",
        //   "Confirm Move",
        //   MessageBoxButtons.YesNo,
        //   MessageBoxIcon.Question);

        // if (result != DialogResult.Yes)
        //   return;

        // Ensure release directory exists
        if (!Directory.Exists(releaseDir))
        {
          Directory.CreateDirectory(releaseDir);
          Program.Log($"SawQueueDialog: Created release directory: {releaseDir}");
        }

        // Move selected files
        var movedCount = 0;
        var errorCount = 0;

        // Get selected FileDisplayItem objects from the DataGridView
        var selectedItems = new List<FileDisplayItem>();
        foreach (DataGridViewRow row in stagingDataGrid.SelectedRows)
        {
          // Store FileDisplayItem in row Tag for easy access
          if (row.Tag is FileDisplayItem fileItem)
          {
            selectedItems.Add(fileItem);
          }
        }

        foreach (var item in selectedItems)
        {
          try
          {
            // Validate both job name fields before moving
            var (isValid, jobName1, jobName2, errorMessage) = ValidateJobNameFields(item.FilePath);

            // If validation failed, prompt user to edit
            if (!isValid)
            {
              var editResult = MessageBox.Show(
                $"The job name for '{item.FileName}' has validation issues:\n\n{errorMessage}\n\n" +

                "Would you like to edit the job name now?",
                "Job Name Too Long",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

              if (editResult == DialogResult.Yes)
              {
                // Get the current job name from the file (use validated jobName1 if available, otherwise extract)
                var currentJobName = jobName1 ?? ExtractJobNameFromPtx(item.FilePath) ?? item.JobName;

                // Suggest a condensed version
                var condensedJobName = CondenseName(currentJobName);

                string newJobName = null;

                // If condensed name is 50 or fewer characters, offer to accept or edit
                if (condensedJobName.Length <= 50)
                {
                  var acceptResult = MessageBox.Show(
                    "We have suggested a job name that will fit within the 50 character limit. Would you like to use this name?" + "\n\n" +
                    "Suggested job name: \n\n" + condensedJobName + "\n\n" +
                    "Click 'Yes' to accept this name, or 'No' to edit it.",
                    "Accept Suggested Name?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                  if (acceptResult == DialogResult.Yes)
                  {
                    // User accepted the condensed name - update the PTX file directly
                    UpdateJobNameInPtx(item.FilePath, condensedJobName);
                    newJobName = condensedJobName;
                    Program.Log($"SawQueueDialog: Accepted condensed job name '{condensedJobName}' for file '{item.FileName}'");
                  }
                  else
                  {
                    // User wants to edit - show the edit dialog with condensed name pre-filled
                    item.JobName = condensedJobName;
                    newJobName = EditJobNameForFileItem(item);
                  }
                }
                else
                {
                  // Condensed name is still too long - show edit dialog with condensed name as starting point
                  item.JobName = condensedJobName;
                  newJobName = EditJobNameForFileItem(item);
                }

                if (newJobName == null)
                {
                  // User cancelled or didn't change - skip this file
                  Program.Log($"SawQueueDialog: Skipping file '{item.FileName}' - job name not updated");
                  continue;
                }

                // Re-validate after update to verify both fields are now valid
                var (isValidAfterEdit, updatedJobName1, updatedJobName2, updatedErrorMessage) = ValidateJobNameFields(item.FilePath);

                if (!isValidAfterEdit)
                {
                  MessageBox.Show(
                    $"The job name for '{item.FileName}' still has validation issues:\n\n{updatedErrorMessage}\n\n" +
                    (updatedJobName1 != null && updatedJobName2 != null
                      ? $"Field 1: '{updatedJobName1}'\nField 2: '{updatedJobName2}'\n\n"
                      : "") +
                    "This file will be skipped. Please edit the job name manually and try again.",
                    "Job Name Still Invalid",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                  continue;
                }
              }
              else
              {
                // User chose not to edit - skip this file
                Program.Log($"SawQueueDialog: Skipping file '{item.FileName}' - job name validation failed and user declined to edit");
                continue;
              }
            }

            var sourceFile = item.FilePath;
            var destFile = Path.Combine(releaseDir, item.FileName);

            // Check if destination file already exists
            if (File.Exists(destFile))
            {
              var overwriteResult = MessageBox.Show(
                $"File '{item.FileName}' already exists in release directory.\n\nOverwrite?",
                "File Exists",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

              if (overwriteResult == DialogResult.Cancel)
                break;
              if (overwriteResult == DialogResult.No)
                continue;

              File.Delete(destFile);
            }

            // Move the file
            File.Move(sourceFile, destFile);

            // Track the file in release history with "pending" status
            // Re-extract job name from moved file to ensure we have the latest value
            var jobName = ExtractJobNameFromPtx(destFile);
            releaseTracker.AddFile(item.FileName, destFile, jobName ?? item.FileName, "pending");

            movedCount++;
            Program.Log($"SawQueueDialog: Moved file from '{sourceFile}' to '{destFile}'");
          }
          catch (Exception ex)
          {
            errorCount++;
            Program.Log($"SawQueueDialog: Error moving file '{item.FileName}'", ex);
          }
        }

        // Refresh the lists
        LoadFiles();

        // Show summary
        var message = $"Moved {movedCount} file(s) successfully.";
        if (errorCount > 0)
          message += $"\n{errorCount} file(s) failed to move. Check the log for details.";

        // MessageBox.Show(message, "Move Complete",
        //   MessageBoxButtons.OK, errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in move operation", ex);
        MessageBox.Show($"Error moving files: {ex.Message}", "Move Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Handle click on staging label to open folder
    private void StagingLabel_Click(object sender, EventArgs e)
    {
      try
      {
        if (!string.IsNullOrEmpty(stagingDir) && Directory.Exists(stagingDir))
        {
          System.Diagnostics.Process.Start("explorer.exe", stagingDir);
          Program.Log($"SawQueueDialog: Opened staging folder: {stagingDir}");
        }
        else
        {
          MessageBox.Show($"Directory does not exist:\n{stagingDir}", "Directory Not Found",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error opening staging folder", ex);
        MessageBox.Show($"Error opening folder: {ex.Message}", "Open Folder Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Handle click on release label to open folder
    private void ReleaseLabel_Click(object sender, EventArgs e)
    {
      try
      {
        if (!string.IsNullOrEmpty(releaseDir) && Directory.Exists(releaseDir))
        {
          System.Diagnostics.Process.Start("explorer.exe", releaseDir);
          Program.Log($"SawQueueDialog: Opened release folder: {releaseDir}");
        }
        else
        {
          MessageBox.Show($"Directory does not exist:\n{releaseDir}", "Directory Not Found",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error opening release folder", ex);
        MessageBox.Show($"Error opening folder: {ex.Message}", "Open Folder Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Handle staging filter text change - debounce so we don't re-apply on every keystroke
    private void StagingFilterTextBox_TextChanged(object sender, EventArgs e)
    {
      try
      {
        if (stagingFilterTimer == null) return;
        stagingFilterTimer.Stop();
        stagingFilterTimer.Start();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in staging filter TextChanged", ex);
      }
    }

    private void StagingFilterTimer_Tick(object sender, EventArgs e)
    {
      try
      {
        if (stagingFilterTimer != null) stagingFilterTimer.Stop();
        ApplyStagingFilter();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error applying staging filter (timer)", ex);
      }
    }

    // Handle release filter text change
    private void ReleaseFilterTextBox_TextChanged(object sender, EventArgs e)
    {
      try
      {
        ApplyReleaseFilter();
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error applying release filter", ex);
      }
    }

    // Apply filter to staging list
    private void ApplyStagingFilter()
    {
      try
      {
        if (allStagingFiles == null)
          return;

        var filterText = stagingFilterTextBox.Text?.Trim() ?? "";
        List<FileDisplayItem> filteredFiles;

        if (string.IsNullOrEmpty(filterText))
        {
          filteredFiles = allStagingFiles;
        }
        else
        {
          // Filter by job name (case-insensitive)
          filteredFiles = allStagingFiles.Where(f =>
            f.JobName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
            f.FileName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0
          ).ToList();
        }

        // Re-apply current sort so filter preserves sort order
        if (!string.IsNullOrEmpty(stagingSortColumnName))
        {
          IEnumerable<FileDisplayItem> ordered = stagingSortColumnName == "JobName" ? filteredFiles.OrderBy(f => f.JobName, StringComparer.OrdinalIgnoreCase)
            : stagingSortColumnName == "FileSize" ? filteredFiles.OrderBy(f => f.FileSize)
            : stagingSortColumnName == "LastModified" ? filteredFiles.OrderBy(f => f.LastModified)
            : (IEnumerable<FileDisplayItem>)filteredFiles;
          if (!stagingSortAscending) ordered = ordered.Reverse();
          filteredFiles = ordered.ToList();
        }

        // Create display items for DataGridView
        var displayItems = filteredFiles.Select(f => new
        {
          JobName = f.JobName,
          FileSize = FormatFileSize(f.FileSize),
          LastModified = f.LastModified.ToString("yyyy-MM-dd HH:mm")
        }).ToList();

        // Suspend layout so grid doesn't repaint for every row during bind and Tag set
        stagingDataGrid.SuspendLayout();
        try
        {
          stagingDataGrid.DataSource = displayItems;

          // Store FileDisplayItem references in row Tag for easy access
          for (int i = 0; i < stagingDataGrid.Rows.Count && i < filteredFiles.Count; i++)
          {
            stagingDataGrid.Rows[i].Tag = filteredFiles[i];
          }
        }
        finally
        {
          stagingDataGrid.ResumeLayout(true);
        }

        // Update status label to show filtered count
        var totalCount = allStagingFiles.Count;
        var filteredCount = filteredFiles.Count;
        if (filteredCount < totalCount)
        {
          stagingStatusLabel.Text = $"{filteredCount} of {totalCount} file(s) shown";
        }
        else
        {
          stagingStatusLabel.Text = $"{totalCount} file(s) in staging";
        }
        stagingStatusLabel.ForeColor = filteredCount > 0 ? Color.DarkGreen : Color.Gray;
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error applying staging filter", ex);
      }
    }

    // Apply filter to release list
    private void ApplyReleaseFilter()
    {
      try
      {
        if (allReleaseFiles == null)
          return;

        var filterText = releaseFilterTextBox.Text?.Trim() ?? "";
        List<TrackedReleaseFile> filteredFiles;

        if (string.IsNullOrEmpty(filterText))
        {
          filteredFiles = allReleaseFiles;
        }
        else
        {
          // Filter by job name or status (case-insensitive)
          filteredFiles = allReleaseFiles.Where(f =>
            f.JobName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
            f.FileName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
            f.Status.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0
          ).ToList();
        }

        // Create a list of display objects for the DataGridView
        var displayItems = filteredFiles.Select(f => new
        {
          JobName = f.JobName,
          Status = f.Status == "pending" ? "Pending" : "Sent to Saw",
          ReleaseDate = f.SentToRelease.ToString("yyyy-MM-dd HH:mm"),
          SentToSawDate = f.SentToSaw.HasValue ? f.SentToSaw.Value.ToString("yyyy-MM-dd HH:mm") : ""
        }).ToList();

        releaseDataGrid.SuspendLayout();
        try
        {
          releaseDataGrid.DataSource = displayItems;
          for (int i = 0; i < releaseDataGrid.Rows.Count && i < filteredFiles.Count; i++)
            releaseDataGrid.Rows[i].Tag = filteredFiles[i];
        }
        finally
        {
          releaseDataGrid.ResumeLayout(true);
        }

        // Update status label to show filtered count
        var totalCount = allReleaseFiles.Count;
        var filteredCount = filteredFiles.Count;
        var pendingCount = filteredFiles.Count(f => f.Status == "pending");
        var sentCount = filteredFiles.Count(f => f.Status == "sent to saw");

        if (filteredCount < totalCount)
        {
          releaseStatusLabel.Text = $"{filteredCount} of {totalCount} tracked file(s) shown - {pendingCount} pending, {sentCount} sent to saw";
        }
        else
        {
          releaseStatusLabel.Text = $"{totalCount} tracked file(s) - {pendingCount} pending, {sentCount} sent to saw";
        }
        releaseStatusLabel.ForeColor = filteredCount > 0 ? Color.DarkGreen : Color.Gray;
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error applying release filter", ex);
      }
    }

    private void StagingDataGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
      try
      {
        if (allStagingFiles == null || stagingDataGrid.Rows.Count == 0) return;
        var items = new List<FileDisplayItem>();
        foreach (DataGridViewRow row in stagingDataGrid.Rows)
        {
          if (row.Tag is FileDisplayItem item) items.Add(item);
        }
        if (items.Count == 0) return;

        var colName = stagingDataGrid.Columns[e.ColumnIndex].Name;
        if (stagingSortColumnIndex == e.ColumnIndex)
          stagingSortAscending = !stagingSortAscending;
        else
        {
          stagingSortColumnIndex = e.ColumnIndex;
          stagingSortColumnName = colName;
          stagingSortAscending = true;
        }
        IEnumerable<FileDisplayItem> ordered = colName == "JobName" ? items.OrderBy(f => f.JobName, StringComparer.OrdinalIgnoreCase)
          : colName == "FileSize" ? items.OrderBy(f => f.FileSize)
          : colName == "LastModified" ? items.OrderBy(f => f.LastModified)
          : (IEnumerable<FileDisplayItem>)items;
        if (!stagingSortAscending)
          ordered = ordered.Reverse();

        var sorted = ordered.ToList();
        var displayItems = sorted.Select(f => new
        {
          JobName = f.JobName,
          FileSize = FormatFileSize(f.FileSize),
          LastModified = f.LastModified.ToString("yyyy-MM-dd HH:mm")
        }).ToList();

        stagingDataGrid.SuspendLayout();
        try
        {
          stagingDataGrid.DataSource = displayItems;
          for (int i = 0; i < stagingDataGrid.Rows.Count && i < sorted.Count; i++)
            stagingDataGrid.Rows[i].Tag = sorted[i];
        }
        finally
        {
          stagingDataGrid.ResumeLayout(true);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error sorting staging grid", ex);
      }
    }

    private void ReleaseDataGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
      try
      {
        if (allReleaseFiles == null || releaseDataGrid.Rows.Count == 0) return;
        var items = new List<TrackedReleaseFile>();
        foreach (DataGridViewRow row in releaseDataGrid.Rows)
        {
          if (row.Tag is TrackedReleaseFile item) items.Add(item);
        }
        if (items.Count == 0) return;

        var colName = releaseDataGrid.Columns[e.ColumnIndex].Name;
        if (releaseSortColumnIndex == e.ColumnIndex)
          releaseSortAscending = !releaseSortAscending;
        else
        {
          releaseSortColumnIndex = e.ColumnIndex;
          releaseSortColumnName = colName;
          releaseSortAscending = true;
        }
        IEnumerable<TrackedReleaseFile> ordered = colName == "JobName" ? items.OrderBy(f => f.JobName, StringComparer.OrdinalIgnoreCase)
          : colName == "Status" ? items.OrderBy(f => f.Status ?? "", StringComparer.OrdinalIgnoreCase)
          : colName == "ReleaseDate" ? items.OrderBy(f => f.SentToRelease)
          : colName == "SentToSawDate" ? items.OrderBy(f => f.SentToSaw ?? DateTime.MinValue)
          : (IEnumerable<TrackedReleaseFile>)items;
        if (!releaseSortAscending)
          ordered = ordered.Reverse();

        var sorted = ordered.ToList();
        var displayItems = sorted.Select(f => new
        {
          JobName = f.JobName,
          Status = f.Status == "pending" ? "Pending" : "Sent to Saw",
          ReleaseDate = f.SentToRelease.ToString("yyyy-MM-dd HH:mm"),
          SentToSawDate = f.SentToSaw.HasValue ? f.SentToSaw.Value.ToString("yyyy-MM-dd HH:mm") : ""
        }).ToList();

        releaseDataGrid.SuspendLayout();
        try
        {
          releaseDataGrid.DataSource = displayItems;
          for (int i = 0; i < releaseDataGrid.Rows.Count && i < sorted.Count; i++)
            releaseDataGrid.Rows[i].Tag = sorted[i];
        }
        finally
        {
          releaseDataGrid.ResumeLayout(true);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error sorting release grid", ex);
      }
    }

    // Context menu handler for staging grid - Move to Release
    private void StagingContextMenu_Move_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if any rows are selected
        if (stagingDataGrid.SelectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more files to move to release.",
            "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Get selected FileDisplayItem objects
        var selectedItems = new List<FileDisplayItem>();
        foreach (DataGridViewRow row in stagingDataGrid.SelectedRows)
        {
          if (row.Tag is FileDisplayItem fileItem)
          {
            selectedItems.Add(fileItem);
          }
        }

        if (selectedItems.Count == 0)
        {
          MessageBox.Show("No valid files selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Confirm the move operation
        var result = MessageBox.Show(
          $"Are you sure you want to move {selectedItems.Count} file(s) from staging to release?",
          "Confirm Move",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
          return;

        // Ensure release directory exists
        if (!Directory.Exists(releaseDir))
        {
          Directory.CreateDirectory(releaseDir);
          Program.Log($"SawQueueDialog: Created release directory: {releaseDir}");
        }

        // Move selected files
        var movedCount = 0;
        var errorCount = 0;

        foreach (var item in selectedItems)
        {
          try
          {
            var sourceFile = item.FilePath;
            var destFile = Path.Combine(releaseDir, item.FileName);

            // Check if destination file already exists
            if (File.Exists(destFile))
            {
              var overwriteResult = MessageBox.Show(
                $"File '{item.FileName}' already exists in release directory.\n\nOverwrite?",
                "File Exists",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

              if (overwriteResult == DialogResult.Cancel)
                break;
              if (overwriteResult == DialogResult.No)
                continue;

              File.Delete(destFile);
            }

            // Move the file
            File.Move(sourceFile, destFile);

            // Track the file in release history with "pending" status
            var jobName = ExtractJobNameFromPtx(destFile);
            releaseTracker.AddFile(item.FileName, destFile, jobName ?? item.FileName, "pending");

            movedCount++;
            Program.Log($"SawQueueDialog: Moved file from '{sourceFile}' to '{destFile}'");
          }
          catch (Exception ex)
          {
            errorCount++;
            Program.Log($"SawQueueDialog: Error moving file '{item.FileName}'", ex);
          }
        }

        // Refresh the lists
        LoadFiles();

        // Show summary
        var message = $"Moved {movedCount} file(s) successfully.";
        if (errorCount > 0)
          message += $"\n{errorCount} file(s) failed to move. Check the log for details.";

        // MessageBox.Show(message, "Move Complete",
        //   MessageBoxButtons.OK, errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in context menu move operation", ex);
        MessageBox.Show($"Error moving files: {ex.Message}", "Move Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Handle double-click on staging grid cells
    private void StagingDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
      try
      {
        // Only handle double-click on JobName column
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
            stagingDataGrid.Columns[e.ColumnIndex].Name == "JobName")
        {
          // Select the row that was double-clicked
          if (e.RowIndex < stagingDataGrid.Rows.Count)
          {
            stagingDataGrid.ClearSelection();
            stagingDataGrid.Rows[e.RowIndex].Selected = true;

            // Call the edit job name method
            EditJobNameForSelectedRow();
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling double-click on staging grid", ex);
      }
    }

    // Edit job name for the currently selected row in staging grid
    // Helper method to edit job name for a specific file item
    // Returns the new job name if changed, null if cancelled or unchanged
    private string EditJobNameForFileItem(FileDisplayItem fileItem)
    {
      try
      {
        if (fileItem == null)
        {
          MessageBox.Show("Unable to get file information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return null;
        }

        // Get current job name
        var currentJobName = fileItem.JobName;

        // Prompt user for new job name using a simple input dialog
        string newJobName = null;
        using (var inputDialog = new Form
        {
          Text = "Edit Job Name",
          Width = 400,
          Height = 150,
          FormBorderStyle = FormBorderStyle.FixedDialog,
          StartPosition = FormStartPosition.CenterParent,
          MaximizeBox = false,
          MinimizeBox = false
        })
        {
          var label = new Label
          {
            Text = $"Enter new job name for:\n{fileItem.FileName}",
            Left = 10,
            Top = 10,
            Width = 360,
            Height = 40
          };
          var textBox = new TextBox
          {
            Left = 10,
            Top = 50,
            Width = 360,
            Text = currentJobName
          };
          var okButton = new Button
          {
            Text = "OK",
            Left = 200,
            Top = 80,
            Width = 80,
            DialogResult = DialogResult.OK
          };
          var cancelButton = new Button
          {
            Text = "Cancel",
            Left = 290,
            Top = 80,
            Width = 80,
            DialogResult = DialogResult.Cancel
          };

          inputDialog.Controls.Add(label);
          inputDialog.Controls.Add(textBox);
          inputDialog.Controls.Add(okButton);
          inputDialog.Controls.Add(cancelButton);
          inputDialog.AcceptButton = okButton;
          inputDialog.CancelButton = cancelButton;

          if (inputDialog.ShowDialog(this) == DialogResult.OK)
          {
            newJobName = textBox.Text.Trim();
          }
        }

        if (string.IsNullOrWhiteSpace(newJobName) || newJobName == currentJobName)
        {
          return null; // User cancelled or didn't change
        }

        // Update the job name in the PTX file
        UpdateJobNameInPtx(fileItem.FilePath, newJobName);

        // Refresh the staging list to show updated job name
        LoadFiles();

        return newJobName;
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error editing job name", ex);
        MessageBox.Show($"Error editing job name: {ex.Message}", "Edit Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
        return null;
      }
    }

    private void EditJobNameForSelectedRow()
    {
      try
      {
        // Check if exactly one row is selected
        if (stagingDataGrid.SelectedRows.Count != 1)
        {
          MessageBox.Show("Please select exactly one file to edit the job name.",
            "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        var selectedRow = stagingDataGrid.SelectedRows[0];
        if (!(selectedRow.Tag is FileDisplayItem fileItem))
        {
          MessageBox.Show("Unable to get file information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Use the helper method to edit the job name
        EditJobNameForFileItem(fileItem);

        // MessageBox.Show("Job name updated successfully.", "Success",
        //   MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error editing job name", ex);
        MessageBox.Show($"Error editing job name: {ex.Message}", "Edit Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Context menu handler for staging grid - Edit Job Name
    private void StagingContextMenu_EditJobName_Click(object sender, EventArgs e)
    {
      // Reuse the common edit method
      EditJobNameForSelectedRow();
    }

    // Context menu handler for staging grid - Show Parts
    private void StagingContextMenu_ShowParts_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if exactly one row is selected
        if (stagingDataGrid.SelectedRows.Count != 1)
        {
          MessageBox.Show("Please select exactly one file to show parts.",
            "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        var selectedRow = stagingDataGrid.SelectedRows[0];
        if (!(selectedRow.Tag is FileDisplayItem fileItem))
        {
          MessageBox.Show("Unable to get file information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Read and parse parts from PTX file
        var parts = ExtractPartsFromPtx(fileItem.FilePath);
        if (parts == null || parts.Count == 0)
        {
          MessageBox.Show("No parts found in this PTX file.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Show parts dialog
        using (var partsDialog = new PartsListDialog(fileItem.FileName, parts))
        {
          partsDialog.ShowDialog(this);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error showing parts", ex);
        MessageBox.Show($"Error showing parts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Context menu Opening event handler - Enable/disable menu items based on selection
    private void StagingContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      try
      {
        var contextMenu = sender as ContextMenuStrip;
        if (contextMenu == null) return;

        // Find the menu items
        var editJobNameMenuItem = contextMenu.Items.OfType<ToolStripMenuItem>()
          .FirstOrDefault(item => item.Text == "Edit Job Name");

        var showPartsMenuItem = contextMenu.Items.OfType<ToolStripMenuItem>()
          .FirstOrDefault(item => item.Text == "Show Parts");

        var compareFilesMenuItem = contextMenu.Items.OfType<ToolStripMenuItem>()
          .FirstOrDefault(item => item.Text == "Compare Files");

        var selectedCount = stagingDataGrid.SelectedRows.Count;

        // "Edit Job Name" is only enabled if exactly one file is selected
        if (editJobNameMenuItem != null)
        {
          editJobNameMenuItem.Enabled = selectedCount == 1;
        }

        // "Show Parts" is only enabled if exactly one file is selected
        if (showPartsMenuItem != null)
        {
          showPartsMenuItem.Enabled = selectedCount == 1;
        }

        // "Compare Files" is only enabled if exactly two files are selected
        if (compareFilesMenuItem != null)
        {
          compareFilesMenuItem.Enabled = selectedCount == 2;
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in staging context menu Opening event", ex);
      }
    }

    // Context menu handler for staging grid - Compare Files
    private void StagingContextMenu_CompareFiles_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if exactly two rows are selected
        if (stagingDataGrid.SelectedRows.Count != 2)
        {
          MessageBox.Show("Please select exactly two files to compare.",
            "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Get selected FileDisplayItem objects
        var selectedItems = new List<FileDisplayItem>();
        foreach (DataGridViewRow row in stagingDataGrid.SelectedRows)
        {
          if (row.Tag is FileDisplayItem fileItem)
          {
            selectedItems.Add(fileItem);
          }
        }

        if (selectedItems.Count != 2)
        {
          MessageBox.Show("Unable to get file information for comparison.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Extract parts from both PTX files
        var parts1 = ExtractPartsFromPtx(selectedItems[0].FilePath);
        var parts2 = ExtractPartsFromPtx(selectedItems[1].FilePath);

        if (parts1 == null || parts1.Count == 0)
        {
          MessageBox.Show($"No parts found in {selectedItems[0].FileName}.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        if (parts2 == null || parts2.Count == 0)
        {
          MessageBox.Show($"No parts found in {selectedItems[1].FileName}.", "No Parts", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Check if files are identical
        if (ArePartsIdentical(parts1, parts2))
        {
          MessageBox.Show(
            $"The files '{selectedItems[0].FileName}' and '{selectedItems[1].FileName}' are identical.\n\n" +
            $"Both files contain {parts1.Count} part(s) with matching part IDs, names, materials, dimensions, and quantities.",
            "Files Are Identical",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
          return;
        }

        // Show comparison dialog
        using (var compareDialog = new PartsCompareDialog(selectedItems[0].FileName, selectedItems[1].FileName, parts1, parts2))
        {
          compareDialog.ShowDialog(this);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error comparing files", ex);
        MessageBox.Show($"Error comparing files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Context menu handler for staging grid - Delete
    private void StagingContextMenu_Delete_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if any rows are selected
        if (stagingDataGrid.SelectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more files to delete.",
            "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Get selected FileDisplayItem objects
        var selectedItems = new List<FileDisplayItem>();
        foreach (DataGridViewRow row in stagingDataGrid.SelectedRows)
        {
          if (row.Tag is FileDisplayItem fileItem)
          {
            selectedItems.Add(fileItem);
          }
        }

        if (selectedItems.Count == 0)
        {
          MessageBox.Show("No valid files selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Confirm the delete operation
        var result = MessageBox.Show(
          $"Are you sure you want to permanently delete {selectedItems.Count} file(s)?\n\nThis action cannot be undone.",
          "Confirm Delete",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
          return;

        // Delete selected files
        var deletedCount = 0;
        var errorCount = 0;
        var errorMessages = new List<string>();

        foreach (var item in selectedItems)
        {
          try
          {
            if (File.Exists(item.FilePath))
            {
              File.Delete(item.FilePath);
              deletedCount++;
              Program.Log($"SawQueueDialog: Deleted file: {item.FilePath}");
            }
            else
            {
              errorCount++;
              errorMessages.Add($"{item.FileName} (file not found)");
            }
          }
          catch (Exception ex)
          {
            errorCount++;
            errorMessages.Add($"{item.FileName}: {ex.Message}");
            Program.Log($"SawQueueDialog: Error deleting file {item.FilePath}", ex);
          }
        }

        // Refresh the staging list
        LoadFiles();

        // Show result message
        if (errorCount == 0)
        {
          MessageBox.Show($"Successfully deleted {deletedCount} file(s).",
            "Delete Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
          var errorMsg = $"Deleted {deletedCount} file(s).\n\nErrors occurred with {errorCount} file(s):\n" +
            string.Join("\n", errorMessages.Take(5));
          if (errorMessages.Count > 5)
          {
            errorMsg += $"\n... and {errorMessages.Count - 5} more error(s)";
          }
          MessageBox.Show(errorMsg, "Delete Complete with Errors",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in delete operation", ex);
        MessageBox.Show($"Error deleting files: {ex.Message}", "Delete Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Shared helper method to modify PTX files with optional file locking
    // Returns true if file was modified, false if no changes needed, throws on error
    private bool ModifyPtxFileWithLock(string filePath, Func<string[], bool> modifyFields, bool useLocking, string operationDescription)
    {
      const int maxRetries = 5;
      const int retryDelayMs = 200;
      var lockFilePath = filePath + ".lock";
      FileStream lockFile = null;

      // Helper to perform the actual file modification
      bool PerformModification()
      {
        // Preserve the original file timestamps before modifying
        var fileInfo = new FileInfo(filePath);
        var originalLastWriteTime = fileInfo.LastWriteTime;
        var originalLastAccessTime = fileInfo.LastAccessTime;
        var originalCreationTime = fileInfo.CreationTime;

        // Read all lines from the file (UTF-8 handles BOM automatically)
        var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
        bool found = false;
        bool modified = false;

        // Find and update the JOBS line
        for (int i = 0; i < lines.Count; i++)
        {
          if (lines[i].StartsWith("JOBS,", StringComparison.OrdinalIgnoreCase))
          {
            var fields = lines[i].Split(',');
            modified = modifyFields(fields);
            if (modified)
            {
              lines[i] = string.Join(",", fields);
            }
            found = true;
            break;
          }
        }

        if (!found)
        {
          throw new InvalidOperationException("JOBS line not found in PTX file");
        }

        if (modified)
        {
          // Write the updated lines back to the file in ASCII format (no BOM)
          File.WriteAllLines(filePath, lines, Encoding.ASCII);

          // Restore the original file timestamps
          fileInfo.Refresh();
          fileInfo.LastWriteTime = originalLastWriteTime;
          fileInfo.LastAccessTime = originalLastAccessTime;
          fileInfo.CreationTime = originalCreationTime;

          Program.Log($"SawQueueDialog: {operationDescription} in {Path.GetFileName(filePath)} (preserved timestamps)");
          return true;
        }

        return false;
      }

      // Helper to clean up lock file
      void CleanupLockFile()
      {
        if (lockFile != null)
        {
          try
          {
            lockFile.Close();
            lockFile.Dispose();
          }
          catch { }
          lockFile = null;
        }

        try
        {
          if (File.Exists(lockFilePath))
          {
            File.Delete(lockFilePath);
          }
        }
        catch (Exception ex)
        {
          Program.Log($"SawQueueDialog: Error deleting lock file {Path.GetFileName(lockFilePath)}", ex);
        }
      }

      // If not using locking, perform modification directly
      if (!useLocking)
      {
        return PerformModification();
      }

      // Use file locking with retries
      for (int attempt = 0; attempt < maxRetries; attempt++)
      {
        try
        {
          // Try to create lock file (exclusive creation)
          lockFile = new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

          // Lock acquired - we can proceed with modification
          try
          {
            return PerformModification();
          }
          finally
          {
            CleanupLockFile();
          }
        }
        catch (IOException) when (attempt < maxRetries - 1)
        {
          // Lock file exists - another instance is modifying the file
          CleanupLockFile();

          // Wait and retry
          Program.Log($"SawQueueDialog: File locked by another instance, retrying ({attempt + 1}/{maxRetries}): {Path.GetFileName(filePath)}");
          System.Threading.Thread.Sleep(retryDelayMs);
        }
        catch (Exception ex)
        {
          CleanupLockFile();
          Program.Log($"SawQueueDialog: Error {operationDescription.ToLower()} in {Path.GetFileName(filePath)}", ex);
          throw;
        }
      }

      // All retries exhausted - file is locked by another instance
      Program.Log($"SawQueueDialog: Could not acquire lock for {Path.GetFileName(filePath)} after {maxRetries} attempts (another instance may be processing it)");
      return false;
    }

    // Update job names in PTX file with batch ID from the JOBS line
    // This method uses file locking to prevent concurrent modifications by multiple instances
    private bool UpdateJobNamesWithBatchId(string filePath)
    {
      return ModifyPtxFileWithLock(filePath, fields =>
      {
        // JOBS format: JOBS,JobIndex,JobName1,JobName2,Date1,Date2,BatchId,...
        // Indices: 0=JOBS, 1=JobIndex, 2=JobName1, 3=JobName2, 4=Date1, 5=Date2, 6=BatchId
        if (fields.Length >= 7)
        {
          var batchId = fields[6]?.Trim();
          if (!string.IsNullOrWhiteSpace(batchId))
          {
            var jobName1 = fields[2]?.Trim() ?? "";
            var jobName2 = fields[3]?.Trim() ?? "";

            var batchIdSuffix = "_" + batchId;

            // Helper function to remove existing batch ID and ensure it's at the end
            string ProcessJobNameWithBatchId(string jobName)
            {
              if (string.IsNullOrEmpty(jobName))
                return jobName + batchIdSuffix;

              // Remove the first occurrence of the batch ID suffix (case-insensitive)
              var index = jobName.IndexOf(batchIdSuffix, StringComparison.OrdinalIgnoreCase);
              if (index >= 0)
              {
                jobName = jobName.Substring(0, index) + jobName.Substring(index + batchIdSuffix.Length);
              }

              // Ensure the batch ID is at the end
              if (!jobName.EndsWith(batchIdSuffix, StringComparison.OrdinalIgnoreCase))
              {
                jobName = jobName + batchIdSuffix;
              }

              return jobName;
            }

            var updatedJobName1 = ProcessJobNameWithBatchId(jobName1);
            var updatedJobName2 = ProcessJobNameWithBatchId(jobName2);

            // Check if updates are needed
            if (updatedJobName1 != jobName1 || updatedJobName2 != jobName2)
            {
              fields[2] = updatedJobName1;
              fields[3] = updatedJobName2;

              Program.Log($"SawQueueDialog: Updating name with batch ID '{batchId}' in {Path.GetFileName(filePath)}");
              return true;
            }
          }
        }
        return false;
      }, useLocking: true, operationDescription: "Successfully updated job names with batch ID");
    }

    // Update job name in PTX file
    private void UpdateJobNameInPtx(string filePath, string newJobName)
    {
      ModifyPtxFileWithLock(filePath, fields =>
      {
        // Ensure we have at least 4 fields (indices 0-3) to update both job name fields
        if (fields.Length >= 4)
        {
          // Update both job name fields (index 2 and 3) to ensure they are identical
          fields[2] = newJobName;
          fields[3] = newJobName;
          return true;
        }
        return false;
      }, useLocking: false, operationDescription: $"Updated job name to '{newJobName}'");
    }

    // Context menu Opening event handler - Enable/disable menu items based on selection
    private void ReleaseContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      try
      {
        var contextMenu = sender as ContextMenuStrip;
        if (contextMenu == null) return;

        // Find the menu items
        var moveToStagingMenuItem = contextMenu.Items.OfType<ToolStripMenuItem>()
          .FirstOrDefault(item => item.Text == "Move back to Staging");

        var archiveMenuItem = contextMenu.Items.OfType<ToolStripMenuItem>()
          .FirstOrDefault(item => item.Text == "Archive");

        if (moveToStagingMenuItem == null && archiveMenuItem == null) return;

        // Check selected rows' statuses
        bool hasSentToSawStatus = false;
        int validRowCount = 0;

        if (releaseDataGrid.SelectedRows.Count > 0)
        {
          foreach (DataGridViewRow row in releaseDataGrid.SelectedRows)
          {
            if (row.Tag is TrackedReleaseFile trackedFile)
            {
              validRowCount++;
              // Status is stored as "sent to saw" (lowercase) in the object
              if (string.Equals(trackedFile.Status, "sent to saw", StringComparison.OrdinalIgnoreCase))
              {
                hasSentToSawStatus = true;
              }
            }
          }
        }

        // "Move back to Staging" is only enabled if NO selected files have "Sent to Saw" status
        if (moveToStagingMenuItem != null)
        {
          moveToStagingMenuItem.Enabled = !hasSentToSawStatus && validRowCount > 0;
        }

        // "Archive" is enabled if any files are selected
        if (archiveMenuItem != null)
        {
          archiveMenuItem.Enabled = validRowCount > 0;
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in release context menu Opening event", ex);
      }
    }

    // Context menu handler for release grid - Move back to Staging
    private void ReleaseContextMenu_MoveToStaging_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if any rows are selected
        if (releaseDataGrid.SelectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more files to move back to staging.",
            "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Get selected TrackedReleaseFile objects
        var selectedFiles = new List<TrackedReleaseFile>();
        foreach (DataGridViewRow row in releaseDataGrid.SelectedRows)
        {
          if (row.Tag is TrackedReleaseFile trackedFile)
          {
            selectedFiles.Add(trackedFile);
          }
        }

        if (selectedFiles.Count == 0)
        {
          MessageBox.Show("No valid files selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Confirm the move operation
        var result = MessageBox.Show(
          $"Are you sure you want to move {selectedFiles.Count} file(s) back to staging?\n\nThis will remove them from the release tracking list.",
          "Confirm Move",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
          return;

        // Ensure staging directory exists
        if (!Directory.Exists(stagingDir))
        {
          Directory.CreateDirectory(stagingDir);
          Program.Log($"SawQueueDialog: Created staging directory: {stagingDir}");
        }

        // Move selected files back to staging
        var movedCount = 0;
        var errorCount = 0;
        var notFoundCount = 0;

        foreach (var trackedFile in selectedFiles)
        {
          try
          {
            var sourceFile = trackedFile.FilePath;
            var destFile = Path.Combine(stagingDir, trackedFile.FileName);

            // Check if source file exists in release directory
            if (!File.Exists(sourceFile))
            {
              notFoundCount++;
              Program.Log($"SawQueueDialog: File not found in release directory: {sourceFile}");
              // Still remove from tracking even if file doesn't exist
              releaseTracker.RemoveFile(trackedFile.FileName);
              continue;
            }

            // Check if destination file already exists in staging
            if (File.Exists(destFile))
            {
              var overwriteResult = MessageBox.Show(
                $"File '{trackedFile.FileName}' already exists in staging directory.\n\nOverwrite?",
                "File Exists",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

              if (overwriteResult == DialogResult.Cancel)
                break;
              if (overwriteResult == DialogResult.No)
                continue;

              File.Delete(destFile);
            }

            // Move the file back to staging
            File.Move(sourceFile, destFile);

            // Remove from tracking since it's back in staging
            releaseTracker.RemoveFile(trackedFile.FileName);

            movedCount++;
            Program.Log($"SawQueueDialog: Moved file back to staging from '{sourceFile}' to '{destFile}'");
          }
          catch (Exception ex)
          {
            errorCount++;
            Program.Log($"SawQueueDialog: Error moving file '{trackedFile.FileName}' back to staging", ex);
          }
        }

        // Refresh both lists
        LoadFiles();

        // Show summary
        var message = $"Moved {movedCount} file(s) back to staging successfully.";
        if (notFoundCount > 0)
          message += $"\n{notFoundCount} file(s) were not found in release directory (removed from tracking).";
        if (errorCount > 0)
          message += $"\n{errorCount} file(s) failed to move. Check the log for details.";

        // MessageBox.Show(message, "Move Complete",
        //   MessageBoxButtons.OK, errorCount > 0 || notFoundCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error moving files back to staging", ex);
        MessageBox.Show($"Error moving files: {ex.Message}", "Move Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Context menu handler for release grid - Archive
    private void ReleaseContextMenu_Archive_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if any rows are selected
        if (releaseDataGrid.SelectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more files to archive.",
            "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
          return;
        }

        // Get selected TrackedReleaseFile objects
        var selectedFiles = new List<TrackedReleaseFile>();
        foreach (DataGridViewRow row in releaseDataGrid.SelectedRows)
        {
          if (row.Tag is TrackedReleaseFile trackedFile)
          {
            selectedFiles.Add(trackedFile);
          }
        }

        if (selectedFiles.Count == 0)
        {
          MessageBox.Show("No valid files selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }

        // Confirm the archive operation
        var result = MessageBox.Show(
          $"Are you sure you want to archive {selectedFiles.Count} file(s)?\n\nThis will move them from the tracking list to the archive.",
          "Confirm Archive",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
          return;

        // Archive files
        var archivedCount = 0;
        var errorCount = 0;
        var errorMessages = new List<string>();

        foreach (var trackedFile in selectedFiles)
        {
          try
          {
            if (releaseTracker.ArchiveFile(trackedFile))
            {
              archivedCount++;
            }
            else
            {
              errorCount++;
              errorMessages.Add($"{trackedFile.FileName} (archive failed)");
            }
          }
          catch (Exception ex)
          {
            errorCount++;
            errorMessages.Add($"{trackedFile.FileName}: {ex.Message}");
            Program.Log($"SawQueueDialog: Error archiving file '{trackedFile.FileName}'", ex);
          }
        }

        // Refresh the release list
        RefreshReleaseList();

        // Show result message
        if (errorCount == 0)
        {
          MessageBox.Show($"Successfully archived {archivedCount} file(s).",
            "Archive Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
          var errorMsg = $"Archived {archivedCount} file(s).\n\nErrors occurred with {errorCount} file(s):\n" +
            string.Join("\n", errorMessages.Take(5));
          if (errorMessages.Count > 5)
          {
            errorMsg += $"\n... and {errorMessages.Count - 5} more error(s)";
          }
          MessageBox.Show(errorMsg, "Archive Complete with Errors",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error in archive operation", ex);
        MessageBox.Show($"Error archiving files: {ex.Message}", "Archive Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    // Context menu handler for release grid - Remove
    // Helper class to represent file display items with metadata
    private class FileDisplayItem
    {
      public string FileName { get; set; }
      public string FilePath { get; set; }
      public long FileSize { get; set; }
      public DateTime LastModified { get; set; }
      public string JobName { get; set; }
    }

    // Helper method to format file size
    private string FormatFileSize(long fileSize)
    {
      if (fileSize < 1024)
        return $"{fileSize} B";
      else if (fileSize < 1024 * 1024)
        return $"{fileSize / 1024.0:F1} KB";
      else
        return $"{fileSize / (1024.0 * 1024.0):F1} MB";
    }

    // Helper class to represent tracked release files with status
    private class TrackedReleaseFile
    {
      public string FileName { get; set; }
      public string FilePath { get; set; }
      public string JobName { get; set; }
      public string Status { get; set; }
      public DateTime SentToRelease { get; set; }
      public DateTime? SentToSaw { get; set; }
    }

    // Helper class to represent part information from PTX file
    private class PartInfo
    {
      public int PartId { get; set; }
      public string PartName { get; set; }
      public double LengthInches { get; set; }
      public double WidthInches { get; set; }
      public int Quantity { get; set; }
      public string Material { get; set; }
    }

    // Extract parts from PTX file by reading PARTS_REQ records and matching with MATERIALS
    private List<PartInfo> ExtractPartsFromPtx(string filePath)
    {
      var parts = new List<PartInfo>();
      var materials = new Dictionary<int, string>(); // Material ID -> Material Name

      try
      {
        if (!File.Exists(filePath))
        {
          Program.Log($"SawQueueDialog: PTX file not found: {filePath}");
          return parts;
        }

        // Read all lines from the file
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        // First pass: Extract MATERIALS records to build material lookup
        foreach (var line in lines)
        {
          if (line.StartsWith("MATERIALS,", StringComparison.OrdinalIgnoreCase))
          {
            try
            {
              var fields = line.Split(',');
              // MATERIALS format: MATERIALS,JobIndex,MaterialId,MaterialName,...
              if (fields.Length >= 4)
              {
                var materialId = int.Parse(fields[2]);
                var materialName = fields[3];
                materials[materialId] = materialName;
              }
            }
            catch (Exception ex)
            {
              Program.Log($"SawQueueDialog: Error parsing MATERIALS line: {line}", ex);
            }
          }
        }

        // Second pass: Extract PARTS_REQ records and match with materials
        foreach (var line in lines)
        {
          // Check if this is a PARTS_REQ record
          if (line.StartsWith("PARTS_REQ,", StringComparison.OrdinalIgnoreCase))
          {
            try
            {
              var fields = line.Split(',');
              // PARTS_REQ format: PARTS_REQ,JobIndex,PartId,PartName,MaterialRef,Length(mm),Width(mm),Quantity,...
              if (fields.Length >= 8)
              {
                var partId = int.Parse(fields[2]);
                var partName = fields[3];
                var materialRef = int.Parse(fields[4]);
                var lengthMm = double.Parse(fields[5]);
                var widthMm = double.Parse(fields[6]);
                var quantity = int.Parse(fields[7]);

                // Convert mm to inches (1 mm = 0.0393701 inches)
                var lengthInches = lengthMm * 0.0393701;
                var widthInches = widthMm * 0.0393701;

                // Get material name from lookup, or use "Unknown" if not found
                var materialName = materials.ContainsKey(materialRef) ? materials[materialRef] : "Unknown";

                parts.Add(new PartInfo
                {
                  PartId = partId,
                  PartName = partName,
                  LengthInches = lengthInches,
                  WidthInches = widthInches,
                  Quantity = quantity,
                  Material = materialName
                });
              }
            }
            catch (Exception ex)
            {
              Program.Log($"SawQueueDialog: Error parsing PARTS_REQ line: {line}", ex);
            }
          }
        }

        Program.Log($"SawQueueDialog: Extracted {parts.Count} parts from {filePath}");
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error extracting parts from {filePath}", ex);
      }

      return parts;
    }

    // Check if two part lists are identical
    private bool ArePartsIdentical(List<PartInfo> parts1, List<PartInfo> parts2)
    {
      try
      {
        // Check if counts match
        if (parts1.Count != parts2.Count)
          return false;

        // Build dictionaries for quick lookup by PartId
        var parts1Dict = parts1.ToDictionary(p => p.PartId);
        var parts2Dict = parts2.ToDictionary(p => p.PartId);

        // Check if all PartIds match
        if (parts1Dict.Keys.Count != parts2Dict.Keys.Count)
          return false;

        // Check each part for exact match
        foreach (var part1 in parts1)
        {
          if (!parts2Dict.ContainsKey(part1.PartId))
            return false;

          var part2 = parts2Dict[part1.PartId];

          // Compare all properties (with tolerance for floating point)
          if (Math.Abs(part1.LengthInches - part2.LengthInches) > 0.01 ||
              Math.Abs(part1.WidthInches - part2.WidthInches) > 0.01 ||
              part1.Quantity != part2.Quantity ||
              !string.Equals(part1.Material, part2.Material, StringComparison.OrdinalIgnoreCase) ||
              !string.Equals(part1.PartName, part2.PartName, StringComparison.OrdinalIgnoreCase))
          {
            return false;
          }
        }

        return true;
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error checking if parts are identical", ex);
        return false; // If error, assume not identical to be safe
      }
    }

    // Class to track release file history with CSV persistence
    private sealed class ReleaseFileTracker : IDisposable
    {
      private readonly List<TrackedReleaseFile> trackedFiles;
      private readonly string csvFilePath;
      private readonly FileSystemWatcher csvWatcher;
      private readonly int maxTrackedFiles;
      private bool isSaving = false; // Flag to ignore changes we make ourselves
      private DateTime lastChangeTime = DateTime.MinValue; // For debouncing rapid changes
      private const int ChangeDebounceMs = 500; // Debounce window for file changes

      // Event fired when CSV file changes (from another instance)
      public event EventHandler CsvFileChanged;

      public ReleaseFileTracker(string releaseDirectory, int maxTrackedFiles = 200)
      {
        this.maxTrackedFiles = maxTrackedFiles;

        // Store CSV file in the release directory so multiple users can share the same tracking file
        if (string.IsNullOrWhiteSpace(releaseDirectory))
        {
          // Fallback to config directory if release directory not provided
          var configDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
          if (string.IsNullOrEmpty(configDir))
            configDir = Environment.CurrentDirectory;
          csvFilePath = Path.Combine(configDir, "release_history.csv");
        }
        else
        {
          // Ensure release directory exists
          try
          {
            if (!Directory.Exists(releaseDirectory))
            {
              Directory.CreateDirectory(releaseDirectory);
              Program.Log($"ReleaseFileTracker: Created release directory: {releaseDirectory}");
            }
            csvFilePath = Path.Combine(releaseDirectory, "release_history.csv");
          }
          catch (Exception ex)
          {
            // Fallback to config directory if we can't access release directory
            Program.Log($"ReleaseFileTracker: Error accessing release directory, falling back to config directory", ex);
            var configDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(configDir))
              configDir = Environment.CurrentDirectory;
            csvFilePath = Path.Combine(configDir, "release_history.csv");
          }
        }

        trackedFiles = new List<TrackedReleaseFile>();
        Program.Log($"ReleaseFileTracker: Using CSV file at: {csvFilePath}");
        LoadFromCsv();

        // Set up file watcher to detect changes from other instances
        csvWatcher = new FileSystemWatcher
        {
          Path = Path.GetDirectoryName(csvFilePath),
          Filter = Path.GetFileName(csvFilePath),
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
          EnableRaisingEvents = true
        };
        csvWatcher.Changed += CsvWatcher_Changed;
        csvWatcher.Error += CsvWatcher_Error;
        Program.Log($"ReleaseFileTracker: Set up file watcher for CSV synchronization");
      }

      // Handle CSV file changes from other instances
      private void CsvWatcher_Changed(object sender, FileSystemEventArgs e)
      {
        try
        {
          // Ignore changes we made ourselves
          if (isSaving)
          {
            return;
          }

          // Debounce rapid file change events (FileSystemWatcher can fire multiple times)
          var now = DateTime.Now;
          var timeSinceLastChange = (now - lastChangeTime).TotalMilliseconds;
          if (timeSinceLastChange < ChangeDebounceMs)
          {
            // Too soon after last change, ignore this event
            return;
          }
          lastChangeTime = now;

          // Wait a moment for file write to complete
          System.Threading.Thread.Sleep(200);

          // Reload CSV file
          Program.Log("ReleaseFileTracker: CSV file changed by another instance, reloading...");
          LoadFromCsv();

          // Notify subscribers that the data has changed
          CsvFileChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
          Program.Log("ReleaseFileTracker: Error handling CSV file change", ex);
        }
      }

      // Handle watcher errors
      private void CsvWatcher_Error(object sender, ErrorEventArgs e)
      {
        try
        {
          Program.Log("ReleaseFileTracker: CSV file watcher error", e.GetException());

          // Try to restart the watcher
          if (csvWatcher != null)
          {
            csvWatcher.EnableRaisingEvents = false;
            System.Threading.Thread.Sleep(1000);

            var csvDir = Path.GetDirectoryName(csvFilePath);
            if (Directory.Exists(csvDir) && File.Exists(csvFilePath))
            {
              csvWatcher.EnableRaisingEvents = true;
              Program.Log("ReleaseFileTracker: CSV file watcher restarted after error");
            }
          }
        }
        catch (Exception ex)
        {
          Program.Log("ReleaseFileTracker: Error handling CSV watcher error", ex);
        }
      }

      // Dispose the file watcher
      public void Dispose()
      {
        if (csvWatcher != null)
        {
          csvWatcher.Changed -= CsvWatcher_Changed;
          csvWatcher.Error -= CsvWatcher_Error;
          csvWatcher.EnableRaisingEvents = false;
          csvWatcher.Dispose();
          Program.Log("ReleaseFileTracker: Disposed CSV file watcher");
        }
      }

      // Load tracked files from CSV
      private void LoadFromCsv()
      {
        try
        {
          if (!File.Exists(csvFilePath))
          {
            Program.Log("ReleaseFileTracker: CSV file does not exist, starting with empty list");
            return;
          }

          trackedFiles.Clear();

          // Read with file sharing to allow concurrent access
          var lines = ReadCsvWithRetry();
          if (lines == null)
          {
            Program.Log("ReleaseFileTracker: Failed to read CSV file after retries");
            return;
          }

          // Skip header line if present
          int startIndex = 0;
          if (lines.Length > 0 && lines[0].StartsWith("FileName,", StringComparison.OrdinalIgnoreCase))
            startIndex = 1;

          for (int i = startIndex; i < lines.Length; i++)
          {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
              continue;

            try
            {
              var fields = ParseCsvLine(line);
              if (fields.Length >= 5)
              {
                var file = new TrackedReleaseFile
                {
                  FileName = fields[0],
                  FilePath = fields[1],
                  JobName = fields[2],
                  Status = fields[3],
                  SentToRelease = DateTime.Parse(fields[4]),
                  SentToSaw = string.IsNullOrWhiteSpace(fields.Length > 5 ? fields[5] : null)
                    ? (DateTime?)null
                    : DateTime.Parse(fields[5])
                };
                trackedFiles.Add(file);
              }
            }
            catch (Exception ex)
            {
              Program.Log($"ReleaseFileTracker: Error parsing CSV line {i + 1}: {line}", ex);
            }
          }

          Program.Log($"ReleaseFileTracker: Loaded {trackedFiles.Count} tracked files from CSV");
        }
        catch (Exception ex)
        {
          Program.Log("ReleaseFileTracker: Error loading from CSV", ex);
        }
      }

      // Parse CSV line handling quoted fields
      private string[] ParseCsvLine(string line)
      {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
          char c = line[i];

          if (c == '"')
          {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
              // Escaped quote
              currentField.Append('"');
              i++; // Skip next quote
            }
            else
            {
              // Toggle quote state
              inQuotes = !inQuotes;
            }
          }
          else if (c == ',' && !inQuotes)
          {
            // End of field
            fields.Add(currentField.ToString());
            currentField.Clear();
          }
          else
          {
            currentField.Append(c);
          }
        }

        // Add last field
        fields.Add(currentField.ToString());
        return fields.ToArray();
      }

      // Read CSV file with retry logic for concurrent access
      private string[] ReadCsvWithRetry(int maxRetries = 5, int delayMs = 200)
      {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
          try
          {
            // Use FileShare.ReadWrite to allow concurrent reads and writes
            using (var fileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
              var lines = new List<string>();
              string line;
              while ((line = reader.ReadLine()) != null)
              {
                lines.Add(line);
              }
              return lines.ToArray();
            }
          }
          catch (IOException ex) when (attempt < maxRetries - 1)
          {
            // File might be locked by another process, retry after delay
            Program.Log($"ReleaseFileTracker: CSV file locked, retrying ({attempt + 1}/{maxRetries}): {ex.Message}");
            System.Threading.Thread.Sleep(delayMs);
          }
          catch (Exception ex)
          {
            Program.Log($"ReleaseFileTracker: Error reading CSV file on attempt {attempt + 1}", ex);
            if (attempt == maxRetries - 1)
              throw;
            System.Threading.Thread.Sleep(delayMs);
          }
        }
        return null;
      }

      // Save tracked files to CSV with retry logic for concurrent access
      private void SaveToCsv()
      {
        const int maxRetries = 5;
        const int delayMs = 200;

        // Set flag to ignore our own changes
        isSaving = true;
        try
        {
          for (int attempt = 0; attempt < maxRetries; attempt++)
          {
            try
            {
              // Use FileShare.Read to allow concurrent reads while writing
              using (var fileStream = new FileStream(csvFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
              using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
              {
                // Write header
                writer.WriteLine("FileName,FilePath,JobName,Status,SentToRelease,SentToSaw");

                // Write data (most recent first)
                foreach (var file in trackedFiles.OrderByDescending(f => f.SentToRelease))
                {
                  var sentToSawStr = file.SentToSaw.HasValue ? file.SentToSaw.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                  writer.WriteLine($"{EscapeCsvField(file.FileName)},{EscapeCsvField(file.FilePath)},{EscapeCsvField(file.JobName)},{EscapeCsvField(file.Status)},{file.SentToRelease:yyyy-MM-dd HH:mm:ss},{sentToSawStr}");
                }
              }

              Program.Log($"ReleaseFileTracker: Saved {trackedFiles.Count} tracked files to CSV");
              return; // Success, exit retry loop
            }
            catch (IOException ex) when (attempt < maxRetries - 1)
            {
              // File might be locked by another process, retry after delay
              Program.Log($"ReleaseFileTracker: CSV file locked during save, retrying ({attempt + 1}/{maxRetries}): {ex.Message}");
              System.Threading.Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
              Program.Log($"ReleaseFileTracker: Error saving to CSV on attempt {attempt + 1}", ex);
              if (attempt == maxRetries - 1)
              {
                // Last attempt failed, log error but don't throw to prevent application crash
                Program.Log("ReleaseFileTracker: Failed to save CSV after all retries", ex);
              }
              else
              {
                System.Threading.Thread.Sleep(delayMs);
              }
            }
          }
        }
        finally
        {
          // Clear flag after a short delay to allow file system to settle
          System.Threading.Thread.Sleep(200);
          isSaving = false;
        }
      }

      // Escape CSV field if it contains comma or quotes
      private string EscapeCsvField(string field)
      {
        if (string.IsNullOrEmpty(field))
          return "";

        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
          return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
      }

      // Add a file to tracking
      public void AddFile(string fileName, string filePath, string jobName, string status)
      {
        // Remove existing entry for this file if present
        trackedFiles.RemoveAll(f => string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));

        var file = new TrackedReleaseFile
        {
          FileName = fileName,
          FilePath = filePath,
          JobName = jobName,
          Status = status,
          SentToRelease = DateTime.Now,
          SentToSaw = null
        };

        trackedFiles.Add(file);

        // Keep only the last maxTrackedFiles; move oldest records to archive CSV
        if (trackedFiles.Count > maxTrackedFiles)
        {
          var toArchive = trackedFiles.OrderBy(f => f.SentToRelease).Take(trackedFiles.Count - maxTrackedFiles).ToList();
          foreach (var oldFile in toArchive)
          {
            if (!ArchiveFile(oldFile))
            {
              trackedFiles.RemoveAll(f => string.Equals(f.FileName, oldFile.FileName, StringComparison.OrdinalIgnoreCase));
              Program.Log($"ReleaseFileTracker: Could not archive '{oldFile.FileName}', removed from tracking to enforce limit");
              SaveToCsv();
            }
          }
        }

        SaveToCsv();
        Program.Log($"ReleaseFileTracker: Added file '{fileName}' with status '{status}'");
      }

      // Update status for files that have been removed from release directory
      public void UpdateStatusForRemovedFiles(string releaseDir)
      {
        bool changed = false;
        var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get list of files currently in release directory
        if (Directory.Exists(releaseDir))
        {
          var files = Directory.GetFiles(releaseDir, "*.PTX", SearchOption.TopDirectoryOnly);
          foreach (var file in files)
          {
            currentFiles.Add(Path.GetFileName(file));
          }
        }

        // Update status for files that are no longer in the directory
        foreach (var trackedFile in trackedFiles)
        {
          if (trackedFile.Status == "pending" && !currentFiles.Contains(trackedFile.FileName))
          {
            trackedFile.Status = "sent to saw";
            trackedFile.SentToSaw = DateTime.Now;
            changed = true;
            Program.Log($"ReleaseFileTracker: Updated status to 'sent to saw' for '{trackedFile.FileName}'");
          }
        }

        if (changed)
        {
          SaveToCsv();
        }
      }

      // Update status for a specific file by filename
      public void UpdateFileStatusToSentToSaw(string fileName)
      {
        bool changed = false;

        foreach (var trackedFile in trackedFiles)
        {
          if (string.Equals(trackedFile.FileName, fileName, StringComparison.OrdinalIgnoreCase)
              && trackedFile.Status == "pending")
          {
            trackedFile.Status = "sent to saw";
            trackedFile.SentToSaw = DateTime.Now;
            changed = true;
            Program.Log($"ReleaseFileTracker: Updated status to 'sent to saw' for '{fileName}'");
            break;
          }
        }

        if (changed)
        {
          SaveToCsv();
        }
      }

      // Archive a file to yearly archive CSV and remove from tracking
      public bool ArchiveFile(TrackedReleaseFile file)
      {
        try
        {
          // Get the archive directory (subdirectory of release directory)
          var archiveDir = Path.Combine(Path.GetDirectoryName(csvFilePath), "archive");

          // Ensure archive directory exists
          if (!Directory.Exists(archiveDir))
          {
            Directory.CreateDirectory(archiveDir);
            Program.Log($"ReleaseFileTracker: Created archive directory: {archiveDir}");
          }

          // Determine the year from the file's SentToRelease date
          var year = file.SentToRelease.Year;
          var archiveFileName = $"release_history_{year}.csv";
          var archiveFilePath = Path.Combine(archiveDir, archiveFileName);

          // Append the file record to the archive CSV
          var sentToSawStr = file.SentToSaw.HasValue ? file.SentToSaw.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
          var csvLine = $"{EscapeCsvField(file.FileName)},{EscapeCsvField(file.FilePath)},{EscapeCsvField(file.JobName)},{EscapeCsvField(file.Status)},{file.SentToRelease:yyyy-MM-dd HH:mm:ss},{sentToSawStr}";

          // Append to archive file (create if doesn't exist, append if it does)
          var fileExists = File.Exists(archiveFilePath);
          using (var writer = new StreamWriter(archiveFilePath, append: true, Encoding.UTF8))
          {
            // Write header if this is a new file
            if (!fileExists)
            {
              writer.WriteLine("FileName,FilePath,JobName,Status,SentToRelease,SentToSaw");
            }
            writer.WriteLine(csvLine);
          }

          // Remove from active tracking
          var removed = trackedFiles.RemoveAll(f => string.Equals(f.FileName, file.FileName, StringComparison.OrdinalIgnoreCase));
          if (removed > 0)
          {
            SaveToCsv();
            Program.Log($"ReleaseFileTracker: Archived file '{file.FileName}' to {archiveFileName}");
            return true;
          }

          return false;
        }
        catch (Exception ex)
        {
          Program.Log($"ReleaseFileTracker: Error archiving file '{file.FileName}'", ex);
          return false;
        }
      }

      // Remove a file from tracking by filename (without archiving)
      public void RemoveFile(string fileName)
      {
        var removed = trackedFiles.RemoveAll(f => string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
          SaveToCsv();
          Program.Log($"ReleaseFileTracker: Removed file '{fileName}' from tracking");
        }
      }

      // Get all tracked files (most recent first)
      public List<TrackedReleaseFile> GetTrackedFiles()
      {
        return trackedFiles.OrderByDescending(f => f.SentToRelease).ToList();
      }
    }

    // Dialog to display parts list from PTX file
    private sealed class PartsListDialog : Form
    {
      public PartsListDialog(string fileName, List<PartInfo> parts)
      {
        InitializeDialog(fileName, parts);
      }

      private void InitializeDialog(string fileName, List<PartInfo> parts)
      {
        Text = $"Parts List - {fileName}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 800;
        Height = 600;
        MinimumSize = new Size(600, 400);
        Icon = SystemIcons.Application;
        ShowInTaskbar = false;
        MaximizeBox = true;

        // Create main panel
        var mainPanel = new Panel { Dock = DockStyle.Fill };

        // Create header label
        var headerLabel = new Label
        {
          Text = $"Parts in {fileName}",
          Dock = DockStyle.Top,
          Height = 40,
          Padding = new Padding(10, 10, 10, 5),
          Font = new Font("Segoe UI", 10F, FontStyle.Bold),
          BackColor = Color.FromArgb(240, 240, 240),
          TextAlign = ContentAlignment.MiddleLeft
        };

        // Create DataGridView for parts
        var partsGrid = new DataGridView
        {
          Dock = DockStyle.Fill,
          Font = GetNonMonospaceFont(9F),
          AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
          SelectionMode = DataGridViewSelectionMode.FullRowSelect,
          ReadOnly = true,
          AllowUserToAddRows = false,
          AllowUserToDeleteRows = false,
          AllowUserToResizeRows = false,
          RowHeadersVisible = false,
          BackgroundColor = SystemColors.Window,
          BorderStyle = BorderStyle.None
        };

        // Set up columns
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "PartId",
          HeaderText = "Part ID",
          DataPropertyName = "PartId",
          FillWeight = 8
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "PartName",
          HeaderText = "Part Name",
          DataPropertyName = "PartName",
          FillWeight = 30
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Material",
          HeaderText = "Material",
          DataPropertyName = "Material",
          FillWeight = 20
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Length",
          HeaderText = "Length (in)",
          DataPropertyName = "Length",
          FillWeight = 12
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Width",
          HeaderText = "Width (in)",
          DataPropertyName = "Width",
          FillWeight = 12
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Quantity",
          HeaderText = "Qty",
          DataPropertyName = "Quantity",
          FillWeight = 8
        });

        // Create display items with formatted sizes
        var displayItems = parts.Select(p => new
        {
          PartId = p.PartId,
          PartName = p.PartName,
          Material = p.Material,
          Length = $"{p.LengthInches:F2}",
          Width = $"{p.WidthInches:F2}",
          Quantity = p.Quantity
        }).ToList();

        partsGrid.DataSource = displayItems;

        // Create status label
        var statusLabel = new Label
        {
          Text = $"{parts.Count} part(s) found",
          Dock = DockStyle.Bottom,
          Height = 30,
          Padding = new Padding(10, 5, 10, 5),
          BackColor = Color.FromArgb(250, 250, 250),
          ForeColor = Color.DarkBlue,
          TextAlign = ContentAlignment.MiddleLeft
        };

        // Create button panel
        var buttonPanel = new Panel
        {
          Dock = DockStyle.Bottom,
          Height = 50,
          Padding = new Padding(10, 5, 10, 10)
        };

        var btnClose = new Button
        {
          Text = "Close",
          AutoSize = true,
          Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
          BackColor = Color.FromArgb(215, 218, 222),
          ForeColor = Color.Black,
          FlatStyle = FlatStyle.Flat,
          Font = new Font("Segoe UI", 9F, FontStyle.Regular),
          UseVisualStyleBackColor = false,
          Padding = new Padding(12, 6, 12, 6),
          Margin = new Padding(4, 0, 4, 0),
          Width = 80,
          DialogResult = DialogResult.OK
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.Location = new Point(buttonPanel.Width - btnClose.Width - 10, 10);

        buttonPanel.Controls.Add(btnClose);
        buttonPanel.Resize += (s, e) =>
        {
          btnClose.Location = new Point(buttonPanel.Width - btnClose.Width - 10, 10);
        };

        // Add controls to form
        mainPanel.Controls.Add(partsGrid);
        mainPanel.Controls.Add(headerLabel);
        mainPanel.Controls.Add(statusLabel);
        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);

        AcceptButton = btnClose;
      }

      // Helper method to get a non-monospace font, preferring Noto Sans
      private Font GetNonMonospaceFont(float size)
      {
        try
        {
          var testFont = new Font("Noto Sans", size);
          testFont.Dispose();
          return new Font("Noto Sans", size);
        }
        catch
        {
          return new Font("Segoe UI", size);
        }
      }
    }

    // Dialog to compare parts from two PTX files side by side
    private sealed class PartsCompareDialog : Form
    {
      private readonly List<PartInfo> parts1;
      private readonly List<PartInfo> parts2;
      private readonly Dictionary<int, PartInfo> parts1Dict; // PartId -> PartInfo
      private readonly Dictionary<int, PartInfo> parts2Dict; // PartId -> PartInfo
      private DataGridView leftGrid;
      private DataGridView rightGrid;
      private bool isScrolling = false; // Flag to prevent infinite scroll loops

      public PartsCompareDialog(string fileName1, string fileName2, List<PartInfo> parts1, List<PartInfo> parts2)
      {
        this.parts1 = parts1;
        this.parts2 = parts2;
        // Build dictionaries for quick lookup by PartId
        this.parts1Dict = parts1.ToDictionary(p => p.PartId);
        this.parts2Dict = parts2.ToDictionary(p => p.PartId);
        InitializeDialog(fileName1, fileName2, parts1, parts2);
      }

      private void InitializeDialog(string fileName1, string fileName2, List<PartInfo> parts1, List<PartInfo> parts2)
      {
        Text = $"Compare Parts - {fileName1} vs {fileName2}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 1400;
        Height = 700;
        MinimumSize = new Size(1000, 500);
        Icon = SystemIcons.Application;
        ShowInTaskbar = false;
        MaximizeBox = true;

        // Create main split container for left/right view (50/50 split)
        var mainSplitContainer = new SplitContainer
        {
          Dock = DockStyle.Fill,
          Orientation = Orientation.Vertical,
          SplitterDistance = (int)(this.ClientSize.Width * 0.50),
          BorderStyle = BorderStyle.Fixed3D
        };

        // Create left panel for file 1
        var panel1 = CreatePartsPanel(fileName1, parts1, "Left", parts1Dict, parts2Dict, out leftGrid);

        // Create right panel for file 2
        var panel2 = CreatePartsPanel(fileName2, parts2, "Right", parts2Dict, parts1Dict, out rightGrid);

        mainSplitContainer.Panel1.Controls.Add(panel1);
        mainSplitContainer.Panel2.Controls.Add(panel2);

        // Set up scroll synchronization
        SetupScrollSynchronization();

        // Create button panel at the bottom
        var buttonPanel = new Panel
        {
          Dock = DockStyle.Bottom,
          Height = 50,
          Padding = new Padding(10, 5, 10, 10)
        };

        var btnClose = new Button
        {
          Text = "Close",
          AutoSize = true,
          Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
          BackColor = Color.FromArgb(215, 218, 222),
          ForeColor = Color.Black,
          FlatStyle = FlatStyle.Flat,
          Font = new Font("Segoe UI", 9F, FontStyle.Regular),
          UseVisualStyleBackColor = false,
          Padding = new Padding(12, 6, 12, 6),
          Margin = new Padding(4, 0, 4, 0),
          Width = 80,
          DialogResult = DialogResult.OK
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.Location = new Point(buttonPanel.Width - btnClose.Width - 10, 10);

        buttonPanel.Controls.Add(btnClose);
        buttonPanel.Resize += (s, e) =>
        {
          btnClose.Location = new Point(buttonPanel.Width - btnClose.Width - 10, 10);
        };

        // Add controls to form
        Controls.Add(mainSplitContainer);
        Controls.Add(buttonPanel);

        AcceptButton = btnClose;

        // Set splitter to 50% on load
        Load += (s, e) =>
        {
          if (mainSplitContainer != null && this.ClientSize.Width > 0)
          {
            mainSplitContainer.SplitterDistance = (int)(this.ClientSize.Width * 0.50);
          }
        };
      }

      private void SetupScrollSynchronization()
      {
        if (leftGrid == null || rightGrid == null) return;

        // Synchronize left grid scrolling to right grid
        leftGrid.Scroll += (s, e) =>
        {
          if (!isScrolling && rightGrid != null)
          {
            isScrolling = true;
            try
            {
              // Check if grids are ready and have rows
              if (leftGrid.Rows.Count == 0 || rightGrid.Rows.Count == 0)
                return;

              var sourceIndex = leftGrid.FirstDisplayedScrollingRowIndex;

              // Validate source index
              if (sourceIndex < 0 || sourceIndex >= leftGrid.Rows.Count)
                return;

              // Calculate proportional scroll position
              var sourceScrollRatio = (double)sourceIndex / leftGrid.Rows.Count;
              var targetIndex = (int)(sourceScrollRatio * rightGrid.Rows.Count);

              // Ensure target index is within bounds
              targetIndex = Math.Max(0, Math.Min(targetIndex, rightGrid.Rows.Count - 1));

              // Only update if the target index is valid
              if (targetIndex >= 0 && targetIndex < rightGrid.Rows.Count)
              {
                rightGrid.FirstDisplayedScrollingRowIndex = targetIndex;
              }
            }
            catch (Exception ex)
            {
              // Silently handle scroll errors to prevent UI disruption
              Program.Log("PartsCompareDialog: Error synchronizing scroll from left to right", ex);
            }
            finally
            {
              isScrolling = false;
            }
          }
        };

        // Synchronize right grid scrolling to left grid
        rightGrid.Scroll += (s, e) =>
        {
          if (!isScrolling && leftGrid != null)
          {
            isScrolling = true;
            try
            {
              // Check if grids are ready and have rows
              if (leftGrid.Rows.Count == 0 || rightGrid.Rows.Count == 0)
                return;

              var sourceIndex = rightGrid.FirstDisplayedScrollingRowIndex;

              // Validate source index
              if (sourceIndex < 0 || sourceIndex >= rightGrid.Rows.Count)
                return;

              // Calculate proportional scroll position
              var sourceScrollRatio = (double)sourceIndex / rightGrid.Rows.Count;
              var targetIndex = (int)(sourceScrollRatio * leftGrid.Rows.Count);

              // Ensure target index is within bounds
              targetIndex = Math.Max(0, Math.Min(targetIndex, leftGrid.Rows.Count - 1));

              // Only update if the target index is valid
              if (targetIndex >= 0 && targetIndex < leftGrid.Rows.Count)
              {
                leftGrid.FirstDisplayedScrollingRowIndex = targetIndex;
              }
            }
            catch (Exception ex)
            {
              // Silently handle scroll errors to prevent UI disruption
              Program.Log("PartsCompareDialog: Error synchronizing scroll from right to left", ex);
            }
            finally
            {
              isScrolling = false;
            }
          }
        };
      }

      private Panel CreatePartsPanel(string fileName, List<PartInfo> parts, string side,
        Dictionary<int, PartInfo> currentDict, Dictionary<int, PartInfo> otherDict, out DataGridView grid)
      {
        var panel = new Panel { Dock = DockStyle.Fill };

        // Create header label
        var headerLabel = new Label
        {
          Text = $"{fileName} ({parts.Count} parts)",
          Dock = DockStyle.Top,
          Height = 40,
          Padding = new Padding(10, 10, 10, 5),
          Font = new Font("Segoe UI", 10F, FontStyle.Bold),
          BackColor = side == "Left" ? Color.FromArgb(240, 245, 255) : Color.FromArgb(255, 245, 240),
          TextAlign = ContentAlignment.MiddleLeft
        };

        // Create DataGridView for parts
        var partsGrid = new DataGridView
        {
          Dock = DockStyle.Fill,
          Font = GetNonMonospaceFont(9F),
          AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
          SelectionMode = DataGridViewSelectionMode.FullRowSelect,
          ReadOnly = true,
          AllowUserToAddRows = false,
          AllowUserToDeleteRows = false,
          AllowUserToResizeRows = false,
          RowHeadersVisible = false,
          BackgroundColor = SystemColors.Window,
          BorderStyle = BorderStyle.None
        };

        // Set up columns (same as PartsListDialog)
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "PartId",
          HeaderText = "Part ID",
          DataPropertyName = "PartId",
          FillWeight = 8
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "PartName",
          HeaderText = "Part Name",
          DataPropertyName = "PartName",
          FillWeight = 30
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Material",
          HeaderText = "Material",
          DataPropertyName = "Material",
          FillWeight = 20
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Length",
          HeaderText = "Length (in)",
          DataPropertyName = "Length",
          FillWeight = 12
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Width",
          HeaderText = "Width (in)",
          DataPropertyName = "Width",
          FillWeight = 12
        });
        partsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
          Name = "Quantity",
          HeaderText = "Qty",
          DataPropertyName = "Quantity",
          FillWeight = 8
        });

        // Create display items with formatted sizes
        var displayItems = parts.Select(p => new
        {
          PartId = p.PartId,
          PartName = p.PartName,
          Material = p.Material,
          Length = $"{p.LengthInches:F2}",
          Width = $"{p.WidthInches:F2}",
          Quantity = p.Quantity
        }).ToList();

        partsGrid.DataSource = displayItems;

        // Store PartInfo in row Tag after data binding for comparison
        partsGrid.DataBindingComplete += (s, e) =>
        {
          for (int i = 0; i < partsGrid.Rows.Count && i < parts.Count; i++)
          {
            partsGrid.Rows[i].Tag = parts[i];
          }
        };

        // Add CellFormatting event to highlight differences
        partsGrid.CellFormatting += (s, e) =>
        {
          try
          {
            if (e.RowIndex < 0 || e.RowIndex >= partsGrid.Rows.Count) return;

            var row = partsGrid.Rows[e.RowIndex];
            if (!(row.Tag is PartInfo currentPart)) return;

            // Check if this part exists in the other file
            if (!otherDict.ContainsKey(currentPart.PartId))
            {
              // Part only exists in current file - highlight in yellow
              e.CellStyle.BackColor = Color.FromArgb(255, 255, 200); // Light yellow
              e.CellStyle.ForeColor = Color.Black;
            }
            else
            {
              // Part exists in both files - check for differences
              var otherPart = otherDict[currentPart.PartId];
              bool isDifferent = false;

              // Compare values (with tolerance for floating point)
              if (Math.Abs(currentPart.LengthInches - otherPart.LengthInches) > 0.01 ||
                  Math.Abs(currentPart.WidthInches - otherPart.WidthInches) > 0.01 ||
                  currentPart.Quantity != otherPart.Quantity ||
                  !string.Equals(currentPart.Material, otherPart.Material, StringComparison.OrdinalIgnoreCase) ||
                  !string.Equals(currentPart.PartName, otherPart.PartName, StringComparison.OrdinalIgnoreCase))
              {
                isDifferent = true;
              }

              if (isDifferent)
              {
                // Part exists but has different values - highlight in orange
                e.CellStyle.BackColor = Color.FromArgb(255, 200, 150); // Light orange
                e.CellStyle.ForeColor = Color.Black;
              }
              else
              {
                // Part is identical - use default styling
                e.CellStyle.BackColor = partsGrid.DefaultCellStyle.BackColor;
                e.CellStyle.ForeColor = partsGrid.DefaultCellStyle.ForeColor;
              }
            }
          }
          catch (Exception ex)
          {
            Program.Log("PartsCompareDialog: Error formatting cell", ex);
          }
        };

        // Count differences for status label
        var uniqueCount = parts.Count(p => !otherDict.ContainsKey(p.PartId));
        var differentCount = parts.Count(p =>
        {
          if (!otherDict.ContainsKey(p.PartId)) return false;
          var otherPart = otherDict[p.PartId];
          return Math.Abs(p.LengthInches - otherPart.LengthInches) > 0.01 ||
                 Math.Abs(p.WidthInches - otherPart.WidthInches) > 0.01 ||
                 p.Quantity != otherPart.Quantity ||
                 !string.Equals(p.Material, otherPart.Material, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(p.PartName, otherPart.PartName, StringComparison.OrdinalIgnoreCase);
        });
        var identicalCount = parts.Count - uniqueCount - differentCount;

        // Create status label with difference summary
        var statusText = $"{parts.Count} part(s)";
        if (uniqueCount > 0 || differentCount > 0)
        {
          statusText += $" - {identicalCount} identical";
          if (uniqueCount > 0) statusText += $", {uniqueCount} unique";
          if (differentCount > 0) statusText += $", {differentCount} different";
        }
        var statusLabel = new Label
        {
          Text = statusText,
          Dock = DockStyle.Bottom,
          Height = 30,
          Padding = new Padding(10, 5, 10, 5),
          BackColor = Color.FromArgb(250, 250, 250),
          ForeColor = Color.DarkBlue,
          TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(partsGrid);
        panel.Controls.Add(headerLabel);
        panel.Controls.Add(statusLabel);

        // Set the grid out parameter
        grid = partsGrid;

        return panel;
      }

      // Helper method to get a non-monospace font, preferring Noto Sans
      private Font GetNonMonospaceFont(float size)
      {
        try
        {
          var testFont = new Font("Noto Sans", size);
          testFont.Dispose();
          return new Font("Noto Sans", size);
        }
        catch
        {
          return new Font("Segoe UI", size);
        }
      }
    }
  }
}
