using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PorksLauncher.Classes
{
    public sealed class ProcessCandidatePolicy
    {
        private readonly string windowsDirectory;
        private readonly string[] programFilesDirectories;
        private readonly string launcherPath;
        private static readonly HashSet<string> WindowsProcessNames = new HashSet<string>(
            new[] { "explorer", "taskmgr", "dwm", "conhost", "csrss", "wininit", "winlogon", "smss",
                "services", "svchost", "lsass", "fontdrvhost", "sihost", "spoolsv", "SearchIndexer",
                "SearchHost", "SearchApp", "StartMenuExperienceHost", "ShellExperienceHost",
                "ApplicationFrameHost", "RuntimeBroker", "SystemSettings", "SecurityHealthSystray" },
            StringComparer.OrdinalIgnoreCase);

        public ProcessCandidatePolicy(string windowsDirectory, IEnumerable<string> programFilesDirectories, string launcherPath)
        {
            this.windowsDirectory = windowsDirectory;
            this.programFilesDirectories = programFilesDirectories.Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            this.launcherPath = launcherPath;
        }

        public static ProcessCandidatePolicy CreateDefault()
        {
            return new ProcessCandidatePolicy(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Environment.GetEnvironmentVariable("ProgramW6432") }, typeof(ProcessMonitor).Assembly.Location);
        }

        public bool ShouldPrompt(string processName, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || WindowsProcessNames.Contains(processName) ||
                string.Equals(filePath, launcherPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase)) return false;

            if (IsInDirectory(filePath, windowsDirectory)) return false;
            foreach (string root in programFilesDirectories)
            {
                if (!IsInDirectory(filePath, root)) continue;
                string relativePath = filePath.Substring(root.TrimEnd('\\', '/').Length + 1);
                // These are built-in Windows packages, not all applications installed in Program Files.
                if (relativePath.StartsWith(@"WindowsApps\Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.StartsWith(@"WindowsApps\MicrosoftCorporationII.", StringComparison.OrdinalIgnoreCase)) return false;

                foreach (string folder in new[] { "Windows Defender", "Windows Mail", "Windows NT",
                    "Windows Photo Viewer", "Windows Media Player", @"Microsoft\Edge", @"Microsoft\EdgeWebView" })
                    if (IsInDirectory(filePath, Path.Combine(root, folder))) return false;
            }
            return true;
        }

        private static bool IsInDirectory(string filePath, string directory)
        {
            return !string.IsNullOrWhiteSpace(directory) &&
                filePath.StartsWith(directory.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
