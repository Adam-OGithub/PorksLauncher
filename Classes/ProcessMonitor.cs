using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PorksLauncher.Classes
{
    public class ProcessMonitor : IDisposable
    {
        private readonly Timer processCheckTimer;
        private readonly ListBox listBox;
        private readonly LauncherDatabase database;
        private readonly ProcessCandidatePolicy candidatePolicy;
        private readonly Func<IEnumerable<LauncherEntry>> getRunningApplications;
        private readonly Func<LauncherEntry, bool> askToAdd;
        private bool checkingProcesses;
        private bool disposed;

        public bool PromptsPaused { get; set; }
        public bool IsMonitoring => !disposed && processCheckTimer.Enabled;

        public ProcessMonitor(ListBox targetListBox, LauncherDatabase database,
            Func<IEnumerable<LauncherEntry>> getRunningApplications = null,
            Func<LauncherEntry, bool> askToAdd = null, ProcessCandidatePolicy candidatePolicy = null)
        {
            listBox = targetListBox;
            this.database = database;
            this.getRunningApplications = getRunningApplications ?? EnumerateRunningApplications;
            this.askToAdd = askToAdd ?? AskToAddProcess;
            this.candidatePolicy = candidatePolicy ?? ProcessCandidatePolicy.CreateDefault();
            processCheckTimer = new Timer { Interval = 3000 };
            processCheckTimer.Tick += (sender, e) => ScanNow();
        }

        public void StartMonitoring()
        {
            if (!disposed) processCheckTimer.Start();
        }

        public void StopMonitoring()
        {
            if (!disposed) processCheckTimer.Stop();
        }

        public void Dispose()
        {
            disposed = true;
            processCheckTimer.Dispose();
        }

        public void ScanNow()
        {
            // Modal dialogs pump timer events. Keep one scan active until its prompt closes.
            if (disposed || checkingProcesses || PromptsPaused || !processCheckTimer.Enabled) return;
            checkingProcesses = true;
            try
            {
                var knownPaths = new HashSet<string>(database.GetLaunchers().Select(entry => entry.FilePath),
                    StringComparer.OrdinalIgnoreCase);
                var deniedPaths = new HashSet<string>(database.GetDeniedExecutables()
                    .Where(entry => entry.IsDenied).Select(entry => entry.FilePath), StringComparer.OrdinalIgnoreCase);
                foreach (var application in getRunningApplications())
                {
                    if (disposed || PromptsPaused || !processCheckTimer.Enabled) break;
                    if (!candidatePolicy.ShouldPrompt(application.DisplayName, application.FilePath)) continue;
                    string fullPath = Path.GetFullPath(application.FilePath);
                    if (knownPaths.Contains(fullPath) || deniedPaths.Contains(fullPath)) continue;

                    bool accepted = askToAdd(application);
                    if (disposed) break;
                    if (accepted)
                    {
                        var entry = database.AddLauncher(fullPath, application.DisplayName);
                        listBox.Items.Add(entry);
                        knownPaths.Add(fullPath);
                    }
                    else
                    {
                        database.SaveDeniedExecutables(new[] { new DeniedExecutable
                        {
                            FilePath = fullPath, DisplayName = application.DisplayName, IsDenied = true
                        } });
                        deniedPaths.Add(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                StopMonitoring();
                MessageBox.Show(listBox.FindForm(), $"Executable scanning stopped: {ex.Message}", "Scanner error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                checkingProcesses = false;
            }
        }

        private bool AskToAddProcess(LauncherEntry application)
        {
            return MessageBox.Show(listBox.FindForm(),
                $"Add {application.DisplayName} ({application.FilePath}) to the list?\n\n" +
                "Choosing No stops future prompts for this executable. You can change this in Settings → Denied executables.",
                "New Application Detected", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IEnumerable<LauncherEntry> EnumerateRunningApplications()
        {
            Process[] processes = Process.GetProcesses();
            try
            {
                foreach (var process in processes)
                {
                    var candidate = GetCandidate(process);
                    if (candidate != null) yield return candidate;
                }
            }
            finally
            {
                foreach (var process in processes) process.Dispose();
            }
        }

        private static LauncherEntry GetCandidate(Process process)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(process.MainWindowTitle)) return null;
                string path = process.MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(path)) return null;
                return new LauncherEntry { DisplayName = process.ProcessName, FilePath = Path.GetFullPath(path) };
            }
            catch (System.ComponentModel.Win32Exception) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (NotSupportedException) { return null; }
        }
    }
}
