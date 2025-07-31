using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            // You might want to keep some minimal user feedback here:
            MessageBox.Show("Invalid arguments. Usage: Updater.exe <extractPath> <parentPID>", "Updater Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string extractPath = args[0];
        string parentPidStr = args[1];
        string targetPath = AppDomain.CurrentDomain.BaseDirectory;

        try
        {
            // Wait for parent to exit
            if (int.TryParse(parentPidStr, out int parentPid))
            {
                try
                {
                    var parentProc = Process.GetProcessById(parentPid);
                    parentProc.WaitForExit();
                }
                catch
                {
                    // Parent already exited
                }
            }

            // Copy files
            string[] relevantFiles = new[]
            {
                "FluentFTP.dll",
                "glFTPd Commander.deps.json",
                "glFTPd Commander.dll",
                "glFTPd Commander.exe",
                "glFTPd Commander.runtimeconfig.json"
            };
            
            foreach (string file in relevantFiles)
            {
                string sourceFile = Path.Combine(extractPath, file);
                string destFile = Path.Combine(targetPath, file);
            
                if (File.Exists(sourceFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    RetryCopy(sourceFile, destFile);
                }
                else
                {
                    // Optionally log missing file
                    Debug.WriteLine($"[Updater] File not found in update: {file}");
                }
            }

            // Launch main app
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(targetPath, "glFTPd Commander.exe"),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed:\n{ex.Message}", "Update Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static void RetryCopy(string source, string dest)
    {
        const int maxRetries = 10;
        int tries = 0;

        while (true)
        {
            try
            {
                File.Copy(source, dest, true);
                return;
            }
            catch (IOException)
            {
                tries++;
                if (tries >= maxRetries)
                    throw new IOException($"Failed to copy {Path.GetFileName(source)} after {maxRetries} retries");

                Thread.Sleep(500);
            }
        }
    }
}
