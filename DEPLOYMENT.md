# WorkOrderBlender Deployment Guide

This guide covers how to set up automatic distribution and updates for WorkOrderBlender using GitHub.

## Prerequisites

- GitHub repository for your project
- .NET Framework 4.8 runtime on target machines
- SQL Server Compact Edition 4.0 on target machines

## Setup Steps

### 1. Update Repository URLs

In `Program.cs`, update the update URL to match your repository:

```csharp
string updateUrl = "https://raw.githubusercontent.com/HomesteadCabinet/WorkOrderBlender/main/update.xml";
```

Replace `YOURUSERNAME` and `YOURREPO` with your actual GitHub username and repository name.

### 2. Configure GitHub Repository

1. **Enable GitHub Actions** in your repository settings
2. **Create necessary secrets** (if needed for advanced scenarios)
3. **Ensure main branch** is the default branch

### 3. Version Management

Update version numbers in `WorkOrderBlender.csproj`:

```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

### 4. Creating Releases

#### Automatic Release (Recommended)

1. **Create and push a version tag:**
   ```bash
   git tag v1.0.1
   git push origin v1.0.1
   ```

2. **GitHub Actions will automatically:**
   - Build the application
   - Create a release
   - Upload the ZIP file
   - Update the `update.xml` file

#### Manual Release

1. Build the project in Release mode
2. Create a ZIP file with all necessary files
3. Create a GitHub release with the ZIP file
4. Manually update `update.xml` with the new version info

### 5. Distribution Methods

#### Method 1: Direct Download
- Users download from GitHub Releases
- Extract and run `WorkOrderBlender.exe`
- Auto-updater will handle future updates

#### Method 2: Network Deployment
- Extract to a network share
- Users run from network location
- Consider using UNC paths for the update URL

#### Method 3: Installer (Advanced)
- Create an MSI installer using WiX or similar
- Include auto-updater functionality
- Handle dependencies and shortcuts

## File Structure for Distribution

```
WorkOrderBlender/
├── WorkOrderBlender.exe
├── WorkOrderBlender.dll
├── AutoUpdater.NET.dll
├── System.Data.SqlServerCe.dll (if bundled)
├── update.xml (automatically updated)
└── CHANGELOG.md
```

## Update Process Flow

1. **Application starts** → Checks for updates silently
2. **Update available** → Shows update dialog to user
3. **User accepts** → Downloads and installs update
4. **Application restarts** → Runs new version

## Troubleshooting

### Common Issues

1. **Update check fails:**
   - Check internet connectivity
   - Verify update URL is correct
   - Check GitHub repository is public

2. **SQL CE dependency issues:**
   - Install SQL Server Compact Edition 4.0
   - Or bundle the required DLLs with the application

3. **Permission issues:**
   - Ensure users have write permissions to application directory
   - Consider using app data folder for updates

### Logs

Check `WorkOrderBlender.log` in the application directory for detailed error information.

## Security Considerations

- **Code signing:** Consider signing your executable for better security
- **HTTPS only:** Always use HTTPS URLs for update checks
- **Validation:** AutoUpdater.NET validates downloads automatically
- **User consent:** Updates require user approval by default

## Advanced Configuration

### Custom Update UI

You can customize the update dialog appearance:

```csharp
AutoUpdater.UpdateFormSize = new System.Drawing.Size(800, 600);
AutoUpdater.Icon = Properties.Resources.YourIcon;
```

### Mandatory Updates

For critical updates, set mandatory flag in `update.xml`:

```xml
<mandatory>true</mandatory>
```

### Update Frequency

Configure how often to check for updates:

```csharp
// Check every time app starts (default)
AutoUpdater.CheckForUpdateEvent += (args) => { /* handle */ };

// Or implement periodic checking
Timer updateTimer = new Timer(24 * 60 * 60 * 1000); // 24 hours
```

## Testing

1. **Test locally:** Set up a local HTTP server to serve update.xml
2. **Test with different versions:** Create test releases
3. **Test network scenarios:** Slow connections, offline mode
4. **Test permissions:** Different user account types

## Deployment Checklist

- [ ] Update repository URLs in code
- [ ] Update version numbers
- [ ] Test build process locally
- [ ] Create and test GitHub Actions workflow
- [ ] Test auto-update functionality
- [ ] Document installation requirements
- [ ] Prepare user documentation
- [ ] Test on clean systems
- [ ] Verify all dependencies are included
