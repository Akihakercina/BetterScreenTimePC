using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using FocusBlocker.Models;

namespace FocusBlocker.Services
{
    /// <summary>Reads installed programs from the same registry keys Windows' "Apps & Features" uses.</summary>
    public static class InstalledProgramsProvider
    {
        private static readonly (RegistryKey Hive, string SubKey)[] UninstallRoots = new[]
        {
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        };

        public static List<InstalledProgram> GetInstalledPrograms(bool includeSystemComponents = false)
        {
            var results = new List<InstalledProgram>();

            foreach (var (hive, subKeyPath) in UninstallRoots)
            {
                using (RegistryKey uninstallKey = hive.OpenSubKey(subKeyPath))
                {
                    if (uninstallKey == null) continue;

                    foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        using (RegistryKey entryKey = uninstallKey.OpenSubKey(subKeyName))
                        {
                            InstalledProgram program = TryReadEntry(entryKey, includeSystemComponents);
                            if (program != null) results.Add(program);
                        }
                    }
                }
            }

            return results
                .GroupBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static InstalledProgram TryReadEntry(RegistryKey entryKey, bool includeSystemComponents)
        {
            if (entryKey == null) return null;

            string displayName = entryKey.GetValue("DisplayName") as string;
            if (string.IsNullOrWhiteSpace(displayName)) return null;

            if (!includeSystemComponents && entryKey.GetValue("SystemComponent") is int flag && flag == 1)
                return null;

            if (entryKey.GetValue("ParentDisplayName") != null)
                return null;

            string installLocation = entryKey.GetValue("InstallLocation") as string;
            string exePath = TryResolveExecutable(entryKey, installLocation);

            return new InstalledProgram
            {
                DisplayName = displayName.Trim(),
                InstallLocation = installLocation,
                ExecutablePath = exePath
            };
        }

        private static string TryResolveExecutable(RegistryKey entryKey, string installLocation)
        {
            string displayIcon = entryKey.GetValue("DisplayIcon") as string;
            if (!string.IsNullOrWhiteSpace(displayIcon))
            {
                string candidate = displayIcon.Split(',')[0].Trim('"');
                if (candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                    return candidate;
            }

            if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
            {
                try
                {
                    return Directory.EnumerateFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => new FileInfo(f).Length)
                        .FirstOrDefault();
                }
                catch (UnauthorizedAccessException) { return null; }
            }

            return null;
        }
    }
}