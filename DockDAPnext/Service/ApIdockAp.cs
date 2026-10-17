using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DockDAPnext.Service.InterFaces;

namespace DockDAPnext.Service
{
    // The one class the ViewModels talk to: composes DubManagerAp (the dub.json file),
    // DprojSyncAp (sourceFiles sync), OutPutDirectoryManagerAp (Output folders) and
    // DubCommandAp (running dub) into the IDubManagerService contract.
    public class ApIdockAp : IDubManagerService
    {
        private readonly DubManagerAp _dubManagerAp;
        private readonly DprojSyncAp _dprojSyncAp;

        public ApIdockAp()
        {
            _dubManagerAp = new DubManagerAp();
            _dprojSyncAp = new DprojSyncAp(_dubManagerAp);
        }

        public async Task<OperationResult<DubConfigAp>> CreateOrLoadDubFileApAsync()
        {
            var found = await _dubManagerAp.FindDubFileApAsync();
            if (found.IsSuccess)
            {
                // One mechanism for old files too: EnsureDebugReleaseConfigurationsAp
                // creates Output/debug + Output/release next to dub.json itself and
                // writes the missing debug/release configurations into the file.
                return _dubManagerAp.EnsureDebugReleaseConfigurationsAp(found.Value);
            }

            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            if (activeProject == null || string.IsNullOrEmpty(activeProject.FullPath))
            {
                return OperationResult<DubConfigAp>.Fail(
                    "No dub.json was found and no active project is selected: " + found.Message);
            }

            // FullPath points at the project file itself; dub.json belongs in its folder.
            var projectDirectory = Path.GetDirectoryName(activeProject.FullPath);
            string newFilePath = Path.Combine(projectDirectory, "dub.json");

            return _dubManagerAp.CreateDubFileWithDefaultsAp(newFilePath);
        }

        // Locates the on-disk dub.json (no creation): the AI tab merges a confirmed
        // proposal straight into that file, so it must know exactly which file it is.
        public Task<OperationResult<string>> GetDubFilePathApAsync()
        {
            return _dubManagerAp.FindDubFileApAsync();
        }

        public Task<DubBuildResult> BuildDubApAsync()
        {
            return BuildWithDubAsync("build", "default");
        }

        public Task<DubBuildResult> BuildDebugDubApAsync()
        {
            return BuildWithDubAsync("build --build=debug", "debug");
        }

        public Task<DubBuildResult> BuildReleaseDubApAsync()
        {
            return BuildWithDubAsync("build --build=release", "release");
        }

        private async Task<DubBuildResult> BuildWithDubAsync(string arguments, string configurationName)
        {
            var found = await _dubManagerAp.FindDubFileApAsync();

            if (!found.IsSuccess)
            {
                return DubBuildResult.NotStarted(
                    "dub.json was not found in this solution. Create it first with 'Create or load File'.");
            }

            var dubFilePath = found.Value;

            // dub reads dub.json from its working directory: pass the folder, not the
            // project file path (a file path makes CreateProcess fail).
            var workingDirectory = Path.GetDirectoryName(dubFilePath);
            if (string.IsNullOrEmpty(workingDirectory))
            {
                return DubBuildResult.NotStarted($"Could not resolve the folder that holds {dubFilePath}.");
            }

            var config = _dubManagerAp.ReadDubFileAp(dubFilePath).Value;

            // Select the matching configuration so its targetPath decides where the
            // binary lands. Files without such a configuration (hand-written or from
            // older versions) keep dub's own defaults - --config would make dub fail.
            var configuration = config?.BuildConfigurations?.FirstOrDefault(
                c => string.Equals(c?.Name, configurationName, StringComparison.OrdinalIgnoreCase));
            if (configuration != null && !string.IsNullOrWhiteSpace(configuration.Name))
            {
                arguments += configuration.Name.Any(char.IsWhiteSpace)
                    ? " --config=\"" + configuration.Name + "\""
                    : " --config=" + configuration.Name;
            }

            var result = await DubCommandAp.BuildDubAsync(workingDirectory, arguments);
            result.WorkingDirectory = workingDirectory;
            result.OutputDirectory = ResolveOutputDirectory(workingDirectory, config, configuration);
            return result;
        }

