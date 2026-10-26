using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Watchdog
{
    internal static class Program
    {
        private const string MainAppProcessName = "BetterScreenTimePC"; // no ".exe"

        private const string IntentionalExitEventName = "BetterScreenTimePC-IntentionalExit";
      
        private const string RequestRelaunchEventName = "BetterScreenTimePC-RequestRelaunch";
        private const string FromWatchdogArg = "--from-watchdog";

        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

        private static string _mainAppExePath;

        private static void Main(string[] args)
        {
            _mainAppExePath = args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                ? args[0]
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MainAppProcessName + ".exe");

            bool isFirstInstance;
            using (new Mutex(true, "BetterScreenTimePC-Watchdog-SingleInstance", out isFirstInstance))
            {
                if (!isFirstInstance)
                    return;
          
                using (var intentionalExitEvent = new EventWaitHandle(false, EventResetMode.ManualReset, IntentionalExitEventName))
                using (var relaunchRequestEvent = new EventWaitHandle(false, EventResetMode.AutoReset, RequestRelaunchEventName))
                {
                    while (true)
                    {
                        if (intentionalExitEvent.WaitOne(0))
                            return; // deliberate exit - stop monitoring, let the main app actually stay closed

                        bool gotExplicitSignal = relaunchRequestEvent.WaitOne(CheckInterval);

                        if (intentionalExitEvent.WaitOne(0))
                            return;

                        try
                        {
                            if (gotExplicitSignal || Process.GetProcessesByName(MainAppProcessName).Length == 0)
                                RelaunchMainApp();
                        }
                        catch
                        {
                            
                        }
                    }
                }
            }
        }

        private static void RelaunchMainApp()
        {
            if (!File.Exists(_mainAppExePath)) return; 

            try
            {
                Process.Start(new ProcessStartInfo(_mainAppExePath)
                {
                    Arguments = FromWatchdogArg,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }
    }
}