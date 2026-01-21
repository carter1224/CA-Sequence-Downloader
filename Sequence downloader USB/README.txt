Sequence Downloader USB - User Guide

Purpose
- Runs the sequence download when a USB labeled SEQUSB is inserted.
- Writes ZIP exports to <USB>\output.

Which EXE to run

SetupSequenceDownloaderUSB.exe
- Run this to prepare a USB drive.
- It copies files and labels the drive SEQUSB.

SequenceDownloaderUSB.exe
- The downloader that runs when the USB is inserted.
- It is placed on the USB by the setup tool.

USB prep (per USB drive)
1) Copy SetupSequenceDownloaderUSB.exe to the USB (any folder is fine).
2) Run it as Administrator.
3) Confirm the drive label was set to SEQUSB.

Manual setup (PowerShell, HMI only)
1) Copy this folder to the USB root:
   <USB>\Sequence downloader USB
2) On the HMI (admin), run:
   powershell -ExecutionPolicy Bypass -File "<USB>\Sequence downloader USB\install_usb_autorun.ps1"
3) This installs the task that runs run_on_insert.ps1 when SEQUSB is inserted.

Using the USB
1) Insert the USB.
2) The download runs automatically.
3) Output is written to <USB>\output\seq_export_*.zip

Troubleshooting
- If the download fails, check <USB>\output\EXPORT_ERROR.txt
- If setup fails, check <USB>\Sequence downloader USB\SETUP_ERROR.txt

Settings
- Edit settings.json to set:
  ip, eth_slot, cpu_slot, out_dir (optional), chunk_size (optional), pretty_json (optional)

Notes
- Run setup once per HMI.
- Re-run setup after updating files.

Windows support
- Windows 10/11 only (not supported on Windows CE).
