using PorksLauncher.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            TestFreshDatabaseAndCrud();
            TestLegacyImport();
            TestFailedImportCanRetry();
            TestFormLoadsDatabase();
            TestApplicationLaunchInfo();
            if (args.Length == 1) TestRealJarLaunch(args[0]);
            Console.WriteLine("PASS: SQLite storage, migration, form initialization, and application launch configuration.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static string NewFolder()
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static LauncherDatabase OpenDatabase(string folder)
    {
        var database = new LauncherDatabase(Path.Combine(folder, "launcher.db"));
        database.Initialize();
        return database;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void TestFreshDatabaseAndCrud()
    {
        string folder = NewFolder();
        var database = OpenDatabase(folder);
        Assert(database.GetLaunchers().Count == 0, "Fresh database must be empty.");
        var settings = database.GetSettings();
        Assert(settings["autoFind"] == "Off" && settings["autoPrompt"] == "Off", "Default settings must be Off.");
        Assert(!File.Exists(Path.Combine(folder, "launch.csv")) && !File.Exists(Path.Combine(folder, "settings.csv")),
            "Fresh initialization must not create CSV files.");

        const string path = @"C:\O'Brien, Apps\工具.exe";
        const string name = "O'Brien, 工具'); DROP TABLE Launchers; --";
        var first = database.AddLauncher(path, name);
        var second = database.AddLauncher(@"C:\Other\app.exe", name);
        var third = database.AddLauncher(@"C:\Other\extended.exe", name + " extended");
        Assert(first.Id != second.Id, "Duplicate names must have separate IDs.");
        database.RenameLauncher(first.Id, "Renamed, '工具'");
        database.RemoveLauncher(second.Id);
        database.SaveSettings(new Dictionary<string, string>
        {
            { "autoFind", "On" }, { "autoPrompt", "Off" }, { "custom", "comma, apostrophe'" }
        });
        database.SaveSettings(new Dictionary<string, string> { { "autoPrompt", "On" } });

        database = OpenDatabase(folder);
        var entries = database.GetLaunchers();
        Assert(entries.Count == 2 && entries[0].Id == first.Id && entries[1].Id == third.Id,
            "Remove must affect only the selected ID and preserve ordering across restart.");
        Assert(entries[0].FilePath == path && entries[0].DisplayName == "Renamed, '工具'" && entries[1].DisplayName == name + " extended",
            "Rename must preserve paths and other entries; punctuation and Unicode must round-trip.");
        settings = database.GetSettings();
        Assert(settings["autoFind"] == "On" && settings["autoPrompt"] == "On" && settings["custom"] == "comma, apostrophe'",
            "Settings must persist and updates must preserve unrelated settings.");
        using (var stream = new FileStream(Path.Combine(folder, "launcher.db"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            byte[] header = new byte[16];
            Assert(stream.Read(header, 0, header.Length) == header.Length && Encoding.ASCII.GetString(header) == "SQLite format 3\0",
                "Storage must be an actual SQLite database.");
        }

        // CSV files added after initialization must not be imported on a later startup.
        File.WriteAllText(Path.Combine(folder, "launch.csv"), @"C:\Late\app.exe,Late" + Environment.NewLine);
        Assert(OpenDatabase(folder).GetLaunchers().Count == 2, "Initialization without CSV must still mark migration complete.");
    }

    private static void TestLegacyImport()
    {
        string folder = NewFolder();
        string launchCsv = @"C:\One, Apps\app.exe,Same, Name" + Environment.NewLine + Environment.NewLine +
            @"C:\Two\app.exe,Same, Name" + Environment.NewLine;
        string settingsCsv = "autoPrompt,Disabled\nautoFind,On\ncustom,keep\n";
        File.WriteAllText(Path.Combine(folder, "launch.csv"), launchCsv);
        File.WriteAllText(Path.Combine(folder, "settings.csv"), settingsCsv);
        var database = OpenDatabase(folder);
        Assert(database.GetLaunchers().Count == 2, "Both duplicate CSV entries must be imported.");
        Assert(database.GetLaunchers()[0].FilePath == @"C:\One, Apps\app.exe" &&
            database.GetLaunchers()[0].DisplayName == "Same, Name", "Legacy commas must be preserved.");
        Assert(database.GetSettings()["autoPrompt"] == "Off" && database.GetSettings()["autoFind"] == "On",
            "Legacy settings must be preserved and Disabled normalized.");
        Assert(File.ReadAllText(Path.Combine(folder, "launch.csv")) == launchCsv &&
            File.ReadAllText(Path.Combine(folder, "settings.csv")) == settingsCsv, "Original CSV files must remain intact.");
        database.RemoveLauncher(database.GetLaunchers().First().Id);
        Assert(OpenDatabase(folder).GetLaunchers().Count == 1, "Restart must not reimport a deleted entry.");
    }

    private static void TestFailedImportCanRetry()
    {
        string folder = NewFolder();
        File.WriteAllText(Path.Combine(folder, "launch.csv"), @"C:\One\app.exe,One" + Environment.NewLine);
        File.WriteAllText(Path.Combine(folder, "settings.csv"), "autoFind,On\nbroken row\n");
        bool failed = false;
        try { OpenDatabase(folder); }
        catch (InvalidDataException) { failed = true; }
        Assert(failed, "Malformed legacy data must fail initialization.");
        File.WriteAllText(Path.Combine(folder, "settings.csv"), "autoFind,On\n");
        var database = OpenDatabase(folder);
        Assert(database.GetLaunchers().Count == 1 && database.GetSettings()["autoFind"] == "On",
            "Failed import must roll back entries and allow a successful retry without duplicates.");
    }

    private static void TestFormLoadsDatabase()
    {
        var database = OpenDatabase(NewFolder());
        var first = database.AddLauncher(@"C:\One\app.exe", "Same");
        var second = database.AddLauncher(@"C:\Two\app.exe", "Same");
        using (var form = new PorksLauncher.Form1(database))
        {
            Assert(form.Icon != null, "The form's embedded icon must load.");
            var listBox = (ListBox)form.Controls.Find("listBox1", true).Single();
            Assert(listBox.Items.Count == 2 && ((LauncherEntry)listBox.Items[0]).Id == first.Id &&
                ((LauncherEntry)listBox.Items[1]).Id == second.Id, "The form must load distinct database entries.");
            Assert(listBox.GetItemText(listBox.Items[0]) == "Same", "The list must display entry names.");
        }
    }

    private static void TestApplicationLaunchInfo()
    {
        string folder = NewFolder();
        string exe = Path.Combine(folder, "app.exe");
        string jar = Path.Combine(folder, "Java app, '工具'.JAR");
        string document = Path.Combine(folder, "document.txt");
        File.WriteAllText(exe, string.Empty);
        File.WriteAllText(jar, string.Empty);
        File.WriteAllText(document, string.Empty);
        var info = ApplicationLauncher.CreateStartInfo(exe);
        Assert(info.FileName == exe && info.UseShellExecute && info.WorkingDirectory == folder,
            "EXE files must launch directly from their own folder.");
        Assert(ApplicationLauncher.CreateStartInfo(document).UseShellExecute,
            "Other files must use their Windows file association.");
        bool missingFile = false;
        try { ApplicationLauncher.CreateStartInfo(Path.Combine(folder, "missing.jar")); }
        catch (FileNotFoundException) { missingFile = true; }
        Assert(missingFile, "A missing saved file must produce a clear error.");

        string originalJavaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        string originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            string javaHome = Path.Combine(folder, "Java Home");
            string javaBin = Path.Combine(javaHome, "bin");
            Directory.CreateDirectory(javaBin);
            string java = Path.Combine(javaBin, "java.exe");
            File.WriteAllText(java, string.Empty);
            Environment.SetEnvironmentVariable("JAVA_HOME", javaHome);
            Environment.SetEnvironmentVariable("PATH", string.Empty);
            info = ApplicationLauncher.CreateStartInfo(jar);
            Assert(info.FileName == java && info.Arguments == "-jar \"" + jar + "\"" &&
                !info.UseShellExecute && info.WorkingDirectory == folder,
                "JAR files must use JAVA_HOME, quote the path, and set the application's folder.");

            Environment.SetEnvironmentVariable("JAVA_HOME", Path.Combine(folder, "missing-java"));
            Environment.SetEnvironmentVariable("PATH", "\"" + javaBin + "\"");
            Assert(ApplicationLauncher.CreateStartInfo(jar).FileName == java,
                "Invalid JAVA_HOME must fall back to Java on PATH, including quoted paths.");
            Environment.SetEnvironmentVariable("PATH", string.Empty);
            bool missingJava = false;
            try { ApplicationLauncher.CreateStartInfo(jar); }
            catch (InvalidOperationException ex) { missingJava = ex.Message.Contains("JAVA_HOME") && ex.Message.Contains("PATH"); }
            Assert(missingJava, "Missing Java must explain how to configure the runtime.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("JAVA_HOME", originalJavaHome);
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }

        var database = OpenDatabase(folder);
        var entry = database.AddLauncher(jar, "Java app");
        database.RenameLauncher(entry.Id, "Renamed JAR");
        Assert(OpenDatabase(folder).GetLaunchers().Single().FilePath == jar,
            "JAR entries must persist through rename and restart.");
        database.RemoveLauncher(entry.Id);
        Assert(database.GetLaunchers().Count == 0, "JAR entries must support removal.");

        string legacyFolder = NewFolder();
        File.WriteAllText(Path.Combine(legacyFolder, "launch.csv"), @"C:\Java, Apps\app.JAR,Java, App" + Environment.NewLine);
        var imported = OpenDatabase(legacyFolder).GetLaunchers().Single();
        Assert(imported.FilePath == @"C:\Java, Apps\app.JAR" && imported.DisplayName == "Java, App",
            "Legacy JAR paths with commas must import correctly.");
    }

    private static void TestRealJarLaunch(string jarPath)
    {
        string fullPath = Path.GetFullPath(jarPath);
        var info = ApplicationLauncher.CreateStartInfo(fullPath);
        info.CreateNoWindow = true;
        using (var process = Process.Start(info))
        {
            Assert(process.WaitForExit(15000), "Java launch probe did not finish within 15 seconds.");
            Assert(process.ExitCode == 0, "Java launch probe must exit successfully.");
        }
        Assert(File.ReadAllText(Path.Combine(Path.GetDirectoryName(fullPath), "jar-launch-result.txt")).Trim() == "JAR launched",
            "The real JAR must execute and write its result in its own working directory.");
        Console.WriteLine("PASS: Real Java JAR launch with spaces and punctuation in its path.");
    }
}
