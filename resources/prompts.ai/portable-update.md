## AI Prompt: Create a Portable Auto-Update System for Windows Applications

Create a comprehensive portable auto-update system for Windows applications that integrates with GitHub releases and deploy keys. The system should work without requiring administrator privileges and provide a seamless update experience for portable applications.

### Core Requirements

**1. Update Management System**
- Create a `PortableUpdateManager` class that handles all update operations
- Implement automatic update checks on application startup (once per day)
- Support manual update checks through application menu
- Handle version comparison and update availability detection
- Implement user preferences for skipped versions and update frequency

**2. GitHub Integration**
- Use GitHub API to check for available releases
- Support deploy key authentication for secure repository access
- Download `update.xml` manifest files for version information
- Handle GitHub rate limiting and API quotas gracefully
- Support both public and private repositories

**3. Deploy Key Management**
- Create a `DeployKeyManager` class for SSH key operations
- Extract deploy keys from embedded resources or user SSH directory
- Set proper file permissions for security
- Validate key integrity using checksums
- Create portable SSH configuration files

**4. Update Process**
- Download release ZIP files to temporary locations
- Create automatic backups before applying updates
- Extract new files to application directory
- Handle file replacement and cleanup
- Implement automatic application restart after updates

**5. User Interface**
- Create a `PortableUpdateDialog` form for update notifications
- Display current vs. available version information
- Show download progress with progress bars
- Provide options: Update Now, Remind Later, Skip Version, View Changelog
- Handle update cancellation gracefully

**6. Security Features**
- Validate update sources (only GitHub releases)
- Prevent downgrade attacks
- Create secure backup mechanisms
- Implement checksum verification for downloaded files
- Handle file permissions securely

### Technical Specifications

**Framework & Dependencies**
- Target .NET Framework 4.8
- Use native Windows Forms components
- Implement async/await patterns for network operations
- Use System.IO.Compression for ZIP handling
- Implement proper error handling and logging

**File Structure**
```
UpdateSystem/
├── PortableUpdateManager.cs      # Main update logic
├── DeployKeyManager.cs           # SSH key management
├── PortableUpdateDialog.cs       # Update UI
├── UpdateInfo.cs                 # Update metadata
├── UpdateProgress.cs             # Progress tracking
└── resources/
    ├── deploy_key                # Embedded SSH key
    ├── ssh_config               # SSH configuration
    └── update.xml               # Update manifest
```

**Key Methods to Implement**
- `CheckForUpdatesAsync()` - Check for available updates
- `DownloadUpdateAsync()` - Download update files
- `InstallUpdateAsync()` - Install downloaded updates
- `InitializeDeployKey()` - Setup SSH authentication
- `CreateBackup()` - Backup current installation
- `ValidateUpdate()` - Verify update integrity

**Configuration Management**
- Store user preferences in XML settings file
- Track last update check timestamps
- Remember skipped version preferences
- Configure update check frequency
- Store GitHub repository information

**Error Handling**
- Network connectivity issues
- GitHub API failures
- Insufficient disk space
- File permission errors
- Update validation failures
- Rollback mechanisms for failed updates

### Integration Requirements

**Application Integration**
- Hook into application startup for automatic checks
- Provide menu integration for manual update checks
- Handle application lifecycle during updates
- Support both portable and installed modes

**GitHub Repository Setup**
- Require tagged releases (e.g., v1.2.1, v1.3.0)
- Generate `update.xml` manifest files
- Create release ZIP assets
- Configure deploy key access

**Portable Mode Support**
- Work without administrator privileges
- Handle file extraction and replacement
- Manage application restarts
- Create portable-friendly backup systems

### Advanced Features

**Update Channels**
- Support multiple update channels (stable, beta, dev)
- Allow users to choose update preferences
- Handle channel-specific versioning

**Rollback Capabilities**
- Automatic backup creation
- Manual rollback options
- Version history tracking
- Safe update verification

**Progress Tracking**
- Download progress with percentage
- Installation progress indicators
- Status message updates
- Cancellation support

**Logging & Diagnostics**
- Comprehensive logging system
- Error reporting mechanisms
- Debug information collection
- User-friendly error messages

### Example Usage

```csharp
// Check for updates on startup
var updateInfo = await PortableUpdateManager.CheckForUpdatesAsync();
if (updateInfo != null && updateInfo.IsUpdateAvailable)
{
    var dialog = new PortableUpdateDialog(updateInfo);
    dialog.ShowDialog();
}

// Manual update check
private async void CheckForUpdatesMenuItem_Click(object sender, EventArgs e)
{
    var updateInfo = await PortableUpdateManager.CheckForUpdatesAsync();
    if (updateInfo != null)
    {
        var dialog = new PortableUpdateDialog(updateInfo);
        dialog.ShowDialog();
    }
}
```

### Security Considerations

- Validate all downloaded files
- Use secure authentication methods
- Implement proper file permissions
- Prevent code injection attacks
- Secure backup and rollback procedures

### Testing Requirements

- Test with various network conditions
- Verify GitHub API integration
- Test update rollback scenarios
- Validate security measures
- Test with different Windows versions
- Verify portable mode functionality

This system should provide a professional, secure, and user-friendly update experience that integrates seamlessly with GitHub's release system while maintaining the portability and security requirements of modern Windows applications.
