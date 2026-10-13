using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace PorksLauncher.Classes
{
    public sealed class LauncherDatabase
    {
        private readonly string databasePath;

        public LauncherDatabase(string databasePath)
        {
            this.databasePath = Path.GetFullPath(databasePath);
        }

        public static LauncherDatabase CreateDefault()
        {
            return new LauncherDatabase(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "pork_launcher", "launcher.db"));
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString());
            connection.Open();
            return connection;
        }

        public void Initialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Launchers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FilePath TEXT NOT NULL,
                        DisplayName TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Settings (
                        Name TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS DeniedExecutables (
                        FilePath TEXT PRIMARY KEY COLLATE NOCASE,
                        DisplayName TEXT NOT NULL,
                        IsDenied INTEGER NOT NULL CHECK (IsDenied IN (0, 1))
                    );";
                command.ExecuteNonQuery();

                command.CommandText = "PRAGMA user_version;";
                if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                {
                    ImportLegacyFiles(connection, transaction);
                    command.CommandText = "PRAGMA user_version = 1;";
                    command.ExecuteNonQuery();
                }

                command.CommandText = @"
                    INSERT OR IGNORE INTO Settings (Name, Value) VALUES ('autoPrompt', 'Off');
                    INSERT OR IGNORE INTO Settings (Name, Value) VALUES ('autoFind', 'Off');";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        private void ImportLegacyFiles(SqliteConnection connection, SqliteTransaction transaction)
        {
            string folder = Path.GetDirectoryName(databasePath);
            string launchFile = Path.Combine(folder, "launch.csv");
            if (File.Exists(launchFile))
            {
                foreach (string line in File.ReadLines(launchFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // Old rows were unquoted. A known file suffix identifies the
                    // separator even when the path or display name contains a comma.
                    int executableSuffix = line.LastIndexOf(".exe,", StringComparison.OrdinalIgnoreCase);
                    int jarSuffix = line.LastIndexOf(".jar,", StringComparison.OrdinalIgnoreCase);
                    int fileSuffix = Math.Max(executableSuffix, jarSuffix);
                    int separator = fileSuffix >= 0 ? fileSuffix + 4 : line.IndexOf(',');
                    if (separator <= 0 || separator == line.Length - 1)
                        throw new InvalidDataException("Invalid launcher row in launch.csv: " + line);

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO Launchers (FilePath, DisplayName) VALUES ($path, $name);";
                        command.Parameters.AddWithValue("$path", line.Substring(0, separator));
                        command.Parameters.AddWithValue("$name", line.Substring(separator + 1));
                        command.ExecuteNonQuery();
                    }
                }
            }

            string settingsFile = Path.Combine(folder, "settings.csv");
            if (!File.Exists(settingsFile)) return;
            foreach (string line in File.ReadLines(settingsFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                int separator = line.IndexOf(',');
                if (separator <= 0 || separator == line.Length - 1)
                    throw new InvalidDataException("Invalid settings row in settings.csv: " + line);

                string name = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if ((name == "autoPrompt" || name == "autoFind") && value == "Disabled")
                    value = "Off";
                SetSetting(connection, transaction, name, value);
            }
        }

        public List<LauncherEntry> GetLaunchers()
        {
            var entries = new List<LauncherEntry>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id, FilePath, DisplayName FROM Launchers ORDER BY Id;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        entries.Add(new LauncherEntry
                        {
                            Id = reader.GetInt64(0),
                            FilePath = reader.GetString(1),
                            DisplayName = reader.GetString(2)
                        });
                }
            }
            return entries;
        }

        public LauncherEntry AddLauncher(string filePath, string displayName)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Launchers (FilePath, DisplayName) VALUES ($path, $name);
                    SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("$path", filePath);
                command.Parameters.AddWithValue("$name", displayName);
                return new LauncherEntry
                {
                    Id = Convert.ToInt64(command.ExecuteScalar()),
                    FilePath = filePath,
                    DisplayName = displayName
                };
            }
        }

        public void RemoveLauncher(long id)
        {
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            using (var command = connection.CreateCommand())
            {
                // Save the denial and remove the entry together, so a failed save
                // cannot leave a removed application eligible for another prompt.
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO DeniedExecutables (FilePath, DisplayName, IsDenied)
                    SELECT FilePath, DisplayName, 1 FROM Launchers WHERE Id = $id
                    ON CONFLICT(FilePath) DO UPDATE SET DisplayName = excluded.DisplayName, IsDenied = 1;
                    DELETE FROM Launchers WHERE Id = $id;";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void RenameLauncher(long id, string displayName)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Launchers SET DisplayName = $name WHERE Id = $id;";
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$name", displayName);
                command.ExecuteNonQuery();
            }
        }

        public List<DeniedExecutable> GetDeniedExecutables()
        {
            var entries = new List<DeniedExecutable>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT FilePath, DisplayName, IsDenied FROM DeniedExecutables ORDER BY DisplayName COLLATE NOCASE, FilePath;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) entries.Add(new DeniedExecutable
                    {
                        FilePath = reader.GetString(0), DisplayName = reader.GetString(1), IsDenied = reader.GetInt32(2) != 0
                    });
                }
            }
            return entries;
        }

        public void SaveDeniedExecutables(IEnumerable<DeniedExecutable> entries)
        {
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var entry in entries)
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO DeniedExecutables (FilePath, DisplayName, IsDenied) VALUES ($path, $name, $denied)
                        ON CONFLICT(FilePath) DO UPDATE SET DisplayName = excluded.DisplayName, IsDenied = excluded.IsDenied;";
                    command.Parameters.AddWithValue("$path", Path.GetFullPath(entry.FilePath));
                    command.Parameters.AddWithValue("$name", entry.DisplayName);
                    command.Parameters.AddWithValue("$denied", entry.IsDenied ? 1 : 0);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public Dictionary<string, string> GetSettings()
        {
            var settings = new Dictionary<string, string>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Name, Value FROM Settings;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) settings[reader.GetString(0)] = reader.GetString(1);
                }
            }
            return settings;
        }

        public void SaveSettings(Dictionary<string, string> settings)
        {
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var setting in settings)
                    SetSetting(connection, transaction, setting.Key, setting.Value);
                transaction.Commit();
            }
        }

        private static void SetSetting(SqliteConnection connection, SqliteTransaction transaction,
            string name, string value)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Settings (Name, Value) VALUES ($name, $value)
                    ON CONFLICT(Name) DO UPDATE SET Value = excluded.Value;";
                command.Parameters.AddWithValue("$name", name);
                command.Parameters.AddWithValue("$value", value);
                command.ExecuteNonQuery();
            }
        }
    }
}
