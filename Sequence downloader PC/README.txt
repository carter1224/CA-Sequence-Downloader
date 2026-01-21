Sequence Downloader (PC) - User Guide

Purpose
- Download SEQ[100] data from a PLC and write a ZIP file.

First-time setup
1) Place these files in the same folder:
   - SequenceDownloader.exe
   - settings.json
   - README.txt
2) Edit settings.json and set the PLC IP address.

Run
1) Double-click SequenceDownloader.exe.
2) The output ZIP is written to the configured output folder.

Settings
- ip: PLC IP address (required)
- eth_slot: Ethernet slot number
- cpu_slot: CPU slot number
- out_dir: output folder (default: output)
- chunk_size: read chunk size (default: 20)
- pretty_json: pretty JSON output (default: true)

Troubleshooting
- If it fails, check output\EXPORT_ERROR.txt

Windows support
- Windows 10/11 only.
