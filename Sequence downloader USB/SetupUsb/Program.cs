using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

namespace SetupSequenceDownloaderUSB;

internal static class Program
{
    private const string DefaultLabel = "SEQUSB";
    private const string DefaultTaskName = "SequenceDownloaderUSB";
    private const string PayloadFolderName = "Sequence downloader USB";
    private const int RetryAttempts = 5;
    private const int RetryDelaySeconds = 3;

    private static string? ErrorFilePath;
    private static string? FallbackErrorFilePath;
    private static bool PauseOnExit = true;
    private static bool DeleteOnSuccess = true;

    public static int Main(string[] args)
    {
        try
        {
            string? driveArg = null;
            string label = DefaultLabel;
            string taskName = DefaultTaskName;
            bool skipPrompt = false;
            bool installTask = false;
            PauseOnExit = true;

            FallbackErrorFilePath = Path.Combine(AppContext.BaseDirectory, "SETUP_ERROR.txt");

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--drive":
                        if (i + 1 >= args.Length)
                        {
                            Fail("Missing value for --drive.");
                        }
                        driveArg = args[++i];
                        break;
                    case "--label":
                        if (i + 1 >= args.Length)
                        {
                            Fail("Missing value for --label.");
                        }
                        label = args[++i];
                        break;
                    case "--task":
                        if (i + 1 >= args.Length)
                        {
                            Fail("Missing value for --task.");
                        }
                        taskName = args[++i];
                        break;
                case "--yes":
                    skipPrompt = true;
                    break;
                case "--install-task":
                    installTask = true;
                    break;
                case "--no-pause":
                    PauseOnExit = false;
                    break;
                case "--keep-setup":
                    DeleteOnSuccess = false;
                    break;
                    case "--help":
                    case "-h":
                    case "/?":
                        PrintHelp();
                        return 0;
                    default:
                        Fail($"Unknown argument: {args[i]}");
                        break;
                }
            }

            if (!IsAdministrator())
            {
                Fail("Please run this installer as Administrator.");
            }

