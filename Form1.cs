using PorksLauncher.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            ApplyPreferences(LauncherPreferences.FromSettings(this.database.GetSettings()));
        }

        private void LoadList()
        {
            var entries = database.GetLaunchers();
            long? selectedId = (listBox1.SelectedItem as LauncherEntry)?.Id;
            listBox1.BeginUpdate();
            try
            {
                listBox1.Items.Clear();
                listBox1.Items.AddRange(entries.ToArray());
                foreach (LauncherEntry entry in listBox1.Items)
                    if (entry.Id == selectedId) { listBox1.SelectedItem = entry; break; }
            }
            finally { listBox1.EndUpdate(); }
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
                    var preferences = LauncherPreferences.FromSettings(database.GetSettings());
                    using (var prompt = new SettingsForm(database, preferences))
                    {
                        if (prompt.ShowDialog(this) != DialogResult.OK) return;
                        UpdatePreferences(prompt.SelectedPreferences);
                    }
                });
            }
            finally
            {
                processMonitor.PromptsPaused = false;
            }
        }

        public bool IsScanning => processMonitor != null && processMonitor.IsMonitoring;

        public void UpdatePreferences(LauncherPreferences preferences)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));
            var validated = LauncherPreferences.FromSettings(preferences.ToSettings());
            database.SaveSettings(validated.ToSettings());
            ApplyPreferences(validated);
        }

        private void ApplyPreferences(LauncherPreferences preferences)
        {
            AppTheme.Apply(this, preferences.ColorScheme);
            if (preferences.ColorScheme == "Original")
            {
                btn_add.ForeColor = Color.ForestGreen;
                btn_rm.ForeColor = Color.Firebrick;
                rename_btn.ForeColor = Color.Blue;
            }
            MinimumSize = SizeFromClientSize(new Size(LauncherPreferences.MinListWidth + 36, LauncherPreferences.MinListHeight + 93));
            MaximumSize = SizeFromClientSize(new Size(LauncherPreferences.MaxListWidth + 36, LauncherPreferences.MaxListHeight + 93));
            listBox1.IntegralHeight = false;
            ClientSize = new Size(preferences.ListWidth + 36, preferences.ListHeight + 93);
            LayoutMainControls();
            listBox1.Sorted = preferences.AlphabeticalSorting;
            LoadList();
            if (preferences.ScanEnabled) processMonitor.StartMonitoring();
            else processMonitor.StopMonitoring();
        }

        private void LayoutMainControls()
        {
            if (listBox1 == null || settings_btn == null) return;
            var buttons = new[] { btn_add, btn_rm, rename_btn, settings_btn };
            int buttonWidth = (ClientSize.Width - 36 - 36) / buttons.Length;
            for (int index = 0; index < buttons.Length; index++)
                buttons[index].SetBounds(18 + index * (buttonWidth + 12), 18, buttonWidth, 35);
            listBox1.SetBounds(18, 75, Math.Max(1, ClientSize.Width - 36), Math.Max(1, ClientSize.Height - 93));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutMainControls();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            if (database == null) return;
            RunDatabaseAction(() =>
            {
                var preferences = LauncherPreferences.FromSettings(database.GetSettings());
                preferences.ListWidth = listBox1.Width;
                preferences.ListHeight = listBox1.Height;
                database.SaveSettings(preferences.ToSettings());
            });
        }
    }
}
