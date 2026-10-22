using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using OutputWindowPane = Community.VisualStudio.Toolkit.OutputWindowPane;

namespace DockDAPnext.Service
{
    // Runs dub.exe for one build: resolves the executable, streams its output into the
    // 'Dub Manager' Output pane, and turns the exit code into a DubBuildResult.
    public static class DubCommandAp
    {
        public static async Task<DubBuildResult> BuildDubAsync(string workingDirectory, string arguments)
        {

            OutputWindowPane dubPane = await GetOrCreatePaneAsync();

            if (dubPane == null)
            {
                return DubBuildResult.NotStarted("The 'Dub Manager' Output pane is unavailable.");
            }

            await dubPane.ActivateAsync();

            var dubExecutable = DubExecutableResolver.ResolveDubExecutable();
            if (dubExecutable == null)
            {
                const string reason = "dub.exe was not found on PATH, via Visual D's DMDInstallDir, or in common D install folders. " +
                    "Install the D toolchain (its bin folder contains dub.exe) or add that folder to PATH.";
                await dubPane.WriteLineAsync($"[DOCKDAP] {reason}");
                return DubBuildResult.NotStarted(reason);
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = dubExecutable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // dub launches the compiler (dmd/ldc2) from PATH. When dub itself was found through
            // the known-location fallback, devenv's PATH lacks that folder, so give the child
            // process dub's own directory first - its sibling compiler becomes reachable.
            var dubDirectory = Path.GetDirectoryName(dubExecutable);
            if (!string.IsNullOrEmpty(dubDirectory))
            {
                var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (currentPath.IndexOf(dubDirectory, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    processStartInfo.EnvironmentVariables["PATH"] =
                        dubDirectory + Path.PathSeparator + currentPath;
                }
            }

            try
            {
                await dubPane.WriteLineAsync($"Starting: dub {arguments} (cwd: {workingDirectory})");

                using (Process process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        return DubBuildResult.NotStarted("dub.exe could not be started (Process.Start returned null).");
                    }

                    // Dedicated reader tasks instead of BeginReadLine + async-void handlers:
                    // an exception escaping an async void event handler would take down devenv.
                    var stdoutReader = StreamToPaneAsync(dubPane, process.StandardOutput, line => line);
                    var stderrReader = StreamToPaneAsync(dubPane, process.StandardError, line => "Error: " + line);

                    await process.WaitForExitAsync();
                    await Task.WhenAll(stdoutReader, stderrReader);

                    int exitCode = process.ExitCode;
                    await dubPane.WriteLineAsync(exitCode == 0
                        ? $"Dub command finished successfully (exit code 0): {arguments}"
                        : $"Dub command failed (exit code {exitCode}): {arguments}");

                    return new DubBuildResult { Started = true, ExitCode = exitCode };
                }
            }
            catch (Exception e)
            {
                await dubPane.WriteLineAsync($"[DOCKDAP] failed to run dub: {e.Message}");
                return DubBuildResult.NotStarted($"Could not run dub.exe: {e.Message}");
            }
        }

        // A message that never starts a process (the pre-build checks) still belongs in the
        // pane the failure message points at; best effort - a pane that cannot be reached
        // must not turn a diagnosis into an error of its own.
        public static async Task WriteToPaneAsync(string message)
        {
            try
            {
                var pane = await GetOrCreatePaneAsync();
                if (pane != null)
                {
                    await pane.WriteLineAsync("[DOCKDAP] " + message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DOCKDAP] pane write failed: " + ex.Message);
            }
        }

        // Looks the 'Dub Manager' pane up, creating it on the first use of a session:
        // GetOutputWindowPaneAsync(guid) can only LOOK UP a custom pane, it builds the
        // wrapper with an empty pane name, so IVsOutputWindow.CreatePane is skipped and
        // GetPane fails (null) until the pane exists.
        private static async Task<OutputWindowPane> GetOrCreatePaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OutputWindowPane dubPane = await VS.Windows.GetOutputWindowPaneAsync(GuidDubAp.DubManagerPaneGuid);

            if (dubPane == null)
            {
                await CreateDubManagerPaneAsync();
                dubPane = await VS.Windows.GetOutputWindowPaneAsync(GuidDubAp.DubManagerPaneGuid);
            }

            return dubPane;
        }

        // Streams one child-process output pipe into the Output pane, line by line.
        private static async Task StreamToPaneAsync(OutputWindowPane pane, StreamReader reader, Func<string, string> decorate)
        {
            try
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    await pane.WriteLineAsync(decorate(line));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DOCKDAP] output stream failed: {ex.Message}");
            }
        }

        // Creates the extension's 'Dub Manager' output pane (stable GUID) so the first build
        // of a session does not abort before dub even starts.
        private static async Task CreateDubManagerPaneAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var output = await VS.Services.GetOutputWindowAsync();
                if (output != null)
                {
                    Guid paneId = GuidDubAp.DubManagerPaneGuid;
                    // HRESULT ignored on purpose: CreatePane "errors" when the pane already exists.
                    output.CreatePane(ref paneId, "Dub Manager", 1, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DOCKDAP] create Dub Manager pane failed: " + ex.Message);
            }
        }

        public static async Task OpenDubFileinVisualStudioApAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            await VS.Documents.OpenAsync(path);
        }
    }
}
