param(
    [string]$UsbLabel = "SEQUSB"
)

$logPath = Join-Path $PSScriptRoot "run_log.txt"
$stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Add-Content -Path $logPath -Value "$stamp START label=$UsbLabel"

$drive = Get-Volume | Where-Object {
    $_.FileSystemLabel -eq $UsbLabel
} | Select-Object -First 1

if (-not $drive) {
    Add-Content -Path $logPath -Value "$stamp NO_DRIVE"
    exit 0
}

$usbRoot = "$($drive.DriveLetter):\"
$exePath = Join-Path $usbRoot "Sequence downloader USB\SequenceDownloaderUSB.exe"

if (-not (Test-Path $exePath)) {
    Add-Content -Path $logPath -Value "$stamp EXE_NOT_FOUND path=$exePath"
    exit 1
}

$exeDir = Split-Path $exePath -Parent
$outDir = Join-Path $exeDir "output"
Add-Content -Path $logPath -Value "$stamp RUN exe=$exePath out=$outDir"
try {
    & $exePath --out-dir $outDir
    Add-Content -Path $logPath -Value "$stamp EXIT code=$LASTEXITCODE"
} catch {
    Add-Content -Path $logPath -Value "$stamp ERROR $($_.Exception.Message)"
    exit 1
}
