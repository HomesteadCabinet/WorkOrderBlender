# WorkOrderBlender Version Update and Release Script
# This script updates version numbers, builds portable package, and triggers auto-update via GitHub release
#
# Parameters:
#   NewVersion        - New version number (e.g., "1.0.2")
#   CommitMessage     - Git commit message (default: "Version bump to {version}")
#   SkipGitPush       - Skip pushing to GitHub (default: false)
#   Force             - Force update even with uncommitted changes (default: false)
#   SkipPortableBuild - Skip building portable package (default: false)
#
# Usage:
#   .\update-version.ps1 -NewVersion "1.0.2"
#   .\update-version.ps1 -NewVersion "1.0.2" -SkipPortableBuild
#   .\update-version.ps1 -NewVersion "1.0.2" -SkipGitPush

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,

    [string]$CommitMessage = "Version bump to $NewVersion",

    [switch]$SkipGitPush,

    [switch]$Force,

    [switch]$SkipPortableBuild
)

# Function to validate version format (e.g., "1.0.2")
function Test-VersionFormat {
    param([string]$Version)
    return $Version -match '^\d+\.\d+\.\d+$'
}

# Function to update XML file
function Update-XmlVersion {
    param(
        [string]$FilePath,
        [string]$Version
    )

    try {
        $xml = [xml](Get-Content $FilePath)
        $xml.item.version = $Version
        $xml.item.url = "https://github.com/HomesteadCabinet/WorkOrderBlender/releases/download/v$Version/WorkOrderBlender-v$Version.zip"
        $xml.Save($FilePath)
        Write-Host "[OK] Updated $FilePath" -ForegroundColor Green
    }
    catch {
        Write-Error "[ERROR] Failed to update ${FilePath}: $($_.Exception.Message)"
        return $false
    }
    return $true
}

# Function to update .csproj file
function Update-CsprojVersion {
    param(
        [string]$FilePath,
        [string]$Version
    )

    try {
        $content = Get-Content $FilePath -Raw

        # Update Version
        $content = $content -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>"

        # Update AssemblyVersion and FileVersion (keep as 3-part version)
        $content = $content -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>"
        $content = $content -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$Version</FileVersion>"

        Set-Content $FilePath $content -NoNewline
        Write-Host "[OK] Updated $FilePath" -ForegroundColor Green
    }
    catch {
        Write-Error "[ERROR] Failed to update ${FilePath}: $($_.Exception.Message)"
        return $false
    }
    return $true
}

# Function to update WiX installer file
function Update-WixVersion {
    param(
        [string]$FilePath,
        [string]$Version
    )

    try {
        $content = Get-Content $FilePath -Raw

        # Update Version (keep as 3-part version)
        $content = $content -replace 'Version="[\d\.]+"', "Version=""$Version"""

        Set-Content $FilePath $content -NoNewline
        Write-Host "[OK] Updated $FilePath" -ForegroundColor Green
    }
    catch {
        Write-Error "[ERROR] Failed to update ${FilePath}: $($_.Exception.Message)"
        return $false
    }
    return $true
}

# Function to build portable package
function Build-PortablePackage {
    param([string]$Version)

    Write-Host "`n[INFO] Building portable package..." -ForegroundColor Cyan

    try {
        # Run the build-portable script
        $buildScript = Join-Path $PSScriptRoot "build-portable.ps1"
        if (-not (Test-Path $buildScript)) {
            Write-Error "[ERROR] build-portable.ps1 not found"
            return $false
        }

        # Execute build-portable.ps1 with Clean parameter
        # We don't need to skip build since we're updating version files first
        Write-Host "[INFO] Executing build-portable.ps1..." -ForegroundColor Cyan
        $buildArgs = @("-Clean")
        & $buildScript @buildArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Error "[ERROR] Portable build failed"
            return $false
        }

        # Ensure dist directory exists
        $distDir = Join-Path $PSScriptRoot "dist"
        if (-not (Test-Path $distDir)) {
            Write-Error "[ERROR] Dist directory not found after build: $distDir"
            return $false
        }

        # Verify the zip file was created
        $expectedZipName = "WorkOrderBlender-$Version-portable.zip"
        $zipDir = Join-Path $PSScriptRoot "dist"
        $zipPath = Join-Path $zipDir $expectedZipName

        if (-not (Test-Path $zipPath)) {
            Write-Error "[ERROR] Portable zip file not found at expected location: $zipPath"
            return $false
        }

        Write-Host "[OK] Portable package built successfully: $expectedZipName" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "[ERROR] Failed to build portable package: $($_.Exception.Message)"
        return $false
    }
}

# Validate inputs
if (-not (Test-VersionFormat $NewVersion)) {
    Write-Error "[ERROR] Invalid version format. Use format like '1.0.2'"
    exit 1
}

Write-Host "=== WorkOrderBlender Version Update Script ===" -ForegroundColor Cyan
Write-Host "New Version: $NewVersion" -ForegroundColor Yellow

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Error "[ERROR] Not in a git repository"
    exit 1
}

# Check for uncommitted changes
$status = git status --porcelain
if ($status -and -not $Force) {
    Write-Warning "[WARNING] You have uncommitted changes:"
    git status --short
    $choice = Read-Host "Continue anyway? (y/N)"
    if ($choice -ne 'y' -and $choice -ne 'Y') {
        Write-Host "[CANCELLED] Aborted" -ForegroundColor Red
        exit 1
    }
}

# Update version files
Write-Host "`n[INFO] Updating version files..." -ForegroundColor Cyan

