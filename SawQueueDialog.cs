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
    private readonly string stagingDir;
    private readonly string releaseDir;
    private readonly ReleaseFileTracker releaseTracker;
    private FileSystemWatcher releaseDirWatcher;
    private FileSystemWatcher stagingDirWatcher;

    public SawQueueDialog()
    {
      // Load directories from configuration
      var cfg = UserConfig.LoadOrDefault();
      stagingDir = cfg.StagingDir ?? @"P:\CadLinkPTX\staging";
      releaseDir = cfg.ReleaseDir ?? @"P:\CadLinkPTX\release";

      // Initialize release file tracker
      releaseTracker = new ReleaseFileTracker();

      InitializeDialog();

      // Create main split container for two vertical panes
      mainSplitContainer = new SplitContainer
      {
        Dock = DockStyle.Fill,
        Orientation = Orientation.Vertical,
        SplitterDistance = (int)(this.ClientSize.Width * 0.60), // Left panel gets 60%, right panel gets 40%
        BorderStyle = BorderStyle.Fixed3D
      };

      // Create left panel (Staging)
      var leftPanel = new Panel { Dock = DockStyle.Fill };
      stagingLabel = new Label
      {
        Text = $"Staging Directory: {stagingDir}",
        Dock = DockStyle.Top,
        Height = 40,
        Padding = new Padding(10, 10, 10, 5),
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        BackColor = Color.FromArgb(240, 240, 240),
        TextAlign = ContentAlignment.MiddleLeft
      };

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

      stagingContextMenu.Items.Add(editJobNameMenuItem);
      stagingContextMenu.Items.Add(moveMenuItem);

      stagingDataGrid.ContextMenuStrip = stagingContextMenu;

      // Add double-click handler to edit job name when clicking on JobName column
      stagingDataGrid.CellDoubleClick += StagingDataGrid_CellDoubleClick;

      leftPanel.Controls.Add(stagingDataGrid);
      leftPanel.Controls.Add(stagingLabel);
      leftPanel.Controls.Add(stagingStatusLabel);

      // Create right panel (Release)
      var rightPanel = new Panel { Dock = DockStyle.Fill };
      releaseLabel = new Label
      {
        Text = $"Release Directory: {releaseDir}",
        Dock = DockStyle.Top,
        Height = 40,
        Padding = new Padding(10, 10, 10, 5),
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        BackColor = Color.FromArgb(240, 240, 240),
        TextAlign = ContentAlignment.MiddleLeft
      };

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

      var removeMenuItem = new ToolStripMenuItem("Remove");
      removeMenuItem.Click += ReleaseContextMenu_Remove_Click;
      releaseContextMenu.Items.Add(removeMenuItem);

      // Add Opening event handler to enable/disable menu items based on selection
      releaseContextMenu.Opening += ReleaseContextMenu_Opening;

      releaseDataGrid.ContextMenuStrip = releaseContextMenu;

      rightPanel.Controls.Add(releaseDataGrid);
      rightPanel.Controls.Add(releaseLabel);
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
        ColumnCount = 5,
        RowCount = 1,
        Padding = new Padding(10, 5, 10, 10),
        Height = 50,
      };

      panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // spacer
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Refresh
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Move to Release
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Open Folder
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Close

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

      var btnOpenFolder = new Button
      {
        Text = "Open Folder",
        AutoSize = true,
        BackColor = Color.FromArgb(255, 193, 7),
        ForeColor = Color.Black,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
        UseVisualStyleBackColor = false,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(4, 0, 4, 0),
        Width = 110
      };
      btnOpenFolder.FlatAppearance.BorderSize = 1;
      btnOpenFolder.Click += BtnOpenFolder_Click;

      var btnClose = new Button
      {
        Text = "Close",
        AutoSize = true,
        BackColor = Color.FromArgb(173, 179, 184),
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

      panel.Controls.Add(btnRefresh, 1, 0);
      panel.Controls.Add(btnMoveToRelease, 2, 0);
      panel.Controls.Add(btnOpenFolder, 3, 0);
      panel.Controls.Add(btnClose, 4, 0);

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
          mainSplitContainer.SplitterDistance = (int)(this.ClientSize.Width * 0.60);
        }

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

    private void SawQueueDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
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
        Program.Log($"SawQueueDialog: File created in staging directory detected by watcher: {fileName}");

        // Wait a moment for file to be fully written
        System.Threading.Thread.Sleep(500);

        // Refresh staging list on UI thread
        if (InvokeRequired)
        {
          Invoke(new Action(RefreshStagingList));
        }
        else
        {
          RefreshStagingList();
        }
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
          Program.Log($"SawQueueDialog: File changed in staging directory: {fileName}");

          // Refresh staging list on UI thread
          if (InvokeRequired)
          {
            Invoke(new Action(RefreshStagingList));
          }
          else
          {
            RefreshStagingList();
          }
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error handling file change event in staging directory", ex);
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
          var stagingFiles = GetFilesWithMetadata(stagingDir);

          // Create display items for DataGridView
          var displayItems = stagingFiles.Select(f => new
          {
            JobName = f.JobName,
            FileSize = FormatFileSize(f.FileSize),
            LastModified = f.LastModified.ToString("yyyy-MM-dd HH:mm")
          }).ToList();

          stagingDataGrid.DataSource = displayItems;

          // Store FileDisplayItem references in row Tag for easy access
          for (int i = 0; i < stagingDataGrid.Rows.Count && i < stagingFiles.Count; i++)
          {
            stagingDataGrid.Rows[i].Tag = stagingFiles[i];
          }

          stagingStatusLabel.Text = $"{stagingFiles.Count} file(s) in staging";
          stagingStatusLabel.ForeColor = stagingFiles.Count > 0 ? Color.DarkGreen : Color.Gray;
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
        var trackedFiles = releaseTracker.GetTrackedFiles();

        // Create a list of display objects for the DataGridView
        var displayItems = trackedFiles.Select(f => new
        {
          JobName = f.JobName,
          Status = f.Status == "pending" ? "Pending" : "Sent to Saw",
          ReleaseDate = f.SentToRelease.ToString("yyyy-MM-dd HH:mm"),
          SentToSawDate = f.SentToSaw.HasValue ? f.SentToSaw.Value.ToString("yyyy-MM-dd HH:mm") : ""
        }).ToList();

        releaseDataGrid.DataSource = displayItems;

        // Store TrackedReleaseFile references in row Tag for easy access
        for (int i = 0; i < releaseDataGrid.Rows.Count && i < trackedFiles.Count; i++)
        {
          releaseDataGrid.Rows[i].Tag = trackedFiles[i];
        }

        var pendingCount = trackedFiles.Count(f => f.Status == "pending");
        var sentCount = trackedFiles.Count(f => f.Status == "sent to saw");
        releaseStatusLabel.Text = $"{trackedFiles.Count} tracked file(s) - {pendingCount} pending, {sentCount} sent to saw";
        releaseStatusLabel.ForeColor = trackedFiles.Count > 0 ? Color.DarkGreen : Color.Gray;
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

        // Load staging files
        if (Directory.Exists(stagingDir))
        {
          var stagingFiles = GetFilesWithMetadata(stagingDir);

          // Create display items for DataGridView
          var displayItems = stagingFiles.Select(f => new
          {
            JobName = f.JobName,
            FileSize = FormatFileSize(f.FileSize),
            LastModified = f.LastModified.ToString("yyyy-MM-dd HH:mm")
          }).ToList();

          stagingDataGrid.DataSource = displayItems;

          // Store FileDisplayItem references in row Tag for easy access
          for (int i = 0; i < stagingDataGrid.Rows.Count && i < stagingFiles.Count; i++)
          {
            stagingDataGrid.Rows[i].Tag = stagingFiles[i];
          }

          stagingStatusLabel.Text = $"{stagingFiles.Count} file(s) in staging";
          stagingStatusLabel.ForeColor = stagingFiles.Count > 0 ? Color.DarkGreen : Color.Gray;
        }
        else
        {
          stagingDataGrid.DataSource = null;
          stagingStatusLabel.Text = "Staging directory not found";
          stagingStatusLabel.ForeColor = Color.DarkRed;
          Program.Log($"SawQueueDialog: Staging directory does not exist: {stagingDir}");
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

    private void BtnRefresh_Click(object sender, EventArgs e)
    {
      try
      {
        Program.Log("SawQueueDialog: Refresh button clicked");
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
        var result = MessageBox.Show(
          $"Are you sure you want to move {stagingDataGrid.SelectedRows.Count} file(s) from staging to release?",
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
        Program.Log("SawQueueDialog: Error in move operation", ex);
        MessageBox.Show($"Error moving files: {ex.Message}", "Move Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void BtnOpenFolder_Click(object sender, EventArgs e)
    {
      try
      {
        // Determine which folder to open based on which grid has focus
        string folderToOpen = stagingDir;

        if (releaseDataGrid.Focused || releaseDataGrid.SelectedRows.Count > 0)
        {
          folderToOpen = releaseDir;
        }

        if (Directory.Exists(folderToOpen))
        {
          System.Diagnostics.Process.Start("explorer.exe", folderToOpen);
          Program.Log($"SawQueueDialog: Opened folder: {folderToOpen}");
        }
        else
        {
          MessageBox.Show($"Directory does not exist:\n{folderToOpen}", "Directory Not Found",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error opening folder", ex);
        MessageBox.Show($"Error opening folder: {ex.Message}", "Open Folder Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
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
          return; // User cancelled or didn't change
        }

        // Update the job name in the PTX file
        UpdateJobNameInPtx(fileItem.FilePath, newJobName);

        // Refresh the staging list to show updated job name
        LoadFiles();

        MessageBox.Show("Job name updated successfully.", "Success",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    // Update job name in PTX file
    private void UpdateJobNameInPtx(string filePath, string newJobName)
    {
      try
      {
        // Preserve the original file timestamps before modifying
        var fileInfo = new FileInfo(filePath);
        var originalLastWriteTime = fileInfo.LastWriteTime;
        var originalLastAccessTime = fileInfo.LastAccessTime;
        var originalCreationTime = fileInfo.CreationTime;

        // Read all lines from the file
        var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
        bool found = false;

        // Find and update the JOBS line
        for (int i = 0; i < lines.Count; i++)
        {
          if (lines[i].StartsWith("JOBS,", StringComparison.OrdinalIgnoreCase))
          {
            var fields = lines[i].Split(',');
            if (fields.Length >= 3)
            {
              // Update the third field (index 2) which contains the job name
              fields[2] = newJobName;
              lines[i] = string.Join(",", fields);
              found = true;
              break;
            }
          }
        }

        if (!found)
        {
          throw new InvalidOperationException("JOBS line not found in PTX file");
        }

        // Write the updated lines back to the file
        File.WriteAllLines(filePath, lines, Encoding.UTF8);

        // Restore the original file timestamps
        fileInfo.Refresh();
        fileInfo.LastWriteTime = originalLastWriteTime;
        fileInfo.LastAccessTime = originalLastAccessTime;
        fileInfo.CreationTime = originalCreationTime;

        Program.Log($"SawQueueDialog: Updated job name in {filePath} to '{newJobName}' (preserved timestamps)");
      }
      catch (Exception ex)
      {
        Program.Log($"SawQueueDialog: Error updating job name in {filePath}", ex);
        throw;
      }
    }

    // Context menu Opening event handler - Enable/disable menu items based on selection
    private void ReleaseContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      try
      {
        var contextMenu = sender as ContextMenuStrip;
        if (contextMenu == null) return;

        // Find the "Move back to Staging" menu item
        var moveToStagingMenuItem = contextMenu.Items.OfType<ToolStripMenuItem>()
          .FirstOrDefault(item => item.Text == "Move back to Staging");

        if (moveToStagingMenuItem == null) return;

        // Check if any selected rows have "Sent to Saw" status
        bool hasSentToSawStatus = false;
        if (releaseDataGrid.SelectedRows.Count > 0)
        {
          foreach (DataGridViewRow row in releaseDataGrid.SelectedRows)
          {
            if (row.Tag is TrackedReleaseFile trackedFile)
            {
              // Status is stored as "sent to saw" (lowercase) in the object
              if (string.Equals(trackedFile.Status, "sent to saw", StringComparison.OrdinalIgnoreCase))
              {
                hasSentToSawStatus = true;
                break;
              }
            }
          }
        }

        // Disable "Move back to Staging" if any selected file has "Sent to Saw" status
        moveToStagingMenuItem.Enabled = !hasSentToSawStatus && releaseDataGrid.SelectedRows.Count > 0;
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

    // Context menu handler for release grid - Remove
    private void ReleaseContextMenu_Remove_Click(object sender, EventArgs e)
    {
      try
      {
        // Check if any rows are selected
        if (releaseDataGrid.SelectedRows.Count == 0)
        {
          MessageBox.Show("Please select one or more files to remove.",
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

        // Confirm the removal
        var result = MessageBox.Show(
          $"Are you sure you want to remove {selectedFiles.Count} file(s) from the tracking list?\n\nThis will remove them from the CSV file permanently.",
          "Confirm Removal",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
          return;

        // Remove files from tracker
        foreach (var trackedFile in selectedFiles)
        {
          releaseTracker.RemoveFile(trackedFile.FileName);
        }

        // Refresh the release list
        RefreshReleaseList();

        MessageBox.Show($"Removed {selectedFiles.Count} file(s) from tracking.", "Removal Complete",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Program.Log("SawQueueDialog: Error removing files from tracking", ex);
        MessageBox.Show($"Error removing files: {ex.Message}", "Remove Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

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

    // Class to track release file history with CSV persistence
    private sealed class ReleaseFileTracker
    {
      private readonly List<TrackedReleaseFile> trackedFiles;
      private readonly string csvFilePath;
      private const int MaxTrackedFiles = 100;

      public ReleaseFileTracker()
      {
        // Store CSV file in the same directory as settings.xml
        var configDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(configDir))
          configDir = Environment.CurrentDirectory;
        csvFilePath = Path.Combine(configDir, "release_history.csv");

        trackedFiles = new List<TrackedReleaseFile>();
        LoadFromCsv();
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
          var lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);

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

      // Save tracked files to CSV
      private void SaveToCsv()
      {
        try
        {
          using (var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
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
        }
        catch (Exception ex)
        {
          Program.Log("ReleaseFileTracker: Error saving to CSV", ex);
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

        // Keep only the last MaxTrackedFiles
        if (trackedFiles.Count > MaxTrackedFiles)
        {
          var toRemove = trackedFiles.OrderBy(f => f.SentToRelease).Take(trackedFiles.Count - MaxTrackedFiles).ToList();
          foreach (var removeFile in toRemove)
          {
            trackedFiles.Remove(removeFile);
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

      // Remove a file from tracking by filename
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
  }
}
