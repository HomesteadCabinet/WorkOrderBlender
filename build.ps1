# WorkOrderBlender Portable Builder
# PowerShell script to produce a zip-able portable build (no installer required)

param(
    [switch]$Clean,
    [switch]$SkipBuild,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "Building WorkOrderBlender Portable package..." -ForegroundColor Green
Write-Host ""

# Resolve project file
$projPath = Join-Path $PSScriptRoot "WorkOrderBlender.csproj"
if (-not (Test-Path $projPath)) {
    Write-Host "ERROR: WorkOrderBlender.csproj not found next to this script." -ForegroundColor Red
    exit 1
}

# Read project metadata
[xml]$projXml = Get-Content $projPath
$assemblyName = $projXml.Project.PropertyGroup.AssemblyName
if ([string]::IsNullOrWhiteSpace($assemblyName)) { $assemblyName = "WorkOrderBlender" }
$version = $projXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) { $version = "0.0.0" }

# Determine build output path for net48 x64 Release
$framework = "net48"
$configuration = "Release"
$platform = "x64"
$outputDir = Join-Path $PSScriptRoot ("bin/" + $platform + "/" + $configuration + "/" + $framework)

# Optionally clean previous output
if ($Clean) {
    if (Test-Path $outputDir) {
        Write-Host "Cleaning output directory..." -ForegroundColor Yellow
        Remove-Item $outputDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Build (unless skipped)
if (-not $SkipBuild) {
    Write-Host "Building project (Release, x64)..." -ForegroundColor Yellow
    $buildArgs = @("build", "-c", $configuration, "-p:Platform=$platform")
    if ($Verbose) { $buildArgs += "-v:n" }
    dotnet @buildArgs | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Project build failed!" -ForegroundColor Red
        exit 1
    }
}

# Verify executable exists
$exePath = Join-Path $outputDir ("$assemblyName.exe")
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Built executable not found at $exePath" -ForegroundColor Red
    Write-Host "Ensure the build succeeded and the Platform is x64." -ForegroundColor Yellow
    exit 1
}

# Prepare portable staging directory
$distRoot = Join-Path $PSScriptRoot "dist"
if (-not (Test-Path $distRoot)) { New-Item -ItemType Directory -Path $distRoot | Out-Null }
$portableDirName = "$assemblyName-$version-portable"
$portableDir = Join-Path $distRoot $portableDirName
if (Test-Path $portableDir) { Remove-Item $portableDir -Recurse -Force }
New-Item -ItemType Directory -Path $portableDir | Out-Null

# Copy build output to portable directory
Write-Host "Copying application files..." -ForegroundColor Yellow
Copy-Item -Path (Join-Path $outputDir "*") -Destination $portableDir -Recurse -Force

# Optionally include helpful files
foreach ($extra in @("README.md")) {
    $extraPath = Join-Path $PSScriptRoot $extra
    if (Test-Path $extraPath) { Copy-Item $extraPath -Destination $portableDir -Force }
}

# Create zip archive
$zipName = "$portableDirName.zip"
$zipPath = Join-Path $distRoot $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "Creating archive: $zipPath" -ForegroundColor Yellow
Compress-Archive -Path (Join-Path $portableDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

# Display result
Write-Host ""
Write-Host "SUCCESS: Portable package created!" -ForegroundColor Green
Write-Host "  Folder: $portableDir" -ForegroundColor Gray
Write-Host "  Archive: $zipPath" -ForegroundColor Gray
if (Test-Path $zipPath) {
    $zipInfo = Get-Item $zipPath
    Write-Host "  Size: $([math]::Round($zipInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "- Unzip anywhere (no install)." -ForegroundColor White
Write-Host "- Ensure \"lib\\amd64\" native DLLs remain alongside the EXE (already included)." -ForegroundColor White
Write-Host "- Run $assemblyName.exe" -ForegroundColor White
Write-Host ""
