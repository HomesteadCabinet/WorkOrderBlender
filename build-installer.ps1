# WorkOrderBlender Installer Builder
# PowerShell script for building Windows installer using WiX v6.0

param(
    [switch]$Clean,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "Building WorkOrderBlender Installer..." -ForegroundColor Green
Write-Host ""

# Check if WiX v6.0 is installed
try {
    $wixCommand = Get-Command wix -ErrorAction Stop
    Write-Host "WiX Toolset v6.0 found:" -ForegroundColor Green
    Write-Host "  Wix: $($wixCommand.Source)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "ERROR: WiX Toolset v6.0 not found!" -ForegroundColor Red
    Write-Host "Please install WiX Toolset v6.0 from: https://wixtoolset.org/releases/" -ForegroundColor Yellow
    Write-Host "After installation, you may need to restart your PowerShell session." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Clean output directory if requested
if ($Clean) {
    if (Test-Path "installer") {
        Write-Host "Cleaning installer directory..." -ForegroundColor Yellow
        Remove-Item "installer" -Recurse -Force
    }
}

# Create output directory
if (-not (Test-Path "installer")) {
    New-Item -ItemType Directory -Path "installer" | Out-Null
}

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
try {
    $buildResult = dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "Project build failed with exit code $LASTEXITCODE"
    }
    Write-Host "Project build successful" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Project build failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Build WiX v6.0 installer
Write-Host "Building WiX v6.0 installer..." -ForegroundColor Yellow
try {
    $wixArgs = @(
        "build",
        "WorkOrderBlender.Installer.wxs",
        "-out", "installer\WorkOrderBlender.msi"
    )

    if ($Verbose) {
        $wixArgs += "-v"
    }

    & wix @wixArgs
    if ($LASTEXITCODE -ne 0) {
        throw "WiX v6.0 build failed with exit code $LASTEXITCODE"
    }
    Write-Host "WiX v6.0 build successful" -ForegroundColor Green
} catch {
    Write-Host "ERROR: WiX v6.0 build failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Get installer file info
$installerPath = "installer\WorkOrderBlender.msi"
if (Test-Path $installerPath) {
    $fileInfo = Get-Item $installerPath
    Write-Host ""
    Write-Host "SUCCESS: Installer created!" -ForegroundColor Green
    Write-Host "  Path: $($fileInfo.FullName)" -ForegroundColor Gray
    Write-Host "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "  Created: $($fileInfo.CreationTime)" -ForegroundColor Gray
    Write-Host "  WiX Version: 6.0" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "ERROR: Installer file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Installation Instructions:" -ForegroundColor Cyan
Write-Host "1. Double-click the .msi file to install" -ForegroundColor White
Write-Host "2. Follow the installation wizard" -ForegroundColor White
Write-Host "3. The application will be available in Start Menu and Desktop" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to exit"
