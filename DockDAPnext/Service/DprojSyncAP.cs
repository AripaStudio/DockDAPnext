using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DockDAPnext.Service
{
    // Keeps dub.json "sourceFiles" in sync with the compiled .d files declared by the
    // active VisualD project (.visualdproj). First step towards MSBuild-style sync.
    public class DprojSyncAp
    {
        private readonly DubManagerAp _dubManager;

        public DprojSyncAp(DubManagerAp dubManager)
        {
            this._dubManager = dubManager;
        }

        public async Task<DprojSyncResult> SyncAsync()
        {
            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            if (activeProject == null || string.IsNullOrEmpty(activeProject.FullPath))
            {
                return Fail("No active project is selected in Solution Explorer.");
            }

            var dprojPath = activeProject.FullPath;
            var extension = Path.GetExtension(dprojPath)?.ToLowerInvariant();
            if (extension != ".visualdproj" && extension != ".dproj")
            {
                return Fail($"The active project is not a D project (.visualdproj / .dproj): {Path.GetFileName(dprojPath)}");
            }

            var found = await _dubManager.FindDubFileApAsync();
            if (!found.IsSuccess)
            {
                return Fail("dub.json was not found in this solution. Create it first with 'Create or load File'.");
            }

            var dubJsonPath = found.Value;

            // Reuse the in-memory config when the tool already loaded one so unsaved edits
            // survive; otherwise read dub.json from disk.
            var config = CurrentDubFileConfigAp.CurrentDubConfigAp;
            if (config == null)
            {
                var read = _dubManager.ReadDubFileAp(dubJsonPath);
                if (!read.IsSuccess)
                {
                    return Fail(read.Message);
                }

                config = read.Value;
            }

            config.SourceFiles = config.SourceFiles ?? new List<string>();

            var dubDirectory = Path.GetDirectoryName(dubJsonPath);

            List<string> projectFiles;
            try
            {
                projectFiles = ReadSourceFiles(dprojPath, dubDirectory);
            }
            catch (Exception ex)
            {
                return Fail($"Could not read {Path.GetFileName(dprojPath)}: {ex.Message}");
            }

            var oldEntries = config.SourceFiles.Select(p => NormalizeEntry(p, dubDirectory)).ToList();
            var oldSet = new HashSet<string>(oldEntries, StringComparer.OrdinalIgnoreCase);
            var newSet = new HashSet<string>(projectFiles, StringComparer.OrdinalIgnoreCase);

            var result = new DprojSyncResult
            {
                Success = true,
                Added = projectFiles.Count(p => !oldSet.Contains(p)),
                Removed = oldEntries.Count(p => !newSet.Contains(p)),
                Total = projectFiles.Count,
                Config = config,
                DubJsonPath = dubJsonPath,
                DprojPath = dprojPath
            };
            result.Changed = result.Added > 0 || result.Removed > 0;

            if (!result.Changed)
            {
                return result;
            }

            config.SourceFiles = projectFiles;
            if (!_dubManager.SaveFileDubAp(dubJsonPath, config))
            {
                result.Success = false;
                result.ErrorMessage =
                    $"sourceFiles was updated in memory but {Path.GetFileName(dubJsonPath)} could not be written to disk.";
            }

            return result;
        }

        // Extracts every <File path="...d" /> entry (nested under <Folder>) from the project
        // XML and returns it as a path relative to the dub.json folder, '/' separated.
        public static List<string> ReadSourceFiles(string dprojPath, string baseDirectory)
        {
            var document = XDocument.Load(dprojPath);
            var files = new List<string>();

            foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "File"))
            {
                var rawPath = (string)element.Attribute("path") ?? (string)element.Attribute("FileName");
                if (string.IsNullOrWhiteSpace(rawPath))
                {
                    continue;
                }

                if (!rawPath.EndsWith(".d", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fullPath = Path.IsPathRooted(rawPath)
                    ? Path.GetFullPath(rawPath)
                    : Path.GetFullPath(Path.Combine(baseDirectory, rawPath.Replace('/', Path.DirectorySeparatorChar)));

                files.Add(PathHelperAp.MakeRelative(baseDirectory, fullPath));
            }

            return files;
        }

        // Case-insensitive equality ignores 'foo.d' vs 'FOO.D' but also means we rewrite
        // nothing when only formatting (absolute vs relative) differs.
        private static string NormalizeEntry(string path, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                var trimmed = path.Trim().Replace('\\', '/');
                if (Path.IsPathRooted(path))
                {
                    return PathHelperAp.MakeRelative(baseDirectory, Path.GetFullPath(path));
                }

                if (trimmed.StartsWith("./", StringComparison.Ordinal))
                {
                    trimmed = trimmed.Substring(2);
                }

                return trimmed;
            }
            catch (Exception)
            {
                // unusable path text: keep it verbatim so the diff stays honest
                return path.Trim();
            }
        }

        private static DprojSyncResult Fail(string message) => new DprojSyncResult
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
