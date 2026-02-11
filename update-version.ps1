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
#
# Note: The script will prompt for release notes during execution. These notes will be included
#       in the commit body and displayed in the update dialog when users check for updates.

param(
    [Parameter(Mandatory=$false)]
    [string]$NewVersion,

    [string]$CommitMessage,

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
        [string]$Version,
        [string]$ReleaseNotes = ""
    )

    try {
        $xml = [xml](Get-Content $FilePath)
        $xml.item.version = $Version
        $xml.item.url = "https://github.com/HomesteadCabinet/WorkOrderBlender/releases/download/v$Version/WorkOrderBlender-v$Version.zip"

        # Update or create releaseNotes element
        if (-not [string]::IsNullOrWhiteSpace($ReleaseNotes)) {
            # Check if releaseNotes element exists, if not create it
            if ($null -eq $xml.item.releaseNotes) {
                $releaseNotesElement = $xml.CreateElement("releaseNotes")
                $xml.item.AppendChild($releaseNotesElement) | Out-Null
            }
            $xml.item.releaseNotes = $ReleaseNotes
            Write-Host "[OK] Updated release notes in $FilePath" -ForegroundColor Green
        } else {
            # If no release notes provided, remove the element if it exists
            if ($null -ne $xml.item.releaseNotes) {
                $xml.item.RemoveChild($xml.item.releaseNotes) | Out-Null
            }
        }

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

# Function to get current version from .csproj file
function Get-CurrentVersion {
    try {
        if (Test-Path "WorkOrderBlender.csproj") {
            $content = Get-Content "WorkOrderBlender.csproj" -Raw
            if ($content -match '<Version>([\d\.]+)</Version>') {
                return $matches[1]
            }
        }
    }
    catch {
        # Ignore errors, return null
    }
    return $null
}

# Function to compute default next version (bump patch: 1.7.17 -> 1.7.18)
function Get-DefaultNextVersion {
    param([string]$CurrentVersion)
    if ([string]::IsNullOrWhiteSpace($CurrentVersion)) { return $null }
    if (-not (Test-VersionFormat $CurrentVersion)) { return $null }
    $parts = $CurrentVersion -split '\.'
    if ($parts.Count -ne 3) { return $null }
    $patchNum = 0
    [int]::TryParse($parts[2], [ref]$patchNum) | Out-Null
    $patch = $patchNum + 1
    return "$($parts[0]).$($parts[1]).$patch"
}

# Function to get multiline release notes from user
function Get-ReleaseNotes {
    Write-Host "`n[INFO] Enter release notes for this update:" -ForegroundColor Cyan
    Write-Host "   (These notes will appear in the commit body and update dialog)" -ForegroundColor Gray
    Write-Host "   (Press Enter on an empty line twice to finish, or just press Enter twice to skip)" -ForegroundColor Gray
    Write-Host ""

    $releaseNotes = @()
    $emptyLineCount = 0
    $lineNumber = 1

    while ($true) {
        $prompt = if ($lineNumber -eq 1) { "   Release notes" } else { "   > " }
        $line = Read-Host $prompt

        if ([string]::IsNullOrWhiteSpace($line)) {
            $emptyLineCount++
            if ($emptyLineCount -ge 2) {
                break
            }
        } else {
            $emptyLineCount = 0
            $releaseNotes += $line
            $lineNumber++
        }
    }

    if ($releaseNotes.Count -eq 0) {
        Write-Host "   [INFO] No release notes provided (skipping)" -ForegroundColor Yellow
        return ""
    }

    Write-Host "   [OK] Release notes captured ($($releaseNotes.Count) line(s))" -ForegroundColor Green
    return $releaseNotes -join "`n"
}


# Display current version first
Write-Host "=== WorkOrderBlender Version Update Script ===" -ForegroundColor Cyan
$currentVersion = Get-CurrentVersion
if ($currentVersion) {
    Write-Host "Current Version: $currentVersion" -ForegroundColor Cyan
} else {
    Write-Host "Current Version: Unable to determine" -ForegroundColor Yellow
}
Write-Host ""

# Compute default next version (bump patch) for prompt and blank input
$defaultVersion = Get-DefaultNextVersion $currentVersion

# Prompt for new version if not provided
if ([string]::IsNullOrWhiteSpace($NewVersion)) {
    $prompt = if ($defaultVersion) {
        "Enter new version number (blank for default ($defaultVersion))"
    } else {
        "Enter new version number (e.g., 1.0.2)"
    }
    $inputVersion = Read-Host $prompt
    $NewVersion = if ([string]::IsNullOrWhiteSpace($inputVersion) -and $defaultVersion) {
        $defaultVersion
    } else {
        $inputVersion
    }
}

# Set default commit message if not provided
if ([string]::IsNullOrWhiteSpace($CommitMessage)) {
    $CommitMessage = "Version bump to $NewVersion"
}

# Validate inputs
if (-not (Test-VersionFormat $NewVersion)) {
    Write-Error "[ERROR] Invalid version format. Use format like '1.0.2'"
    exit 1
}

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

# Get release notes from user BEFORE updating files
# This allows us to include release notes in update.xml
Write-Host "`n[INFO] Collecting release notes..." -ForegroundColor Cyan
$releaseNotes = Get-ReleaseNotes

# Update version files
Write-Host "`n[INFO] Updating version files..." -ForegroundColor Cyan

$success = $true

# Update WorkOrderBlender.csproj
if (-not (Update-CsprojVersion "WorkOrderBlender.csproj" $NewVersion)) {
    $success = $false
}

# Update update.xml (with release notes if provided)
if (-not (Update-XmlVersion "update.xml" $NewVersion $releaseNotes)) {
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

    # Commit changes with release notes in body
    if ([string]::IsNullOrWhiteSpace($releaseNotes)) {
        # No release notes provided, use simple commit
        git commit -m $CommitMessage
        Write-Host "[OK] Committed changes: $CommitMessage" -ForegroundColor Green
    } else {
        # Create temporary file for commit message with body
        $tempCommitFile = [System.IO.Path]::GetTempFileName()
        try {
            # Write commit message: subject line, blank line, then body
            $commitContent = $CommitMessage + "`n`n" + $releaseNotes
            Set-Content -Path $tempCommitFile -Value $commitContent -NoNewline

            # Commit using the file
            git commit -F $tempCommitFile
            Write-Host "[OK] Committed changes: $CommitMessage" -ForegroundColor Green
            Write-Host "[OK] Release notes included in commit body" -ForegroundColor Green
        }
        finally {
            # Clean up temporary file
            if (Test-Path $tempCommitFile) {
                Remove-Item $tempCommitFile -Force -ErrorAction SilentlyContinue
            }
        }
    }

    # Create and push tag
    $tagName = "v$NewVersion"
    git tag $tagName
    Write-Host "[OK] Created tag: $tagName" -ForegroundColor Green

    if (-not $SkipGitPush) {
        Write-Host "[INFO] Pushing to GitHub..." -ForegroundColor Cyan

        # Push commits (explicitly specify branch ref to avoid ambiguity with tags)
        git push origin refs/heads/main:refs/heads/main
        Write-Host "[OK] Pushed commits to main branch" -ForegroundColor Green

        # Push tag (this triggers the GitHub Action release workflow)
        git push origin refs/tags/$tagName
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
