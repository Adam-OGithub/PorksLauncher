using System.Collections.Generic;
using System.Globalization;

namespace PorksLauncher.Classes
{
    public sealed class LauncherPreferences
    {
        public const int MinListWidth = 380;
        public const int MaxListWidth = 1600;
        public const int MinListHeight = 200;
        public const int MaxListHeight = 1200;
        public static readonly string[] ColorSchemes = { "Original", "Light", "Dark", "Forest" };

        public string ColorScheme { get; set; } = "Original";
        public int ListWidth { get; set; } = 667;
        public int ListHeight { get; set; } = 565;
        public bool ScanEnabled { get; set; }
        public bool AlphabeticalSorting { get; set; }

        public static LauncherPreferences FromSettings(Dictionary<string, string> settings)
        {
            var preferences = new LauncherPreferences();
            if (settings.TryGetValue("colorScheme", out string scheme) && System.Array.IndexOf(ColorSchemes, scheme) >= 0)
                preferences.ColorScheme = scheme;
            preferences.ListWidth = ReadSize(settings, "listWidth", preferences.ListWidth, MinListWidth, MaxListWidth);
            preferences.ListHeight = ReadSize(settings, "listHeight", preferences.ListHeight, MinListHeight, MaxListHeight);
            preferences.ScanEnabled = settings.TryGetValue("autoFind", out string scan) && scan == "On";
            preferences.AlphabeticalSorting = settings.TryGetValue("alphabeticalSorting", out string sort) && sort == "On";
            return preferences;
        }

        public Dictionary<string, string> ToSettings() => new Dictionary<string, string>
        {
            { "colorScheme", ColorScheme },
            { "listWidth", ListWidth.ToString(CultureInfo.InvariantCulture) },
            { "listHeight", ListHeight.ToString(CultureInfo.InvariantCulture) },
            { "autoFind", ScanEnabled ? "On" : "Off" },
            { "alphabeticalSorting", AlphabeticalSorting ? "On" : "Off" }
        };

        private static int ReadSize(Dictionary<string, string> settings, string name, int fallback, int minimum, int maximum)
        {
            if (!settings.TryGetValue(name, out string value) ||
                !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size)) return fallback;
            return System.Math.Max(minimum, System.Math.Min(maximum, size));
        }
    }
}
