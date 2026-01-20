Sequence Downloader USB - Setup and Use

What it does
- Runs the sequence download when a USB labeled SEQUSB is inserted.
- Writes ZIP exports to <USB>\output.

USB prep (per USB drive)
1) Label the USB drive: SEQUSB
2) Copy this folder to the root of the USB:
   <USB>\Sequence downloader USB
3) Make sure these files exist in that folder:
   - SequenceDownloaderUSB.exe
   - run_on_insert.ps1
   - install_usb_autorun.ps1
   - settings.json

PC setup (HMI only)
1) Open PowerShell as Administrator.
2) Run:
   powershell -ExecutionPolicy Bypass -File "<USB>\Sequence downloader USB\install_usb_autorun.ps1"
   (Replace <USB> with the actual drive letter.)
   - Or run the installer EXE (preferred):
     <USB>\Sequence downloader USB\SetupSequenceDownloaderUSB.exe

Using it
- Plug the USB in. The task runs automatically.
- Output is written to <USB>\output\seq_export_*.zip
- If it fails, check <USB>\output\EXPORT_ERROR.txt
- If setup fails, check <USB>\Sequence downloader USB\SETUP_ERROR.txt

Settings
- Edit settings.json to set:
  ip, eth_slot, cpu_slot, out_dir (optional), chunk_size (optional), pretty_json (optional)

Notes
- You only need to run the installer once per PC.
- If you update files, re-run the installer.
Windows support
- Windows 10/11 only (not supported on Windows CE).
