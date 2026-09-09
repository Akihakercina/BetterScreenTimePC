using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FocusBlocker.Models;
using FocusBlocker.Services;

namespace BetterScreenTimePC
{
    public partial class MainForm : Form
    {
        private readonly string storageFolder;
        private readonly string appsFile;
        private readonly string sitesFile;
        private readonly string focusFile;
        private readonly string concentrationFile;

        // --- The actual blocking engine ---
        private readonly ProcessWatcher _processWatcher = new ProcessWatcher();
        private readonly FocusScheduler _focusScheduler = new FocusScheduler();
        private readonly WebsiteGuardProxy _webGuardProxy = new WebsiteGuardProxy();
        private readonly System.Windows.Forms.Timer _schedulerTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer _concentrationCountdownTimer;

        private static readonly TimeSpan WebsiteChallengeGrantDuration = TimeSpan.FromMinutes(5);

        private List<InstalledProgram> _installedPrograms = new List<InstalledProgram>();
        private List<BlockedWebsite> _currentBlockedWebsites = new List<BlockedWebsite>();
        private bool _concentrationModeActive;

        // Prevents ChallengeForm from stacking multiple times when a multi-process
        // app (Chrome, Discord, etc.) launches several processes at once, or a
        // website is hit by several requests while one challenge is still open.
        private readonly Dictionary<string, List<int>> _pendingAppChallengePids =
            new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _domainsWithOpenChallenge =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            InitializeComponent();
            storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockerApp");
            appsFile = Path.Combine(storageFolder, "blocked_apps.txt");
            sitesFile = Path.Combine(storageFolder, "blocked_sites.txt");
            focusFile = Path.Combine(storageFolder, "focus_time.txt");
            concentrationFile = Path.Combine(storageFolder, "concentration_allowed.txt");

            this.Load += MainForm_Load;
            btnSaveApps.Click += BtnSaveApps_Click;
            btnAddWebsite.Click += BtnAddWebsite_Click;
            btnRemoveWebsite.Click += BtnRemoveWebsite_Click;
            btnSaveWebsites.Click += BtnSaveWebsites_Click;
            btnSaveFocus.Click += BtnSaveFocus_Click;
            btnAddAllowed.Click += BtnAddAllowed_Click;
            btnRemoveAllowed.Click += BtnRemoveAllowed_Click;
            btnStartConcentration.Click += BtnStartConcentration_Click;
            btnSaveConcentration.Click += BtnSaveConcentration_Click;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(storageFolder);

            dtpStart.ShowUpDown = true;
            dtpEnd.ShowUpDown = true;

            LoadInstalledApps();
            LoadBlockedApps();
            LoadBlockedWebsites();
            LoadFocusTime();
            LoadConcentrationAllowed();

            WireBlockingEngine();
        }

        // ---------- App list ----------

        private void LoadInstalledApps()
        {
            _installedPrograms = InstalledProgramsProvider.GetInstalledPrograms();
            clbApps.DisplayMember = "DisplayName";
            clbApps.DataSource = _installedPrograms;
        }

        private void LoadBlockedApps()
        {
            if (!File.Exists(appsFile)) return;

            // Each line is "ProcessName|DisplayName".
            var blockedProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(appsFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string processName = line.Split('|')[0].Trim();
                if (!string.IsNullOrEmpty(processName)) blockedProcessNames.Add(processName);
            }

            for (int i = 0; i < clbApps.Items.Count; i++)
            {
                if (clbApps.Items[i] is InstalledProgram program &&
                    program.GuessedProcessName != null &&
                    blockedProcessNames.Contains(program.GuessedProcessName))
                {
                    clbApps.SetItemChecked(i, true);
                }
            }
        }

