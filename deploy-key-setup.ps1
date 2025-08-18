# Deploy Key Setup Script for WorkOrderBlender
# This script helps set up and test the deploy key integration across multiple systems

param(
    [switch]$Test,
    [switch]$Clean,
    [switch]$ListResources,
    [switch]$Help
)

function Show-Help {
    Write-Host "Deploy Key Setup Script for WorkOrderBlender" -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\deploy-key-setup.ps1 [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Test          Test the deploy key integration" -ForegroundColor White
    Write-Host "  -Clean         Clean up portable deploy key files" -ForegroundColor White
    Write-Host "  -ListResources List available embedded resources" -ForegroundColor White
    Write-Host "  -Help          Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\deploy-key-setup.ps1 -Test" -ForegroundColor White
    Write-Host "  .\deploy-key-setup.ps1 -Clean" -ForegroundColor White
    Write-Host "  .\deploy-key-setup.ps1 -ListResources" -ForegroundColor White
}

function Test-DeployKeyIntegration {
    Write-Host "Testing Deploy Key Integration..." -ForegroundColor Cyan
    Write-Host "=================================" -ForegroundColor Cyan

    # Check if we're in the right directory
    $exePath = "WorkOrderBlender.exe"
    if (-not (Test-Path $exePath)) {
        $exePath = "bin\Debug\net48\WorkOrderBlender.exe"
        if (-not (Test-Path $exePath)) {
            Write-Host "Error: WorkOrderBlender.exe not found" -ForegroundColor Red
            return $false
        }
    }

    # Check current application version
    try {
        $assembly = [System.Reflection.Assembly]::LoadFile((Resolve-Path $exePath))
        $version = $assembly.GetName().Version
        Write-Host "✓ Application version: $version" -ForegroundColor Green
    } catch {
        Write-Host "✗ Could not determine application version" -ForegroundColor Red
        return $false
    }

    # Check if deploy key exists in user's SSH directory
    $userSshDir = Join-Path $env:USERPROFILE ".ssh"
    $deployKeyPath = Join-Path $userSshDir "workorderblender_deploy_key"
    
    if (Test-Path $deployKeyPath) {
        Write-Host "✓ Deploy key found in user SSH directory" -ForegroundColor Green
        $keyInfo = Get-Item $deployKeyPath
        Write-Host "  Key size: $($keyInfo.Length) bytes" -ForegroundColor Cyan
    } else {
        Write-Host "⚠ Deploy key not found in user SSH directory" -ForegroundColor Yellow
        Write-Host "  Will use embedded resources instead" -ForegroundColor Cyan
    }

    # Check if Git is available
    try {
        $gitVersion = git --version 2>$null
        if ($gitVersion) {
            Write-Host "✓ Git is available: $gitVersion" -ForegroundColor Green
        } else {
            Write-Host "✗ Git is not available" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Git is not available" -ForegroundColor Red
    }

    # Check if SSH is available
    try {
        $sshVersion = ssh -V 2>&1
        if ($sshVersion -match "OpenSSH") {
            Write-Host "✓ SSH is available: $($sshVersion.Split()[0])" -ForegroundColor Green
        } else {
            Write-Host "⚠ SSH is not available" -ForegroundColor Yellow
            Write-Host "  Updates will use HTTP fallback" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "⚠ SSH is not available" -ForegroundColor Yellow
        Write-Host "  Updates will use HTTP fallback" -ForegroundColor Cyan
    }

    # Check if update.xml is accessible
    $updateXmlUrl = "https://raw.githubusercontent.com/HomesteadCabinet/WorkOrderBlender/main/update.xml"
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Headers.Add("User-Agent", "WorkOrderBlender-Test/1.0")
        $updateXml = $webClient.DownloadString($updateXmlUrl)
        
        if ($updateXml -match "<version>") {
            $versionMatch = [regex]::Match($updateXml, '<version>([^<]+)</version>')
            if ($versionMatch.Success) {
                $latestVersion = $versionMatch.Groups[1].Value
                Write-Host "✓ Latest version available: $latestVersion" -ForegroundColor Green
            }
        }
        
        Write-Host "✓ Update XML is accessible" -ForegroundColor Green
    } catch {
        Write-Host "✗ Could not access update XML: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Check if portable files exist (after running the app once)
    $portableFiles = @(
        "workorderblender_deploy_key",
        "ssh_config", 
        "workorderblender_deploy_key.sha256"
    )

    Write-Host "`nChecking for portable deploy key files..." -ForegroundColor Cyan
    foreach ($file in $portableFiles) {
        if (Test-Path $file) {
            $fileInfo = Get-Item $file
            Write-Host "  ✓ $file ($($fileInfo.Length) bytes)" -ForegroundColor Green
        } else {
            Write-Host "  - $file (not found)" -ForegroundColor Gray
        }
    }

    Write-Host "`nTest Summary:" -ForegroundColor Yellow
    Write-Host "=============" -ForegroundColor Yellow

    if (Test-Path $deployKeyPath) {
        Write-Host "✓ Deploy key integration is ready" -ForegroundColor Green
        Write-Host "  The application will use SSH-based updates when possible" -ForegroundColor Cyan
    } else {
        Write-Host "⚠ Deploy key integration will use embedded resources" -ForegroundColor Yellow
        Write-Host "  The application will use HTTP-based updates" -ForegroundColor Cyan
    }

    Write-Host "`nTo test the integration:" -ForegroundColor Cyan
    Write-Host "1. Start WorkOrderBlender.exe" -ForegroundColor White
    Write-Host "2. Go to Help menu and select Check for Updates" -ForegroundColor White
    Write-Host "3. Check the application logs for deploy key messages" -ForegroundColor White
    Write-Host "4. Verify that portable files are created in the app directory" -ForegroundColor White

    return $true
}

function Clean-PortableFiles {
    Write-Host "Cleaning up portable deploy key files..." -ForegroundColor Cyan
    
    $filesToDelete = @(
        "workorderblender_deploy_key",
        "ssh_config", 
        "workorderblender_deploy_key.sha256"
    )

    foreach ($file in $filesToDelete) {
        if (Test-Path $file) {
            try {
                # Remove read-only attribute before deleting
                $fileInfo = Get-Item $file
                $fileInfo.Attributes = $fileInfo.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
                Remove-Item $file -Force
                Write-Host "✓ Removed $file" -ForegroundColor Green
            } catch {
                Write-Host "✗ Could not remove $file: $($_.Exception.Message)" -ForegroundColor Red
            }
        } else {
            Write-Host "- $file (not found)" -ForegroundColor Gray
        }
    }

    Write-Host "`nCleanup completed!" -ForegroundColor Green
}

function List-EmbeddedResources {
    Write-Host "Listing embedded resources..." -ForegroundColor Cyan
    
    $exePath = "WorkOrderBlender.exe"
    if (-not (Test-Path $exePath)) {
        $exePath = "bin\Debug\net48\WorkOrderBlender.exe"
        if (-not (Test-Path $exePath)) {
            Write-Host "Error: WorkOrderBlender.exe not found" -ForegroundColor Red
            return
        }
    }

    try {
        $assembly = [System.Reflection.Assembly]::LoadFile((Resolve-Path $exePath))
        $resources = $assembly.GetManifestResourceNames()
        
        Write-Host "Available embedded resources:" -ForegroundColor Green
        foreach ($resource in $resources) {
            Write-Host "  - $resource" -ForegroundColor White
        }
    } catch {
        Write-Host "Error listing embedded resources: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main script logic
if ($Help) {
    Show-Help
    exit 0
}

if ($Clean) {
    Clean-PortableFiles
    exit 0
}

if ($ListResources) {
    List-EmbeddedResources
    exit 0
}

if ($Test) {
    $success = Test-DeployKeyIntegration
    if ($success) {
        Write-Host "`nTest completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "`nTest completed with issues." -ForegroundColor Yellow
    }
    exit 0
}

# Default action: show help
Show-Help
