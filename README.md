# WorkOrderBlender

A portable Windows Forms application for consolidating work order SDF files with pending changes preview.

## Build Instructions

### Portable Build Only
This project now focuses exclusively on portable builds. No installer builds are supported.

```powershell
# Build the portable package
.\build.ps1

# Clean build (removes previous output)
.\build.ps1 -Clean

# Skip build, just package existing build
.\build.ps1 -SkipBuild

# Verbose output
.\build.ps1 -Verbose
```

### Output
- **Portable folder**: `dist/WorkOrderBlender-{version}-portable/`
- **ZIP archive**: `dist/WorkOrderBlender-{version}-portable.zip`

### Usage
1. Unzip the portable package anywhere
2. Ensure `lib\amd64\` native DLLs remain alongside the EXE
3. Run `WorkOrderBlender.exe`

## Features
- Consolidate multiple SDF files
- Preview pending changes
- Customizable column layouts
- Virtual columns support
- Portable deployment (no installation required)

## Requirements
- .NET Framework 4.8
- Windows x64
- SQL Server Compact Edition runtime (included)
