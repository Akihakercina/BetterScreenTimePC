using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using FocusBlocker.Models;

namespace FocusBlocker.Services
{
    public class BlockedAppDetectedEventArgs : EventArgs
    {
        public BlockedApp App { get; }
        public int ProcessId { get; }
        public BlockedAppDetectedEventArgs(BlockedApp app, int processId) { App = app; ProcessId = processId; }
    }

    /// <summary>
    /// Watches for process launches and raises BlockedAppLaunched whenever one
    /// matches BlockedApps. Uses WMI (instant, needs admin) if possible, falls
    /// back to polling automatically otherwise. Requires "System.Management" as
    /// an assembly reference/NuGet package.
    /// </summary>
    public class ProcessWatcher : IDisposable
    {
        private ManagementEventWatcher _wmiWatcher;
        private Thread _pollingThread;
        private volatile bool _pollingActive;
        private readonly HashSet<int> _alreadyHandledPids = new HashSet<int>();
        private System.Threading.Timer _grantExpiryWatchdog;

        // Process-name -> UTC time until which a solved challenge grants a
        // pass, so a multi-process app (new renderer/helper processes spawning
        // after the main one) doesn't re-challenge every new process it opens
        // once you've already solved it once. Mirrors WebsiteGuardProxy's grants.
        private readonly Dictionary<string, DateTime> _temporaryGrantsUtc =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public int PollingIntervalMs { get; set; } = 1000;
        public List<BlockedApp> BlockedApps { get; set; } = new List<BlockedApp>();
        public bool IsArmed { get; set; } = true;
        public bool IsUsingWmi { get; private set; }

        public event EventHandler<BlockedAppDetectedEventArgs> BlockedAppLaunched;

        public void Start()
        {
            if (TryStartWmiWatcher()) { IsUsingWmi = true; }
            else { IsUsingWmi = false; StartPolling(); }

            // Checks every 2 seconds for expired grants and, if the app is
            // still running under that name, fires a fresh challenge directly -
            // this is necessary because a long-lived single process (no new
            // tabs/helpers spawning) would otherwise never get re-evaluated:
            // _alreadyHandledPids marks a PID as handled permanently, so normal
            // detection alone can't catch "this grant just expired" for a
            // process that's been running the whole time.
            _grantExpiryWatchdog = new System.Threading.Timer(CheckExpiredGrants, null, 2000, 2000);
        }

        public void Stop()
        {
            StopWmiWatcher();
            StopPolling();
            _alreadyHandledPids.Clear();
            lock (_temporaryGrantsUtc) { _temporaryGrantsUtc.Clear(); }
            _grantExpiryWatchdog?.Dispose();
            _grantExpiryWatchdog = null;
        }

        private bool TryStartWmiWatcher()
        {
            try
            {
                var query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
                _wmiWatcher = new ManagementEventWatcher(query);
                _wmiWatcher.EventArrived += OnWmiProcessStarted;
                _wmiWatcher.Start();
                return true;
            }
            catch (ManagementException) { CleanupWmiWatcher(); return false; }
            catch (UnauthorizedAccessException) { CleanupWmiWatcher(); return false; }
        }

