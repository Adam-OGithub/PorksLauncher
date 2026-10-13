namespace PorksLauncher.Classes
{
    public sealed class LauncherEntry
    {
        public long Id { get; set; }
        public string FilePath { get; set; }
        public string DisplayName { get; set; }

        public override string ToString() => DisplayName;
    }
}
