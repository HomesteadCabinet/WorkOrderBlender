using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace WorkOrderBlender
{
    /// <summary>
    /// Manages updates for portable installations using GitHub API with deploy key authentication
    /// </summary>
    internal static class PortableUpdateManager
    {
        private const string GITHUB_API_BASE = "https://api.github.com";
        private const string UPDATE_XML_URL = "https://raw.githubusercontent.com/HomesteadCabinet/WorkOrderBlender/main/update.xml";
        private const string USER_AGENT = "WorkOrderBlender-Portable-Updater/1.0";
        private static readonly HttpClient httpClient = new HttpClient();
        private static bool isDeployKeyAvailable = false;

        static PortableUpdateManager()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            // Initialize deploy key for portable mode
            isDeployKeyAvailable = DeployKeyManager.InitializeDeployKey();
        }





        /// <summary>
        /// Checks for updates using the deploy key if available, falls back to HTTP if not
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                Program.Log("PortableUpdateManager: Checking for updates...");

                // Try deploy key method first if available
                if (isDeployKeyAvailable)
                {
                    var updateInfo = await CheckForUpdatesWithDeployKeyAsync();
                    if (updateInfo != null)
                    {
                        return updateInfo;
                    }
                    Program.Log("PortableUpdateManager: Deploy key method failed, falling back to HTTP");
                }

                // Fall back to HTTP method
                return await CheckForUpdatesWithHttpAsync();
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error checking for updates", ex);
                return null;
            }
        }

        /// <summary>
        /// Checks for updates using the deploy key and Git operations
        /// </summary>
        private static async Task<UpdateInfo> CheckForUpdatesWithDeployKeyAsync()
        {
            try
            {
                // Check if Git is available
                if (!IsGitAvailable())
                {
                    Program.Log("PortableUpdateManager: Git is not available, cannot use deploy key method");
                    return null;
                }

                // Use Git to fetch the latest update.xml from the repository
                var tempDir = Path.Combine(Path.GetTempPath(), "WOB_Update_Check_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Set up environment for Git operations
                    var deployKeyPath = DeployKeyManager.GetDeployKeyPath();
                    var sshConfigPath = DeployKeyManager.GetSshConfigPath();

                    // Clone the repository to get the latest update.xml
                    var cloneResult = await RunGitCommandAsync(tempDir, "clone", "--depth", "1",
                        "git@github.com-workorderblender:HomesteadCabinet/WorkOrderBlender.git", ".");

                    if (cloneResult.ExitCode != 0)
                    {
                        Program.Log($"PortableUpdateManager: Git clone failed: {cloneResult.Output}");
                        if (!string.IsNullOrEmpty(cloneResult.Error))
                        {
                            Program.Log($"PortableUpdateManager: Git error: {cloneResult.Error}");
                        }
                        return null;
                    }

                    // Read the update.xml file
                    var updateXmlPath = Path.Combine(tempDir, "update.xml");
                    if (!File.Exists(updateXmlPath))
                    {
                        Program.Log("PortableUpdateManager: update.xml not found in repository");
                        return null;
                    }

                    var updateXml = XDocument.Load(updateXmlPath);
                    return ParseUpdateInfo(updateXml.Root);
                }
                finally
                {
                    // Clean up temp directory
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error checking for updates with deploy key", ex);
                return null;
            }
        }

        /// <summary>
        /// Checks for updates using HTTP (fallback method)
        /// </summary>
        private static async Task<UpdateInfo> CheckForUpdatesWithHttpAsync()
        {
            try
            {
                var updateXml = await DownloadUpdateXmlAsync();
                if (updateXml == null) return null;

                return ParseUpdateInfo(updateXml);
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error checking for updates with HTTP", ex);
                return null;
            }
        }

        /// <summary>
        /// Parses update information from XML
        /// </summary>
        private static UpdateInfo ParseUpdateInfo(XElement updateXml)
        {
            try
            {
                var currentVersion = GetCurrentVersion();
                var availableVersion = ParseVersion(updateXml.Element("version")?.Value);

                if (availableVersion > currentVersion)
                {
                    return new UpdateInfo
                    {
                        IsUpdateAvailable = true,
                        CurrentVersion = currentVersion.ToString(),
                        AvailableVersion = availableVersion.ToString(),
                        DownloadUrl = updateXml.Element("url")?.Value,
                        ChangelogUrl = updateXml.Element("changelog")?.Value,
                        IsMandatory = bool.Parse(updateXml.Element("mandatory")?.Value ?? "false")
                    };
                }

                Program.Log($"PortableUpdateManager: No updates available. Current: {currentVersion}, Latest: {availableVersion}");
                return new UpdateInfo { IsUpdateAvailable = false };
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error parsing update info", ex);
                return null;
            }
        }

        /// <summary>
        /// Runs a Git command with the specified environment variables
        /// </summary>
        private static Task<GitCommandResult> RunGitCommandAsync(string workingDir, params string[] args)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = string.Join(" ", args),
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Add environment variables
                var deployKeyPath = DeployKeyManager.GetDeployKeyPath();
                var sshConfigPath = DeployKeyManager.GetSshConfigPath();
                foreach (var envVar in new Dictionary<string, string>
                {
                    ["GIT_SSH_COMMAND"] = $"ssh -i \"{deployKeyPath}\" -F \"{sshConfigPath}\" -o StrictHostKeyChecking=no",
                    ["GIT_SSH_VARIANT"] = "ssh"
                })
                {
                    startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }

                using (var process = new Process { StartInfo = startInfo })
                {
                    var output = new StringBuilder();
                    var error = new StringBuilder();

                    process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    return Task.FromResult(new GitCommandResult
                    {
                        ExitCode = process.ExitCode,
                        Output = output.ToString().Trim(),
                        Error = error.ToString().Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error running Git command", ex);
                return Task.FromResult(new GitCommandResult { ExitCode = -1, Error = ex.Message });
            }
        }

        /// <summary>
        /// Downloads and parses the update.xml file (HTTP fallback method)
        /// </summary>
        private static async Task<XElement> DownloadUpdateXmlAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(UPDATE_XML_URL);
                var doc = XDocument.Parse(response);
                return doc.Root;
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error downloading update.xml", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the current application version
        /// </summary>
        private static Version GetCurrentVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            }
            catch
            {
                return new Version(1, 0, 0, 0);
            }
        }

        /// <summary>
        /// Parses a version string into a Version object
        /// </summary>
        private static Version ParseVersion(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString)) return new Version(0, 0, 0, 0);

            try
            {
                // Remove 'v' prefix if present
                versionString = versionString.TrimStart('v');
                return Version.Parse(versionString);
            }
            catch
            {
                return new Version(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Downloads and installs the update
        /// </summary>
        public static async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<int> progress = null)
        {
            try
            {
                if (updateInfo?.DownloadUrl == null)
                {
                    throw new ArgumentException("Invalid update information");
                }

                Program.Log($"PortableUpdateManager: Starting update download from {updateInfo.DownloadUrl}");

                // Create temp directory for download
                var tempDir = Path.Combine(Path.GetTempPath(), "WOB_Update_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Download the update
                    var zipPath = Path.Combine(tempDir, "update.zip");
                    await DownloadFileAsync(updateInfo.DownloadUrl, zipPath, progress);

                    // Extract to temp location
                    var extractPath = Path.Combine(tempDir, "extracted");
                    Directory.CreateDirectory(extractPath);
                    ZipFile.ExtractToDirectory(zipPath, extractPath);

                    // Create update script
                    var updateScript = CreateUpdateScript(tempDir, extractPath);
                    var scriptPath = Path.Combine(tempDir, "update.bat");
                    File.WriteAllText(scriptPath, updateScript);

                    // Launch update script and exit
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        UseShellExecute = true,
                        Verb = "runas" // Run as admin if needed
                    });

                    return true;
                }
                finally
                {
                    // Clean up temp files (update script will handle the rest)
                    try
                    {
                        if (File.Exists(Path.Combine(tempDir, "update.zip")))
                            File.Delete(Path.Combine(tempDir, "update.zip"));
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error during update installation", ex);
                return false;
            }
        }

        /// <summary>
        /// Downloads a file with progress reporting
        /// </summary>
        private static async Task DownloadFileAsync(string url, string filePath, IProgress<int> progress)
        {
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(filePath))
                {
                    var buffer = new byte[8192];
                    var totalBytesRead = 0L;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        if (progress != null && totalBytes > 0)
                        {
                            var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                            progress.Report(progressPercentage);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a batch script to perform the update
        /// </summary>
        private static string CreateUpdateScript(string tempDir, string extractPath)
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var exeName = "WorkOrderBlender.exe";

            return $@"@echo off
echo Updating WorkOrderBlender...
echo.

REM Wait for application to close
timeout /t 2 /nobreak >nul

REM Backup current installation
echo Creating backup...
if not exist ""{currentDir}\backup"" mkdir ""{currentDir}\backup""
xcopy ""{currentDir}\*"" ""{currentDir}\backup\"" /E /I /Y >nul 2>&1

REM Copy new files
echo Installing update...
xcopy ""{extractPath}\*"" ""{currentDir}"" /E /I /Y >nul 2>&1

REM Clean up
echo Cleaning up...
rmdir /s /q ""{tempDir}""

echo Update complete!
echo Starting WorkOrderBlender...
start """" ""{currentDir}\{exeName}""
exit
";
        }

        /// <summary>
        /// Checks if Git is available on the system
        /// </summary>
        private static bool IsGitAvailable()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    return process.ExitCode == 0 && output.Contains("git version");
                }
            }
            catch (Exception ex)
            {
                Program.Log("PortableUpdateManager: Error checking Git availability", ex);
                return false;
            }
        }

        /// <summary>
        /// Disposes the HTTP client
        /// </summary>
        public static void Dispose()
        {
            httpClient?.Dispose();
        }

        /// <summary>
        /// Cleans up portable deploy key files
        /// </summary>
        public static void CleanupPortableFiles()
        {
            DeployKeyManager.CleanupPortableFiles();
        }
    }

    /// <summary>
    /// Information about an available update
    /// </summary>
    internal class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string AvailableVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ChangelogUrl { get; set; }
        public bool IsMandatory { get; set; }
    }

    /// <summary>
    /// Result of a Git command execution
    /// </summary>
    internal class GitCommandResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
    }
}