        // Where dub puts the binary for this build: the configuration's targetPath,
        // else the package targetPath, else the dub.json folder (dub's own fallback).
        private static string ResolveOutputDirectory(
            string workingDirectory, DubConfigAp config, BuildConfiguration configuration)
        {
            var targetPath = configuration?.TargetPath ?? config?.TargetPath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return workingDirectory;
            }

            try
            {
                return Path.GetFullPath(Path.Combine(
                    workingDirectory, targetPath.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (ArgumentException)
            {
                return workingDirectory;
            }
        }

        // Run-after-build: launch <targetName>.exe from the folder the build wrote to
        // (the configuration's targetPath), falling back to the dub.json folder. Only
        // an executable targetType can run; libraries/static libs are refused.
        public Task<DubRunResult> RunBuiltExecutableApAsync(string workingDirectory, string outputDirectory)
        {
            // Launching the program is synchronous (Process.Start), so the Task is
            // completed right away; the interface stays async for future providers.
            return Task.FromResult(RunBuiltExecutableCore(workingDirectory, outputDirectory));
        }

        private DubRunResult RunBuiltExecutableCore(string workingDirectory, string outputDirectory)
        {
            if (string.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                return DubRunResult.Fail("The build folder is unknown; build first, then run.");
            }

            // Prefer the on-disk dub.json; it wins over a possibly stale in-memory config.
            var config = _dubManagerAp.ReadDubFileAp(Path.Combine(workingDirectory, "dub.json")).Value
                ?? CurrentDubFileConfigAp.CurrentDubConfigAp;

            if (config == null)
            {
                return DubRunResult.Fail("dub.json was not loaded; create or load it first.");
            }

            var targetType = (config.TargetType ?? "executable").Trim().ToLowerInvariant();
            if (targetType != "executable")
            {
                return DubRunResult.Fail($"targetType is '{config.TargetType}', not 'executable': a library produces no program to run.");
            }

            var targetName = string.IsNullOrWhiteSpace(config.TargetName) ? config.Name : config.TargetName;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return DubRunResult.Fail("dub.json declares neither 'targetName' nor 'name', so the output file cannot be predicted.");
            }

            var exePath = FindBuiltExecutable(outputDirectory, workingDirectory, targetName);
            if (exePath == null)
            {
                var expectedFolder = string.IsNullOrWhiteSpace(outputDirectory) ? workingDirectory : outputDirectory;
                return DubRunResult.Fail($"The program was not found in {expectedFolder}. Build the project first.");
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false
                });

                return DubRunResult.Ok(exePath);
            }
            catch (Exception e)
            {
                return DubRunResult.Fail($"Could not start {exePath}: {e.Message}");
            }
        }

        // The configuration's targetPath folder first, then the dub.json folder for
        // builds that were not redirected.
        private static string FindBuiltExecutable(string outputDirectory, string workingDirectory, string targetName)
        {
            foreach (var folder in new[] { outputDirectory, workingDirectory }
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var candidate = Path.Combine(folder, targetName + ".exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public async Task<OperationResult<string>> OpenDubFileApAsync()
        {
            var found = await _dubManagerAp.FindDubFileApAsync();
            if (!found.IsSuccess)
            {
                return OperationResult<string>.Fail(found.Message);
            }

            await DubCommandAp.OpenDubFileinVisualStudioApAsync(found.Value);
            return OperationResult<string>.Ok(found.Value, $"Opened {found.Value}.");
        }

        public async Task<bool> SaveFileApAsync(DubConfigAp dubConfigAp)
        {
            var found = await _dubManagerAp.FindDubFileApAsync();
            if (!found.IsSuccess)
            {
                return false;
            }

            return _dubManagerAp.SaveFileDubAp(found.Value, dubConfigAp);
        }

        public Task<DprojSyncResult> SyncSourceFilesFromDprojApAsync()
        {
            return _dprojSyncAp.SyncAsync();
        }
    }
}
