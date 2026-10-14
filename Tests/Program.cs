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
            TestProcessFilteringAndDecisions();
            TestDeniedExecutablesForm();
            TestRemovalDeniesExecutable();
            TestAppearanceAndSortingPreferences();
            if (args.Length == 1) TestRealJarLaunch(args[0]);
            Console.WriteLine("PASS: Storage, migration, launching, scanner decisions, denied-executable form, themes, resizing, sorting, and settings persistence.");
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

    private static void TestProcessFilteringAndDecisions()
    {
        var policy = new ProcessCandidatePolicy(@"D:\Windows", new[] { @"C:\Program Files", @"C:\Program Files (x86)" },
            @"C:\Launcher\PorksLauncher.exe");
        Assert(!policy.ShouldPrompt("notepad", @"D:\Windows\System32\notepad.exe"), "Windows paths on any drive must be ignored.");
        Assert(!policy.ShouldPrompt("explorer", @"C:\Other\explorer.exe"), "Windows shell processes must be ignored.");
        Assert(!policy.ShouldPrompt("Notepad", @"C:\Program Files\WindowsApps\Microsoft.WindowsNotepad_1\Notepad.exe"),
            "Packaged Windows apps must be ignored.");
        Assert(!policy.ShouldPrompt("msedge", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"), "Built-in Edge must be ignored.");
        Assert(!policy.ShouldPrompt("PorksLauncher", @"C:\Launcher\PorksLauncher.exe"), "The launcher must ignore itself.");
        Assert(policy.ShouldPrompt("Discord", @"D:\WindowsTools\Discord.exe"), "A folder sharing the Windows prefix is not a Windows directory.");
        Assert(policy.ShouldPrompt("ThirdParty", @"C:\Program Files\WindowsApps\Vendor.App_1\app.exe"), "Third-party packaged apps remain eligible.");

        string databaseFolder = NewFolder();
        var database = OpenDatabase(databaseFolder);
        var applications = new[]
        {
            new LauncherEntry { DisplayName = "notepad", FilePath = @"D:\Windows\notepad.exe" },
            new LauncherEntry { DisplayName = "Discord", FilePath = @"C:\Users\Person\Apps\Discord.exe" },
            new LauncherEntry { DisplayName = "Discord", FilePath = @"c:\users\person\apps\DISCORD.EXE" },
            new LauncherEntry { DisplayName = "Steam", FilePath = @"C:\Program Files (x86)\Steam\steam.exe" },
            new LauncherEntry { DisplayName = "notepad++", FilePath = @"C:\Program Files\Notepad++\notepad++.exe" }
        };
        int prompts = 0;
        using (var list = new ListBox())
        {
            ProcessMonitor monitor = null;
            using (monitor = new ProcessMonitor(list, database, () => applications, entry =>
            {
                prompts++;
                monitor.ScanNow(); // Simulate a timer tick while the modal prompt pumps messages.
                return entry.DisplayName != "Discord";
            }, policy))
            {
                monitor.StartMonitoring();
                monitor.PromptsPaused = true;
                monitor.ScanNow();
                Assert(prompts == 0, "Opening settings must suppress scanner prompts.");
                monitor.PromptsPaused = false;
                monitor.ScanNow();
                monitor.ScanNow();
                Assert(prompts == 3 && list.Items.Count == 2, "Duplicate processes and reentrant scans must not repeat prompts.");
                monitor.StopMonitoring();
                monitor.ScanNow();
                Assert(prompts == 3, "Disabled scanning must not prompt.");
            }
        }

        database = OpenDatabase(databaseFolder);
        var denied = database.GetDeniedExecutables().Single();
        Assert(denied.IsDenied && denied.DisplayName == "Discord", "No must persist by executable path across restart.");
        database.RenameLauncher(database.GetLaunchers().First().Id, "Renamed Steam");
        using (var list = new ListBox())
        using (var monitor = new ProcessMonitor(list, database, () => applications, entry => { prompts++; return false; }, policy))
        {
            monitor.StartMonitoring();
            monitor.ScanNow();
            Assert(prompts == 3, "Restart and renaming saved entries must not cause repeated prompts.");
            denied.IsDenied = false;
            database.SaveDeniedExecutables(new[] { denied });
            monitor.ScanNow();
            Assert(prompts == 4, "Unchecking a denied executable must allow the next scan to ask again.");
            monitor.ScanNow();
            Assert(prompts == 4, "Choosing No again must restore the persisted denial.");
        }
        database.SaveDeniedExecutables(new[] { new DeniedExecutable
        {
            FilePath = denied.FilePath.ToUpperInvariant(), DisplayName = "Discord", IsDenied = false
        } });
        Assert(database.GetDeniedExecutables().Count == 1 && !database.GetDeniedExecutables()[0].IsDenied,
            "Case variants must update the same executable decision.");
    }

    private static void TestDeniedExecutablesForm()
    {
        var database = OpenDatabase(NewFolder());
        database.SaveDeniedExecutables(new[] { new DeniedExecutable
        {
            FilePath = @"C:\Apps\Discord.exe", DisplayName = "Discord", IsDenied = true
        } });
        using (var form = new PorksLauncher.DeniedExecutablesForm(database))
        {
            var list = (CheckedListBox)form.Controls.Find("executableList", true).Single();
            Assert(list.Items.Count == 1 && list.GetItemChecked(0), "Denied executable must appear checked in the management form.");
            list.SetItemChecked(0, false);
            typeof(PorksLauncher.DeniedExecutablesForm).GetMethod("SaveDecisions",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(form, null);
            Assert(form.DialogResult == DialogResult.OK && !database.GetDeniedExecutables().Single().IsDenied,
                "Saving an unchecked entry must enable future prompts.");
        }
        using (var form = new PorksLauncher.DeniedExecutablesForm(database))
        {
            var list = (CheckedListBox)form.Controls.Find("executableList", true).Single();
            Assert(!list.GetItemChecked(0), "Allowed history must remain unchecked when the form reopens.");
            list.SetItemChecked(0, true);
            // Closing without Save must not persist a checkbox change.
        }
        Assert(!database.GetDeniedExecutables().Single().IsDenied, "Cancel must leave existing decisions unchanged.");
    }

    private static void TestRemovalDeniesExecutable()
    {
        string folder = NewFolder();
        var database = OpenDatabase(folder);
        var removed = database.AddLauncher(@"C:\Apps\Discord.exe", "Discord");
        var retained = database.AddLauncher(@"C:\Other\Discord.exe", "Discord");
        database.SaveDeniedExecutables(new[] { new DeniedExecutable
        {
            FilePath = removed.FilePath, DisplayName = "Old name", IsDenied = false
        } });
        database.RenameLauncher(removed.Id, "My Discord");
        database.RemoveLauncher(removed.Id);
        database = OpenDatabase(folder);
        var denial = database.GetDeniedExecutables().Single();
        Assert(denial.FilePath == removed.FilePath && denial.DisplayName == "My Discord" && denial.IsDenied,
            "Removal must persist a checked denial and update existing allowed history with the current display name.");
        Assert(database.GetLaunchers().Single().Id == retained.Id, "Removal must leave entries with the same name intact.");
        database.RemoveLauncher(removed.Id);
        Assert(database.GetDeniedExecutables().Count == 1, "Repeated removal must not duplicate denial history.");

        int prompts = 0;
        using (var list = new ListBox())
        using (var monitor = new ProcessMonitor(list, database, () => new[] { removed }, entry => { prompts++; return false; }))
        {
            monitor.StartMonitoring();
            monitor.ScanNow();
            Assert(prompts == 0, "The scanner must not suggest a removed application after restart.");
            denial.IsDenied = false;
            database.SaveDeniedExecutables(new[] { denial });
            monitor.ScanNow();
            Assert(prompts == 1, "Unchecking a removed executable must allow another prompt.");
        }

        string failedFolder = NewFolder();
        var failedDatabase = OpenDatabase(failedFolder);
        var entryToKeep = failedDatabase.AddLauncher(@"C:\Apps\Steam.exe", "Steam");
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            "Data Source=" + Path.Combine(failedFolder, "launcher.db")))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TRIGGER FailDenial BEFORE INSERT ON DeniedExecutables BEGIN SELECT RAISE(ABORT, 'Simulated denial failure'); END;";
                command.ExecuteNonQuery();
            }
        }
        bool failed = false;
        try { failedDatabase.RemoveLauncher(entryToKeep.Id); }
        catch (Microsoft.Data.Sqlite.SqliteException) { failed = true; }
        Assert(failed && failedDatabase.GetLaunchers().Single().Id == entryToKeep.Id &&
            failedDatabase.GetDeniedExecutables().Count == 0, "A failed denial save must leave the launcher entry intact.");
    }

    private static void TestAppearanceAndSortingPreferences()
    {
        string folder = NewFolder();
        var database = OpenDatabase(folder);
        var defaults = LauncherPreferences.FromSettings(database.GetSettings());
        Assert(defaults.ColorScheme == "Original" && defaults.ListWidth == 667 && defaults.ListHeight == 565 &&
            !defaults.ScanEnabled && !defaults.AlphabeticalSorting, "New preferences must have safe defaults.");
        var invalid = LauncherPreferences.FromSettings(new Dictionary<string, string>
        {
            { "colorScheme", "unknown" }, { "listWidth", "-10" }, { "listHeight", "bad" }
        });
        Assert(invalid.ColorScheme == "Original" && invalid.ListWidth == LauncherPreferences.MinListWidth && invalid.ListHeight == 565,
            "Invalid saved preferences must fall back or clamp to usable values.");
        var first = database.AddLauncher(@"C:\Apps\Charlie.exe", "Charlie");
        database.AddLauncher(@"C:\Apps\Alpha.exe", "Alpha");
        var preferences = new LauncherPreferences
        {
            ColorScheme = "Dark", ListWidth = 720, ListHeight = 420, ScanEnabled = true, AlphabeticalSorting = true
        };
        using (var form = new PorksLauncher.Form1(database))
        {
            var list = (ListBox)form.Controls.Find("listBox1", true).Single();
            list.SelectedIndex = 0;
            form.UpdatePreferences(preferences);
            Assert(form.IsScanning && list.Sorted && list.Width == 720 && list.Height == 420,
                "Settings must enable scanning, sorting, and the requested list size immediately.");
            Assert(((LauncherEntry)list.Items[0]).DisplayName == "Alpha" && ((LauncherEntry)list.SelectedItem).Id == first.Id,
                "Sorting must change display order while preserving the selected executable.");
            Assert(form.BackColor.R == 28 && list.BackColor.R == 42 && list.ForeColor.R == 240,
                "Dark theme must apply readable background and text colors.");
            using (var scanner = new ProcessMonitor(list, database,
                () => new[] { new LauncherEntry { FilePath = @"C:\Apps\Bravo.exe", DisplayName = "Bravo" } }, entry => true))
            {
                scanner.StartMonitoring();
                scanner.ScanNow();
            }
            Assert(((LauncherEntry)list.Items[1]).DisplayName == "Bravo", "Scanner additions must stay alphabetically sorted.");
            database.RenameLauncher(first.Id, "Aardvark");
            form.UpdatePreferences(preferences);
            Assert(((LauncherEntry)list.Items[0]).Id == first.Id, "Renamed entries must move to their new alphabetical position.");
            preferences.ScanEnabled = false;
            preferences.AlphabeticalSorting = false;
            preferences.ColorScheme = "Light";
            form.UpdatePreferences(preferences);
            Assert(!form.IsScanning && !list.Sorted && ((LauncherEntry)list.Items[0]).Id == first.Id &&
                list.BackColor == System.Drawing.Color.White, "Disabling scan and sorting must restore insertion order and apply the new theme.");

            form.ClientSize = new System.Drawing.Size(836, 593);
            typeof(PorksLauncher.Form1).GetMethod("OnResizeEnd",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(form, new object[] { EventArgs.Empty });
            Assert(list.Width == 800 && list.Height == 500, "Dragging the main window must resize the list with margins.");
            foreach (Control button in form.Controls.OfType<Button>())
                Assert(button.Right <= form.ClientSize.Width && button.Bottom < list.Top, "Toolbar buttons must fit above the resized list.");
        }
        using (var reopened = new PorksLauncher.Form1(OpenDatabase(folder)))
        {
            var list = (ListBox)reopened.Controls.Find("listBox1", true).Single();
            Assert(list.Width == 800 && list.Height == 500 && !list.Sorted && !reopened.IsScanning && list.BackColor == System.Drawing.Color.White,
                "Appearance, resize, sorting, and scan choices must persist across restart.");
        }
        using (var settings = new PorksLauncher.SettingsForm(database, LauncherPreferences.FromSettings(database.GetSettings())))
        {
            var scheme = (ComboBox)settings.Controls.Find("colorScheme", true).Single();
            Assert(scheme.Items.Count == 4 && (string)scheme.SelectedItem == "Light", "Settings must load the saved theme and provide four schemes.");
            scheme.SelectedItem = "Forest";
            Assert(settings.BackColor.G == 45 && database.GetSettings()["colorScheme"] == "Light",
                "Theme preview and Cancel must not write settings before Save.");
            ((NumericUpDown)settings.Controls.Find("listWidth", true).Single()).Value = 900;
            ((NumericUpDown)settings.Controls.Find("listHeight", true).Single()).Value = 600;
            ((CheckBox)settings.Controls.Find("scanEnabled", true).Single()).Checked = true;
            ((CheckBox)settings.Controls.Find("alphabeticalSorting", true).Single()).Checked = true;
            var save = (Button)settings.Controls.Find("saveButton", true).Single();
            typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(save, new object[] { EventArgs.Empty });
            Assert(settings.SelectedPreferences.ColorScheme == "Forest" && settings.SelectedPreferences.ListWidth == 900 &&
                settings.SelectedPreferences.ListHeight == 600 && settings.SelectedPreferences.ScanEnabled && settings.SelectedPreferences.AlphabeticalSorting,
                "The Settings Save button must collect all five preferences.");
        }
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
