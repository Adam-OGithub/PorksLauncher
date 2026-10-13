using System;
using System.Diagnostics;
using System.IO;

namespace PorksLauncher.Classes
{
    public static class ApplicationLauncher
    {
        public const string FileFilter =
            "Applications (*.exe;*.jar)|*.exe;*.jar|Executables (*.exe)|*.exe|Java applications (*.jar)|*.jar|All files (*.*)|*.*";

        public static ProcessStartInfo CreateStartInfo(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("The saved file no longer exists. Remove it and add its new location.", fullPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = fullPath,
                WorkingDirectory = Path.GetDirectoryName(fullPath),
                UseShellExecute = true
            };

            if (string.Equals(Path.GetExtension(fullPath), ".jar", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = FindJavaExecutable();
                // A Windows file name cannot contain quotes, and a JAR path ends in
                // .jar, so quoting the full path keeps it a single Java argument.
                startInfo.Arguments = "-jar \"" + fullPath + "\"";
                startInfo.UseShellExecute = false;
            }
            return startInfo;
        }

        private static string FindJavaExecutable()
        {
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                string java = FindJavaInDirectory(javaHome.Trim().Trim('"'), "bin");
                if (java != null) return java;
            }

            string searchPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in searchPath.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                string java = FindJavaInDirectory(directory.Trim().Trim('"'), string.Empty);
                if (java != null) return java;
            }

            throw new InvalidOperationException(
                "Java was not found. Install a Java runtime and set JAVA_HOME to its installation folder " +
                "or add the folder containing java.exe to PATH. Then restart PorksLauncher.");
        }

        private static string FindJavaInDirectory(string directory, string subfolder)
        {
            try
            {
                string candidate = Path.GetFullPath(Path.Combine(
                    Environment.ExpandEnvironmentVariables(directory), subfolder, "java.exe"));
                return File.Exists(candidate) ? candidate : null;
            }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
            catch (PathTooLongException) { return null; }
        }
    }
}
