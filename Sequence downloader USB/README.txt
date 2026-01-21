Sequence Downloader USB - Setup and Use

What it does
- Runs the sequence download when a USB labeled SEQUSB is inserted.
- Writes ZIP exports to <USB>\output.

Executables

SetupSequenceDownloaderUSB.exe
- Prepares a USB drive: copies files and labels it SEQUSB.
- Optional: installs the auto-run task on an HMI (with --install-task).

SequenceDownloaderUSB.exe
- The downloader that actually runs on USB insert.
- It is copied to the USB by the setup tool.

USB prep (per USB drive)
1) Drag SetupSequenceDownloaderUSB.exe onto the USB (any folder is fine).
2) Run it as Administrator. It will copy files and set the label to SEQUSB.

HMI setup (auto-run)
1) Run SetupSequenceDownloaderUSB.exe with --install-task on the HMI:
   <USB>\SetupSequenceDownloaderUSB.exe --install-task

Using it
1) Plug the USB in. The task runs automatically.
2) Output is written to <USB>\output\seq_export_*.zip
3) If it fails, check <USB>\output\EXPORT_ERROR.txt
4) If setup fails, check <USB>\Sequence downloader USB\SETUP_ERROR.txt

Settings
- Edit settings.json to set:
  ip, eth_slot, cpu_slot, out_dir (optional), chunk_size (optional), pretty_json (optional)

Notes
- You only need to run the installer once per HMI.
- If you update files, re-run the installer.
Windows support
- Windows 10/11 only (not supported on Windows CE).
