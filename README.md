# CA Sequence Downloader

Two tools are provided for end users:

- `SequenceDownloader.exe` (PC tool)
- `SetupSequenceDownloaderUSB.exe` (USB setup tool)

## PC tool (SequenceDownloader_PC.zip)

Download `SequenceDownloader_PC.zip`. It contains `SequenceDownloader.exe`,
`settings.json`, and `README.txt`.

Steps:
1) Extract the ZIP.
2) Edit `settings.json` and set the PLC IP.
3) Run `SequenceDownloader.exe`.
4) The output ZIP is written to the configured output folder.

Notes:
- Requires network access to the PLC.
- Windows 10/11 only.

## USB tool (SetupSequenceDownloaderUSB.exe)

Use this to prepare a USB drive and enable auto-run on an HMI.

SequenceDownloaderUSB setup (per USB drive):
1) Copy `SetupSequenceDownloaderUSB.exe` to the USB (any folder is fine).
2) Run it as Administrator. It copies the required files and labels the drive `SEQUSB`.
3) On the HMI (admin), run:

```powershell
powershell -ExecutionPolicy Bypass -File "<USB>\Sequence downloader USB\install_usb_autorun.ps1"
```

This installs the task that runs `run_on_insert.ps1` whenever a SEQUSB drive is inserted.

Notes:
- Windows 10/11 only. Not supported on Windows CE.
