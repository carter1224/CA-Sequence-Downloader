# CA Sequence Downloader

This repo builds two separate Windows tools:

- `SequenceDownloader.exe` (PC tool)
- `SetupSequenceDownloaderUSB.exe` (USB setup tool)

Both are produced into a single `release/` folder when you build.

## Quick start

1) Install prerequisites:
- Python 3.11+
- `pip install pyinstaller pycomm3`

2) Build both EXEs into `release/`:

```powershell
.\build.ps1
```

3) Grab the outputs:
- `release\SequenceDownloader.exe`
- `release\SetupSequenceDownloaderUSB.exe`

## Executables

### SequenceDownloader.exe (PC tool)

Purpose:
- Run downloads on a Windows 10/11 PC.

Run:
```powershell
.\SequenceDownloader.exe --ip 192.168.1.11
```

Notes:
- Requires network access to the PLC.
- Output ZIPs are written to the configured output folder.

### SetupSequenceDownloaderUSB.exe (USB setup tool)

Purpose:
- Prepare a USB drive with the downloader files and label it `SEQUSB`.
- Optionally install the auto-run task on an HMI.

Run (USB prep only):
```powershell
.\SetupSequenceDownloaderUSB.exe
```

Run (USB prep + HMI auto-run task):
```powershell
.\SetupSequenceDownloaderUSB.exe --install-task
```

Notes:
- Intended for Windows 10/11 HMIs (not Windows CE).

## USB setup (for HMI)

The USB tool needs a one-time setup on each HMI where you want auto-run.

1) Drag `SetupSequenceDownloaderUSB.exe` onto the USB (any folder is fine).

2) Run it on the HMI as Administrator.
   It will copy the required files, set the label to `SEQUSB`, and (if you pass
   `--install-task`) install the autorun task.

After that, inserting a `SEQUSB` drive runs the downloader and writes output to:
`<USB>\output\seq_export_*.zip`

## Project layout

- `Sequence downloader PC/` - PC downloader source and PyInstaller spec
- `Sequence downloader USB/` - USB downloader source and PyInstaller spec
- `release/` - combined output folder (created by build)

## Windows support

Requires Windows 10/11. This does not run on Windows CE-based HMIs.

## CI build

GitHub Actions builds on release publish and uploads a `release` artifact
containing both EXEs.
