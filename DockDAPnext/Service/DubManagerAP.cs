
using EnvDTE80;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using OutputWindowPane = Community.VisualStudio.Toolkit.OutputWindowPane;
using Windows = Community.VisualStudio.Toolkit.Windows;

namespace DockDAP.Ruls
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
        public Dictionary<string, BuildConfiguration> BuildConfigurations { get; set; } =
            new Dictionary<string, BuildConfiguration>();

        [JsonExtensionData]
        public Dictionary<string, object> OtherData { get; set; } = new Dictionary<string, object>();
    }

    public class BuildConfiguration
    {
        [JsonProperty("dflags")] public List<string> DFlags { get; set; } = new List<string>();

        [JsonProperty("lflags")] public List<string> LFlags { get; set; } = new List<string>();

        [JsonProperty("versions")] public List<string> Versions { get; set; } = new List<string>();

        [JsonProperty("buildOptions")] public List<string> BuildOptions { get; set; } = new List<string>();

        [JsonExtensionData]
        public Dictionary<string, object> OtherDataBuildConfiguration { get; set; } = new Dictionary<string, object>();

    }


    public static class DubManagerAP
    {
        public static DTE2 FindMainPathDte2AP()
        {
            return ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
        }

        public static string FindDubFileAP(DTE2 dte2)
        {
            if (dte2 == null || dte2.Solution == null || dte2.Solution.Projects == null)
            {
                return null;
            }

            foreach (EnvDTE.Project project in dte2.Solution.Projects)
            {
                string projectDirectory = System.IO.Path.GetDirectoryName(project.FullName);
                if (projectDirectory != null)
                {
                    string dubJsonPath = System.IO.Path.Combine(projectDirectory, "dub.json");

                    if (File.Exists(dubJsonPath))
                    {
                        return dubJsonPath;
                    }
                }
            }

            return null;
        }

        public static DubConfigAP ReadDubFileAP(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            string jsonString = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<DubConfigAP>(jsonString);
        }

        public static DubConfigAP CreateDubFileWithDefaultsAP(string path)
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

        public static bool SaveFileDubAP(string path, DubConfigAP inputConfigAp)
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



        public static void OpenDubFileinVisualStudioAP(string path)
        {
            DTE2 dte = DubManagerAP.FindMainPathDte2AP();
            if (dte == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            dte.ItemOperations.OpenFile(path);
        }
    }

    public static class APIdockAP
    {
        public static DubConfigAP CreateOrLoadDubFileAP()
        {
            DTE2 dte2 = DubManagerAP.FindMainPathDte2AP();
            if (dte2 == null)
            {
                return null;
            }

            string dubFilePath = DubManagerAP.FindDubFileAP(dte2);
            if (dubFilePath != null)
            {
                return DubManagerAP.ReadDubFileAP(dubFilePath);
            }
            else
            {
                string projectDirectory = System.IO.Path.GetDirectoryName(dte2.Solution.Projects.Item(1).FullName);
                string newFilePath = System.IO.Path.Combine(projectDirectory, "dub.json");
                return DubManagerAP.CreateDubFileWithDefaultsAP(newFilePath);
            }
        }

        public static void BuildDubAP()
        {
            var PathFile = DubManagerAP.FindMainPathDte2AP();
            if (PathFile == null)
            {
                return;
            }

            string FindDubFilePath = DubManagerAP.FindDubFileAP(PathFile);

            if (FindDubFilePath == null)
            {
                return;
            }

            string ProjectPath = System.IO.Path.GetDirectoryName(FindDubFilePath);

            DubCommandAP.BuildDubAsync(ProjectPath, "build");
        }

        public static void BuildDebugDubAP()
        {
            var PathFile = DubManagerAP.FindMainPathDte2AP();
            if (PathFile == null)
            {
                return;
            }

            string FindDubFilePath = DubManagerAP.FindDubFileAP(PathFile);

            if (FindDubFilePath == null)
            {
                return;
            }

            string ProjectPath = System.IO.Path.GetDirectoryName(FindDubFilePath);

            DubCommandAP.BuildDubAsync(ProjectPath, " build  --build=debug");
        }

        public static void BuildReleaseDubAP()
        {
            var PathFile = DubManagerAP.FindMainPathDte2AP();
            if (PathFile == null)
            {
                return;
            }

            string FindDubFilePath = DubManagerAP.FindDubFileAP(PathFile);

            if (FindDubFilePath == null)
            {
                return;
            }

            string ProjectPath = System.IO.Path.GetDirectoryName(FindDubFilePath);

            DubCommandAP.BuildDubAsync(ProjectPath, "build  --build=release");
        }

        public static void OpenDubFileAP()
        {
            var PathFile = DubManagerAP.FindMainPathDte2AP();
            if (PathFile == null)
            {
                return;
            }

            string FindDubFilePath = DubManagerAP.FindDubFileAP(PathFile);

            if (FindDubFilePath == null)
            {
                return;
            }

            DubCommandAP.OpenDubFileinVisualStudioAP(FindDubFilePath);
        }

        public static void SaveFileAP(DubConfigAP dubConfigAp)
        {
            var PathFile = DubManagerAP.FindMainPathDte2AP();
            if (PathFile == null)
            {
                return;
            }

            string FindDubFilePath = DubManagerAP.FindDubFileAP(PathFile);

            if (FindDubFilePath == null)
            {
                return;
            }

            DubManagerAP.SaveFileDubAP(FindDubFilePath, dubConfigAp);
        }

        public static DubConfigAP AddToConfigAp(string name, string description, List<string> authors, string license,
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