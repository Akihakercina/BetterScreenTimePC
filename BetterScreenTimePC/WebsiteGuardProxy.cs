using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace FocusBlocker.Services
{
    public enum ProxyMode
    {
        Blocklist,//BlockedWebsites
        Allowlist//ConcentrationMode
    }

    public class ChallengeRequestedEventArgs : EventArgs
    {
        public string Domain { get; }
        public ChallengeRequestedEventArgs(string domain) { Domain = domain; }
    }

    public class GrantExpiredEventArgs : EventArgs
    {
        public string Domain { get; }
        public GrantExpiredEventArgs(string domain) { Domain = domain; }
    }


    public class WebsiteGuardProxy
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        private readonly ConcurrentDictionary<string, DateTime> _temporaryGrants =
            new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, DateTime> _lockouts =
            new ConcurrentDictionary<string, DateTime>();
        private readonly HashSet<string> _pendingChallenges = new HashSet<string>();
        private readonly object _pendingLock = new object();


        private readonly ConcurrentDictionary<string, DateTime> _lastGrantExpiredFiredUtc =
            new ConcurrentDictionary<string, DateTime>();
        private static readonly TimeSpan GrantExpiredCooldown = TimeSpan.FromSeconds(3);

        private const string ProxyRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

        public ProxyMode Mode { get; set; } = ProxyMode.Blocklist;

        public List<string> Domains { get; set; } = new List<string>();

        public List<string> PermanentlyBlockedDomains { get; set; } = new List<string>();

        private bool IsPermanentlyBlocked(string lowerCaseHost)
        {
            var permanent = PermanentlyBlockedDomains; // snapshot - the list can be swapped at any time
            return permanent.Any(d =>
                lowerCaseHost.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                lowerCaseHost.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsRunning { get; private set; }

       
        public event EventHandler<ChallengeRequestedEventArgs> ChallengeRequested;
        public event EventHandler<GrantExpiredEventArgs> GrantExpired;

        public WebsiteGuardProxy(int port = 8891)
        {
            _port = port;
        }

        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            IsRunning = true;

            _ = AcceptLoopAsync(_cts.Token);
            EnableSystemProxy();
        }

        public void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            try { _cts?.Cancel(); } catch { /* ignore */ }
            try { _listener?.Stop(); } catch { /* ignore */ }
            DisableSystemProxy();
        }


        public void GrantTemporaryAccess(string domain, TimeSpan duration)
        {
            domain = domain?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(domain)) return;

            _temporaryGrants[domain] = DateTime.UtcNow.Add(duration);
            lock (_pendingLock) { _pendingChallenges.Remove(domain); }
        }

        public void DismissChallenge(string domain)
        {
            domain = domain?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(domain)) return;

            lock (_pendingLock) { _pendingChallenges.Remove(domain); }
        }

        public void BlockForDuration(string domain, TimeSpan duration)
        {
            domain = domain?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(domain)) return;

            _lockouts[domain] = DateTime.UtcNow.Add(duration);
            _temporaryGrants.TryRemove(domain, out _);
            lock (_pendingLock) { _pendingChallenges.Remove(domain); }
        }

        private bool IsLockedOut(string matchedRootDomain)
        {
            return _lockouts.TryGetValue(matchedRootDomain, out DateTime until) && until > DateTime.UtcNow;
        }
        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(); }
                catch { break; }

                _ = HandleClientAsync(client, token);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream clientStream = client.GetStream())
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                try { bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length, token); }
                catch { return; }

                if (bytesRead == 0) return;

                string requestHeader = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                bool isConnect = requestHeader.StartsWith("CONNECT ", StringComparison.OrdinalIgnoreCase);

                string host;
                int port;

                if (isConnect)
                {
                    string firstLine = requestHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    string target = firstLine.Split(' ')[1];
                    string[] parts = target.Split(':');
                    host = parts[0];
                    port = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : 443;
                }
                else
                {
                    host = ExtractHostHeader(requestHeader);
                    port = 80;
                }

                bool allowed = IsRequestAllowed(host, out bool shouldRaiseChallenge, out string matchedRootDomain);

                if (shouldRaiseChallenge)
                    ChallengeRequested?.Invoke(this, new ChallengeRequestedEventArgs(matchedRootDomain));

                if (!allowed)
                {
                    byte[] deny = Encoding.ASCII.GetBytes("HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\n\r\n");
                    try { await clientStream.WriteAsync(deny, 0, deny.Length, token); } catch { /* ignore */ }
                    return;
                }

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    Task watchdog = WatchTunnelAsync(host, linkedCts);

                    try
                    {
                        using (var remote = new TcpClient())
                        {
                            await remote.ConnectAsync(host, port);
                            using (NetworkStream remoteStream = remote.GetStream())
                            {
                                if (isConnect)
                                {
                                    byte[] ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                                    await clientStream.WriteAsync(ok, 0, ok.Length, linkedCts.Token);
                                }
                                else
                                {
                                    await remoteStream.WriteAsync(buffer, 0, bytesRead, linkedCts.Token);
                                }

                                Task clientToRemote = clientStream.CopyToAsync(remoteStream, 81920, linkedCts.Token);
                                Task remoteToClient = remoteStream.CopyToAsync(clientStream, 81920, linkedCts.Token);
                                await Task.WhenAny(clientToRemote, remoteToClient);
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                    finally
                    {
                        linkedCts.Cancel(); 
                        try { await watchdog; } catch { /* ignore */ }
                    }
                }
            }
        }

        private async Task WatchTunnelAsync(string host, CancellationTokenSource linkedCts)
        {
            try
            {
                while (!linkedCts.IsCancellationRequested)
                {
                    await Task.Delay(1000, linkedCts.Token);

                    if (IsHostPermittedNow(host, out string blocklistRootDomain))
                        continue;

                    if (blocklistRootDomain != null)
                        RaiseGrantExpiredWithCooldown(blocklistRootDomain);

                    linkedCts.Cancel();
                    return;
                }
            }
            catch (OperationCanceledException) { /* expected on shutdown/teardown */ }
        }

        private void RaiseGrantExpiredWithCooldown(string domain)
        {
            DateTime now = DateTime.UtcNow;
            bool shouldNotify = false;

            _lastGrantExpiredFiredUtc.AddOrUpdate(
                domain,
                addValueFactory: _ => { shouldNotify = true; return now; },
                updateValueFactory: (_, last) =>
                {
                    if (now - last >= GrantExpiredCooldown)
                    {
                        shouldNotify = true;
                        return now;
                    }
                    return last; // still within the cooldown - suppress this one, keep the original timestamp
                });

            if (shouldNotify)
                GrantExpired?.Invoke(this, new GrantExpiredEventArgs(domain));
        }

        
        private bool IsHostPermittedNow(string host, out string blocklistRootDomain)
        {
            blocklistRootDomain = null;

            if (string.IsNullOrWhiteSpace(host))
                return false;

            host = host.ToLowerInvariant();

            if (IsPermanentlyBlocked(host))
                return false; // silent: no challenge, nothing to grant

            string matched = Domains.FirstOrDefault(d =>
                host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));

            if (Mode == ProxyMode.Allowlist)
                return matched != null;

            if (matched == null)
                return true;
            if (IsLockedOut(matched))
                return false;

            bool granted = _temporaryGrants.TryGetValue(matched, out DateTime expiry) && expiry > DateTime.UtcNow;
            if (!granted)
                blocklistRootDomain = matched;
            return granted;
        }

        private static string ExtractHostHeader(string requestHeader)
        {
            foreach (string line in requestHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                    return line.Substring(5).Trim().Split(':')[0];
            }
            return string.Empty;
        }

        private bool IsRequestAllowed(string host, out bool shouldRaiseChallenge, out string matchedRootDomain)
        {
            shouldRaiseChallenge = false;
            matchedRootDomain = null;

            if (string.IsNullOrWhiteSpace(host))
                return false;

            host = host.ToLowerInvariant();

            if (IsPermanentlyBlocked(host))
                return false;

            string matched = Domains.FirstOrDefault(d =>
                host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));

            if (Mode == ProxyMode.Allowlist)
                return matched != null; // no challenge logic in allowlist mode

            // Blocklist mode:
            if (matched == null)
                return true; // not on the blocked list - allow freely

            matchedRootDomain = matched;

            if (IsLockedOut(matched))
                return false; // gave up recently: refuse silently, no new challenge

            if (_temporaryGrants.TryGetValue(matched, out DateTime expiry) && expiry > DateTime.UtcNow)
                return true;

            lock (_pendingLock)
            {
                if (!_pendingChallenges.Contains(matched))
                {
                    _pendingChallenges.Add(matched);
                    shouldRaiseChallenge = true;
                }
            }

            if (shouldRaiseChallenge)
                LogChallengeTrigger(host, matched);

            return false;
        }

        private static void LogChallengeTrigger(string host, string matchedRootDomain)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockerApp");
                Directory.CreateDirectory(folder);
                string logPath = Path.Combine(folder, "proxy_debug.log");
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | blocked domain={matchedRootDomain} | actual host connected to={host}{Environment.NewLine}";
                File.AppendAllText(logPath, line);
            }
            catch
            {
                // Best effort - logging must never break the proxy itself.
            }
        }

        private void EnableSystemProxy()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, writable: true))
            {
                key?.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                key?.SetValue("ProxyServer", $"127.0.0.1:{_port}", RegistryValueKind.String);
            }
            NativeMethods.RefreshInternetSettings();
        }

        private void DisableSystemProxy()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, writable: true))
            {
                key?.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
            }
            NativeMethods.RefreshInternetSettings();
        }

        private static class NativeMethods
        {
            private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
            private const int INTERNET_OPTION_REFRESH = 37;

            [DllImport("wininet.dll", SetLastError = true)]
            private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

            public static void RefreshInternetSettings()
            {
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            }
        }
    }
}