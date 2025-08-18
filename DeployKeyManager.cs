using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

namespace WorkOrderBlender
{
    /// <summary>
    /// Manages deploy key operations for portable mode updates
    /// </summary>
    internal static class DeployKeyManager
    {
        private const string DEPLOY_KEY_FILENAME = "workorderblender_deploy_key";
        private const string SSH_CONFIG_FILENAME = "ssh_config";
        private const string DEPLOY_KEY_CHECKSUM_FILENAME = "workorderblender_deploy_key.sha256";
        private const string EMBEDDED_DEPLOY_KEY_RESOURCE = "WorkOrderBlender.resources.workorderblender_deploy_key";
        private const string EMBEDDED_SSH_CONFIG_RESOURCE = "WorkOrderBlender.resources.ssh_config";

        /// <summary>
        /// Initializes the deploy key for portable mode
        /// </summary>
        /// <returns>True if deploy key is available and ready for use</returns>
        public static bool InitializeDeployKey()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var deployKeyPath = Path.Combine(appDir, DEPLOY_KEY_FILENAME);
                var sshConfigPath = Path.Combine(appDir, SSH_CONFIG_FILENAME);
                var checksumPath = Path.Combine(appDir, DEPLOY_KEY_CHECKSUM_FILENAME);

                // Check if deploy key already exists and is valid
                if (File.Exists(deployKeyPath) && File.Exists(sshConfigPath))
                {
                    if (ValidateDeployKey(deployKeyPath, checksumPath))
                    {
                        Program.Log("DeployKeyManager: Deploy key already available in portable directory");
                        return true;
                    }
                    else
                    {
                        Program.Log("DeployKeyManager: Deploy key validation failed, re-copying");
                        CleanupPortableFiles(appDir);
                    }
                }

                // Try to copy deploy key from user's SSH directory first (for development)
                if (TryCopyFromUserSshDirectory(appDir, deployKeyPath, sshConfigPath, checksumPath))
                {
                    return true;
                }

                // Fall back to embedded resources
                if (TryExtractFromEmbeddedResources(appDir, deployKeyPath, sshConfigPath, checksumPath))
                {
                    return true;
                }

