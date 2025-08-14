# WorkOrderBlender Windows Installer

This document explains how to create a Windows installer (.msi file) for WorkOrderBlender using WiX Toolset v6.0.

## Prerequisites

### 1. Install WiX Toolset v6.0
Download and install WiX Toolset v6.0 from: https://wixtoolset.org/releases/

**Required version:** WiX v6.0 or later

**Installation steps:**
1. Download WiX v6.0 for your system
2. Run the installer as Administrator
3. Restart your command prompt/PowerShell after installation
4. Verify installation by running `wix --version`

### 2. .NET Framework 4.8
Ensure the target machine has .NET Framework 4.8 installed. This is typically pre-installed on Windows 10/11.

## Building the Installer

### Option 1: Using PowerShell Script (Recommended)
```powershell
# Basic build
.\build-installer.ps1

# Clean build (removes previous installer)
.\build-installer.ps1 -Clean

# Verbose output
.\build-installer.ps1 -Verbose
```

### Option 2: Using Batch File
```cmd
build-installer.bat
```

### Option 3: Manual WiX v6.0 Command
```cmd
# Build installer directly
wix build WorkOrderBlender.Installer.wxs -out installer\WorkOrderBlender.msi
```

## Installer Features

The generated installer includes:

- **Main Application**: WorkOrderBlender.exe with all dependencies
- **Start Menu Shortcut**: Accessible from Programs menu
- **Desktop Shortcut**: Quick access from desktop
- **Automatic Launch**: Option to launch app after installation
- **Proper Uninstallation**: Clean removal via Control Panel
- **Registry Integration**: Proper Windows integration

## Distribution

### File Structure
After building, you'll have:
```
installer/
├── WorkOrderBlender.msi          # Main installer
└── WorkOrderBlender.wixobj       # Intermediate file (can be deleted)
```

### Distribution Methods
1. **Direct Distribution**: Share the .msi file directly
2. **Network Deployment**: Use Group Policy or SCCM
3. **Web Download**: Host on your website
4. **USB/Media**: Include on installation media

## Installation Instructions for End Users

1. **Double-click** the `WorkOrderBlender.msi` file
2. **Follow** the installation wizard
3. **Choose** installation location (default: `C:\Program Files\WorkOrderBlender\`)
4. **Complete** the installation
5. **Launch** the application from Start Menu or Desktop

## Customization

### Modifying the Installer

Edit `WorkOrderBlender.Installer.wxs` to customize:

- **Product Information**: Name, version, manufacturer
- **Installation Path**: Default installation directory
- **Shortcuts**: Start Menu and Desktop shortcuts
- **Dependencies**: Additional files or prerequisites
- **UI**: Installation wizard appearance

### Common Customizations

#### Change Product Version
```xml
<Product Id="*"
         Name="Work Order Blender"
         Language="1033"
         Version="1.0.2.0"  <!-- Update this -->
         Manufacturer="WorkOrderBlender"
         UpgradeCode="PUT-GUID-HERE">
```

#### Add Additional Files
```xml
<Component Id="AdditionalFiles" Guid="*">
  <File Id="README.txt"
        Name="README.txt"
        Source="docs\README.txt" />
</Component>
```

#### Change Installation Directory
```xml
<Directory Id="ProgramFilesFolder">
  <Directory Id="INSTALLFOLDER" Name="MyCompany\WorkOrderBlender">  <!-- Custom path -->
```

## Troubleshooting

### Common Issues

#### "WiX Toolset not found"
- Ensure WiX is properly installed
- Restart command prompt after installation
- Check PATH environment variable

#### "Project build failed"
- Ensure .NET SDK is installed
- Run `dotnet restore` first
- Check for compilation errors

#### "WiX compilation failed"
- Verify .wxs file syntax
- Check file paths in Source attributes
- Ensure all referenced files exist

#### "WiX linking failed"
- Check for missing components
- Verify GUIDs are properly formatted
- Check for circular dependencies

### Debugging

Enable verbose output:
```powershell
.\build-installer.ps1 -Verbose
```

Check WiX logs:
```cmd
candle -v WorkOrderBlender.Installer.wxs
light -v installer\WorkOrderBlender.wixobj
```

## Advanced Features

### Prerequisites
Add .NET Framework check:
```xml
<Property Id="WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT" Value="Launch Work Order Blender" />
<Property Id="WIXUI_EXITDIALOGOPTIONALCHECKBOX" Value="1" />
```

### Custom Actions
Execute custom code during installation:
```xml
<CustomAction Id="CustomAction"
              FileKey="WorkOrderBlender.exe"
              ExeCommand="--post-install"
              Return="check" />
```

### Upgrade Handling
Support for application updates:
```xml
<MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
<Upgrade Id="PUT-GUID-HERE">
  <UpgradeVersion Property="PREVIOUSFOUND"
                  Minimum="1.0.0.0"
                  IncludeMinimum="yes"
                  Maximum="1.0.1.0"
                  IncludeMaximum="no" />
</Upgrade>
```

## Support

For issues with the installer:
1. Check this README for common solutions
2. Review WiX documentation: https://wixtoolset.org/documentation/
3. Check WiX community forums
4. Verify all prerequisites are installed

## License

This installer configuration is provided as-is for use with WorkOrderBlender.
