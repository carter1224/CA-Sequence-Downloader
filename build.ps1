param(
    [string]$ReleaseDir = "release"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command pyinstaller -ErrorAction SilentlyContinue)) {
    throw "pyinstaller not found. Run: pip install pyinstaller pycomm3"
}

if (Test-Path $ReleaseDir) {
    Remove-Item $ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ReleaseDir | Out-Null

Push-Location "Sequence downloader PC"
pyinstaller "SequenceDownloader.spec" --noconfirm --clean
Copy-Item "dist\\SequenceDownloader.exe" "..\\$ReleaseDir\\SequenceDownloader.exe" -Force
Pop-Location

Push-Location "Sequence downloader USB"
pyinstaller "SequenceDownloaderUSB.spec" --noconfirm --clean
Copy-Item "dist\\SequenceDownloaderUSB.exe" "..\\$ReleaseDir\\SequenceDownloaderUSB.exe" -Force
Pop-Location

Write-Host "Built:"
Get-ChildItem $ReleaseDir
