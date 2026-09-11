using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        /// <summary>Everything is allowed EXCEPT domains in Domains (used for listBox1, focus time).</summary>
        Blocklist,
        /// <summary>ONLY domains in Domains are allowed, everything else refused (used for listBox2, concentration mode).</summary>
        Allowlist
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

    /// <summary>
    /// A local proxy that intercepts outbound web connections (HTTP + HTTPS via
    /// CONNECT) and decides whether to let them through, based on Mode:
    ///
    ///   - Blocklist: domains in Domains are blocked UNLESS the user has solved
    ///     a challenge for them recently (see GrantTemporaryAccess). Every time
    ///     a blocked domain is requested with no active grant, ChallengeRequested
    ///     fires ONCE (deduped) so the UI can show OverlayForm. The connection
    ///     itself is still refused immediately - the user needs to reload the
    ///     page after solving the challenge for the retry to succeed.
    ///
    ///   - Allowlist: only domains in Domains are reachable; no challenge logic
    ///     involved (matches the original concentration-mode behavior).
    ///
    /// Only one instance is meant to run at a time (it owns the single system
    /// proxy registry setting) - switch Mode/Domains on the same instance
    /// rather than running two of these simultaneously.
    /// </summary>
    public class WebsiteGuardProxy
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        private readonly ConcurrentDictionary<string, DateTime> _temporaryGrants =
            new ConcurrentDictionary<string, DateTime>();
        private readonly HashSet<string> _pendingChallenges = new HashSet<string>();
        private readonly object _pendingLock = new object();

        private const string ProxyRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

        public ProxyMode Mode { get; set; } = ProxyMode.Blocklist;

        /// <summary>Interpreted according to Mode - either "domains to block" or "domains to allow".</summary>
        public List<string> Domains { get; set; } = new List<string>();

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Fires (once per domain until resolved) when, in Blocklist mode, a
        /// blocked domain is requested with no active temporary grant.
        /// NOTE: raised from a background thread - marshal to the UI thread
        /// before touching any Forms.
        /// </summary>
        public event EventHandler<ChallengeRequestedEventArgs> ChallengeRequested;

        /// <summary>Fired when an active grant expires while a tunnel for that domain is still open.</summary>
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

        /// <summary>
        /// Call this after the user solves the challenge for a domain - lets
        /// them through for the given duration without re-triggering a challenge.
        /// </summary>
        public void GrantTemporaryAccess(string domain, TimeSpan duration)
        {
            domain = domain?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(domain)) return;

            _temporaryGrants[domain] = DateTime.UtcNow.Add(duration);
            lock (_pendingLock) { _pendingChallenges.Remove(domain); }
        }

        /// <summary>Call this if the user gives up on the challenge, so a future visit can re-trigger it.</summary>
        public void DismissChallenge(string domain)
        {
            domain = domain?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(domain)) return;

            lock (_pendingLock) { _pendingChallenges.Remove(domain); }
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

                // If this connection is only open because of a temporary grant
                // (matchedRootDomain != null means it's on the blocklist, and we
                // only got this far because it's currently granted), watch the
                // grant while the tunnel stays open and forcibly tear it down the
                // moment it expires - otherwise a long-lived HTTPS connection
                // (HTTP/2 reuse, keep-alive) could keep working indefinitely past
                // the 5 minutes, since without this the access check only ever
                // runs once, at connection setup.
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    Task watchdog = matchedRootDomain != null
                        ? WatchGrantAsync(matchedRootDomain, linkedCts)
                        : Task.CompletedTask;

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
                        // Remote unreachable, connection reset, grant expired
                        // (cancellation), etc. - just drop it either way.
                    }
                    finally
                    {
                        linkedCts.Cancel(); // make sure the watchdog loop exits promptly
                        try { await watchdog; } catch { /* ignore */ }
                    }
                }
            }
        }

        /// <summary>
        /// While a tunnel for a grant-dependent domain is open, checks every
        /// second whether the grant is still valid. Cancels linkedCts the moment
        /// it expires, which tears down the tunnel's read/write operations and
        /// forces the browser to reconnect (and get re-checked) next time.
        /// </summary>
        private async Task WatchGrantAsync(string rootDomain, CancellationTokenSource linkedCts)
        {
            try
            {
                while (!linkedCts.IsCancellationRequested)
                {
                    await Task.Delay(1000, linkedCts.Token);

                    bool stillGranted = _temporaryGrants.TryGetValue(rootDomain, out DateTime expiry) &&
                                         expiry > DateTime.UtcNow;

                    if (!stillGranted)
                    {
                        GrantExpired?.Invoke(this, new GrantExpiredEventArgs(rootDomain));
                        linkedCts.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) { /* expected on shutdown/teardown */ }
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

            // The domain from Domains that matched (root domain, not the exact
            // requested host) - grants/challenges are tracked against THIS, so
            // solving once covers every subdomain of the same blocked site.
            string matched = Domains.FirstOrDefault(d =>
                host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));

            if (Mode == ProxyMode.Allowlist)
                return matched != null; // no challenge logic in allowlist mode

            // Blocklist mode:
            if (matched == null)
                return true; // not on the blocked list - allow freely

            matchedRootDomain = matched;

            if (_temporaryGrants.TryGetValue(matched, out DateTime expiry) && expiry > DateTime.UtcNow)
                return true; // still within an active grace period

            lock (_pendingLock)
            {
                if (!_pendingChallenges.Contains(matched))
                {
                    _pendingChallenges.Add(matched);
                    shouldRaiseChallenge = true;
                }
            }
            return false;
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