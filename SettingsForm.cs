using PorksLauncher.Classes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PorksLauncher
{
    public sealed class SettingsForm : Form
    {
        private readonly ComboBox colorScheme;
        private readonly NumericUpDown listWidth;
        private readonly NumericUpDown listHeight;
        private readonly CheckBox scanEnabled;
        private readonly CheckBox alphabeticalSorting;
        public LauncherPreferences SelectedPreferences { get; private set; }

        public SettingsForm(LauncherDatabase database, LauncherPreferences preferences)
        {
            Text = "Settings";
            ClientSize = new Size(430, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var schemeLabel = new Label { Text = "Color scheme", Left = 20, Top = 25, AutoSize = true };
            colorScheme = new ComboBox { Name = "colorScheme", Left = 230, Top = 20, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            colorScheme.Items.AddRange(LauncherPreferences.ColorSchemes);
            colorScheme.SelectedItem = preferences.ColorScheme;
            var widthLabel = new Label { Text = "List width (pixels)", Left = 20, Top = 70, AutoSize = true };
            var heightLabel = new Label { Text = "List height (pixels)", Left = 20, Top = 110, AutoSize = true };
            listWidth = new NumericUpDown { Name = "listWidth", Left = 230, Top = 65, Width = 170,
                Minimum = LauncherPreferences.MinListWidth, Maximum = LauncherPreferences.MaxListWidth, Value = preferences.ListWidth, Increment = 20 };
            listHeight = new NumericUpDown { Name = "listHeight", Left = 230, Top = 105, Width = 170,
                Minimum = LauncherPreferences.MinListHeight, Maximum = LauncherPreferences.MaxListHeight, Value = preferences.ListHeight, Increment = 20 };
            var resizeHint = new Label { Text = "You can also drag the main window border to resize the list.", Left = 20, Top = 145, Width = 380, Height = 35 };
            scanEnabled = new CheckBox { Name = "scanEnabled", Text = "Enable automatic executable scan", Left = 20, Top = 190,
                AutoSize = true, Checked = preferences.ScanEnabled };
            alphabeticalSorting = new CheckBox { Name = "alphabeticalSorting", Text = "Sort applications alphabetically (A–Z)", Left = 20, Top = 225,
                AutoSize = true, Checked = preferences.AlphabeticalSorting };
            var denied = new Button { Text = "Denied executables…", Left = 20, Top = 270, Width = 180 };
            denied.Click += (sender, e) =>
            {
                try
                {
                    using (var form = new DeniedExecutablesForm(database))
                    {
                        AppTheme.Apply(form, (string)colorScheme.SelectedItem);
                        form.ShowDialog(this);
                    }
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Unable to open denied executables", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            var save = new Button { Name = "saveButton", Text = "Save", Left = 230, Top = 310, Width = 80, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 320, Top = 310, Width = 80, DialogResult = DialogResult.Cancel };
            save.Click += (sender, e) => SelectedPreferences = new LauncherPreferences
            {
                ColorScheme = (string)colorScheme.SelectedItem, ListWidth = (int)listWidth.Value, ListHeight = (int)listHeight.Value,
                ScanEnabled = scanEnabled.Checked, AlphabeticalSorting = alphabeticalSorting.Checked
            };
            Controls.AddRange(new Control[] { schemeLabel, colorScheme, widthLabel, heightLabel, listWidth, listHeight, resizeHint,
                scanEnabled, alphabeticalSorting, denied, save, cancel });
            AcceptButton = save;
            CancelButton = cancel;
            colorScheme.SelectedIndexChanged += (sender, e) => AppTheme.Apply(this, (string)colorScheme.SelectedItem);
            AppTheme.Apply(this, preferences.ColorScheme);
        }
    }
}