        private void BtnSaveApps_Click(object sender, EventArgs e)
        {
            var lines = clbApps.CheckedItems.Cast<object>()
                .OfType<InstalledProgram>()
                .Where(p => p.GuessedProcessName != null)
                .Select(p => $"{p.GuessedProcessName}|{p.DisplayName}")
                .ToArray();

            File.WriteAllLines(appsFile, lines);
            RefreshEngineFromSavedFiles();
            MessageBox.Show("Blocked apps saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- Blocked websites (listBox: lbWebsites) ----------

        private void BtnAddWebsite_Click(object sender, EventArgs e)
        {
            string w = BlockedWebsite.NormalizeDomain(tbWebsite.Text);
            if (string.IsNullOrEmpty(w)) return;
            if (!lbWebsites.Items.Contains(w)) lbWebsites.Items.Add(w);
            tbWebsite.Clear();
        }

        private void BtnRemoveWebsite_Click(object sender, EventArgs e)
        {
            var sel = lbWebsites.SelectedItems.Cast<object>().ToList();
            foreach (var s in sel) lbWebsites.Items.Remove(s);
        }

        private void BtnSaveWebsites_Click(object sender, EventArgs e)
        {
            var lines = lbWebsites.Items.Cast<object>().Select(o => o.ToString()).ToArray();
            File.WriteAllLines(sitesFile, lines);
            RefreshEngineFromSavedFiles();
            MessageBox.Show("Blocked websites saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadBlockedWebsites()
        {
            lbWebsites.Items.Clear();
            if (!File.Exists(sitesFile)) return;
            foreach (var l in File.ReadAllLines(sitesFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                lbWebsites.Items.Add(l.Trim());
        }

        // ---------- Focus time ----------

        private void BtnSaveFocus_Click(object sender, EventArgs e)
        {
            File.WriteAllLines(focusFile, new[] { dtpStart.Value.ToString("HH:mm"), dtpEnd.Value.ToString("HH:mm") });
            RefreshEngineFromSavedFiles();
            MessageBox.Show("Focus time saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadFocusTime()
        {
            if (!File.Exists(focusFile)) return;
            var lines = File.ReadAllLines(focusFile);
            if (lines.Length >= 2)
            {
                if (TimeSpan.TryParse(lines[0], out var st)) dtpStart.Value = DateTime.Today.Add(st);
                if (TimeSpan.TryParse(lines[1], out var et)) dtpEnd.Value = DateTime.Today.Add(et);
            }
        }

        // ---------- Concentration mode allowlist (lbAllowed) ----------

        private void BtnAddAllowed_Click(object sender, EventArgs e)
        {
            string w = BlockedWebsite.NormalizeDomain(tbAllowedSite.Text);
            if (string.IsNullOrEmpty(w)) return;
            if (!lbAllowed.Items.Contains(w)) lbAllowed.Items.Add(w);
            tbAllowedSite.Clear();
        }

        private void BtnRemoveAllowed_Click(object sender, EventArgs e)
        {
            var sel = lbAllowed.SelectedItems.Cast<object>().ToList();
            foreach (var s in sel) lbAllowed.Items.Remove(s);
        }

        private void BtnSaveConcentration_Click(object sender, EventArgs e)
        {
            var lines = lbAllowed.Items.Cast<object>().Select(o => o.ToString()).ToArray();
            File.WriteAllLines(concentrationFile, lines);
            MessageBox.Show("Concentration allowed list saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadConcentrationAllowed()
        {
            lbAllowed.Items.Clear();
            if (!File.Exists(concentrationFile)) return;
            foreach (var l in File.ReadAllLines(concentrationFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                lbAllowed.Items.Add(l.Trim());
        }

        private void BtnStartConcentration_Click(object sender, EventArgs e)
        {
            if (_concentrationModeActive) return;

            int minutes = (int)nudDuration.Value;
            btnStartConcentration.Enabled = false;

            _focusScheduler.ManualOverrideActive = true;
            _processWatcher.IsArmed = true;
            _concentrationModeActive = true;
            RefreshWebGuardProxyState();

            _concentrationCountdownTimer = new System.Windows.Forms.Timer { Interval = minutes * 60 * 1000 };
            _concentrationCountdownTimer.Tick += (s, ev) =>
            {
                _concentrationCountdownTimer.Stop();
                _concentrationModeActive = false;
                _focusScheduler.ManualOverrideActive = false;
                RefreshWebGuardProxyState();
                _focusScheduler.CheckAndRaiseEvents();
                btnStartConcentration.Enabled = true;
                MessageBox.Show("Concentration period ended.", "Concentration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _concentrationCountdownTimer.Start();

            MessageBox.Show($"Concentration mode started for {minutes} minutes. Only allowed sites are accessible.",
                "Concentration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- Wiring the blocking engine ----------

        private void WireBlockingEngine()
        {
            RefreshEngineFromSavedFiles();

            _processWatcher.BlockedAppLaunched += OnBlockedAppLaunched;
            _processWatcher.Start();

            _webGuardProxy.ChallengeRequested += OnWebsiteChallengeRequested;

            _focusScheduler.FocusStarted += (s, e) =>
            {
                _processWatcher.IsArmed = true;
                RefreshWebGuardProxyState();
            };
            _focusScheduler.FocusEnded += (s, e) =>
            {
                _processWatcher.IsArmed = false;
                RefreshWebGuardProxyState();
            };

            _processWatcher.IsArmed = _focusScheduler.IsBlockingActive();

            _schedulerTimer.Interval = 15000;
            _schedulerTimer.Tick += (s, e) => _focusScheduler.CheckAndRaiseEvents();
            _schedulerTimer.Start();
        }

        /// <summary>
        /// Re-reads everything from the saved files and applies it to the engine.
        /// Called on startup and after every Save button, so a change takes
        /// effect immediately even if focus time is already active.
        /// </summary>
        private void RefreshEngineFromSavedFiles()
        {
            // Apps
            var blockedApps = new List<BlockedApp>();
            if (File.Exists(appsFile))
            {
                foreach (string line in File.ReadAllLines(appsFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split('|');
                    string processName = parts[0].Trim();
                    string displayName = parts.Length > 1 ? parts[1].Trim() : processName;
                    if (!string.IsNullOrEmpty(processName))
                        blockedApps.Add(new BlockedApp(processName, displayName));
                }
            }
            _processWatcher.BlockedApps = blockedApps;

            // Websites (always-blocked list)
            _currentBlockedWebsites.Clear();
            if (File.Exists(sitesFile))
            {
                foreach (string line in File.ReadAllLines(sitesFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                    _currentBlockedWebsites.Add(new BlockedWebsite(line.Trim()));
            }

            // Focus time
            TimeSpan start = TimeSpan.Zero, end = new TimeSpan(22, 0, 0);
            if (File.Exists(focusFile))
            {
                var lines = File.ReadAllLines(focusFile);
                if (lines.Length >= 2)
                {
                    TimeSpan.TryParse(lines[0], out start);
                    TimeSpan.TryParse(lines[1], out end);
                }
            }
            _focusScheduler.SetSessions(new List<FocusSession> { new FocusSession(start, end, DaysOfWeekFlags.Everyday) });

            _processWatcher.IsArmed = _focusScheduler.IsBlockingActive();
            RefreshWebGuardProxyState();
            _focusScheduler.CheckAndRaiseEvents();
        }

        /// <summary>
        /// Decides what the shared web proxy should be doing right now.
        /// Concentration mode (allowlist) takes priority over the scheduled
        /// focus-time blocklist while it's running.
        /// </summary>
        private void RefreshWebGuardProxyState()
        {
            if (_concentrationModeActive)
            {
                _webGuardProxy.Mode = ProxyMode.Allowlist;
                _webGuardProxy.Domains = lbAllowed.Items.Cast<object>()
                    .Select(i => BlockedWebsite.NormalizeDomain(i.ToString()))
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .ToList();

                if (!_webGuardProxy.IsRunning) _webGuardProxy.Start();
            }
            else if (_focusScheduler.IsBlockingActive())
            {
                _webGuardProxy.Mode = ProxyMode.Blocklist;
                _webGuardProxy.Domains = _currentBlockedWebsites
                    .Select(w => w.Domain)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .ToList();

                if (!_webGuardProxy.IsRunning) _webGuardProxy.Start();
            }
            else
            {
                if (_webGuardProxy.IsRunning) _webGuardProxy.Stop();
            }
        }

        /// <summary>
        /// Fires when ProcessWatcher detects a blocked app was launched. If a
        /// challenge for this app's process name is already open (e.g. a
        /// multi-process app like Chrome launched several processes at once),
        /// this just queues the extra PID instead of stacking another dialog.
        /// </summary>
        private void OnBlockedAppLaunched(object sender, BlockedAppDetectedEventArgs e)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnBlockedAppLaunched(sender, e))); return; }

            string key = e.App.ProcessName;

            if (_pendingAppChallengePids.TryGetValue(key, out var pendingPids))
            {
                pendingPids.Add(e.ProcessId);
                return;
            }

            _pendingAppChallengePids[key] = new List<int> { e.ProcessId };

            using (var challenge = new ChallengeForm())
            {
                var result = challenge.ShowDialog();

                var pids = _pendingAppChallengePids[key];
                _pendingAppChallengePids.Remove(key);

                if (result == DialogResult.Abort)
                {
                    foreach (int pid in pids)
                        ProcessWatcher.KillProcess(pid);
                }
                // DialogResult.OK -> solved, let all of them (including any that
                // launched while the dialog was open) keep running.
            }
        }

        /// <summary>
        /// Fires (background thread - marshaled here) when a blocked website is
        /// visited. If a challenge for this domain is already open (e.g. a page
        /// fires several requests to the same blocked domain), this is a no-op -
        /// the open dialog's outcome will apply to all of them.
        /// </summary>
        private void OnWebsiteChallengeRequested(object sender, ChallengeRequestedEventArgs e)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnWebsiteChallengeRequested(sender, e))); return; }

            if (_domainsWithOpenChallenge.Contains(e.Domain))
                return;

            _domainsWithOpenChallenge.Add(e.Domain);

            using (var challenge = new ChallengeForm())
            {
                var result = challenge.ShowDialog();
                _domainsWithOpenChallenge.Remove(e.Domain);

                if (result == DialogResult.OK)
                    _webGuardProxy.GrantTemporaryAccess(e.Domain, WebsiteChallengeGrantDuration);
                else
                    _webGuardProxy.DismissChallenge(e.Domain);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _processWatcher.Stop();
            _schedulerTimer.Stop();
            _concentrationCountdownTimer?.Stop();
            _webGuardProxy.Stop(); // restores normal system proxy settings
            base.OnFormClosing(e);
        }
    }
}