using System;
using System.Collections.Generic;
using System.Linq;
using FocusBlocker.Models;

namespace FocusBlocker.Services
{
    /// <summary>
    /// Tracks the configured focus session(s) and whether blocking should be
    /// active right now. Call CheckAndRaiseEvents() periodically (e.g. every
    /// 15-30s from a Timer) so FocusStarted/FocusEnded fire promptly.
    /// </summary>
    public class FocusScheduler
    {
        private readonly List<FocusSession> _sessions = new List<FocusSession>();
        private bool _lastKnownActiveState;

        public event EventHandler FocusStarted;
        public event EventHandler FocusEnded;

        /// <summary>Manual "force blocking on" switch, used for concentration mode.</summary>
        public bool ManualOverrideActive { get; set; }

        public void SetSessions(IEnumerable<FocusSession> sessions)
        {
            _sessions.Clear();
            if (sessions != null) _sessions.AddRange(sessions);
        }

        public bool IsBlockingActive(DateTime? at = null)
        {
            if (ManualOverrideActive) return true;
            DateTime moment = at ?? DateTime.Now;
            return _sessions.Any(s => s.IsActiveAt(moment));
        }

        public void CheckAndRaiseEvents()
        {
            bool currentlyActive = IsBlockingActive();

            if (currentlyActive && !_lastKnownActiveState)
            {
                _lastKnownActiveState = true;
                FocusStarted?.Invoke(this, EventArgs.Empty);
            }
            else if (!currentlyActive && _lastKnownActiveState)
            {
                _lastKnownActiveState = false;
                FocusEnded?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}