                Program.Log("DeployKeyManager: No deploy key available from any source");
                return false;
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error initializing deploy key", ex);
                return false;
            }
        }

        /// <summary>
        /// Tries to copy deploy key from user's SSH directory (for development/testing)
        /// </summary>
        private static bool TryCopyFromUserSshDirectory(string appDir, string deployKeyPath, string sshConfigPath, string checksumPath)
        {
            try
            {
                var userSshDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
                var sourceDeployKey = Path.Combine(userSshDir, DEPLOY_KEY_FILENAME);

                if (!File.Exists(sourceDeployKey))
                {
                    return false;
                }

                // Copy deploy key to portable directory
                File.Copy(sourceDeployKey, deployKeyPath);

                // Set restrictive permissions on the copied key
                SetRestrictiveFilePermissions(deployKeyPath);

                // Create SSH config for portable mode
                CreatePortableSshConfig(sshConfigPath);

                // Create checksum for validation
                CreateDeployKeyChecksum(deployKeyPath, checksumPath);

                Program.Log("DeployKeyManager: Deploy key copied from user SSH directory");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error copying from user SSH directory", ex);
                return false;
            }
        }

        /// <summary>
        /// Tries to extract deploy key from embedded resources
        /// </summary>
        private static bool TryExtractFromEmbeddedResources(string appDir, string deployKeyPath, string sshConfigPath, string checksumPath)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Try to extract deploy key
                if (!TryExtractResource(assembly, EMBEDDED_DEPLOY_KEY_RESOURCE, deployKeyPath))
                {
                    Program.Log("DeployKeyManager: Embedded deploy key resource not found");
                    return false;
                }

                // Try to extract SSH config
                if (!TryExtractResource(assembly, EMBEDDED_SSH_CONFIG_RESOURCE, sshConfigPath))
                {
                    // Create default SSH config if embedded one not found
                    CreatePortableSshConfig(sshConfigPath);
                }

                // Set restrictive permissions on the extracted key
                SetRestrictiveFilePermissions(deployKeyPath);

                // Create checksum for validation
                CreateDeployKeyChecksum(deployKeyPath, checksumPath);

                Program.Log("DeployKeyManager: Deploy key extracted from embedded resources");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error extracting from embedded resources", ex);
                return false;
            }
        }

        /// <summary>
        /// Tries to extract a resource from the assembly
        /// </summary>
        private static bool TryExtractResource(Assembly assembly, string resourceName, string targetPath)
        {
            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return false;
                    }

                    using (var fileStream = File.Create(targetPath))
                    {
                        stream.CopyTo(fileStream);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.Log($"DeployKeyManager: Error extracting resource {resourceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// Validates the deploy key using checksum
        /// </summary>
        private static bool ValidateDeployKey(string deployKeyPath, string checksumPath)
        {
            try
            {
                if (!File.Exists(checksumPath))
                    return false;

                var expectedChecksum = File.ReadAllText(checksumPath).Trim();
                var actualChecksum = CalculateFileChecksum(deployKeyPath);

                return string.Equals(expectedChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error validating deploy key", ex);
                return false;
            }
        }

        /// <summary>
        /// Calculates SHA256 checksum of a file
        /// </summary>
        private static string CalculateFileChecksum(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error calculating file checksum", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates a checksum file for the deploy key
        /// </summary>
        private static void CreateDeployKeyChecksum(string deployKeyPath, string checksumPath)
        {
            try
            {
                var checksum = CalculateFileChecksum(deployKeyPath);
                File.WriteAllText(checksumPath, checksum);
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error creating deploy key checksum", ex);
            }
        }

        /// <summary>
        /// Sets restrictive file permissions on the deploy key for security
        /// </summary>
        private static void SetRestrictiveFilePermissions(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.Attributes |= FileAttributes.ReadOnly;
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Could not set restrictive permissions on deploy key", ex);
            }
        }

        /// <summary>
        /// Creates a portable SSH config file for the deploy key
        /// </summary>
        private static void CreatePortableSshConfig(string sshConfigPath)
        {
            try
            {
                var sshConfig = $@"Host github.com-workorderblender
    HostName github.com
    User git
    IdentityFile {DEPLOY_KEY_FILENAME}
    IdentitiesOnly yes
    StrictHostKeyChecking no
    UserKnownHostsFile /dev/null
";
                File.WriteAllText(sshConfigPath, sshConfig);
                Program.Log("DeployKeyManager: Created portable SSH config");
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error creating portable SSH config", ex);
            }
        }

        /// <summary>
        /// Cleans up portable deploy key files
        /// </summary>
        public static void CleanupPortableFiles(string appDir = null)
        {
            try
            {
                if (appDir == null)
                    appDir = AppDomain.CurrentDomain.BaseDirectory;

                var filesToDelete = new[]
                {
                    Path.Combine(appDir, DEPLOY_KEY_FILENAME),
                    Path.Combine(appDir, SSH_CONFIG_FILENAME),
                    Path.Combine(appDir, DEPLOY_KEY_CHECKSUM_FILENAME)
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                    {
                        // Remove read-only attribute before deleting
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                        }
                        catch { }

                        File.Delete(file);
                    }
                }

                Program.Log("DeployKeyManager: Cleaned up portable deploy key files");
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error cleaning up portable files", ex);
            }
        }

        /// <summary>
        /// Gets the path to the deploy key in the portable directory
        /// </summary>
        public static string GetDeployKeyPath()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, DEPLOY_KEY_FILENAME);
        }

        /// <summary>
        /// Gets the path to the SSH config in the portable directory
        /// </summary>
        public static string GetSshConfigPath()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, SSH_CONFIG_FILENAME);
        }

        /// <summary>
        /// Checks if the deploy key is available and ready for use
        /// </summary>
        public static bool IsDeployKeyAvailable()
        {
            try
            {
                var deployKeyPath = GetDeployKeyPath();
                var sshConfigPath = GetSshConfigPath();
                var checksumPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEPLOY_KEY_CHECKSUM_FILENAME);

                return File.Exists(deployKeyPath) &&
                       File.Exists(sshConfigPath) &&
                       ValidateDeployKey(deployKeyPath, checksumPath);
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error checking deploy key availability", ex);
                return false;
            }
        }

        /// <summary>
        /// Lists all available embedded resources for debugging
        /// </summary>
        public static void ListEmbeddedResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resources = assembly.GetManifestResourceNames();

                Program.Log("DeployKeyManager: Available embedded resources:");
                foreach (var resource in resources)
                {
                    Program.Log($"  - {resource}");
                }
            }
            catch (Exception ex)
            {
                Program.Log("DeployKeyManager: Error listing embedded resources", ex);
            }
        }
    }
}
