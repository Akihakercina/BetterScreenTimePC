using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusBlocker.Services
{
    /// <summary>
    /// Simulates pressing F5, but only when a known browser is the foreground
    /// window - used to force a stale/blocked page to reload after a website
    /// challenge is solved/given up, or after a temporary grant expires.
    ///
    /// This is a best-effort approximation, not precise targeting: simulated
    /// input goes to whatever window currently has focus, not to a specific
    /// tab or window we choose. If multiple browser tabs are open, only the
    /// currently active one gets refreshed. The browser-process check exists
    /// so this never fires F5 into an unrelated app (e.g. an IDE, where F5
    /// often means "start debugging").
    /// </summary>
    public static class BrowserRefreshHelper
    {
        private const byte VK_F5 = 0x74;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private static readonly HashSet<string> KnownBrowserProcessNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "chrome", "firefox", "msedge", "opera", "brave", "iexplore", "vivaldi"
            };

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// Call this BEFORE showing any of our own windows (e.g. right when a
        /// challenge is triggered, before ChallengeForm opens) - captures which
        /// window was in the foreground at that moment, if it's a known browser.
        /// Checking again AFTER our dialog closes doesn't work reliably: closing
        /// a modal dialog typically hands focus back to its own owner (our
        /// MainForm), not to whatever was active before the dialog stole focus.
        /// </summary>
        public static IntPtr CaptureForegroundBrowserWindow()
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return IntPtr.Zero;

                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == 0) return IntPtr.Zero;

                using (var proc = Process.GetProcessById((int)pid))
                {
                    return KnownBrowserProcessNames.Contains(proc.ProcessName) ? hWnd : IntPtr.Zero;
                }
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// If the foreground window belongs to a known browser, simulates F5.
        /// Otherwise does nothing. Never throws - this is a best-effort nicety,
        /// not something the rest of the app should depend on succeeding. Use
        /// this for events with no preceding dialog (e.g. a grant expiring in
        /// the background) - for anything that shows a dialog first, capture
        /// the window beforehand with CaptureForegroundBrowserWindow() and use
        /// RefreshWindow() instead once the dialog closes.
        /// </summary>
        public static void TryRefreshForegroundBrowser()
        {
            RefreshWindow(CaptureForegroundBrowserWindow());
        }

        /// <summary>
        /// Restores focus to a specific previously-captured browser window and
        /// simulates F5. hWnd should come from CaptureForegroundBrowserWindow(),
        /// captured before something else (like our own dialog) took focus.
        /// Note: Windows can refuse SetForegroundWindow in some circumstances
        /// (its anti focus-stealing heuristics) - this is best-effort, not
        /// guaranteed to succeed every time.
        /// </summary>
        public static void RefreshWindow(IntPtr hWnd)
        {
            try
            {
                if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return;

                SetForegroundWindow(hWnd);

                keybd_event(VK_F5, 0, 0, UIntPtr.Zero);               // key down
                keybd_event(VK_F5, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // key up
            }
            catch
            {
                // Best-effort only - a failure here should never break blocking itself.
            }
        }
    }
}