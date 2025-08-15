# WorkOrderBlender Version Update and Release Script
# This script updates version numbers and triggers auto-update via GitHub release

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

        # Update AssemblyVersion and FileVersion (append .0 for 4-part version)
        $fourPartVersion = "$Version.0"
        $content = $content -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$fourPartVersion</AssemblyVersion>"
        $content = $content -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$fourPartVersion</FileVersion>"

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

if (-not $success) {
    Write-Error "[ERROR] Failed to update version files"
    exit 1
}

# Git operations
Write-Host "`n[INFO] Git operations..." -ForegroundColor Cyan

try {
    # Add changed files
    git add WorkOrderBlender.csproj update.xml
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
Write-Host "   - WorkOrderBlender.csproj (Version: $NewVersion, AssemblyVersion: $NewVersion.0)" -ForegroundColor White
Write-Host "   - update.xml (Version: $NewVersion)" -ForegroundColor White

if (-not $SkipGitPush) {
    Write-Host "`n[INFO] The GitHub Actions workflow will:" -ForegroundColor Cyan
    Write-Host "   1. Build the application" -ForegroundColor White
    Write-Host "   2. Create a GitHub release" -ForegroundColor White
    Write-Host "   3. Upload WorkOrderBlender-v$NewVersion.zip" -ForegroundColor White
    Write-Host "   4. Update the auto-updater XML" -ForegroundColor White
    Write-Host "   5. Trigger auto-updates for existing users" -ForegroundColor White
}
