using PorksLauncher.Classes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PorksLauncher
{
    public sealed class DeniedExecutablesForm : Form
    {
        private readonly LauncherDatabase database;
        private readonly CheckedListBox executableList;

        public DeniedExecutablesForm(LauncherDatabase database)
        {
            this.database = database;
            Text = "Denied executables";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 420);
            MinimumSize = new Size(520, 300);
            MaximizeBox = false;
            MinimizeBox = false;

            var instructions = new Label
            {
                Text = "Checked executables will not prompt. Uncheck an entry and click Save to allow it to ask again.",
                Dock = DockStyle.Top, Height = 46, Padding = new Padding(12, 10, 12, 0)
            };
            executableList = new CheckedListBox
            {
                Name = "executableList", Dock = DockStyle.Fill, CheckOnClick = true,
                HorizontalScrollbar = true, IntegralHeight = false
            };
            foreach (var entry in database.GetDeniedExecutables()) executableList.Items.Add(entry, entry.IsDenied);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var save = new Button { Name = "saveButton", Text = "Save", AutoSize = true };
            save.Click += (sender, e) => SaveDecisions();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            Controls.Add(executableList);
            Controls.Add(instructions);
            Controls.Add(buttons);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void SaveDecisions()
        {
            var entries = new System.Collections.Generic.List<DeniedExecutable>();
            for (int i = 0; i < executableList.Items.Count; i++)
            {
                var entry = (DeniedExecutable)executableList.Items[i];
                entries.Add(new DeniedExecutable
                {
                    FilePath = entry.FilePath, DisplayName = entry.DisplayName, IsDenied = executableList.GetItemChecked(i)
                });
            }
            try
            {
                database.SaveDeniedExecutables(entries);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to save denied executables: {ex.Message}", "Database error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
