namespace PorksLauncher.Classes
{
    public sealed class DeniedExecutable
    {
        public string FilePath { get; set; }
        public string DisplayName { get; set; }
        public bool IsDenied { get; set; }

        public override string ToString() => DisplayName + " — " + FilePath;
    }
}
