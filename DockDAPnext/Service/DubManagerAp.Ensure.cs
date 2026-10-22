using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DockDAPnext.Service
{
    // The dub.json upgrade half of DubManagerAp: every file the extension loads gains
    // the debug/release configuration pair, so the three build buttons always find a
    // matching --config and both executables land in Output/debug / Output/release.
    // There is exactly one mechanism: paths are anchored at the dub.json FOLDER (never
    // at the active project) and the Output trio is created on disk here, so the
    // folders exist no matter which caller reaches this code. Name matching is
    // case-insensitive so an existing 'Debug' entry is never duplicated as 'debug';
    // an unreadable file fails with a message and is never rewritten.
    public partial class DubManagerAp
    {
        public OperationResult<DubConfigAp> EnsureDebugReleaseConfigurationsAp(string dubFilePath)
        {
            if (string.IsNullOrEmpty(dubFilePath) || !File.Exists(dubFilePath))
            {
                return OperationResult<DubConfigAp>.Fail($"dub.json was not found: {dubFilePath}");
            }

            var read = ReadDubFileAp(dubFilePath);
            if (!read.IsSuccess)
            {
                return OperationResult<DubConfigAp>.Fail(read.Message, read.Exception);
            }

            var config = read.Value;
            if (config.BuildConfigurations == null)
            {
                config.BuildConfigurations = new List<BuildConfiguration>();
            }

            var dubDirectory = Path.GetDirectoryName(dubFilePath);
            if (string.IsNullOrEmpty(dubDirectory))
            {
                return OperationResult<DubConfigAp>.Fail($"The folder that holds {dubFilePath} could not be resolved.");
            }

            // The folders are created HERE, not by the caller: relying on an active
            // project let old dub.json files load without any Output folder on disk.
            var folders = CreateOutputFolders(dubDirectory);
            if (!folders.IsSuccess)
            {
                return OperationResult<DubConfigAp>.Fail(folders.Message, folders.Exception);
            }

            bool changed = EnsureConfigurationAp(config, "debug", dubDirectory,
                folders.Value.DebugOutputDirectoryInfo);
            changed |= EnsureConfigurationAp(config, "release", dubDirectory,
                folders.Value.ReleaseOutputDirectoryInfo);

            // An explicitly declared targetType (e.g. 'library') is the user's decision and
            // stays; an omitted one would resolve to 'library' in dub, so the upgraded
            // file declares 'executable' to actually produce runnable binaries.
            if (string.IsNullOrWhiteSpace(config.TargetType))
            {
                config.TargetType = "executable";
                changed = true;
            }

            if (changed)
            {
                // Loud, not silent: the in-memory config must never claim a disk state
                // that dub.json does not have.
                var saved = SaveFileDubAp(dubFilePath, config);
                if (!saved.IsSuccess)
                {
                    return OperationResult<DubConfigAp>.Fail(
                        $"{dubFilePath} was upgraded in memory but {saved.Message}", saved.Exception, config);
                }
            }

            return OperationResult<DubConfigAp>.Ok(config, changed
                ? $"dub.json upgraded: added the debug/release configurations; Output folders created at {folders.Value.OutputDirectoryInfo.FullName}"
                : $"dub.json already carries the debug/release pair; Output folders verified at {folders.Value.OutputDirectoryInfo.FullName}");
        }

        // Creates <baseDirectory>/Output, /Output/debug and /Output/release. Idempotent:
        // CreateDirectory returns the folder whether it existed or not, so no
        // Exists/Create branching is needed. Failures carry their reason in Message.
        public OperationResult<OutputDirectoryPathsStruct> CreateOutputFolders(string baseDirectory)
        {
            if (string.IsNullOrEmpty(baseDirectory))
            {
                return OperationResult<OutputDirectoryPathsStruct>.Fail(
                    "The project folder is unknown, so Output/debug and Output/release could not be created.");
            }

            try
            {
                var outputDirectory = Directory.CreateDirectory(Path.Combine(baseDirectory, "Output"));

                return OperationResult<OutputDirectoryPathsStruct>.Ok(new OutputDirectoryPathsStruct
                {
                    OutputDirectoryInfo = outputDirectory,
                    DebugOutputDirectoryInfo = Directory.CreateDirectory(Path.Combine(outputDirectory.FullName, "debug")),
                    ReleaseOutputDirectoryInfo = Directory.CreateDirectory(Path.Combine(outputDirectory.FullName, "release")),
                });
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return OperationResult<OutputDirectoryPathsStruct>.Fail(
                    $"Output folders could not be created under {baseDirectory}: {ex.Message}", ex);
            }
        }

        // Adds one named configuration when it is missing. An existing configuration
        // without a targetPath gets one too (the old file kept 'release' bare, so
        // release builds landed in the project root); a targetPath the user already
        // chose is never overwritten.
        private static bool EnsureConfigurationAp(
            DubConfigAp config, string name, string dubDirectory, DirectoryInfo outputDirectory)
        {
            var targetPath = ResolveTargetPath(dubDirectory, outputDirectory, name);
            var existing = config.BuildConfigurations.FirstOrDefault(
                c => string.Equals(c?.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(existing.TargetPath))
                {
                    return false;
                }

                existing.TargetPath = targetPath;
                return true;
            }

            config.BuildConfigurations.Add(new BuildConfiguration
            {
                Name = name,
                TargetPath = targetPath,
            });

            return true;
        }

        // Relative to the dub.json folder; absolute when the Output folder sits on a
        // different drive, which a relative string cannot express.
        private static string ResolveTargetPath(string dubDirectory, DirectoryInfo outputDirectory, string name)
        {
            if (outputDirectory == null || string.IsNullOrEmpty(dubDirectory))
            {
                return "Output/" + name;
            }

            return PathHelperAp.MakeRelative(dubDirectory, outputDirectory.FullName);
        }
    }
}
