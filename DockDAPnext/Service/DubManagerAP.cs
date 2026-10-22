using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Project = Community.VisualStudio.Toolkit.Project;

namespace DockDAPnext.Service
{
    // Everything that touches the dub.json FILE: locating it in the solution,
    // reading (with legacy-key migration), creating a default one, and saving.
    public partial class DubManagerAp
    {
        // dub.json must never be picked from build output or VCS metadata folders.
        private static readonly string[] ExcludedFolderNames = { ".git", ".vs", "bin", "obj" };

        public async System.Threading.Tasks.Task<OperationResult<string>> FindDubFileApAsync()
        {
            // An explicit selection wins over every heuristic: the user decides the managed file.
            var selected = CurrentDubFileConfigAp.SelectedProject;
            if (selected != null && !string.IsNullOrEmpty(selected.Directory))
            {
                var selectedDubFile = FindDubJsonInDirectory(selected.Directory);
                if (selectedDubFile != null)
                {
                    return OperationResult<string>.Ok(selectedDubFile);
                }

                // Loud, not silent: searching other projects here would edit the wrong file.
                return OperationResult<string>.Fail(
                    $"The selected project '{selected.Name}' has no dub.json in {selected.Directory}. "
                    + "Use 'Create or load File' to create it there.");
            }

            // Without a selection: active project first, because the first file found at random is often wrong.
            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            var activeDubFile = FindDubJsonInDirectory(GetProjectDirectory(activeProject));
            if (activeDubFile != null)
            {
                return OperationResult<string>.Ok(activeDubFile);
            }

            var getAllProject = await VS.Solutions.GetAllProjectsAsync();

            foreach (Project project in getAllProject)
            {
                var dubFile = FindDubJsonInDirectory(GetProjectDirectory(project));
                if (dubFile != null)
                {
                    return OperationResult<string>.Ok(dubFile);
                }
            }

            return OperationResult<string>.Fail(
                "No dub.json exists in this solution (the active project and every other project were checked).");
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

        internal static string FindDubJsonInDirectory(string rootPath)
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
            catch (Exception ex)
            {
                // Inaccessible or malformed branch: skip it, but say so instead of failing silently.
                Debug.WriteLine($"[DOCKDAP] dub.json search skipped {currentDirectory}: {ex.Message}");
            }

            return null;
        }

        public OperationResult<DubConfigAp> ReadDubFileAp(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return OperationResult<DubConfigAp>.Fail("No dub.json path was given.");
            }

            if (!File.Exists(path))
            {
                return OperationResult<DubConfigAp>.Fail($"dub.json does not exist: {path}");
            }

            try
            {
                string jsonString = File.ReadAllText(path);

                var config = JsonConvert.DeserializeObject<DubConfigAp>(jsonString);
                if (config == null)
                {
                    return OperationResult<DubConfigAp>.Fail($"dub.json contains no JSON object: {path}");
                }

                MigrateLegacyBuildConfigurations(config);
                return OperationResult<DubConfigAp>.Ok(config);
            }
            catch (JsonException ex)
            {
                // Broken JSON must not reach the ViewModel: it used to crash devenv.
                return OperationResult<DubConfigAp>.Fail(
                    $"dub.json is not valid JSON, so it was left untouched: {path} ({ex.Message})", ex);
            }
            catch (IOException ex)
            {
                return OperationResult<DubConfigAp>.Fail($"dub.json could not be read: {path} ({ex.Message})", ex);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                // Access denied is a result, not an exception to throw across the service boundary.
                return OperationResult<DubConfigAp>.Fail($"dub.json could not be read (access denied): {path} ({ex.Message})", ex);
            }
        }

