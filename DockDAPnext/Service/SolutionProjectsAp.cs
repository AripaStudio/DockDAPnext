using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Project = Community.VisualStudio.Toolkit.Project;

namespace DockDAPnext.Service
{
    // Builds the project list behind the tool window's selector, so the tabs never manage a guessed dub.json.
    public class SolutionProjectsAp
    {
        private static readonly string[] DprojExtensions = { ".visualdproj", ".dproj" };

        public async Task<List<SolutionDubProject>> DiscoverApAsync()
        {
            var projects = await VS.Solutions.GetAllProjectsAsync();
            var result = new List<SolutionDubProject>();

            foreach (Project project in projects)
            {
                var entry = ToEntry(project);
                if (entry == null)
                {
                    continue;
                }

                // Two project files in one folder would load the same dub.json twice.
                if (result.Any(p => SameDirectory(p.Directory, entry.Directory)))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        private static SolutionDubProject ToEntry(Project project) =>
            CreateEntry(project?.Name, project?.FullPath);

        // Pure mapping: the rules are testable without a live Visual Studio solution.
        public static SolutionDubProject CreateEntry(string projectName, string projectPath)
        {
            var directory = GetDirectory(projectPath);
            if (directory == null)
            {
                return null;
            }

            var dprojPath = IsDProject(projectPath) ? projectPath : string.Empty;
            var dubJsonPath = DubManagerAp.FindDubJsonInDirectory(directory) ?? string.Empty;

            // A D project without dub.json is still listed: that is where the user wants to create it.
            if (string.IsNullOrEmpty(dprojPath) && string.IsNullOrEmpty(dubJsonPath))
            {
                return null;
            }

            return new SolutionDubProject
            {
                Name = string.IsNullOrWhiteSpace(projectName) ? Path.GetFileName(directory) : projectName,
                DprojPath = dprojPath,
                Directory = directory,
                DubJsonPath = dubJsonPath,
            };
        }

        public static bool IsDProject(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return false;
            }

            try
            {
                var extension = Path.GetExtension(projectPath);
                return DprojExtensions.Contains(extension ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                // A malformed path must read as 'not a D project', never crash the discovery.
                return false;
            }
        }

        // True when the path lives inside the directory. Used to ignore project events that
        // belong to another project of the same solution.
        public static bool IsInsideDirectory(string path, string directory)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(directory))
            {
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);

                // The trailing separator keeps 'C:\sol\A' from matching 'C:\sol\AB\file.d'.
                return fullPath.StartsWith(
                    fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (
                ex is ArgumentException || ex is NotSupportedException || ex is IOException)
            {
                // A malformed path is 'not inside', never a crash inside an event handler.
                return false;
            }
        }

        public static bool SameDirectory(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            return string.Equals(
                left.TrimEnd(Path.DirectorySeparatorChar),
                right.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetDirectory(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return null;
            }

            try
            {
                return Path.GetDirectoryName(projectPath);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
