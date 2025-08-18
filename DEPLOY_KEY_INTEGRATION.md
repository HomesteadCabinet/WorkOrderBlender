# Deploy Key Integration for Portable Mode Updates

This document explains how the read-only deploy key is integrated into WorkOrderBlender for portable mode updates.

## Overview

The deploy key integration allows WorkOrderBlender to securely check for updates in portable mode without requiring:
- SSH agent setup
- User SSH configuration
- Admin privileges
- Network configuration changes

## How It Works

### 1. Automatic Deploy Key Setup
When WorkOrderBlender starts in portable mode:
- Automatically detects if deploy key exists in user's SSH directory
- Copies the deploy key to the portable installation directory
- Creates a portable SSH configuration
- Sets restrictive file permissions for security
- Creates checksums for validation

### 2. Update Check Process
The update system uses a two-tier approach:
1. **Primary Method**: SSH-based Git operations using the deploy key
2. **Fallback Method**: HTTP-based API calls if SSH method fails

### 3. Security Features
- Deploy key is copied with read-only permissions
- SHA256 checksums validate file integrity
- Temporary files are cleaned up automatically
- No persistent SSH agent required

## File Structure

```
WorkOrderBlender/
├── WorkOrderBlender.exe
├── workorderblender_deploy_key          # Copied deploy key
├── ssh_config                           # Portable SSH config
├── workorderblender_deploy_key.sha256   # Key integrity check
└── ... (other application files)
```

## Requirements

### User Setup
1. **Deploy Key**: Must exist at `~/.ssh/workorderblender_deploy_key`
2. **Git**: Must be installed and accessible in PATH
3. **SSH**: Standard SSH client must be available

### Repository Setup
1. **Deploy Key**: Added to GitHub repository with read access
2. **update.xml**: Must exist in repository root
3. **Releases**: Tagged releases with ZIP assets

## Configuration

### SSH Configuration
The portable SSH config is automatically generated:
```ssh
Host github.com-workorderblender
    HostName github.com
    User git
    IdentityFile workorderblender_deploy_key
    IdentitiesOnly yes
    StrictHostKeyChecking no
    UserKnownHostsFile /dev/null
```

### Environment Variables
Git operations use these environment variables:
```bash
GIT_SSH_COMMAND=ssh -i "workorderblender_deploy_key" -F "ssh_config" -o StrictHostKeyChecking=no
GIT_SSH_VARIANT=ssh
```

## Update Process

### 1. Update Check
```csharp
// Check if deploy key is available
if (DeployKeyManager.IsDeployKeyAvailable())
{
    // Use SSH-based Git operations
    var updateInfo = await CheckForUpdatesWithDeployKeyAsync();
}
else
{
    // Fall back to HTTP method
    var updateInfo = await CheckForUpdatesWithHttpAsync();
}
```

### 2. Git Operations
- Clone repository with `--depth 1` for minimal download
- Read `update.xml` from cloned repository
- Parse version information
- Clean up temporary files

### 3. Fallback Handling
If SSH method fails:
- Log the failure
- Automatically switch to HTTP method
- Continue with update process
- User experience remains seamless

## Error Handling

### Common Issues
1. **Deploy Key Not Found**
   - Logs error and falls back to HTTP
   - No user intervention required

2. **Git Command Failure**
   - Logs detailed error information
   - Falls back to HTTP method
   - Continues update process

3. **Network Issues**
   - Both methods handle network failures gracefully
   - User-friendly error messages
   - Retry mechanisms available

### Logging
All operations are logged with detailed information:
```
DeployKeyManager: Deploy key successfully copied to portable directory
PortableUpdateManager: Checking for updates using deploy key
PortableUpdateManager: Git clone successful, reading update.xml
```

## Testing

### Manual Test
1. Start WorkOrderBlender in portable mode
2. Check logs for deploy key initialization
3. Use Help → Check for Updates
4. Verify SSH-based update check works

### Verification
- Deploy key files should exist in portable directory
- SSH config should be properly formatted
- Checksum file should validate key integrity
- Update checks should work without SSH agent

## Troubleshooting

### Deploy Key Issues
1. **Key Not Copied**
   - Check if key exists in `~/.ssh/`
   - Verify file permissions
   - Check application logs

2. **SSH Config Issues**
   - Verify `ssh_config` file format
   - Check file paths in config
   - Ensure proper line endings

3. **Git Command Issues**
   - Verify Git is installed and in PATH
   - Check SSH client availability
   - Review environment variables

### Performance
- First run: Deploy key setup may take a moment
- Subsequent runs: Uses cached deploy key
- Update checks: SSH method is typically faster than HTTP

## Security Considerations

### Key Protection
- Deploy key is copied with read-only permissions
- Checksums prevent tampering
- Temporary files are cleaned up
- No persistent SSH connections

### Access Control
- Deploy key has read-only access to repository
- No write permissions granted
- Limited to specific repository
- Can be revoked at any time

## Future Enhancements

### Planned Features
1. **Key Rotation**: Automatic deploy key updates
2. **Multiple Keys**: Support for different update sources
3. **Encryption**: Encrypted deploy key storage
4. **Remote Management**: Centralized key management

### Configuration Options
1. **Custom SSH Config**: User-defined SSH settings
2. **Update Sources**: Multiple repository support
3. **Authentication Methods**: Token-based alternatives
4. **Proxy Support**: Corporate network compatibility

## Support

For issues with the deploy key integration:
1. Check application logs for detailed error messages
2. Verify deploy key exists and has correct permissions
3. Ensure Git and SSH are properly installed
4. Test SSH connectivity manually if needed

## Conclusion

The deploy key integration provides a secure, reliable way to check for updates in portable mode without requiring complex SSH setup or user intervention. The system automatically handles key management and gracefully falls back to HTTP methods when needed, ensuring a smooth user experience.