        // Files written by older versions stored the list under 'buildConfigurations',
        // a key dub ignores, so their flags and options never reached a build. Move that
        // list into the real 'configurations' field once; the next save writes it there
        // and drops the stale key. The real key always wins when both are present.
        private static void MigrateLegacyBuildConfigurations(DubConfigAp config)
        {
            if (config.OtherData == null ||
                !config.OtherData.TryGetValue("buildConfigurations", out var legacyToken))
            {
                return;
            }

            config.OtherData.Remove("buildConfigurations");

            if (config.BuildConfigurations != null && config.BuildConfigurations.Count > 0)
            {
                return;
            }

            try
            {
                var token = legacyToken as Newtonsoft.Json.Linq.JToken
                    ?? Newtonsoft.Json.Linq.JToken.FromObject(legacyToken);
                config.BuildConfigurations = token.ToObject<List<BuildConfiguration>>()
                    ?? new List<BuildConfiguration>();
            }
            catch (JsonException)
            {
                // unreadable legacy list: keep an empty one instead of crashing the UI
                config.BuildConfigurations = new List<BuildConfiguration>();
            }
        }

        public OperationResult<DubConfigAp> CreateDubFileWithDefaultsAp(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return OperationResult<DubConfigAp>.Fail("No target path for dub.json was given.");
            }

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                return OperationResult<DubConfigAp>.Fail($"The folder of {path} could not be resolved.");
            }

            // The Output trio is part of the default file itself: the configurations
            // below must never point at folders that do not exist yet.
            var folders = CreateOutputFolders(directory);
            if (!folders.IsSuccess)
            {
                return OperationResult<DubConfigAp>.Fail(folders.Message, folders.Exception);
            }

            DubConfigAp config = new DubConfigAp
            {
                // dub warns when a package name is not lower case; targetName keeps the old exe name.
                Name = "initial-dubfile-dockap",
                TargetName = "initial-DubFile-DockAP",
                Description = "A new Dub.json file by DockDAP , AripaParsStudio",
                Authors = new List<string>() { "DockAPUser" },
                // Omitted targetType makes dub resolve 'library' on current toolchains,
                // which would put a .lib into Output and leave nothing to run.
                TargetType = "executable",
                // One configuration per Output folder. 'default' is first because dub picks
                // the first configuration when --config is not passed: a plain build then
                // lands in Output, debug in Output/debug, release in Output/release.
                // Paths are relative to the dub.json folder, which sits next to Output.
                TargetPath = "Output",
                BuildConfigurations = new List<BuildConfiguration>
                {
                    new BuildConfiguration { Name = "default" },
                    new BuildConfiguration { Name = "debug", TargetPath = "Output/debug" },
                    new BuildConfiguration { Name = "release", TargetPath = "Output/release" },
                },
            };

            var written = WriteAllTextAtomic(path, JsonConvert.SerializeObject(config, SerializerSettings()));
            if (!written.IsSuccess)
            {
                return OperationResult<DubConfigAp>.Fail($"dub.json could not be written: {written.Message}", written.Exception);
            }

            return OperationResult<DubConfigAp>.Ok(config,
                $"dub.json created at {path}; Output folders created at {folders.Value.OutputDirectoryInfo.FullName}");
        }

        public OperationResult<string> SaveFileDubAp(string path, DubConfigAp inputConfigAp)
        {
            if (string.IsNullOrEmpty(path))
            {
                return OperationResult<string>.Fail("No dub.json path was given.");
            }

            // A null model would serialize to the literal 'null' and wipe the file.
            if (inputConfigAp == null)
            {
                return OperationResult<string>.Fail($"Nothing was saved: the in-memory twin of {path} is empty.");
            }

            if (!File.Exists(path))
            {
                return OperationResult<string>.Fail($"dub.json does not exist (any more): {path}");
            }

            string jsonString;
            try
            {
                jsonString = JsonConvert.SerializeObject(inputConfigAp, SerializerSettings());
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                return OperationResult<string>.Fail($"dub.json could not be serialized: {path} ({ex.Message})", ex);
            }

            return WriteAllTextAtomic(path, jsonString);
        }

        // Temp file + replace: auto-sync writes on a worker thread while a reader may load the
        // file, so a half-written or torn dub.json must never be observable on disk.
        private static OperationResult<string> WriteAllTextAtomic(string path, string contents)
        {
            var tempPath = path + ".tmp-" + Guid.NewGuid().ToString("N");

            try
            {
                File.WriteAllText(tempPath, contents);

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }

                return OperationResult<string>.Ok(path);
            }
            catch (Exception ex) when (
                ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                return OperationResult<string>.Fail($"{path} could not be written: {ex.Message}", ex);
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Best effort: a leftover temp file must not hide the real write result.
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
}
