param(
    [string]$UsbLabel = "SEQUSB",
    [string]$TaskName = "SequenceDownloaderUSB"
)

$sourceHelper = Join-Path $PSScriptRoot "run_on_insert.ps1"
if (-not (Test-Path $sourceHelper)) {
    throw "run_on_insert.ps1 not found in $PSScriptRoot"
}

$helperDir = Join-Path $env:ProgramData "SequenceDownloaderUSB"
New-Item -ItemType Directory -Path $helperDir -Force | Out-Null
$helperPath = Join-Path $helperDir "run_on_insert.ps1"
Copy-Item $sourceHelper $helperPath -Force

$action = "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$helperPath`" -UsbLabel `"$UsbLabel`""
$eventLog = "Microsoft-Windows-DriverFrameworks-UserMode/Operational"
$eventQuery = "*[System[(EventID=2100 or EventID=2101 or EventID=2105 or EventID=2106)]]"

schtasks.exe /Create /TN $TaskName /SC ONEVENT /EC $eventLog /MO $eventQuery /TR $action /F | Out-Null
Write-Host "Installed scheduled task '$TaskName' for USB label '$UsbLabel'."