            string driveLetter = GetDriveLetter(driveArg) ?? GetDriveLetter(AppContext.BaseDirectory);
            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                Fail("Unable to determine target drive. Use --drive E: to specify one.");
            }

            ErrorFilePath = Path.Combine($"{driveLetter}:\\", PayloadFolderName, "SETUP_ERROR.txt");

            DriveInfo drive;
            try
            {
                drive = new DriveInfo(driveLetter);
            }
            catch
            {
                Fail($"Drive '{driveLetter}:' not found.");
                return 1;
            }

            if (drive.DriveType != DriveType.Removable && !IsUsbDrive(driveLetter))
            {
                Fail($"Refusing to run on non-removable, non-USB drive '{driveLetter}:'.");
            }

            if (!drive.IsReady)
            {
                Fail($"Drive '{driveLetter}:' is not ready.");
            }

            string sizeText = $"{Math.Round(drive.TotalSize / (1024.0 * 1024 * 1024), 1)}GB";
            Console.WriteLine($"Target USB: {driveLetter}: label='{drive.VolumeLabel}' size={sizeText}");
            if (!skipPrompt)
            {
                Console.Write("Continue and configure this USB? (y/N) ");
                string? response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Canceled.");
                    return 0;
                }
            }

            string targetRoot = Path.Combine($"{driveLetter}:\\", PayloadFolderName);
            Directory.CreateDirectory(targetRoot);

            ExtractPayload("payload.SequenceDownloaderUSB.exe", Path.Combine(targetRoot, "SequenceDownloaderUSB.exe"));
            ExtractPayload("payload.run_on_insert.ps1", Path.Combine(targetRoot, "run_on_insert.ps1"));
            ExtractPayload("payload.install_usb_autorun.ps1", Path.Combine(targetRoot, "install_usb_autorun.ps1"));
            ExtractPayload("payload.settings.json", Path.Combine(targetRoot, "settings.json"));
            ExtractPayload("payload.README.txt", Path.Combine(targetRoot, "README.txt"));

            Retry("Set USB label", () => SetVolumeLabel(driveLetter, label));
            if (installTask)
            {
                Retry("Install scheduled task", () => InstallScheduledTask(taskName, label));
            }

            Console.WriteLine("Setup complete.");
            PauseIfEnabled("Press Enter to close...");
            if (DeleteOnSuccess)
            {
                TrySelfDelete();
            }
            return 0;
        }
        catch (Exception ex)
        {
            WriteErrorFile($"Unhandled error: {ex.Message}", ex);
            Console.Error.WriteLine(ex.Message);
            PauseIfEnabled("Press Enter to close...");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SetupSequenceDownloaderUSB");
        Console.WriteLine("  --drive E:   Target removable drive letter (defaults to EXE location)");
        Console.WriteLine("  --label NAME USB volume label (default: SEQUSB)");
        Console.WriteLine("  --task NAME  Scheduled task name (default: SequenceDownloaderUSB)");
        Console.WriteLine("  --yes        Skip confirmation prompt");
        Console.WriteLine("  --install-task  Install the USB autorun scheduled task on this PC");
        Console.WriteLine("  --keep-setup    Do not delete this installer after success");
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string? GetDriveLetter(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        string trimmed = input.Trim();
        if (trimmed.Length >= 2 && trimmed[1] == ':')
        {
            return trimmed.Substring(0, 1).ToUpperInvariant();
        }
        if (trimmed.Length == 1 && char.IsLetter(trimmed[0]))
        {
            return trimmed.ToUpperInvariant();
        }
        string root = Path.GetPathRoot(trimmed) ?? string.Empty;
        if (root.Length >= 2 && root[1] == ':')
        {
            return root.Substring(0, 1).ToUpperInvariant();
        }
        return null;
    }

    private static void ExtractPayload(string resourceName, string destinationPath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            Fail($"Missing embedded resource: {resourceName}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        using FileStream fileStream = File.Create(destinationPath);
        stream!.CopyTo(fileStream);
    }

    private static void SetVolumeLabel(string driveLetter, string label)
    {
        string command = $"Set-Volume -DriveLetter {driveLetter} -NewFileSystemLabel '{label}'";
        int exitCode = RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to set USB label. Exit code {exitCode}.");
        }
    }

    private static void InstallScheduledTask(string taskName, string usbLabel)
    {
        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string helperDir = Path.Combine(programData, "SequenceDownloaderUSB");
        Directory.CreateDirectory(helperDir);
        string helperPath = Path.Combine(helperDir, "run_on_insert.ps1");

        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream? stream = assembly.GetManifestResourceStream("payload.run_on_insert.ps1"))
        {
            if (stream is null)
            {
                Fail("Missing embedded resource: payload.run_on_insert.ps1");
            }
            using FileStream fileStream = File.Create(helperPath);
            stream!.CopyTo(fileStream);
        }

        string action = $"powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{helperPath}\" -UsbLabel \"{usbLabel}\"";
        string eventLog = "Microsoft-Windows-DriverFrameworks-UserMode/Operational";
        string eventQuery = "*[System[(EventID=2100 or EventID=2101 or EventID=2105 or EventID=2106)]]";
        string args = $"/Create /TN \"{taskName}\" /SC ONEVENT /EC \"{eventLog}\" /MO \"{eventQuery}\" /TR \"{action}\" /F";

        int exitCode = RunProcess("schtasks.exe", args);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to install scheduled task. Exit code {exitCode}.");
        }
    }

    private static void Retry(string actionName, Action action)
    {
        for (int attempt = 1; attempt <= RetryAttempts; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= RetryAttempts)
                {
                    throw new InvalidOperationException(
                        $"{actionName} failed after {RetryAttempts} attempts.",
                        ex);
                }
                Console.WriteLine($"{actionName} failed (attempt {attempt}/{RetryAttempts}). Retrying in {RetryDelaySeconds}s.");
                Thread.Sleep(TimeSpan.FromSeconds(RetryDelaySeconds));
            }
        }
    }

    private static int RunProcess(string fileName, string arguments)
    {
        using Process process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (!process.Start())
        {
            Fail($"Failed to start {fileName}.");
        }
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine(error);
            }
        }

        return process.ExitCode;
    }

    private static bool IsUsbDrive(string driveLetter)
    {
        string command = $"Get-Partition -DriveLetter {driveLetter} | Get-Disk | Select-Object -ExpandProperty BusType";
        int exitCode = RunProcessCapture("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"", out string output);
        if (exitCode != 0)
        {
            return false;
        }
        return output.Trim().Equals("USB", StringComparison.OrdinalIgnoreCase);
    }

    private static int RunProcessCapture(string fileName, string arguments, out string output)
    {
        using Process process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (!process.Start())
        {
            Fail($"Failed to start {fileName}.");
        }
        output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine(error);
            }
        }

        return process.ExitCode;
    }

    private static void Fail(string message)
    {
        WriteErrorFile(message, null);
        Console.Error.WriteLine(message);
        PauseIfEnabled("Press Enter to close...");
        Environment.Exit(1);
    }

    private static void PauseIfEnabled(string message)
    {
        if (!PauseOnExit)
        {
            return;
        }
        Console.WriteLine(message);
        Console.ReadLine();
    }

    private static void TrySelfDelete()
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }
        string args = $"/c ping 127.0.0.1 -n 2 > nul & del \"{exePath}\"";
        try
        {
            using Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
        }
        catch
        {
            // Best-effort only.
        }
    }

    private static void WriteErrorFile(string message, Exception? ex)
    {
        string detail = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR\n{message}\n";
        if (ex != null)
        {
            detail += $"{ex}\n";
        }
        TryWrite(ErrorFilePath, detail);
        TryWrite(FallbackErrorFilePath, detail);
    }

    private static void TryWrite(string? path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
        catch
        {
            // Best-effort only.
        }
    }
}
