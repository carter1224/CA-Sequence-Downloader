param(
    [string]$ReleaseDir = "release"
)

$ErrorActionPreference = "Stop"

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    throw "python not found. Install Python 3.11+"
}

try {
    & $python.Source -m PyInstaller --version | Out-Null
} catch {
    throw "pyinstaller not found. Run: pip install pyinstaller pycomm3"
}

if (Test-Path $ReleaseDir) {
    Remove-Item $ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ReleaseDir | Out-Null

Push-Location "Sequence downloader PC"
& $python.Source -m PyInstaller "SequenceDownloader.spec" --noconfirm --clean
Copy-Item "dist\\SequenceDownloader.exe" "..\\$ReleaseDir\\SequenceDownloader.exe" -Force
Pop-Location

$pcBundleDir = Join-Path $ReleaseDir "SequenceDownloader_PC"
New-Item -ItemType Directory -Path $pcBundleDir | Out-Null
Copy-Item "Sequence downloader PC\\dist\\SequenceDownloader.exe" "$pcBundleDir\\SequenceDownloader.exe" -Force
Copy-Item "Sequence downloader PC\\settings.json" "$pcBundleDir\\settings.json" -Force
Copy-Item "Sequence downloader PC\\README.txt" "$pcBundleDir\\README.txt" -Force
Compress-Archive -Path "$pcBundleDir\\*" -DestinationPath (Join-Path $ReleaseDir "SequenceDownloader_PC.zip") -Force
Remove-Item $pcBundleDir -Recurse -Force

Push-Location "Sequence downloader USB"
& $python.Source -m PyInstaller "SequenceDownloaderUSB.spec" --noconfirm --clean
Pop-Location

Push-Location "Sequence downloader USB\\SetupUsb"
dotnet publish "SetupUsb.csproj" -c Release
Copy-Item "bin\\Release\\net8.0-windows\\win-x64\\publish\\SetupSequenceDownloaderUSB.exe" "..\\..\\$ReleaseDir\\SetupSequenceDownloaderUSB.exe" -Force
Pop-Location

Write-Host "Built:"
Get-ChildItem $ReleaseDir
