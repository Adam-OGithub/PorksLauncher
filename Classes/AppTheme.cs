using System.Drawing;
using System.Windows.Forms;

namespace PorksLauncher.Classes
{
    public static class AppTheme
    {
        public static void Apply(Control root, string scheme)
        {
            Color background, surface, foreground, button, buttonText;
            switch (scheme)
            {
                case "Light":
                    background = Color.FromArgb(240, 242, 245); surface = Color.White; foreground = Color.FromArgb(25, 30, 35);
                    button = Color.FromArgb(222, 229, 239); buttonText = foreground; break;
                case "Dark":
                    background = Color.FromArgb(28, 30, 34); surface = Color.FromArgb(42, 45, 50); foreground = Color.FromArgb(240, 242, 245);
                    button = Color.FromArgb(62, 67, 75); buttonText = foreground; break;
                case "Forest":
                    background = Color.FromArgb(24, 45, 35); surface = Color.FromArgb(34, 62, 47); foreground = Color.FromArgb(234, 244, 236);
                    button = Color.FromArgb(58, 86, 64); buttonText = foreground; break;
                default:
                    background = Color.Black; surface = Color.Cyan; foreground = Color.Black;
                    button = Color.White; buttonText = Color.Black; break;
            }
            ApplyColors(root, background, surface, foreground, button, buttonText);
        }

        private static void ApplyColors(Control control, Color background, Color surface, Color foreground, Color button, Color buttonText)
        {
            if (control is Button buttonControl)
            {
                buttonControl.UseVisualStyleBackColor = false;
                control.BackColor = button; control.ForeColor = buttonText;
            }
            else if (control is ListBox || control is ComboBox || control is TextBox || control is NumericUpDown)
            {
                control.BackColor = surface; control.ForeColor = foreground;
            }
            else
            {
                control.BackColor = background;
                control.ForeColor = background == Color.Black ? Color.White : foreground;
            }
            foreach (Control child in control.Controls) ApplyColors(child, background, surface, foreground, button, buttonText);
        }
    }
}
