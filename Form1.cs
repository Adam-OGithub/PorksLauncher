using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PorksLauncher
{
    public partial class Form1 : Form
    {
        public static class Functions
        {
            private static readonly string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            private static readonly string AppFolder = Path.Combine(AppDataFolder, "pork_launcher");

            public static string GetCsvFilePath(string csvName) => Path.Combine(AppFolder, csvName);

            public static void EnsureAppFilesExist()
            {
                if (!Directory.Exists(AppFolder))
                    Directory.CreateDirectory(AppFolder);

                var filesToCreate = new[] { "launch.csv", "settings.csv" };
                foreach (var file in filesToCreate)
                {
                    string filePath = GetCsvFilePath(file);
                    if (!File.Exists(filePath))
                    {
                        File.Create(filePath).Dispose();
                        if (file == "settings.csv")
                        {
                            File.AppendAllLines(filePath, new[] { "autoPrompt,Disabled", "autoFind,Disabled" });
                        }
                    }
                }
            }

            public static string[] GetCsvInfo(string csvFile, string searchText)
            {
                if (!File.Exists(csvFile))
                    return new string[0];

                return File.ReadLines(csvFile)
                           .Select(line => line.Split(','))
                           .Where(parts => parts.Length > 1 && parts[1] == searchText)
                           .Select(parts => new string[] { parts[1], parts[0] })
                           .FirstOrDefault() ?? new string[0];
            }

            public static bool LoadList(bool reload, ListBox listBox)
            {
                if (reload) listBox.Items.Clear();
                string csvFile = GetCsvFilePath("launch.csv");

                if (!File.Exists(csvFile)) return false;

                listBox.Items.AddRange(File.ReadLines(csvFile)
                                           .Where(line => !string.IsNullOrWhiteSpace(line))
                                           .Select(line => line.Split(',')[1])
                                           .ToArray());
                return reload;
            }

            public static bool RemoveEntryFromCsv(string csvFile, string searchText)
            {
                if (!File.Exists(csvFile))
                    return false;

                var newLines = File.ReadLines(csvFile).Where(line => !line.Contains(searchText)).ToList();
                File.WriteAllLines(csvFile, newLines);
                return true;
            }

            public static string ShowPromptDialog(string text, string caption)
            {
                using (var prompt = new Form { Width = 300, Height = 150, Text = caption, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterScreen })
                {
                    var label = new Label { Left = 50, Top = 20, Text = text, Width = 200 };
                    var textBox = new TextBox { Left = 50, Top = 50, Width = 200 };
                    var okButton = new Button { Text = "OK", Left = 50, Width = 100, Top = 70, DialogResult = DialogResult.OK };
                    var cancelButton = new Button { Text = "Cancel", Left = 150, Width = 100, Top = 70, DialogResult = DialogResult.Cancel };

                    prompt.Controls.Add(label);
                    prompt.Controls.Add(textBox);
                    prompt.Controls.Add(okButton);
                    prompt.Controls.Add(cancelButton);
                    prompt.AcceptButton = okButton;

                    return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
                }
            }

            public static string GetAppDataFolder()
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // Roaming
            }

            public static string GetAppFolder(string appDataFolder)
            {
                return Path.Combine(appDataFolder, "pork_launcher");
            }

            public static string GetcsvFile(string csvName)
            {
                string appFolder = GetAppFolder(GetAppDataFolder());
                return Path.Combine(appFolder, csvName);
            }

            public static Dictionary<string, string> GetCsvContent(string filePath)
            {
                Dictionary<string, string> settings = new Dictionary<string, string>();

                if (!File.Exists(filePath))
                {
                    return settings;
                }

                foreach (var line in File.ReadAllLines(filePath))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        settings[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                return settings;
            }

            public static void SaveCsvContent(string filePath, Dictionary<string, string> settings)
            {
                List<string> lines = settings.Select(kvp => $"{kvp.Key},{kvp.Value}").ToList();
                File.WriteAllLines(filePath, lines);
            }


        }

        private static class Globals
        {
            public static int Counter { get; set; }
            public static string LastValue { get; set; } = "";
        }

        public Form1()
        {
            InitializeComponent();
            Functions.EnsureAppFilesExist();
            settings_btn.Visible = true;
            Functions.LoadList(false, listBox1);
        }

        private void btn_add_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog { Filter = "Executables (*.exe)|*.exe", InitialDirectory = "C:\\", RestoreDirectory = true })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string filePath = openFileDialog.FileName;
                string executableName = Path.GetFileNameWithoutExtension(filePath);
                string csvFile = Functions.GetCsvFilePath("launch.csv");

                File.AppendAllText(csvFile, $"{filePath},{executableName}{Environment.NewLine}");
                listBox1.Items.Add(executableName);
            }
        }

        private void btn_rm_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                MessageBox.Show("No executable selected to remove!");
                return;
            }

            string selectedText = listBox1.GetItemText(listBox1.SelectedItem);
            string csvFile = Functions.GetCsvFilePath("launch.csv");

            if (Functions.GetCsvInfo(csvFile, selectedText).Length > 1)
            {
                if (MessageBox.Show($"Do you want to remove {selectedText}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Functions.RemoveEntryFromCsv(csvFile, selectedText);
                    listBox1.Items.Remove(selectedText);
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedText = listBox1.GetItemText(listBox1.SelectedItem);

            if (Globals.Counter >= 1 && Globals.LastValue == selectedText)
            {
                Globals.Counter = 0;
                string csvFile = Functions.GetCsvFilePath("launch.csv");
                var fileInfo = Functions.GetCsvInfo(csvFile, selectedText);

                if (fileInfo.Length > 1)
                {
                    try
                    {
                        Process.Start(fileInfo[1]);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error launching application: {ex.Message}");
                    }
                }
            }

            Globals.LastValue = selectedText;
            Globals.Counter++;
        }

        private void rename_btn_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                MessageBox.Show("No executable selected to rename!");
                return;
            }

            string selectedText = listBox1.GetItemText(listBox1.SelectedItem);
            string csvFile = Functions.GetCsvFilePath("launch.csv");
            var fileInfo = Functions.GetCsvInfo(csvFile, selectedText);

            if (fileInfo.Length > 1)
            {
                string newName = Functions.ShowPromptDialog($"Enter a new name for: {selectedText}", $"Renaming: {selectedText}");
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    var lines = File.ReadAllLines(csvFile);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(',');
                        if (parts.Length > 1 && parts[1] == selectedText)
                        {
                            lines[i] = $"{parts[0]},{newName}";
                        }
                    }

                    File.WriteAllLines(csvFile, lines);
                    Functions.LoadList(true, listBox1);
                }
            }
        }


        private void settings_btn_Click(object sender, EventArgs e)
        {
            string csvFile = Functions.GetcsvFile("settings.csv");
            Dictionary<string, string> settings = Functions.GetCsvContent(csvFile);

            string autoPromptBtnStatus = settings.ContainsKey("autoPrompt") ? settings["autoPrompt"] : "Off";
            string autoFindBtnStatus = settings.ContainsKey("autoFind") ? settings["autoFind"] : "Off";

            Form prompt = new Form()
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Settings",
                StartPosition = FormStartPosition.CenterScreen
            };

            Label labelAutoPrompt = new Label() { Left = 20, Top = 20, Text = "Enable/Disable Auto Prompt", Width = 200 };
            Label labelAutoFind = new Label() { Left = 20, Top = 60, Text = "Enable/Disable Auto Executable Scan", Width = 250 };

            Button autoPrompt_btn = new Button() { Text = autoPromptBtnStatus, Left = 250, Width = 80, Top = 15 };
            Button autoFind_btn = new Button() { Text = autoFindBtnStatus, Left = 250, Width = 80, Top = 55 };
            Button confirmation = new Button() { Text = "OK", Left = 100, Width = 80, Top = 120, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 200, Width = 80, Top = 120, DialogResult = DialogResult.Cancel };

            autoPrompt_btn.Click += (s, ev) => ToggleSetting(autoPrompt_btn);
            autoFind_btn.Click += (s, ev) => ToggleSetting(autoFind_btn);
            confirmation.Click += (s, ev) => SaveSettings(csvFile, autoPrompt_btn.Text, autoFind_btn.Text);

            prompt.Controls.Add(labelAutoPrompt);
            prompt.Controls.Add(autoPrompt_btn);
            prompt.Controls.Add(labelAutoFind);
            prompt.Controls.Add(autoFind_btn);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);

            prompt.ShowDialog();
        }

        private void ToggleSetting(Button btn)
        {
            btn.Text = (btn.Text == "On") ? "Off" : "On";
        }

        private void SaveSettings(string filePath, string autoPromptStatus, string autoFindStatus)
        {
            Dictionary<string, string> settings = new Dictionary<string, string>
        {
            { "autoPrompt", autoPromptStatus },
            { "autoFind", autoFindStatus }
        };

            Functions.SaveCsvContent(filePath, settings);
            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}