$success = $true

# Update WorkOrderBlender.csproj
if (-not (Update-CsprojVersion "WorkOrderBlender.csproj" $NewVersion)) {
    $success = $false
}

# Update update.xml
if (-not (Update-XmlVersion "update.xml" $NewVersion)) {
    $success = $false
}

# Update WiX installer
if (Test-Path "WorkOrderBlender.Installer.wxs") {
    if (-not (Update-WixVersion "WorkOrderBlender.Installer.wxs" $NewVersion)) {
        $success = $false
    }
}

if (-not $success) {
    Write-Error "[ERROR] Failed to update version files"
    exit 1
}

# Build portable package (unless skipped)
if (-not $SkipPortableBuild) {
    # Check if build-portable.ps1 exists
    $buildScript = Join-Path $PSScriptRoot "build-portable.ps1"
    if (-not (Test-Path $buildScript)) {
        Write-Error "[ERROR] build-portable.ps1 not found. Cannot build portable package."
        exit 1
    }

    if (-not (Build-PortablePackage $NewVersion)) {
        Write-Error "[ERROR] Failed to build portable package"
        exit 1
    }
} else {
    Write-Host "`n[INFO] Skipping portable build (use -SkipPortableBuild:`$false to build)" -ForegroundColor Yellow
}

# Git operations
Write-Host "`n[INFO] Git operations..." -ForegroundColor Cyan

try {
    # Add changed files
    git add WorkOrderBlender.csproj update.xml
    if (Test-Path "WorkOrderBlender.Installer.wxs") {
        git add WorkOrderBlender.Installer.wxs
    }

    # Add the new portable zip file if it was built
    if (-not $SkipPortableBuild) {
        $expectedZipName = "WorkOrderBlender-$NewVersion-portable.zip"
        $zipDir = Join-Path $PSScriptRoot "dist"
        $zipPath = Join-Path $zipDir $expectedZipName
        if (Test-Path $zipPath) {
            git add $zipPath
            Write-Host "[OK] Staged portable zip file: $expectedZipName" -ForegroundColor Green
        } else {
            Write-Warning "[WARNING] Portable zip file not found: $expectedZipName"
        }
    }

    Write-Host "[OK] Staged version files" -ForegroundColor Green

    # Commit changes
    git commit -m $CommitMessage
    Write-Host "[OK] Committed changes: $CommitMessage" -ForegroundColor Green

    # Create and push tag
    $tagName = "v$NewVersion"
    git tag $tagName
    Write-Host "[OK] Created tag: $tagName" -ForegroundColor Green

    if (-not $SkipGitPush) {
        Write-Host "[INFO] Pushing to GitHub..." -ForegroundColor Cyan

        # Push commits
        git push origin main
        Write-Host "[OK] Pushed commits to main branch" -ForegroundColor Green

        # Push tag (this triggers the GitHub Action release workflow)
        git push origin $tagName
        Write-Host "[OK] Pushed tag: $tagName" -ForegroundColor Green

        Write-Host "`n[SUCCESS] Release workflow should start automatically." -ForegroundColor Green
        Write-Host "[INFO] Check progress at: https://github.com/HomesteadCabinet/WorkOrderBlender/actions" -ForegroundColor Cyan
        Write-Host "[INFO] Release will be available at: https://github.com/HomesteadCabinet/WorkOrderBlender/releases/tag/$tagName" -ForegroundColor Cyan
    } else {
        Write-Host "`n[INFO] Git push skipped (use -SkipGitPush:`$false to push)" -ForegroundColor Yellow
        Write-Host "[INFO] To manually push: git push origin main; git push origin $tagName" -ForegroundColor Cyan
    }
}
catch {
    Write-Error "[ERROR] Git operation failed: $($_.Exception.Message)"
    exit 1
}

Write-Host "`n[SUCCESS] Version update complete!" -ForegroundColor Green
Write-Host "[INFO] Updated files:" -ForegroundColor Cyan
Write-Host "   - WorkOrderBlender.csproj (Version: $NewVersion, AssemblyVersion: $NewVersion)" -ForegroundColor White
Write-Host "   - update.xml (Version: $NewVersion)" -ForegroundColor White
if (Test-Path "WorkOrderBlender.Installer.wxs") {
    Write-Host "   - WorkOrderBlender.Installer.wxs (Version: $NewVersion)" -ForegroundColor White
}

if (-not $SkipPortableBuild) {
    Write-Host "   - WorkOrderBlender-$NewVersion-portable.zip (Portable package)" -ForegroundColor White
}

if (-not $SkipGitPush) {
    Write-Host "`n[INFO] The GitHub Actions workflow will:" -ForegroundColor Cyan
    Write-Host "   1. Build the application" -ForegroundColor White
    Write-Host "   2. Create a GitHub release" -ForegroundColor White
    Write-Host "   3. Upload WorkOrderBlender-v$NewVersion.zip" -ForegroundColor White
    Write-Host "   4. Update the auto-updater XML" -ForegroundColor White
    Write-Host "   5. Trigger auto-updates for existing users" -ForegroundColor White
}

Write-Host "`n[INFO] Script completed successfully with:" -ForegroundColor Cyan
Write-Host "   - Version files updated" -ForegroundColor White
if (-not $SkipPortableBuild) {
    Write-Host "   - Portable package built and committed" -ForegroundColor White
}
Write-Host "   - Git commit and tag created" -ForegroundColor White
if (-not $SkipGitPush) {
    Write-Host "   - Changes pushed to GitHub" -ForegroundColor White
}
