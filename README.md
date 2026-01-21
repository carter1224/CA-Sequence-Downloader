# CA Sequence Downloader

Two tools are provided for end users:

- `SequenceDownloader.exe` (PC tool)
- `SetupSequenceDownloaderUSB.exe` (USB setup tool)

## PC tool (SequenceDownloader.exe)

Use this on a Windows 10/11 PC to download sequences directly.

Steps:
1) Run `SequenceDownloader.exe`.
2) Enter the PLC IP and start the download.
3) The ZIP output is written to the configured output folder.

Notes:
- Requires network access to the PLC.
- Windows 10/11 only.

## USB tool (SetupSequenceDownloaderUSB.exe)

Use this to prepare a USB drive and (optionally) enable auto-run on an HMI.

USB prep (per USB drive):
1) Copy `SetupSequenceDownloaderUSB.exe` to the USB (any folder is fine).
2) Run it as Administrator.
3) It copies the required files and labels the drive `SEQUSB`.

HMI auto-run (optional):
1) On the HMI, run:
   `SetupSequenceDownloaderUSB.exe --install-task`
2) After setup, inserting the USB runs the downloader automatically.

Notes:
- Windows 10/11 only. Not supported on Windows CE.

## Manual setup (PowerShell, HMI only)

If you cannot use the setup EXE, you can install the auto-run task manually.

1) Copy the `Sequence downloader USB` folder to the USB root.
2) On the HMI (admin), run:

```powershell
powershell -ExecutionPolicy Bypass -File "<USB>\Sequence downloader USB\install_usb_autorun.ps1"
```

This installs the task that runs `run_on_insert.ps1` whenever a SEQUSB drive is inserted.
