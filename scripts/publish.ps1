<#
.SYNOPSIS
    Builds and publishes the Modern Windows Task Manager WPF application as a self-contained single-file win-x64 executable.
.DESCRIPTION
    Compiles TaskManager.App for .NET 10 LTS, packaging all managed assemblies, native runtimes, and resources.
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "./publish/win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Modern Windows Task Manager - Release Packaging Tool    " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$projectPath = Resolve-Path "src/TaskManager.App/TaskManager.App.csproj"
Write-Host "Target Project: $projectPath" -ForegroundColor Gray
Write-Host "Configuration : $Configuration" -ForegroundColor Gray
Write-Host "Runtime       : $Runtime" -ForegroundColor Gray
Write-Host "Output Directory: $OutputDir" -ForegroundColor Gray

# Ensure clean publish directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning existing output directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputDir
}

Write-Host "`n[1/3] Restoring solution dependencies..." -ForegroundColor Green
dotnet restore TaskManager.sln

Write-Host "`n[2/3] Building solution with zero warnings allowed..." -ForegroundColor Green
dotnet build TaskManager.sln -c $Configuration --no-incremental /warnaserror

Write-Host "`n[3/3] Publishing self-contained win-x64 single-file executable..." -ForegroundColor Green
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    -o $OutputDir

# Validate publish output
$exePath = Join-Path $OutputDir "TaskManager.App.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $fileSizeMb = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-Host "`n==========================================================" -ForegroundColor Green
    Write-Host "  Publish Successful!                                     " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "Executable: $($fileInfo.FullName)" -ForegroundColor White
    Write-Host "File Size : $fileSizeMb MB" -ForegroundColor White
    Write-Host "Target OS : Windows (x64 Self-Contained, .NET 10 LTS)" -ForegroundColor White
} else {
    Write-Error "Publish failed: Executable not found at $exePath"
}
