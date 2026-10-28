using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using FocusBlocker.Models;
using FocusBlocker.Services;

namespace BetterScreenTimePC
{
    public partial class MainForm : Form
    {
        public static int option;
        private readonly string storageFolder;
        private readonly string appsFile;
        private readonly string sitesFile;
        private readonly string focusFile;
        private readonly string concentrationFile;
        private readonly string optionsFile;

        private readonly ProcessWatcher _processWatcher = new ProcessWatcher();
        private readonly FocusScheduler _focusScheduler = new FocusScheduler();
        private readonly WebsiteGuardProxy _webGuardProxy = new WebsiteGuardProxy();
        private readonly System.Windows.Forms.Timer _schedulerTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer _concentrationCountdownTimer;
        private DateTime _concentrationEndTimeUtc;

        private static readonly TimeSpan WebsiteChallengeGrantDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan AppChallengeGrantDuration = TimeSpan.FromMinutes(5);

        private readonly BindingList<InstalledProgram> _installedPrograms = new BindingList<InstalledProgram>();
        private List<BlockedWebsite> _currentBlockedWebsites = new List<BlockedWebsite>();
        private bool _concentrationModeActive;

        private const int UncheckAppChallengeCount = 5;
        private const int RemoveWebsiteChallengeCount = 5;
        private const int ShortenFocusChallengeCount = 3;
        private const int ExitChallengeCount = 10;

        private bool _suppressAppCheckGuard;
        private bool _realExitApproved;

        private const string SingleInstanceMutexName = "BetterScreenTimePC-SingleInstance";
        private const string ShowRequestEventName = "BetterScreenTimePC-ShowRequest";
        private Mutex _singleInstanceMutex;
        private EventWaitHandle _showRequestEvent;

        private NotifyIcon _trayIcon;

        private const string WatchdogProcessName = "BlockerWatchdog";
        private const string IntentionalExitEventName = "BetterScreenTimePC-IntentionalExit";
        private const string FromWatchdogArg = "--from-watchdog";
        private const string RequestRelaunchEventName = "BetterScreenTimePC-RequestRelaunch";
        private EventWaitHandle _intentionalExitEvent;
        private volatile bool _watchdogMonitorRunning = true;
        private Thread _watchdogMonitorThread;

