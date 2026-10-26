using System;

namespace FocusBlocker.Models
{
    /// <summary>Represents one application the user has chosen to block.</summary>
    public class BlockedApp
    {
        /// <summary>Process name WITHOUT .exe (e.g. "chrome") - matched against live processes.</summary>
        public string ProcessName { get; set; }

        /// <summary>Friendly name shown in the UI (e.g. "Google Chrome").</summary>
        public string DisplayName { get; set; }

        public BlockedApp() { }

        public BlockedApp(string processName, string displayName = null)
        {
            ProcessName = processName;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? processName : displayName;
        }

        public bool Matches(string liveProcessName)
        {
            if (string.IsNullOrWhiteSpace(liveProcessName) || string.IsNullOrWhiteSpace(ProcessName))
                return false;
            return string.Equals(ProcessName.Trim(), liveProcessName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => DisplayName;
    }
}