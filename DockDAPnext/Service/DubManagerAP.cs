
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
   

    public class DubManagerAP
    {

        public async Task<string> FindDubFileAP()
        {
            var GetAllProject = await VS.Solutions.GetAllProjectsAsync();

            foreach (Project project in GetAllProject)
            {
                if (project.FullPath != null)
                {
                    if (string.IsNullOrEmpty(project.FullPath))
                    {
                        continue;
                    }

                    try
                    {
                        string rootPath = Path.GetDirectoryName(project.FullPath);

                        if (Directory.Exists(rootPath))
                        {
                            var dubFile = Directory.EnumerateFiles(rootPath, "dub.json", SearchOption.AllDirectories)
                                .FirstOrDefault();

                            if (dubFile != null) return dubFile;
                        }
                        

                      
                    }
                    catch (Exception)
                    {
                        continue;
                    }


                }
            }


            return null;
        }

        public DubConfigAP ReadDubFileAP(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            string jsonString = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<DubConfigAP>(jsonString);
        }

        public DubConfigAP CreateDubFileWithDefaultsAP(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new DubConfigAP();
            }

            DubConfigAP config = new DubConfigAP
            {
                Name = "initial-DubFile-DockAP",
                Description = "A new Dub.json file by DockDAP , AripaParsStudio",
                Authors = new List<string>() { "DockAPUser" },
            };
            string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
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
                var jsonString = JsonConvert.SerializeObject(inputConfigAp, Formatting.Indented);
                System.IO.File.WriteAllText(path, jsonString);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }
    }

    public static class DubCommandAP
    {

        public static async Task BuildDubAsync(string projectPath, string arguments)
        {

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();


            OutputWindowPane dubPane = await VS.Windows.GetOutputWindowPaneAsync(GuidDubAP.DubManagerPaneGuid);

            if (dubPane == null) return;

            await dubPane.ActivateAsync();


            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "dub.exe",
                Arguments = arguments,
                WorkingDirectory = projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                await dubPane.WriteLineAsync($"Starting dub with arguments: {arguments} in {projectPath}");

                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(processStartInfo))
                {
                    if (process == null) return;

                    process.OutputDataReceived += async (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            await dubPane.WriteLineAsync(e.Data);
                        }
                    };

                    process.ErrorDataReceived += async (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            await dubPane.WriteLineAsync("Error: " + e.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    await dubPane.WriteLineAsync("Dub command finished.");
                }
            }
            catch (Exception e)
            {
                await dubPane.WriteLineAsync($"Error in run Command: {e.Message}");
            }
        }



        public static async Task OpenDubFileinVisualStudioAP(string path)
        {
            DubManagerAP dbM = new DubManagerAP();
            var dubFileAddress = await dbM.FindDubFileAP();
            if (dubFileAddress == null || string.IsNullOrEmpty(dubFileAddress) || !File.Exists(dubFileAddress))
            {
                return;
            }

            VS.Documents.OpenAsync(dubFileAddress);
        }
    }

    public class APIdockAP : IdubManagerService
    {
        private DubManagerAP dubManagerAP;

        public APIdockAP()
        {
            dubManagerAP = new DubManagerAP();
        }

        public async Task<DubConfigAP> CreateOrLoadDubFileAP()
        {


            var dubFilePath = await dubManagerAP.FindDubFileAP();
            if (dubFilePath != null)
            {
                return dubManagerAP.ReadDubFileAP(dubFilePath);
            }
            else
            {
                var projectDirectory = await VS.Solutions.GetActiveProjectAsync();
                string newFilePath = System.IO.Path.Combine(projectDirectory.FullPath, "dub.json");
                return dubManagerAP.CreateDubFileWithDefaultsAP(newFilePath);
            }
        }

        public async Task BuildDubAP()
        {

            var FindDubFilePath = await dubManagerAP.FindDubFileAP();

            if (FindDubFilePath == null)
            {
                return;
            }

            var ProjectPath = await VS.Solutions.GetActiveProjectAsync();

            DubCommandAP.BuildDubAsync(ProjectPath.FullPath, "dub build");
        }

        public async Task BuildDebugDubAP()
        {

            var FindDubFilePath = await dubManagerAP.FindDubFileAP();

            if (FindDubFilePath == null)
            {
                return;
            }

            var ProjectPath = await VS.Solutions.GetActiveProjectAsync();

            DubCommandAP.BuildDubAsync(ProjectPath.FullPath, "dub build  --build=debug");
        }

        public async Task BuildReleaseDubAP()
        {


            var FindDubFilePath = await dubManagerAP.FindDubFileAP();

            if (FindDubFilePath == null)
            {
                return;
            }

            var ProjectPath = await VS.Solutions.GetActiveProjectAsync();

            DubCommandAP.BuildDubAsync(ProjectPath.FullPath, "dub build  --build=release");
        }

        public async Task OpenDubFileAP()
        {

            var FindDubFilePath = await dubManagerAP.FindDubFileAP();

            if (FindDubFilePath == null)
            {
                return;
            }

            DubCommandAP.OpenDubFileinVisualStudioAP(FindDubFilePath);
        }

        public async Task SaveFileAP(DubConfigAP dubConfigAp)
        {


            var FindDubFilePath = await dubManagerAP.FindDubFileAP();

            if (FindDubFilePath == null)
            {
                return;
            }

            dubManagerAP.SaveFileDubAP(FindDubFilePath, dubConfigAp);
        }

        public DubConfigAP AddToConfigAp(string name, string description, List<string> authors, string license,
            string targetName, string TargetType, List<string> sourceFiles, Dictionary<string, string> dependencies,
            Dictionary<string, BuildConfiguration> buildConfigurations
            , Dictionary<string, object> otherData)
        {
            DubConfigAP configOutput;
            configOutput = new DubConfigAP()
            {
                Name = name,
                Description = description,
                Authors = authors,
                License = license,
                BuildConfigurations = buildConfigurations,
                Dependencies = dependencies,
                SourceFiles = sourceFiles,
                OtherData = otherData
            };

            return configOutput;
        }
    }

    public static class GuidDubAP
    {
        public const string DubManagerPaneString = "d4b8f6e2-3c5a-4f2e-9f7b-1a2b3c4d5e6f";
        public static readonly Guid DubManagerPaneGuid = new Guid(DubManagerPaneString);
    }
}