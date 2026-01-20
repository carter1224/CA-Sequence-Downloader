param(
    [string]$UsbLabel = "SEQUSB",
    [string]$TaskName = "SequenceDownloaderUSB",
    [string]$TargetDrive
)

function Get-DriveLetter([string]$input) {
    if ([string]::IsNullOrWhiteSpace($input)) {
        return $null
    }
    $s = $input.Trim()
    if ($s.Length -ge 2 -and $s[1] -eq ':') {
        return $s.Substring(0, 1)
    }
    if ($s.Length -eq 1) {
        return $s
    }
    return $null
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Please run this script as Administrator."
    exit 1
}

$driveLetter = Get-DriveLetter $TargetDrive
if (-not $driveLetter) {
    $qualifier = Split-Path -Qualifier $PSScriptRoot
    $driveLetter = Get-DriveLetter $qualifier
}
if (-not $driveLetter) {
    Write-Error "Unable to determine target drive. Pass -TargetDrive like 'E:'."
    exit 1
}

$volume = Get-Volume -DriveLetter $driveLetter -ErrorAction SilentlyContinue
if (-not $volume) {
    Write-Error "Drive '$driveLetter:' not found."
    exit 1
}
if ($volume.DriveType -ne "Removable") {
    Write-Error "Refusing to run on non-removable drive '$driveLetter:'."
    exit 1
}

$sizeGb = [math]::Round($volume.Size / 1GB, 1)
Write-Host "Target USB: $driveLetter: label='$($volume.FileSystemLabel)' size=${sizeGb}GB"
$confirm = Read-Host "Continue and configure this USB? (y/N)"
if ($confirm -notin @("y", "Y")) {
    Write-Host "Canceled."
    exit 0
}

$targetRoot = "$driveLetter`:\Sequence downloader USB"
New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

$files = @(
    "SequenceDownloaderUSB.exe",
    "run_on_insert.ps1",
    "install_usb_autorun.ps1",
    "settings.json",
    "README.txt"
)

foreach ($f in $files) {
    $src = Join-Path $PSScriptRoot $f
    if (-not (Test-Path $src)) {
        Write-Error "Missing required file: $src"
        exit 1
    }
    $dest = Join-Path $targetRoot $f
    $srcFull = (Resolve-Path $src).Path
    $destFull = $dest
    if (Test-Path $dest) {
        $destFull = (Resolve-Path $dest).Path
    }
    if ($srcFull -ne $destFull) {
        Copy-Item $src $dest -Force
    }
}

if ($volume.FileSystemLabel -ne $UsbLabel) {
    try {
        Set-Volume -DriveLetter $driveLetter -NewFileSystemLabel $UsbLabel | Out-Null
        Write-Host "Set USB label to '$UsbLabel'."
    } catch {
        Write-Error "Failed to set USB label. $($_.Exception.Message)"
        exit 1
    }
}

$installer = Join-Path $targetRoot "install_usb_autorun.ps1"
if (-not (Test-Path $installer)) {
    Write-Error "Installer not found at $installer"
    exit 1
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -UsbLabel $UsbLabel -TaskName $TaskName
if ($LASTEXITCODE -ne 0) {
    Write-Error "Scheduled task install failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host "Setup complete."
