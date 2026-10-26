namespace FocusBlocker.Models
{
    /// <summary>One entry read from the Windows installed-programs registry keys.</summary>
    public class InstalledProgram
    {
        public string DisplayName { get; set; }
        public string InstallLocation { get; set; }
        public string ExecutablePath { get; set; }

        /// <summary>Best-guess process name (no .exe) resolved from the install info, used for blocking.</summary>
        public string GuessedProcessName =>
            string.IsNullOrWhiteSpace(ExecutablePath)
                ? null
                : System.IO.Path.GetFileNameWithoutExtension(ExecutablePath);

        public override string ToString() => DisplayName;
    }
}