using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DockDAPnext.Service
{
    // Answers one question before 'dub build' is started: does this package contain
    // anything dub can compile? Without a single .d file dub gives up with a bare
    // exit code 2 whose details only live in the output pane, so the build is refused
    // earlier and the message says what is missing and how to fix it.
    public static class SourceFileProbeAp
    {
        // null -> the package has sources, let dub run.
        // text -> the reason the build must not start (shown instead of dub's exit code 2).
        public static string ExplainNoSourceFiles(string baseDirectory, DubConfigAp config, BuildConfiguration configuration)
        {
            if (config == null || string.IsNullOrEmpty(baseDirectory))
            {
                return null;
            }

            // targetType 'none' is dub's own "build nothing" (meta packages): no sources is correct.
            var targetType = (config.TargetType ?? string.Empty).Trim().ToLowerInvariant();
            if (targetType == "none")
            {
                return null;
            }

            var declared = new List<string>();
            CollectEntries(declared, config.SourceFiles);
            CollectEntries(declared, ReadList(config.OtherData, "sourceFiles"));
            CollectEntries(declared, ReadList(configuration?.OtherDataBuildConfiguration, "sourceFiles"));

            var existing = 0;
            string firstMissing = null;
            foreach (var entry in declared)
            {
                if (File.Exists(Resolve(baseDirectory, entry)))
                {
                    existing++;
                }
                else if (firstMissing == null)
                {
                    firstMissing = entry;
                }
            }

            if (existing > 0)
            {
                return null;
            }

            // dub compiles mainSourceFile as well, whatever the lists say.
            var mainSourceFile = ReadList(config.OtherData, "mainSourceFile").FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(mainSourceFile) && File.Exists(Resolve(baseDirectory, mainSourceFile)))
            {
                return null;
            }

            foreach (var folder in SourceFolders(config, configuration))
            {
                if (FolderHasDSource(Resolve(baseDirectory, folder)))
                {
                    return null;
                }
            }

            var name = string.IsNullOrWhiteSpace(config.Name) ? "This package" : config.Name;

            if (declared.Count > 0)
            {
                return $"dub.json lists {declared.Count} 'sourceFiles' path(s), but none of them exists on disk " +
                       $"(first missing: {firstMissing}). Correct the paths or press 'Sync source files from .dproj', " +
                       "then build again.";
            }

            return $"{name} has no source files to compile: dub.json declares none in 'sourceFiles' and no .d file " +
                   "sits in a source folder ('source' or 'src') next to it. Add the project's .d files and press " +
                   "'Sync source files from .dproj' (or add their paths in the source files list), then build again.";
        }

        // Folders dub scans for sources: the ones dub.json declares (which replace dub's
        // defaults) plus the ones the selected configuration adds.
        private static List<string> SourceFolders(DubConfigAp config, BuildConfiguration configuration)
        {
            var folders = ReadList(config.OtherData, "sourcePaths");
            if (folders.Count == 0)
            {
                folders.Add("source");
                folders.Add("src");
            }

            folders.AddRange(ReadList(configuration?.OtherDataBuildConfiguration, "sourcePaths"));
            return folders;
        }

        // A folder counts when it really holds a compilable file; enumeration is lazy so
        // one hit stops the walk (large trees are the reason not to materialize a list).
        private static bool FolderHasDSource(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            try
            {
                return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any(IsDSourceFile);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                       ex is System.Security.SecurityException || ex is ArgumentException ||
                                       ex is NotSupportedException)
            {
                // an unreadable folder tells us nothing: keep it out of the way of the check
                return false;
            }
        }

        private static bool IsDSourceFile(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".d", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".di", StringComparison.OrdinalIgnoreCase);
        }

        private static void CollectEntries(List<string> target, IEnumerable<string> entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    target.Add(entry.Trim());
                }
            }
        }

        // dub.json values kept through JsonExtensionData arrive as JToken: sourcePaths may
        // be a string array or a single string, and the key may be absent entirely.
        private static List<string> ReadList(Dictionary<string, object> data, string key)
        {
            var result = new List<string>();
            if (data == null || !data.TryGetValue(key, out var raw) || raw == null)
            {
                return result;
            }

            var token = raw as JToken ?? JToken.FromObject(raw);
            if (token is JArray array)
            {
                foreach (var item in array)
                {
                    if (item?.Type == JTokenType.String && !string.IsNullOrWhiteSpace(item.Value<string>()))
                    {
                        result.Add(item.Value<string>().Trim());
                    }
                }
            }
            else if (token.Type == JTokenType.String && !string.IsNullOrWhiteSpace(token.Value<string>()))
            {
                result.Add(token.Value<string>().Trim());
            }

            return result;
        }

        // Entry text to a path dub would read: '/' separators, relative to the dub.json folder.
        private static string Resolve(string baseDirectory, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                var normalized = path.Trim().Replace('/', Path.DirectorySeparatorChar);
                return Path.IsPathRooted(normalized)
                    ? normalized
                    : Path.GetFullPath(Path.Combine(baseDirectory, normalized));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException ||
                                       ex is PathTooLongException || ex is IOException)
            {
                return string.Empty;
            }
        }
    }
}