        private readonly Dictionary<string, List<int>> _pendingAppChallengePids =
            new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _domainsWithOpenChallenge =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool isFirstInstance);
            _showRequestEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowRequestEventName);

            if (!isFirstInstance)
            {
                _showRequestEvent.Set();
                Environment.Exit(0);
            }

            bool launchedByWatchdog = Environment.GetCommandLineArgs().Skip(1).Any(a => a == FromWatchdogArg);

            if (!launchedByWatchdog)
            {
                LaunchWatchdogIfNotRunning();

                try
                {
                   
                    using (var relaunchRequestEvent = new EventWaitHandle(false, EventResetMode.AutoReset, RequestRelaunchEventName))
                        relaunchRequestEvent.Set();
                }
                catch
                {
                    // Best effort - the watchdog's own periodic check will catch it within ~2s regardless.
                }

                _singleInstanceMutex.ReleaseMutex();
                Environment.Exit(0);
            }


            InitializeComponent();

            var showRequestListener = new Thread(() =>
            {
                while (true)
                {
                    _showRequestEvent.WaitOne();
                    try { BeginInvoke(new Action(RestoreWindow)); }
                    catch (ObjectDisposedException) { return; }
                }
            })
            { IsBackground = true };
            showRequestListener.Start();

            InitializeTrayIcon();

            storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockerApp");
            appsFile = Path.Combine(storageFolder, "blocked_apps.txt");
            sitesFile = Path.Combine(storageFolder, "blocked_sites.txt");
            focusFile = Path.Combine(storageFolder, "focus_time.txt");
            concentrationFile = Path.Combine(storageFolder, "concentration_allowed.txt");
            optionsFile = Path.Combine(storageFolder, "options.txt");

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
            btnAddCustomApp.Click += BtnAddCustomApp_Click;
            clbApps.ItemCheck += ClbApps_ItemCheck;
            btnSaveOptions.Click += btnSaveOptions_Click;

            _webGuardProxy.PermanentlyBlockedDomains = BuiltInBlocklist.Domains
                .Select(BlockedWebsite.NormalizeDomain)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .ToList();

            _intentionalExitEvent = new EventWaitHandle(false, EventResetMode.ManualReset, IntentionalExitEventName);
            LaunchWatchdogIfNotRunning();
            _watchdogMonitorThread = new Thread(WatchdogMonitorLoop) { IsBackground = true };
            _watchdogMonitorThread.Start();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "BetterScreenTimePC",
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => RestoreWindow();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => RestoreWindow());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Close());
            _trayIcon.ContextMenuStrip = menu;
        }
        
        private static Icon LoadTrayIcon()
        {
            try
            {
                string ownDirectory = AppDomain.CurrentDomain.BaseDirectory ?? "";
                string projectFolder = Path.GetFullPath(Path.Combine(ownDirectory, @"..\..\..\"));
                string iconPath = Path.Combine(projectFolder+"BetterScreenTimePC\\", "tray_icon.ico");
                if (File.Exists(iconPath))
                    return new Icon(iconPath);
                MessageBox.Show(iconPath);
            }
            catch
            {
                
            }

            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }


        private void RestoreWindow()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(storageFolder);

            dtpStart.ShowUpDown = true;
            dtpEnd.ShowUpDown = true;

            _suppressAppCheckGuard = true;
            try
            {
                LoadInstalledApps();
                LoadBlockedApps();
            }
            finally { _suppressAppCheckGuard = false; }
            LoadBlockedWebsites();
            LoadFocusTime();
            LoadConcentrationAllowed();
            LoadOptions();
            lblConcentrationRemaining.Text = "";

            WireBlockingEngine();
            EnsureAutostartRegistered();
        }

        // ---------- Watchdog launch/monitor ----------

        private void LaunchWatchdogIfNotRunning()
        {
            try
            {
                if (Process.GetProcessesByName(WatchdogProcessName).Length > 0)
                    return;

                string ownExePath = Assembly.GetExecutingAssembly().Location;
                string ownDirectory = AppDomain.CurrentDomain.BaseDirectory ?? "";
                string projectFolder = Path.GetFullPath(Path.Combine(ownDirectory, @"..\..\..\"));
                string watchdogPath = Path.Combine(projectFolder + "BlockerWatchdog\\bin\\Debug", WatchdogProcessName + ".exe");

                if (!File.Exists(watchdogPath))
                {
                    Debug.WriteLine(watchdogPath);
                    return;
                }

                Process.Start(new ProcessStartInfo(watchdogPath)
                {
                    Arguments = "\"" + ownExePath + "\"",
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        private void WatchdogMonitorLoop()
        {
            while (_watchdogMonitorRunning)
            {
                Thread.Sleep(2000);
                if (!_watchdogMonitorRunning) return;

                try
                {
                    if (Process.GetProcessesByName(WatchdogProcessName).Length == 0)
                        LaunchWatchdogIfNotRunning();
                }
                catch { }
            }
        }
        // ---------- Autostart ----------

        private const string AutostartTaskName = "BetterScreenTimePC-Autostart";

        private void EnsureAutostartRegistered()
        {
            try
            {
                var query = Process.Start(new ProcessStartInfo("schtasks.exe", $"/Query /TN \"{AutostartTaskName}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                query.WaitForExit();
                if (query.ExitCode == 0)
                    return; // already registered

                string exePath = Assembly.GetExecutingAssembly().Location;
                string createArgs =
                    "/Create /TN \"" + AutostartTaskName + "\"" +
                    " /TR \"\\\"" + exePath + "\\\"\"" +
                    " /SC ONLOGON /RL HIGHEST /F";

                var create = Process.Start(new ProcessStartInfo("schtasks.exe", createArgs)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                create.WaitForExit();
            }
            catch
            {
                
            }
        }


        // ---------- App list ----------

        private void LoadInstalledApps()
        {
            foreach (var program in InstalledProgramsProvider.GetInstalledPrograms())
                _installedPrograms.Add(program);

            clbApps.DisplayMember = "DisplayName";
            clbApps.DataSource = _installedPrograms;
        }

        private void LoadBlockedApps()
        {
            if (!File.Exists(appsFile)) return;

            var savedProcessNames = new List<string>();

            foreach (string line in File.ReadAllLines(appsFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('|');
                string processName = parts[0].Trim();
                string displayName = parts.Length > 1 ? parts[1].Trim() : processName;
                if (string.IsNullOrEmpty(processName)) continue;

                savedProcessNames.Add(processName);

                if (FindByProcessName(processName) < 0)
                {
                    _installedPrograms.Add(new InstalledProgram
                    {
                        DisplayName = displayName,
                        ExecutablePath = processName + ".exe"
                    });
                }
            }

            foreach (string processName in savedProcessNames)
            {
                int index = FindByProcessName(processName);
                if (index >= 0) clbApps.SetItemChecked(index, true);
            }
        }

        private void AddProgramKeepingChecks(InstalledProgram program)
        {
            var checkedNames = new HashSet<string>(
                clbApps.CheckedItems.OfType<InstalledProgram>()
                    .Select(p => p.GuessedProcessName)
                    .Where(n => n != null),
                StringComparer.OrdinalIgnoreCase);

            _suppressAppCheckGuard = true;
            try
            {
                _installedPrograms.Add(program);

                for (int i = 0; i < _installedPrograms.Count; i++)
                {
                    string name = _installedPrograms[i].GuessedProcessName;
                    if (name != null && checkedNames.Contains(name))
                        clbApps.SetItemChecked(i, true);
                }
            }
            finally { _suppressAppCheckGuard = false; }
        }

        private void ClbApps_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_suppressAppCheckGuard) return;
            if (e.CurrentValue != CheckState.Checked || e.NewValue != CheckState.Unchecked) return;

            var program = clbApps.Items[e.Index] as InstalledProgram;
            if (program == null || !IsSavedAsBlocked(program)) return;

            if (!RunChallengeSequence(UncheckAppChallengeCount))
                e.NewValue = CheckState.Checked;
        }

        private bool IsSavedAsBlocked(InstalledProgram program)
        {
            string name = program.GuessedProcessName;
            return name != null && _processWatcher.BlockedApps != null &&
                   _processWatcher.BlockedApps.Any(a => a.Matches(name));
        }

        private bool RunChallengeSequence(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                using (var challenge = new ChallengeForm { HeaderText = $"{i}/{count}" })
                {
                    if (challenge.ShowDialog() != DialogResult.OK)
                        return false;
                }
            }
            return true;
        }

        private int FindByProcessName(string processName)
        {
            for (int i = 0; i < _installedPrograms.Count; i++)
            {
                if (string.Equals(_installedPrograms[i].GuessedProcessName, processName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void BtnAddCustomApp_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Executable files (*.exe)|*.exe";
            openFileDialog1.Title = "Select an application to block";

            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            string exePath = openFileDialog1.FileName;
            string processName = Path.GetFileNameWithoutExtension(exePath);

            int existingIndex = FindByProcessName(processName);
            if (existingIndex >= 0)
            {
                clbApps.SetItemChecked(existingIndex, true);
                return;
            }

            var custom = new InstalledProgram
            {
                DisplayName = processName,
                ExecutablePath = exePath
            };
            AddProgramKeepingChecks(custom);
            clbApps.SetItemChecked(_installedPrograms.Count - 1, true);
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

        

        private void BtnAddWebsite_Click(object sender, EventArgs e)
        {
            string w = BlockedWebsite.NormalizeDomain(tbWebsite.Text);
            if (string.IsNullOrEmpty(w)) return;
            if (!lbWebsites.Items.Contains(w)) lbWebsites.Items.Add(w);
            tbWebsite.Clear();
        }

        private void BtnRemoveWebsite_Click(object sender, EventArgs e)
        {
            var selected = lbWebsites.SelectedItems.Cast<object>().Select(o => o.ToString()).ToList();
            if (selected.Count == 0) return;

            if (selected.Any(IsSavedAsBlockedWebsite) &&
                !RunChallengeSequence(RemoveWebsiteChallengeCount))
                return;

            foreach (var domain in selected)
                lbWebsites.Items.Remove(domain);
        }

        private bool IsSavedAsBlockedWebsite(string domain)
        {
            if (string.IsNullOrEmpty(domain) || !File.Exists(sitesFile)) return false;
            return File.ReadAllLines(sitesFile)
                .Select(l => l.Trim())
                .Any(l => string.Equals(l, domain, StringComparison.OrdinalIgnoreCase));
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

        

        private void BtnSaveFocus_Click(object sender, EventArgs e)
        {
            var newStart = new TimeSpan(dtpStart.Value.Hour, dtpStart.Value.Minute, 0);
            var newEnd = new TimeSpan(dtpEnd.Value.Hour, dtpEnd.Value.Minute, 0);

            if (TryGetSavedFocusWindow(out TimeSpan savedStart, out TimeSpan savedEnd) &&
                ReducesCoverage(savedStart, savedEnd, newStart, newEnd))
            {
                if (!RunChallengeSequence(ShortenFocusChallengeCount))
                {
                    dtpStart.Value = DateTime.Today.Add(savedStart);
                    dtpEnd.Value = DateTime.Today.Add(savedEnd);
                    MessageBox.Show("Focus time was not changed.", "Focus time", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            File.WriteAllLines(focusFile, new[] { newStart.ToString(@"hh\:mm"), newEnd.ToString(@"hh\:mm") });
            RefreshEngineFromSavedFiles();
            MessageBox.Show("Focus time saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool TryGetSavedFocusWindow(out TimeSpan start, out TimeSpan end)
        {
            start = TimeSpan.Zero;
            end = TimeSpan.Zero;

            if (!File.Exists(focusFile)) return false;
            var lines = File.ReadAllLines(focusFile);
            return lines.Length >= 2 &&
                   TimeSpan.TryParse(lines[0], out start) &&
                   TimeSpan.TryParse(lines[1], out end);
        }

        private static bool ReducesCoverage(TimeSpan oldStart, TimeSpan oldEnd, TimeSpan newStart, TimeSpan newEnd)
        {
            for (int minute = 0; minute < 1440; minute++)
            {
                if (CoversMinute(oldStart, oldEnd, minute) && !CoversMinute(newStart, newEnd, minute))
                    return true;
            }
            return false;
        }

        private static bool CoversMinute(TimeSpan start, TimeSpan end, int minuteOfDay)
        {
            int s = (int)start.TotalMinutes;
            int e = (int)end.TotalMinutes;
            return s <= e
                ? minuteOfDay >= s && minuteOfDay < e
                : minuteOfDay >= s || minuteOfDay < e;
        }

        private static string FormatTimeSpan(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
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

            if (lbAllowed.Items.Count == 0)
            {
                var answer = MessageBox.Show(
                    "The allowed websites list is empty, so ALL websites will be blocked for the whole session.\n\nStart anyway?",
                    "Concentration mode", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return;
            }

            int minutes = (int)nudDuration.Value;
            btnStartConcentration.Enabled = false;

            _focusScheduler.ManualOverrideActive = true;
            _processWatcher.IsArmed = true;
            _concentrationModeActive = true;
            RefreshWebGuardProxyState();

            _concentrationEndTimeUtc = DateTime.UtcNow.AddMinutes(minutes);

            _concentrationCountdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _concentrationCountdownTimer.Tick += (s, ev) => UpdateConcentrationCountdownLabel();
            _concentrationCountdownTimer.Start();
            UpdateConcentrationCountdownLabel();

            MessageBox.Show($"Concentration mode started for {minutes} minutes. Only allowed sites are accessible.",
                "Concentration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateConcentrationCountdownLabel()
        {
            TimeSpan remaining = _concentrationEndTimeUtc - DateTime.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                _concentrationCountdownTimer.Stop();
                _concentrationModeActive = false;
                _focusScheduler.ManualOverrideActive = false;
                RefreshWebGuardProxyState();
                _focusScheduler.CheckAndRaiseEvents();
                btnStartConcentration.Enabled = true;
                lblConcentrationRemaining.Text = "";
                MessageBox.Show("Concentration period ended.", "Concentration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            lblConcentrationRemaining.Text = FormatTimeSpan(remaining);
        }

        

        private void WireBlockingEngine()
        {
            RefreshEngineFromSavedFiles();

            _processWatcher.BlockedAppLaunched += OnBlockedAppLaunched;
            _processWatcher.Start();

            _webGuardProxy.ChallengeRequested += OnWebsiteChallengeRequested;
            _webGuardProxy.GrantExpired += OnWebsiteGrantExpired;

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

        private void RefreshEngineFromSavedFiles()
        {
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

            _currentBlockedWebsites.Clear();
            if (File.Exists(sitesFile))
            {
                foreach (string line in File.ReadAllLines(sitesFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                    _currentBlockedWebsites.Add(new BlockedWebsite(line.Trim()));
            }

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
            else if (_webGuardProxy.PermanentlyBlockedDomains.Count > 0)
            {
                _webGuardProxy.Mode = ProxyMode.Blocklist;
                _webGuardProxy.Domains = new List<string>();

                if (!_webGuardProxy.IsRunning) _webGuardProxy.Start();
            }
            else
            {
                if (_webGuardProxy.IsRunning) _webGuardProxy.Stop();
            }
        }

        private void OnBlockedAppLaunched(object sender, BlockedAppDetectedEventArgs e)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnBlockedAppLaunched(sender, e))); return; }

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

                _pendingAppChallengePids.Remove(key);

                if (result == DialogResult.Abort)
                {
                    ProcessWatcher.KillAllProcessesByName(key);
                }
                else
                {
                    _processWatcher.GrantTemporaryAccess(key, AppChallengeGrantDuration);
                }
            }
        }

        private void OnWebsiteChallengeRequested(object sender, ChallengeRequestedEventArgs e)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnWebsiteChallengeRequested(sender, e))); return; }

            if (_domainsWithOpenChallenge.Contains(e.Domain))
                return;

            _domainsWithOpenChallenge.Add(e.Domain);

            IntPtr browserWindow = BrowserRefreshHelper.CaptureForegroundBrowserWindow();

            using (var challenge = new ChallengeForm())
            {
                var result = challenge.ShowDialog();
                _domainsWithOpenChallenge.Remove(e.Domain);

                if (result == DialogResult.OK)
                    _webGuardProxy.GrantTemporaryAccess(e.Domain, WebsiteChallengeGrantDuration);
                else
                    _webGuardProxy.DismissChallenge(e.Domain);

                BrowserRefreshHelper.RefreshWindow(browserWindow);
            }
        }

        private void OnWebsiteGrantExpired(object sender, GrantExpiredEventArgs e)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnWebsiteGrantExpired(sender, e))); return; }

            BrowserRefreshHelper.TryRefreshForegroundBrowser();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_realExitApproved)
            {
                if (_trayIcon != null) _trayIcon.Visible = false; 
                _watchdogMonitorRunning = false;
                _intentionalExitEvent?.Set();
                _processWatcher.Stop();
                _schedulerTimer.Stop();
                _concentrationCountdownTimer?.Stop();
                _webGuardProxy.Stop();
                _singleInstanceMutex?.ReleaseMutex();
                base.OnFormClosing(e);
                return;
            }

            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                _realExitApproved = true;
                OnFormClosing(e);
                return;
            }

            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;

            if (RunChallengeSequence(ExitChallengeCount))
            {
                _realExitApproved = true;
                Close();
            }
        }
        //--------Options---------

        private void btnSaveOptions_Click(object sender, EventArgs e)
        {
            if (systemOfEquationsRB.Checked)
                File.WriteAllText(optionsFile, "0");
            if (textRetypeRB.Checked)
                File.WriteAllText(optionsFile, "1");
        }
        private void LoadOptions()
        {
            if (!File.Exists(optionsFile)) return;
            if (!int.TryParse(File.ReadAllText(optionsFile),out option)) option = 0;
            if (option == 0) 
                systemOfEquationsRB.Checked = true;
            if (option == 1)
                textRetypeRB.Checked = true;
        }

    }
}