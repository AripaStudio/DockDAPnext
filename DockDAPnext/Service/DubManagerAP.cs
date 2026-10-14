using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DockDAPnext.Service.InterFaces;
using OutputWindowPane = Community.VisualStudio.Toolkit.OutputWindowPane;
using Project = Community.VisualStudio.Toolkit.Project;
using Windows = Community.VisualStudio.Toolkit.Windows;

namespace DockDAPnext.Service
{
    public class DubConfigAP
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("authors")] public List<string> Authors { get; set; } = new List<string>();

        [JsonProperty("license")] public string License { get; set; }

        [JsonProperty("targetName")] public string TargetName { get; set; }

        [JsonProperty("targetType")] public string TargetType { get; set; }

        [JsonProperty("sourceFiles")] public List<string> SourceFiles { get; set; } = new List<string>();

        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies { get; set; } = new Dictionary<string, string>();

        [JsonProperty("buildConfigurations")]
        public List<BuildConfiguration> BuildConfigurations { get; set; } = new List<BuildConfiguration>();

        [JsonExtensionData]
        public Dictionary<string, object> OtherData { get; set; } = new Dictionary<string, object>();
    }

    public class BuildConfiguration
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("dflags")] public List<string> DFlags { get; set; } = new List<string>();

        [JsonProperty("lflags")] public List<string> LFlags { get; set; } = new List<string>();

        [JsonProperty("versions")] public List<string> Versions { get; set; } = new List<string>();

        [JsonProperty("buildOptions")] public List<BuildOptionTypes> BuildOptions { get; set; } = new List<BuildOptionTypes>();

        [JsonExtensionData]
        public Dictionary<string, object> OtherDataBuildConfiguration { get; set; } = new Dictionary<string, object>();

    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum BuildOptionTypes
    {
        debugMode,
        releaseMode,
        coverage,
        unoptimized,
        inline,
        noBoundsCheck,
        debugInfo,
        optimize,
        warnings,
        warningsAsErrors
    }

    // Outcome of one 'dub build' run; the ViewModel turns it into visible status + a notification.
    public class DubBuildResult
    {
        public bool Started { get; set; }

        public int ExitCode { get; set; }

        public string FailureReason { get; set; }

        public bool Success => Started && ExitCode == 0;

        public static DubBuildResult NotStarted(string reason) =>
            new DubBuildResult { Started = false, FailureReason = reason };
    }

    public class DubManagerAP
    {
        // dub.json must never be picked from build output or VCS metadata folders.
        private static readonly string[] ExcludedFolderNames = { ".git", ".vs", "bin", "obj" };

        public async Task<string> FindDubFileAP()
        {
            // Active project first: in a multi-project solution the first dub.json found
            // by walking every project in arbitrary order belongs to the wrong project.
            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            var activeDubFile = FindDubJsonInDirectory(GetProjectDirectory(activeProject));
            if (activeDubFile != null)
            {
                return activeDubFile;
            }

            var getAllProject = await VS.Solutions.GetAllProjectsAsync();

            foreach (Project project in getAllProject)
            {
                var dubFile = FindDubJsonInDirectory(GetProjectDirectory(project));
                if (dubFile != null)
                {
                    return dubFile;
                }
            }

            return null;
        }

        private static string GetProjectDirectory(Project project)
        {
            if (project == null || string.IsNullOrEmpty(project.FullPath))
            {
                return null;
            }

            try
            {
                return Path.GetDirectoryName(project.FullPath);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string FindDubJsonInDirectory(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                return null;
            }

            var direct = Path.Combine(rootPath, "dub.json");
            if (File.Exists(direct))
            {
                return direct;
            }

            return FindDubJsonRecursive(rootPath);
        }

        // Manual recursion instead of SearchOption.AllDirectories so excluded folders are
        // never even entered (EnumerationOptions is unavailable on this target framework).
        private static string FindDubJsonRecursive(string currentDirectory)
        {
            try
            {
                var found = Directory.EnumerateFiles(currentDirectory, "dub.json").FirstOrDefault();
                if (found != null)
                {
                    return found;
                }

                foreach (var subDirectory in Directory.EnumerateDirectories(currentDirectory))
                {
                    if (ExcludedFolderNames.Contains(Path.GetFileName(subDirectory), StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var foundInSubDirectory = FindDubJsonRecursive(subDirectory);
                    if (foundInSubDirectory != null)
                    {
                        return foundInSubDirectory;
                    }
                }
            }
            catch (Exception)
            {
                // inaccessible or malformed path: skip this branch, keep searching
            }

            return null;
        }

        public DubConfigAP ReadDubFileAP(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                string jsonString = File.ReadAllText(path);

                return JsonConvert.DeserializeObject<DubConfigAP>(jsonString);
            }
            catch (JsonException)
            {
                // Broken JSON must not reach the ViewModel: it used to crash devenv.
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        public DubConfigAP CreateDubFileWithDefaultsAP(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            DubConfigAP config = new DubConfigAP
            {
                // dub warns when a package name is not lower case; targetName keeps the old exe name.
                Name = "initial-dubfile-dockap",
                TargetName = "initial-DubFile-DockAP",
                Description = "A new Dub.json file by DockDAP , AripaParsStudio",
                Authors = new List<string>() { "DockAPUser" },
            };
            string jsonString = JsonConvert.SerializeObject(config, SerializerSettings());
            File.WriteAllText(path, jsonString);

            return config;
        }

        public bool SaveFileDubAP(string path, DubConfigAP inputConfigAp)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var jsonString = JsonConvert.SerializeObject(inputConfigAp, SerializerSettings());
                System.IO.File.WriteAllText(path, jsonString);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        // Null fields (e.g. "license": null) are dropped: dub expects strings where we hold
        // strings, and a null literal can fail its config reader.
        private static JsonSerializerSettings SerializerSettings() => new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    public static class DubCommandAP
    {
        public static async Task<DubBuildResult> BuildDubAsync(string workingDirectory, string arguments)
        {

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OutputWindowPane dubPane = await VS.Windows.GetOutputWindowPaneAsync(GuidDubAP.DubManagerPaneGuid);

            if (dubPane == null)
            {
                // GetOutputWindowPaneAsync(guid) can only LOOK UP a custom pane: it builds the
                // wrapper with an empty pane name, so IVsOutputWindow.CreatePane is skipped and
                // GetPane fails (null) the first time in a session. Create the pane ourselves,
                // then retry the lookup.
                await CreateDubManagerPaneAsync();
                dubPane = await VS.Windows.GetOutputWindowPaneAsync(GuidDubAP.DubManagerPaneGuid);
            }

            if (dubPane == null)
            {
                return DubBuildResult.NotStarted("The 'Dub Manager' Output pane is unavailable.");
            }

            await dubPane.ActivateAsync();

            var dubExecutable = ResolveDubExecutable();
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

                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        return DubBuildResult.NotStarted("dub.exe could not be started (Process.Start returned null).");
                    }

                    process.OutputDataReceived += async (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            return;
                        }

                        try
                        {
                            await dubPane.WriteLineAsync(e.Data);
                        }
                        catch (Exception ex)
                        {
                            // async void handler: an escaping exception would kill devenv.
                            Debug.WriteLine($"[DOCKDAP] output write failed: {ex.Message}");
                        }
                    };

                    process.ErrorDataReceived += async (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            return;
                        }

                        try
                        {
                            await dubPane.WriteLineAsync("Error: " + e.Data);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DOCKDAP] error write failed: {ex.Message}");
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

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
                    Guid paneId = GuidDubAP.DubManagerPaneGuid;
                    // HRESULT ignored on purpose: CreatePane "errors" when the pane already exists.
                    output.CreatePane(ref paneId, "Dub Manager", 1, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DOCKDAP] create Dub Manager pane failed: " + ex.Message);
            }
        }

        // PATH scan instead of relying on CreateProcess lookup, so a missing dub.exe is
        // reported as a clear message instead of a Win32Exception.
        private static string ResolveDubExecutable()
        {
            const string fileName = "dub.exe";

            // Visual D remembers where DMD lives, and a D install ships dub next to dmd,
            // so Visual D's folder is checked before the generic known-location scan below.
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

        public static async Task OpenDubFileinVisualStudioAP(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            await VS.Documents.OpenAsync(path);
        }
    }

    public class APIdockAP : IdubManagerService
    {
        private DubManagerAP dubManagerAP;
        private DprojSyncAP dprojSyncAP;

        public APIdockAP()
        {
            dubManagerAP = new DubManagerAP();
            dprojSyncAP = new DprojSyncAP(dubManagerAP);
        }

        public async Task<DubConfigAP> CreateOrLoadDubFileAP()
        {


            var dubFilePath = await dubManagerAP.FindDubFileAP();
            if (dubFilePath != null)
            {
                return dubManagerAP.ReadDubFileAP(dubFilePath);
            }

            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            if (activeProject == null || string.IsNullOrEmpty(activeProject.FullPath))
            {
                return null;
            }

            // FullPath points at the project file itself; dub.json belongs in its folder.
            var projectDirectory = System.IO.Path.GetDirectoryName(activeProject.FullPath);
            string newFilePath = System.IO.Path.Combine(projectDirectory, "dub.json");

            return dubManagerAP.CreateDubFileWithDefaultsAP(newFilePath);
        }

        public Task<DubBuildResult> BuildDubAP()
        {
            return BuildWithDubAsync("build");
        }

        public Task<DubBuildResult> BuildDebugDubAP()
        {
            return BuildWithDubAsync("build --build=debug");
        }

        public Task<DubBuildResult> BuildReleaseDubAP()
        {
            return BuildWithDubAsync("build --build=release");
        }

        private async Task<DubBuildResult> BuildWithDubAsync(string arguments)
        {
            var dubFilePath = await dubManagerAP.FindDubFileAP();

            if (dubFilePath == null)
            {
                return DubBuildResult.NotStarted(
                    "dub.json was not found in this solution. Create it first with 'Create or load File'.");
            }

            // dub reads dub.json from its working directory: pass the folder, not the
            // project file path (a file path makes CreateProcess fail).
            var workingDirectory = System.IO.Path.GetDirectoryName(dubFilePath);
            if (string.IsNullOrEmpty(workingDirectory))
            {
                return DubBuildResult.NotStarted($"Could not resolve the folder that holds {dubFilePath}.");
            }

            return await DubCommandAP.BuildDubAsync(workingDirectory, arguments);
        }

        public async Task OpenDubFileAP()
        {

            var FindDubFilePath = await dubManagerAP.FindDubFileAP();

            if (FindDubFilePath == null)
            {
                return;
            }

            await DubCommandAP.OpenDubFileinVisualStudioAP(FindDubFilePath);
        }

        public async Task<bool> SaveFileAP(DubConfigAP dubConfigAp)
        {
            var FindDubFilePath = await dubManagerAP.FindDubFileAP();
            if (FindDubFilePath == null)
            {
                return false;
            }

            return dubManagerAP.SaveFileDubAP(FindDubFilePath, dubConfigAp);
        }

        public Task<DprojSyncResult> SyncSourceFilesFromDprojAP()
        {
            return dprojSyncAP.SyncAsync();
        }
    }

    public static class GuidDubAP
    {
        public const string DubManagerPaneString = "d4b8f6e2-3c5a-4f2e-9f7b-1a2b3c4d5e6f";
        public static readonly Guid DubManagerPaneGuid = new Guid(DubManagerPaneString);
    }
}
