using PorksLauncher.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PorksLauncher
{
    public partial class Form1 : Form
    {
        private readonly LauncherDatabase database;
        private readonly ProcessMonitor processMonitor;

        public Form1() : this(LauncherDatabase.CreateDefault())
        {
        }

        public Form1(LauncherDatabase database)
        {
            InitializeComponent();
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.database.Initialize();
            settings_btn.Visible = true;
            LoadList();
            processMonitor = new ProcessMonitor(listBox1, this.database);
            ApplySettings(this.database.GetSettings());
        }

        private void LoadList()
        {
            var entries = database.GetLaunchers();
            listBox1.Items.Clear();
            listBox1.Items.AddRange(entries.ToArray());
        }

        private void RunDatabaseAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to update launcher data: {ex.Message}",
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_add_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = ApplicationLauncher.FileFilter, InitialDirectory = "C:\\", RestoreDirectory = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                RunDatabaseAction(() =>
                {
                    var entry = database.AddLauncher(dialog.FileName,
                        Path.GetFileNameWithoutExtension(dialog.FileName));
                    listBox1.Items.Add(entry);
                });
            }
        }

        private void btn_rm_Click(object sender, EventArgs e)
        {
            var entry = listBox1.SelectedItem as LauncherEntry;
            if (entry == null)
            {
                MessageBox.Show("No application selected to remove!");
                return;
            }
            if (MessageBox.Show(this, $"Remove \"{entry.DisplayName}\" from your application list?\n\n" +
                "Its executable will also be added to the Denied executables list, so the scanner won't ask to add it again.\n\n" +
                "You can allow future prompts in Settings → Denied executables.", "Confirm removal",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            RunDatabaseAction(() =>
            {
                database.RemoveLauncher(entry.Id);
                listBox1.Items.Remove(entry);
            });
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBox1.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches) return;
            var entry = listBox1.Items[index] as LauncherEntry;
            if (entry == null) return;
            try
            {
                using (var process = Process.Start(ApplicationLauncher.CreateStartInfo(entry.FilePath)))
                {
                    // Disposing the process handle leaves the launched application running.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching application: {ex.Message}");
            }
        }

        private void rename_btn_Click(object sender, EventArgs e)
        {
            var entry = listBox1.SelectedItem as LauncherEntry;
            if (entry == null)
            {
                MessageBox.Show("No application selected to rename!");
                return;
            }
            string newName = ShowPromptDialog($"Enter a new name for: {entry.DisplayName}",
                $"Renaming: {entry.DisplayName}");
            if (string.IsNullOrWhiteSpace(newName)) return;
            RunDatabaseAction(() =>
            {
                database.RenameLauncher(entry.Id, newName);
                LoadList();
            });
        }

        private string ShowPromptDialog(string text, string caption)
        {
            using (var prompt = new Form
            {
                Width = 300, Height = 170, Text = caption,
                FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent
            })
            {
                var label = new Label { Left = 25, Top = 15, Text = text, Width = 250 };
                var textBox = new TextBox { Left = 25, Top = 45, Width = 250 };
                var ok = new Button { Text = "OK", Left = 25, Width = 100, Top = 80, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Left = 150, Width = 100, Top = 80, DialogResult = DialogResult.Cancel };
                prompt.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
                prompt.AcceptButton = ok;
                prompt.CancelButton = cancel;
                return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text : string.Empty;
            }
        }

        private void settings_btn_Click(object sender, EventArgs e)
        {
            processMonitor.PromptsPaused = true;
            try
            {
                RunDatabaseAction(() =>
                {
                    var settings = database.GetSettings();
                    using (var prompt = new Form
                    {
                        Width = 420, Height = 220, FormBorderStyle = FormBorderStyle.FixedDialog,
                        Text = "Settings", StartPosition = FormStartPosition.CenterParent
                    })
                    {
                        var labelAutoFind = new Label { Left = 20, Top = 20, Text = "Enable/Disable Auto Executable Scan", Width = 230 };
                        var autoFind = new Button { Text = settings["autoFind"], Left = 250, Width = 80, Top = 15 };
                        var denied = new Button { Text = "Denied executables…", Left = 20, Width = 180, Top = 60 };
                        denied.Click += (s, ev) =>
                        {
                            RunDatabaseAction(() =>
                            {
                                using (var deniedForm = new DeniedExecutablesForm(database)) deniedForm.ShowDialog(prompt);
                            });
                        };
                        var ok = new Button { Text = "OK", Left = 100, Width = 80, Top = 125, DialogResult = DialogResult.OK };
                        var cancel = new Button { Text = "Cancel", Left = 200, Width = 80, Top = 125, DialogResult = DialogResult.Cancel };
                        autoFind.Click += (s, ev) => ToggleSetting(autoFind);
                        prompt.Controls.AddRange(new Control[] { labelAutoFind, autoFind, denied, ok, cancel });
                        prompt.AcceptButton = ok;
                        prompt.CancelButton = cancel;
                        if (prompt.ShowDialog(this) != DialogResult.OK) return;
                        settings["autoFind"] = autoFind.Text;
                        database.SaveSettings(settings);
                        ApplySettings(settings);
                        MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                });
            }
            finally
            {
                processMonitor.PromptsPaused = false;
            }
        }

        private static void ToggleSetting(Button button)
        {
            button.Text = button.Text == "On" ? "Off" : "On";
        }

        private void ApplySettings(Dictionary<string, string> settings)
        {
            if (settings.TryGetValue("autoFind", out string autoFind) && autoFind == "On")
                processMonitor.StartMonitoring();
            else
                processMonitor.StopMonitoring();
        }

    }
}
