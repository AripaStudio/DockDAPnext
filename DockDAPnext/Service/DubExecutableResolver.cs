using System;
using System.IO;
using System.Linq;

namespace DockDAPnext.Service
{
    // Finds dub.exe: Visual D remembers where DMD lives (dub ships next to dmd), then
    // the PATH, then the common install folders. PATH is scanned instead of relying on
    // CreateProcess lookup so a missing dub.exe becomes a clear message, not a
    // Win32Exception. Used by DubCommandAp before every build.
    public static class DubExecutableResolver
    {
        public static string ResolveDubExecutable()
        {
            const string fileName = "dub.exe";

            var fromVisualD = ResolveDubFromVisualDInstallDir(fileName);
            if (fromVisualD != null)
            {
                return fromVisualD;
            }

            try
            {
                if (File.Exists(fileName))
                {
                    return Path.GetFullPath(fileName);
                }
            }
            catch (Exception)
            {
                // current directory unusable; fall through to the PATH scan
            }

            var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var entry in pathVariable.Split(Path.PathSeparator))
            {
                var directory = entry.Trim().Trim('"');
                if (directory.Length == 0)
                {
                    continue;
                }

                try
                {
                    var candidate = Path.Combine(directory, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch (ArgumentException)
                {
                    // PATH can contain entries with invalid characters; skip them
                }
            }

            // Last resort: dub may be installed while devenv's own environment block still lacks
            // the PATH entry (Visual Studio builds its environment at startup). Probe the common
            // D toolchain install locations, newest first, so builds still work.
            foreach (var root in new[] { @"C:\D", @"D:\D" })
            {
                try
                {
                    if (!Directory.Exists(root))
                    {
                        continue;
                    }

                    foreach (var dir in Directory.EnumerateDirectories(root, "dmd*").OrderByDescending(d => d))
                    {
                        var candidate = Path.Combine(dir, "windows", "bin", fileName);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }
                catch (Exception)
                {
                    // unreadable root or invalid path: keep probing
                }
            }

            return null;
        }

        private static string ResolveDubFromVisualDInstallDir(string fileName)
        {
            foreach (var view in new[] { Microsoft.Win32.RegistryView.Registry32, Microsoft.Win32.RegistryView.Registry64 })
            {
                try
                {
                    using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view))
                    using (var visualD = baseKey.OpenSubKey(@"SOFTWARE\visuald"))
                    {
                        var dub = FindDubBesideDmd(visualD?.GetValue("DMDInstallDir") as string, fileName);
                        if (dub != null)
                        {
                            return dub;
                        }
                    }
                }
                catch (Exception)
                {
                    // registry view unavailable: try the next one
                }
            }

            try
            {
                using (var visualD = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\visuald"))
                {
                    return FindDubBesideDmd(visualD?.GetValue("DMDInstallDir") as string, fileName);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FindDubBesideDmd(string dmdInstallDir, string fileName)
        {
            if (string.IsNullOrEmpty(dmdInstallDir))
            {
                return null;
            }

            var candidates = new[]
            {
                Path.Combine(dmdInstallDir, "windows", "bin", fileName),
                Path.Combine(dmdInstallDir, "bin", fileName),
                dmdInstallDir.EndsWith("bin", StringComparison.OrdinalIgnoreCase)
                    ? Path.Combine(dmdInstallDir, fileName)
                    : null
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
