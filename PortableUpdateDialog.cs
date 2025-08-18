using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkOrderBlender
{
    /// <summary>
    /// Dialog for handling portable updates with progress tracking
    /// </summary>
    internal partial class PortableUpdateDialog : Form
    {
        private readonly UpdateInfo updateInfo;
        private bool isUpdating = false;

        public PortableUpdateDialog(UpdateInfo updateInfo)
        {
            this.updateInfo = updateInfo ?? throw new ArgumentNullException(nameof(updateInfo));
            InitializeComponent();
            LoadUpdateInfo();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form setup
            this.Text = "WorkOrderBlender Update Available";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // Title label
            var lblTitle = new Label
            {
                Text = "A new version is available!",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 40
            };

            // Version info
            var lblVersion = new Label
            {
                Text = $"Current Version: {updateInfo.CurrentVersion}\nAvailable Version: {updateInfo.AvailableVersion}",
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50
            };

            // Changelog link
            var lnkChangelog = new LinkLabel
            {
                Text = "View Changelog",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 25
            };
            lnkChangelog.LinkClicked += (s, e) => OpenChangelog();

            // Progress bar
            var progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous,
                Dock = DockStyle.Top,
                Height = 25,
                Margin = new Padding(10, 10, 10, 5),
                Visible = false
            };

            // Status label
            var lblStatus = new Label
            {
                Text = "Click 'Update Now' to download and install the update.",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(10, 5, 10, 10)
            };

            // Buttons panel
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10),
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = false
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            var btnUpdate = new Button
            {
                Text = "Update Now",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Right
            };
            btnUpdate.Click += async (s, e) => await StartUpdate(progressBar, lblStatus, btnUpdate);

            var btnLater = new Button
            {
                Text = "Remind Me Later",
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 9),
                Anchor = AnchorStyles.None
            };
            btnLater.Click += (s, e) => this.Close();

            var btnSkip = new Button
            {
                Text = "Skip This Version",
                Size = new Size(140, 35),
                Font = new Font("Segoe UI", 9),
                Anchor = AnchorStyles.Left
            };
            btnSkip.Click += (s, e) => SkipVersion();

            buttonPanel.Controls.Add(btnSkip, 0, 0);
            buttonPanel.Controls.Add(btnLater, 1, 0);
            buttonPanel.Controls.Add(btnUpdate, 2, 0);

            // Add controls to form
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblVersion,
                lnkChangelog,
                progressBar,
                lblStatus,
                buttonPanel
            });

            this.ResumeLayout(false);
        }

        private void LoadUpdateInfo()
        {
            if (updateInfo.IsMandatory)
            {
                this.Text += " (Required)";
                // Disable skip button for mandatory updates
                foreach (Control c in this.Controls)
                {
                    if (c is Panel p)
                    {
                        foreach (Control btn in p.Controls)
                        {
                            if (btn.Text == "Skip This Version")
                            {
                                btn.Enabled = false;
                                btn.Text = "Update Required";
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OpenChangelog()
        {
            try
            {
                if (!string.IsNullOrEmpty(updateInfo.ChangelogUrl))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = updateInfo.ChangelogUrl,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Program.Log("Error opening changelog", ex);
                MessageBox.Show("Could not open changelog: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task StartUpdate(ProgressBar progressBar, Label statusLabel, Button updateButton)
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;
                updateButton.Enabled = false;
                progressBar.Visible = true;
                progressBar.Value = 0;

                var progress = new Progress<int>(value =>
                {
                    progressBar.Value = value;
                    statusLabel.Text = $"Downloading update... {value}%";
                    Application.DoEvents();
                });

                statusLabel.Text = "Starting update download...";
                Application.DoEvents();

                var success = await PortableUpdateManager.DownloadAndInstallUpdateAsync(updateInfo, progress);

                if (success)
                {
                    statusLabel.Text = "Update downloaded successfully. The application will restart.";
                    progressBar.Value = 100;

                    // Wait a moment for user to see the message
                    await Task.Delay(2000);

                    // Close the application to allow the update script to run
                    Application.Exit();
                }
                else
                {
                    statusLabel.Text = "Update failed. Please try again or download manually.";
                    updateButton.Enabled = true;
                    progressBar.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Program.Log("Error during update", ex);
                statusLabel.Text = "Update error: " + ex.Message;
                updateButton.Enabled = true;
                progressBar.Visible = false;
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void SkipVersion()
        {
            try
            {
                // Store skipped version in user config
                var config = UserConfig.LoadOrDefault();
                config.SetSkippedVersion(updateInfo.AvailableVersion);
                config.Save();

                MessageBox.Show($"Version {updateInfo.AvailableVersion} has been skipped. " +
                    "You can check for updates again later.", "Version Skipped",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                Program.Log("Error skipping version", ex);
                MessageBox.Show("Could not skip version: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
