using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PorksLauncher.Classes
{
    public class ProcessMonitor : IDisposable
    {
        private Timer processCheckTimer;
        private ListBox listBox;
        private readonly LauncherDatabase database;
        private bool checkingProcesses;
        private HashSet<string> declinedProcesses; // Stores rejected processes
        private string selfProcessName; // Stores the application's own process name

        public ProcessMonitor(ListBox targetListBox, LauncherDatabase database)
        {
            listBox = targetListBox;
            this.database = database;
            declinedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            selfProcessName = Process.GetCurrentProcess().ProcessName; // Get self process name

            processCheckTimer = new Timer { Interval = 3000 }; // Check every 3 seconds
            processCheckTimer.Tick += ProcessCheckTimer_Tick;
        }

        public void StartMonitoring()
        {
            processCheckTimer.Start();
        }

        public void StopMonitoring()
        {
            processCheckTimer.Stop();
        }

        public void Dispose()
        {
            processCheckTimer.Dispose();
        }

        private void ProcessCheckTimer_Tick(object sender, EventArgs e)
        {
            // A modal prompt pumps timer events; do not scan again while one is open.
            if (checkingProcesses) return;
            checkingProcesses = true;
            Process[] runningProcesses = null;
            try
            {
                var knownPaths = new HashSet<string>(database.GetLaunchers().Select(entry => entry.FilePath),
                    StringComparer.OrdinalIgnoreCase);
                runningProcesses = Process.GetProcesses();
                foreach (Process process in runningProcesses)
                {
                    string processName;
                    string processPath;
                    try
                    {
                        if (string.IsNullOrEmpty(process.MainWindowTitle) || IsSystemProcess(process)) continue;
                        processName = process.ProcessName;
                        processPath = GetProcessPath(process);
                    }
                    catch (InvalidOperationException) { continue; } // Process exited during the scan.
                    catch (System.ComponentModel.Win32Exception) { continue; }

                    if (string.IsNullOrEmpty(processPath) ||
                        processName.Equals(selfProcessName, StringComparison.OrdinalIgnoreCase) ||
                        knownPaths.Contains(processPath) || declinedProcesses.Contains(processPath)) continue;

                    if (AskToAddProcess(processName, processPath))
                    {
                        var entry = database.AddLauncher(processPath, processName);
                        listBox.Items.Add(entry);
                        knownPaths.Add(processPath);
                    }
                    else
                    {
                        declinedProcesses.Add(processPath);
                    }
                }
            }
            catch (Exception ex)
            {
                StopMonitoring();
                MessageBox.Show($"Executable scanning stopped: {ex.Message}", "Scanner error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (runningProcesses != null)
                    foreach (var process in runningProcesses) process.Dispose();
                checkingProcesses = false;
            }
        }

        private bool AskToAddProcess(string processName, string processPath)
        {
            string[] systemProcesses =
            {
                "explorer", "taskmgr", "dwm", "conhost", "csrss", "wininit", "winlogon", "smss",
                "services", "svchost", "lsass", "fontdrvhost", "sihost", "spoolsv", "SearchIndexer"
            };

            if (systemProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                return false; // Skip system processes
            }

            // If the process has already been declined, do not prompt again
            if (declinedProcesses.Contains(processPath))
            {
                return false;
            }

            return MessageBox.Show($"Add {processName} ({processPath}) to the list?", "New Process Detected", MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        private bool IsSystemProcess(Process process)
        {
            try
            {
                string processPath = GetProcessPath(process);

                if (string.IsNullOrEmpty(processPath) ||
                    processPath.StartsWith(@"C:\Windows\", StringComparison.OrdinalIgnoreCase))
                {
                    return true; // It's a system process
                }
            }
            catch
            {
                return true; // Likely a system process if access is restricted
            }

            return false;
        }

        private string GetProcessPath(Process process)
        {
            try
            {
                return process.MainModule.FileName; // Try to get the process path
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return string.Empty; // Return empty if access is denied
            }
            catch (Exception)
            {
                return string.Empty; // Catch any unexpected errors
            }
        }
    }
}






