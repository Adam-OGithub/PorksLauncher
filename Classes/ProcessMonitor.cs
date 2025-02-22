using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PorksLauncher.Classes
{
    public class ProcessMonitor
    {
        private Timer processCheckTimer;
        private ListBox listBox;
        private string csvFilePath;
        private HashSet<string> declinedProcesses; // Stores rejected processes
        private string selfProcessName; // Stores the application's own process name

        public ProcessMonitor(ListBox targetListBox)
        {
            listBox = targetListBox;
            csvFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pork_launcher", "launch.csv");
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

        private bool IsProcessInCsv(string processName)
        {
            // Read all lines from the CSV and check if any contains the process name
            if (!File.Exists(csvFilePath))
                return false;

            var lines = File.ReadAllLines(csvFilePath);
            return lines.Any(line => line.Split(',')[1] == processName); // Check if the name exists
        }

        private void ProcessCheckTimer_Tick(object sender, EventArgs e)
        {
            var runningProcesses = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)) // Only visible window processes
                .Where(p => !IsSystemProcess(p)) // Skip system processes
                .ToList();

            foreach (Process process in runningProcesses)
            {
                string processName = process.ProcessName;
                string processPath = GetProcessPath(process);

                if (string.IsNullOrEmpty(processPath) || processName.Equals(selfProcessName, StringComparison.OrdinalIgnoreCase))
                    continue; // Skip self and processes with no valid path

                if (!listBox.Items.Contains(processPath) && !declinedProcesses.Contains(processPath)) // Use path for tracking
                {
                    // Skip if the process is already in the list box or CSV file
                    if (listBox.Items.Contains(processName) || IsProcessInCsv(processName))
                    {
                        continue;
                    }

                    if (AskToAddProcess(processName, processPath))
                    {
                        listBox.Items.Add(processName);
                        // Save path first, then name
                        File.AppendAllText(csvFilePath, $"{processPath},{processName}{Environment.NewLine}"); // Path first, then name
                    }
                    else
                    {
                        declinedProcesses.Add(processPath); // Store rejected paths
                    }
                }
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