        private void OnWmiProcessStarted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string processName = e.NewEvent.Properties["ProcessName"]?.Value as string;
                int processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"]?.Value ?? 0);
                if (string.IsNullOrEmpty(processName)) return;
                EvaluateProcess(System.IO.Path.GetFileNameWithoutExtension(processName), processId);
            }
            catch { /* malformed WMI payload - ignore */ }
        }

        private void StopWmiWatcher()
        {
            if (_wmiWatcher == null) return;
            try { _wmiWatcher.Stop(); } catch { /* ignore */ }
            CleanupWmiWatcher();
        }

        private void CleanupWmiWatcher()
        {
            if (_wmiWatcher != null)
            {
                _wmiWatcher.EventArrived -= OnWmiProcessStarted;
                _wmiWatcher.Dispose();
                _wmiWatcher = null;
            }
        }

        private void StartPolling()
        {
            if (_pollingThread != null && _pollingThread.IsAlive) return;
            _pollingActive = true;
            _pollingThread = new Thread(PollingLoop) { IsBackground = true, Name = "FocusBlocker-ProcessPolling" };
            _pollingThread.Start();
        }

        private void StopPolling()
        {
            _pollingActive = false;
            _pollingThread = null;
        }

        private void PollingLoop()
        {
            while (_pollingActive)
            {
                try
                {
                    var liveProcesses = Process.GetProcesses();
                    var livePids = new HashSet<int>();

                    foreach (var proc in liveProcesses)
                    {
                        try { livePids.Add(proc.Id); EvaluateProcess(proc.ProcessName, proc.Id); }
                        catch { /* access denied on some system processes */ }
                        finally { proc.Dispose(); }
                    }

                    _alreadyHandledPids.RemoveWhere(pid => !livePids.Contains(pid));
                }
                catch { /* never let a transient failure kill the thread */ }

                Thread.Sleep(PollingIntervalMs);
            }
        }

        private void EvaluateProcess(string processName, int processId)
        {
            if (!IsArmed || string.IsNullOrEmpty(processName)) return;

            lock (_alreadyHandledPids)
            {
                if (_alreadyHandledPids.Contains(processId)) return;
            }

            BlockedApp match = BlockedApps?.FirstOrDefault(a => a.Matches(processName));
            if (match == null) return;

            lock (_alreadyHandledPids) { _alreadyHandledPids.Add(processId); }

            // Already solved recently for this app name? Let this new process
            // (e.g. a new tab's renderer, a background helper) through silently
            // instead of challenging again.
            lock (_temporaryGrantsUtc)
            {
                if (_temporaryGrantsUtc.TryGetValue(match.ProcessName, out DateTime expiry) && expiry > DateTime.UtcNow)
                    return;
            }

            BlockedAppLaunched?.Invoke(this, new BlockedAppDetectedEventArgs(match, processId));
        }

        /// <summary>Call after the user solves the challenge for this app - grants new processes of the same name a pass for the given duration.</summary>
        public void GrantTemporaryAccess(string processName, TimeSpan duration)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            lock (_temporaryGrantsUtc)
            {
                _temporaryGrantsUtc[processName] = DateTime.UtcNow.Add(duration);
            }
        }

        /// <summary>
        /// Runs on a background timer. For any grant that just expired, checks
        /// whether the app is still running under that name and, if so, fires
        /// BlockedAppLaunched directly for each currently-running matching
        /// process - bypassing normal detection entirely, since those PIDs may
        /// already be marked "handled" from before and would otherwise never
        /// trigger a fresh challenge on their own.
        /// </summary>
        private void CheckExpiredGrants(object state)
        {
            if (!IsArmed) return;

            List<string> expiredNames;
            lock (_temporaryGrantsUtc)
            {
                expiredNames = _temporaryGrantsUtc
                    .Where(kv => kv.Value <= DateTime.UtcNow)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (string name in expiredNames)
                    _temporaryGrantsUtc.Remove(name);
            }

            foreach (string processName in expiredNames)
            {
                BlockedApp match = BlockedApps?.FirstOrDefault(a =>
                    string.Equals(a.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;

                Process[] runningProcesses;
                try { runningProcesses = Process.GetProcessesByName(processName); }
                catch { continue; }

                foreach (var proc in runningProcesses)
                {
                    try
                    {
                        BlockedAppLaunched?.Invoke(this, new BlockedAppDetectedEventArgs(match, proc.Id));
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
        }

        public static void KillProcess(int processId)
        {
            try
            {
                using (var proc = Process.GetProcessById(processId))
                {
                    if (!proc.HasExited) proc.Kill();
                }
            }
            catch (ArgumentException) { /* already exited */ }
            catch { /* access denied etc - nothing more to do */ }
        }

        /// <summary>
        /// Kills every currently running process with this name - needed because
        /// apps like Chrome run multiple processes sharing one process name, and
        /// killing only the single PID that triggered detection leaves the rest
        /// running (which can also keep re-triggering the block challenge).
        /// </summary>
        public static void KillAllProcessesByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;

            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    using (proc)
                    {
                        if (!proc.HasExited) proc.Kill();
                    }
                }
                catch { /* access denied, already exited, etc. - keep going */ }
            }
        }

        public void Dispose() => Stop();
    }
}