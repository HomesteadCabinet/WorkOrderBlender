# WorkOrderBlender Version Update and Release Script
# This script updates version numbers, commits all changes, and triggers auto-update via GitHub release
# Note: GitHub Actions workflow generates the .zip file automatically
#
# Parameters:
#   NewVersion        - New version number (e.g., "1.0.2")
#   CommitMessage     - Git commit message (default: "Version bump to {version}")
#   SkipGitPush       - Skip pushing to GitHub (default: false)
#   Force             - Force update even with uncommitted changes (default: false)
#
# Usage:
#   .\update-version.ps1 -NewVersion "1.0.2"
#   .\update-version.ps1 -NewVersion "1.0.2" -SkipGitPush

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,

    [string]$CommitMessage = "Version bump to $NewVersion",

    [switch]$SkipGitPush,

    [switch]$Force
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

# Check for uncommitted changes (informational only - we'll add them all)
$status = git status --porcelain
if ($status -and -not $Force) {
    Write-Host "[INFO] Found uncommitted changes (will be included in commit):" -ForegroundColor Cyan
    git status --short
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

# Git operations
Write-Host "`n[INFO] Git operations..." -ForegroundColor Cyan

try {
    # Add all unstaged files (including version files and any other changes)
    git add -A
    Write-Host "[OK] Staged all changes (including version files and unstaged files)" -ForegroundColor Green

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
Write-Host "   - All other unstaged files included in commit" -ForegroundColor White

if (-not $SkipGitPush) {
    Write-Host "`n[INFO] The GitHub Actions workflow will:" -ForegroundColor Cyan
    Write-Host "   1. Build the application" -ForegroundColor White
    Write-Host "   2. Create a GitHub release" -ForegroundColor White
    Write-Host "   3. Generate and upload WorkOrderBlender-v$NewVersion.zip" -ForegroundColor White
    Write-Host "   4. Update the auto-updater XML" -ForegroundColor White
    Write-Host "   5. Trigger auto-updates for existing users" -ForegroundColor White
}

Write-Host "`n[INFO] Script completed successfully with:" -ForegroundColor Cyan
Write-Host "   - Version files updated" -ForegroundColor White
Write-Host "   - All changes committed" -ForegroundColor White
Write-Host "   - Git commit and tag created" -ForegroundColor White
if (-not $SkipGitPush) {
    Write-Host "   - Changes pushed to GitHub" -ForegroundColor White
}
