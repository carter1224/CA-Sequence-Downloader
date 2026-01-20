# CA Sequence Downloader

This repo contains two separate Windows executables:

- `SequenceDownloader.exe` (PC tool)
- `SequenceDownloaderUSB.exe` (USB tool)

Both are built automatically and placed into a single `release/` folder.

## Layout

- `Sequence downloader PC/` - PC downloader source and PyInstaller spec
- `Sequence downloader USB/` - USB downloader source and PyInstaller spec
- `release/` - combined output folder (created by build)

## Build (local)

Prereqs:
- Python 3.11+
- `pip install pyinstaller pycomm3`

Build both and place EXEs into `release/`:

```powershell
.\build.ps1
```

## Build (CI)

GitHub Actions builds on every push and uploads a `release` artifact
containing both EXEs.
