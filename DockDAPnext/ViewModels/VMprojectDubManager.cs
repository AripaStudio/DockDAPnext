using DockDAPnext.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using DockDAPnext.Service.InterFaces;

namespace DockDAPnext.ViewModels
{
    public class VMprojectDubManager : BaseViewModel
    {

        #region ----- Authors Section ----
        public ReactiveCommand<Unit, Unit> ButtonCommand_Authors_Add { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_Authors_Remove { get; }

        private string _txtAddNewAuthors;

        public string txtAddNewAuthors
        {
            get => _txtAddNewAuthors;
            set { this.RaiseAndSetIfChanged(ref _txtAddNewAuthors, value); }
        }

        private ObservableCollection<string> _authorsList = new ObservableCollection<string>();

        public ObservableCollection<string> AuthorsList
        {
            get => _authorsList;
            set => this.RaiseAndSetIfChanged(ref _authorsList, value);
        }

        private string _selectedAuthor;

        public string SelectedAuthor
        {
            get => _selectedAuthor;
            set => this.RaiseAndSetIfChanged(ref _selectedAuthor, value);
        }

        #endregion
        #region ----- Project Configuration ----

        private string _txtName_Project;

        public string txtName_Project
        {
            get => _txtName_Project;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtName_Project, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.Name = _txtName_Project;
                }
            }
        }

        private string _txtDescription_Project;

        public string txtDescription_Project
        {
            get => _txtDescription_Project;

            set
            {
                this.RaiseAndSetIfChanged(ref _txtDescription_Project, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.Description = _txtDescription_Project;
                }
            }
        }




        private string _txtLicense_project;

        public string txtLicense_project
        {
            get => _txtLicense_project;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtLicense_project, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.License = _txtLicense_project;
                }
            }
        }


        private string _txtTargetName_project;

        public string txtTargetName_project
        {
            get => _txtTargetName_project;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtTargetName_project, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.TargetName = _txtTargetName_project;
                }
            }
        }

        public enum TargetTypeEnum
        {
            autodetect,
            none,
            executable,
            sourceLibrary,
            library,
            dynamicLibrary,
            staticLibrary
        }

        private TargetTypeEnum _selectedTargetType;

        public TargetTypeEnum SelectedTargetType
        {
            get => _selectedTargetType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTargetType, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.TargetType = _selectedTargetType.ToString();
                }
            }
        }

        public List<TargetTypeEnum> TargetTypeListComboBox { get; } = new List<TargetTypeEnum>()
        {
            TargetTypeEnum.executable,
            TargetTypeEnum.sourceLibrary,
            TargetTypeEnum.library,
            TargetTypeEnum.dynamicLibrary,
            TargetTypeEnum.staticLibrary,
            TargetTypeEnum.autodetect,
            TargetTypeEnum.none
        };

        #endregion
        #region ----- Dependencies -----
        public ReactiveCommand<Unit, Unit> ButtonCommand_AddDependency { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_RemoveSelectedDependency { get; }


        private string _txtAddDependencyKey;

        public string txtAddDependencyKey
        {
            get => _txtAddDependencyKey;
            set => this.RaiseAndSetIfChanged(ref _txtAddDependencyKey, value);
        }

        private string _txtAddDependencyValue;

        public string txtAddDependencyValue
        {
            get => _txtAddDependencyValue;
            set => this.RaiseAndSetIfChanged(ref _txtAddDependencyValue, value);
        }

        private ObservableCollection<string> _dependenciesListCollection = new ObservableCollection<string>();

        public ObservableCollection<string> dependenciesListCollection
        {
            get => _dependenciesListCollection;
            set => this.RaiseAndSetIfChanged(ref _dependenciesListCollection, value);
        }

        private string _selectedDependencyRemove;

        public string SelectedDependencyRemove
        {
            get => _selectedDependencyRemove;
            set => this.RaiseAndSetIfChanged(ref _selectedDependencyRemove, value);
        }


        #endregion
        #region build Configuration 



        //----- Name Config ----
        private string _txtNameBuildConfig;

        public string txtNameBuildConfig
        {
            get => _txtNameBuildConfig;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtNameBuildConfig, value);
                UpdateCurrentConfigIndex();
            }
        }

        public ReactiveCommand<Unit, Unit> ButtonCommand_ApplayNameBuildConfigurations { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AddNewNameBuildConfiguarations { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_DeleteNameConfigurations { get; }

        private string _txtAddNewConfigurationsName;

        public string txtAddNewConfigurationsName
        {
            get => _txtAddNewConfigurationsName;
            set => this.RaiseAndSetIfChanged(ref _txtAddNewConfigurationsName, value);
        }


        private ObservableCollection<string> _nameConfigListCollection = new ObservableCollection<string>();

        public ObservableCollection<string> NameConfigListCollection
        {
            get => _nameConfigListCollection;
            set => this.RaiseAndSetIfChanged(ref _nameConfigListCollection, value);
        }

        private string _selectedDeletedNameConfigOfList;

        public string SelectedDeletedNameConfigOfList
        {
            get => _selectedDeletedNameConfigOfList;
            set => this.RaiseAndSetIfChanged(ref _selectedDeletedNameConfigOfList, value);
        }




        //----- D Flags -----
        public ReactiveCommand<Unit, Unit> ButtonCommand_DFlag_Add { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_DFlag_Remove { get; }

        private string _txtAddDFlag;
        public string txtAddDFlag
        {
            get => _txtAddDFlag;
            set => this.RaiseAndSetIfChanged(ref _txtAddDFlag, value);
        }

        private ObservableCollection<string> _dFlagsList = new ObservableCollection<string>();
        public ObservableCollection<string> DFlagsList
        {
            get => _dFlagsList;
            set => this.RaiseAndSetIfChanged(ref _dFlagsList, value);
        }

        private string _selectedDFlag;
        public string SelectedDFlag
        {
            get => _selectedDFlag;
            set => this.RaiseAndSetIfChanged(ref _selectedDFlag, value);
        }

        //----- L Flags -----
        public ReactiveCommand<Unit, Unit> ButtonCommand_LFlag_Add { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_LFlag_Remove { get; }

        private string _txtAddLFlag;
        public string txtAddLFlag
        {
            get => _txtAddLFlag;
            set => this.RaiseAndSetIfChanged(ref _txtAddLFlag, value);
        }

        private ObservableCollection<string> _lFlagsList = new ObservableCollection<string>();
        public ObservableCollection<string> LFlagsList
        {
            get => _lFlagsList;
            set => this.RaiseAndSetIfChanged(ref _lFlagsList, value);
        }

        private string _selectedLFlag;
        public string SelectedLFlag
        {
            get => _selectedLFlag;
            set => this.RaiseAndSetIfChanged(ref _selectedLFlag, value);
        }

        //----- Versions -----
        public ReactiveCommand<Unit, Unit> ButtonCommand_Version_Add { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_Version_Remove { get; }

        private string _txtAddVersion;
        public string txtAddVersion
        {
            get => _txtAddVersion;
            set => this.RaiseAndSetIfChanged(ref _txtAddVersion, value);
        }

        private ObservableCollection<string> _versionsList = new ObservableCollection<string>();
        public ObservableCollection<string> VersionsList
        {
            get => _versionsList;
            set => this.RaiseAndSetIfChanged(ref _versionsList, value);
        }

        private string _selectedVersion;
        public string SelectedVersion
        {
            get => _selectedVersion;
            set => this.RaiseAndSetIfChanged(ref _selectedVersion, value);
        }

        //----- Other Configs -----
        public ReactiveCommand<Unit, Unit> ButtonCommand_OtherConfig_Add { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_OtherConfig_Remove { get; }

        private string _txtOtherConfigName;
        public string txtOtherConfigName
        {
            get => _txtOtherConfigName;
            set => this.RaiseAndSetIfChanged(ref _txtOtherConfigName, value);
        }

        private string _txtOtherConfigValue;
        public string txtOtherConfigValue
        {
            get => _txtOtherConfigValue;
            set => this.RaiseAndSetIfChanged(ref _txtOtherConfigValue, value);
        }

        private ObservableCollection<Tuple<string, string>> _otherConfigsList = new ObservableCollection<Tuple<string, string>>();
        public ObservableCollection<Tuple<string, string>> OtherConfigsList
        {
            get => _otherConfigsList;
            set => this.RaiseAndSetIfChanged(ref _otherConfigsList, value);
        }

        private Tuple<string, string> _selectedOtherConfig;
        public Tuple<string, string> SelectedOtherConfig
        {
            get => _selectedOtherConfig;
            set => this.RaiseAndSetIfChanged(ref _selectedOtherConfig, value);
        }

        // ----- Build Options -----
        private ObservableCollection<BuildOptionTypes> _buildOptionsList = new ObservableCollection<BuildOptionTypes>();
        public ObservableCollection<BuildOptionTypes> BuildOptionsList
        {
            get => _buildOptionsList;
            set => this.RaiseAndSetIfChanged(ref _buildOptionsList, value);
        }

        public List<BuildOptionTypes> BuildPOptionTypesComboBox { get; } = new List<BuildOptionTypes>
        {
            BuildOptionTypes.debugMode,
            BuildOptionTypes.releaseMode,
            BuildOptionTypes.coverage,
            BuildOptionTypes.unoptimized,
            BuildOptionTypes.inline,
            BuildOptionTypes.noBoundsCheck,
            BuildOptionTypes.debugInfo,
            BuildOptionTypes.optimize,
            BuildOptionTypes.warnings,
            BuildOptionTypes.warningsAsErrors
        };

        private BuildOptionTypes _selectedBuildOptionComboBox;
        public BuildOptionTypes SelectedBuildOptionComboBox
        {
            get => _selectedBuildOptionComboBox;
            set => this.RaiseAndSetIfChanged(ref _selectedBuildOptionComboBox, value);
        }



        private BuildOptionTypes _selectedBuildOption;
        public BuildOptionTypes SelectedBuildOption
        {
            get => _selectedBuildOption;
            set => this.RaiseAndSetIfChanged(ref _selectedBuildOption, value);
        }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AddBuildOption { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_RemoveBuildOption { get; }

        #endregion
        #region sourceFile
        public ReactiveCommand<Unit, Unit> ButtonCommand_SourceFileAddPath { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_SourceFileRemovePath { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_SyncFromDproj { get; }


        private string _txtAddSourcePath;

        public string txtAddSourcePath
        {
            get => _txtAddSourcePath;
            set => this.RaiseAndSetIfChanged(ref _txtAddSourcePath, value);
        }

        private ObservableCollection<string> _sourceFilePathListCollection = new ObservableCollection<string>();
        public ObservableCollection<string> sourceFilePathListCollection
        {
            get => _sourceFilePathListCollection;
            set => this.RaiseAndSetIfChanged(ref _sourceFilePathListCollection, value);
        }

        private string _selectedSourcePathListRemove;

        public string selectedSourcePathListRemove
        {
            get => _selectedSourcePathListRemove;
            set => this.RaiseAndSetIfChanged(ref _selectedSourcePathListRemove, value);
        }
        #endregion


        private int CurrentBuildConfigID = 0;

        // Silent: this runs on every keystroke of the build-config name box, so a MessageBox
        // here would interrupt typing; failures are reported by the buttons instead.
        private void UpdateCurrentConfigIndex()
        {
            var configurations = CurrentConfig?.BuildConfigurations;
            if (configurations == null)
            {
                return;
            }

            var index = configurations.FindIndex(s => s.Name == txtNameBuildConfig);
            if (index >= 0)
            {
                CurrentBuildConfigID = index;
            }
        }

        private bool TryGetCurrentBuildConfiguration(out BuildConfiguration configuration)
        {
            configuration = null;

            if (!EnsureConfigLoaded())
            {
                return false;
            }

            var configurations = CurrentConfig.BuildConfigurations;
            if (configurations == null || configurations.Count == 0)
            {
                ShowWarning("This file has no build configuration; create one in the Build Configs section first.");
                return false;
            }

            UpdateCurrentConfigIndex();

            if (CurrentBuildConfigID < 0 || CurrentBuildConfigID >= configurations.Count)
            {
                CurrentBuildConfigID = 0;
            }

            configuration = configurations[CurrentBuildConfigID];
            return true;
        }

        // Brings the UI in sync with a freshly created or loaded dub.json, and repairs
        // collections that the JSON may have left null.
        public void LoadFromConfig(DubConfigAP config)
        {
            if (config == null)
            {
                return;
            }

            Normalize(config);

            txtName_Project = config.Name;
            txtDescription_Project = config.Description;
            txtLicense_project = config.License;
            txtTargetName_project = config.TargetName;

            if (Enum.TryParse(config.TargetType, out TargetTypeEnum targetType))
            {
                SelectedTargetType = targetType;
            }

            AuthorsList = new ObservableCollection<string>(config.Authors);
            dependenciesListCollection = new ObservableCollection<string>(
                config.Dependencies.Select(d => $"{d.Key}: {d.Value}"));
            sourceFilePathListCollection = new ObservableCollection<string>(config.SourceFiles);
            NameConfigListCollection = new ObservableCollection<string>(
                config.BuildConfigurations.Select(c => c.Name));

            CurrentBuildConfigID = 0;
            LoadCurrentBuildConfigurationLists();
        }

        private void LoadCurrentBuildConfigurationLists()
        {
            var configuration = CurrentConfig?.BuildConfigurations?.ElementAtOrDefault(CurrentBuildConfigID);

            DFlagsList = configuration == null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(configuration.DFlags);
            LFlagsList = configuration == null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(configuration.LFlags);
            VersionsList = configuration == null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(configuration.Versions);
            BuildOptionsList = configuration == null
                ? new ObservableCollection<BuildOptionTypes>()
                : new ObservableCollection<BuildOptionTypes>(configuration.BuildOptions);
            OtherConfigsList = configuration == null
                ? new ObservableCollection<Tuple<string, string>>()
                : new ObservableCollection<Tuple<string, string>>(
                    configuration.OtherDataBuildConfiguration.Select(d => new Tuple<string, string>(d.Key, d.Value?.ToString())));
        }

        private static void Normalize(DubConfigAP config)
        {
            config.Authors = config.Authors ?? new List<string>();
            config.Dependencies = config.Dependencies ?? new Dictionary<string, string>();
            config.SourceFiles = config.SourceFiles ?? new List<string>();
            config.BuildConfigurations = config.BuildConfigurations ?? new List<BuildConfiguration>();
            config.OtherData = config.OtherData ?? new Dictionary<string, object>();

            foreach (var configuration in config.BuildConfigurations)
            {
                configuration.DFlags = configuration.DFlags ?? new List<string>();
                configuration.LFlags = configuration.LFlags ?? new List<string>();
                configuration.Versions = configuration.Versions ?? new List<string>();
                configuration.BuildOptions = configuration.BuildOptions ?? new List<BuildOptionTypes>();
                configuration.OtherDataBuildConfiguration = configuration.OtherDataBuildConfiguration ?? new Dictionary<string, object>();
                configuration.Name = configuration.Name ?? string.Empty;
            }
        }

        // Applies a successful source sync to the shared config and refreshes the list UI.
        public void ApplySyncResult(DprojSyncResult result)
        {
            if (result == null || !result.Success || result.Config == null)
            {
                return;
            }

            CurrentDubFileConfigAP.CurrentDubConfigAp = result.Config;
            sourceFilePathListCollection = new ObservableCollection<string>(result.Config.SourceFiles);
        }

        private IdubManagerService dubManager;
        public VMprojectDubManager(IdubManagerService dubManagerService)
        {
            dubManager = dubManagerService;


            //name Config - build configurations
            ButtonCommand_ApplayNameBuildConfigurations = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded() || string.IsNullOrWhiteSpace(txtNameBuildConfig))
                {
                    return;
                }

                var configurations = CurrentConfig.BuildConfigurations;
                if (configurations.Count == 0)
                {
                    ShowWarning("This file has no build configuration; add one first.");
                    return;
                }

                UpdateCurrentConfigIndex();
                configurations[CurrentBuildConfigID].Name = txtNameBuildConfig;
                NameConfigListCollection = new ObservableCollection<string>(configurations.Select(c => c.Name));
            }));

            ButtonCommand_DeleteNameConfigurations = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedDeletedNameConfigOfList))
                {
                    ShowWarning("Select a name from the list before removing it.");
                    return;
                }

                var selectedId = CurrentConfig.BuildConfigurations
                    .FindIndex(s => s.Name == SelectedDeletedNameConfigOfList);
                if (selectedId == -1)
                {
                    ShowWarning("The selected name was not found in dub.json.");
                    return;
                }

                CurrentConfig.BuildConfigurations.RemoveAt(selectedId);
                NameConfigListCollection.Remove(SelectedDeletedNameConfigOfList);
                CurrentBuildConfigID = 0;
                LoadCurrentBuildConfigurationLists();
            }));


            ButtonCommand_AddNewNameBuildConfiguarations = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddNewConfigurationsName))
                {
                    ShowWarning("Build configuration name is empty.");
                    return;
                }

                var configurations = CurrentConfig.BuildConfigurations;
                if (configurations.Any(s => s.Name == txtAddNewConfigurationsName))
                {
                    ShowWarning("This name has already been added.");
                    return;
                }

                configurations.Add(new BuildConfiguration { Name = txtAddNewConfigurationsName });
                NameConfigListCollection.Add(txtAddNewConfigurationsName);
                CurrentBuildConfigID = configurations.Count - 1;
                LoadCurrentBuildConfigurationLists();
            }));

            // Authors Section
            ButtonCommand_Authors_Add = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddNewAuthors))
                {
                    ShowWarning("Author name is empty.");
                    return;
                }

                CurrentConfig.Authors.Add(txtAddNewAuthors);
                AuthorsList.Add(txtAddNewAuthors);
            }));

            ButtonCommand_Authors_Remove = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedAuthor))
                {
                    ShowWarning("Select an author before removing it.");
                    return;
                }

                CurrentConfig.Authors.Remove(SelectedAuthor);
                AuthorsList.Remove(SelectedAuthor);
            }));

            // Dependencies
            ButtonCommand_AddDependency = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddDependencyKey) || string.IsNullOrWhiteSpace(txtAddDependencyValue))
                {
                    ShowWarning("Dependency name or value is empty.");
                    return;
                }

                CurrentConfig.Dependencies[txtAddDependencyKey] = txtAddDependencyValue;
                dependenciesListCollection.Add($"{txtAddDependencyKey}: {txtAddDependencyValue}");
            }));

            ButtonCommand_RemoveSelectedDependency = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedDependencyRemove))
                {
                    ShowWarning("Select a dependency before removing it.");
                    return;
                }

                var keyToRemove = SelectedDependencyRemove.Split(':')[0].Trim();
                CurrentConfig.Dependencies.Remove(keyToRemove);
                dependenciesListCollection.Remove(SelectedDependencyRemove);
            }));

            // D Flags
            ButtonCommand_DFlag_Add = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddDFlag))
                {
                    ShowWarning("D flag value is empty.");
                    return;
                }

                configuration.DFlags.Add(txtAddDFlag);
                DFlagsList.Add(txtAddDFlag);
            }));

            ButtonCommand_DFlag_Remove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedDFlag))
                {
                    ShowWarning("Select a D flag before removing it.");
                    return;
                }

                configuration.DFlags.Remove(SelectedDFlag);
                DFlagsList.Remove(SelectedDFlag);
            }));

            // L Flags
            ButtonCommand_LFlag_Add = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddLFlag))
                {
                    ShowWarning("L flag value is empty.");
                    return;
                }

                configuration.LFlags.Add(txtAddLFlag);
                LFlagsList.Add(txtAddLFlag);
            }));

            ButtonCommand_LFlag_Remove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedLFlag))
                {
                    ShowWarning("Select an L flag before removing it.");
                    return;
                }

                configuration.LFlags.Remove(SelectedLFlag);
                LFlagsList.Remove(SelectedLFlag);
            }));

            // Versions
            ButtonCommand_Version_Add = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddVersion))
                {
                    ShowWarning("Version value is empty.");
                    return;
                }

                configuration.Versions.Add(txtAddVersion);
                VersionsList.Add(txtAddVersion);
            }));

            ButtonCommand_Version_Remove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedVersion))
                {
                    ShowWarning("Select a version before removing it.");
                    return;
                }

                configuration.Versions.Remove(SelectedVersion);
                VersionsList.Remove(SelectedVersion);
            }));

            // Other Configs
            ButtonCommand_OtherConfig_Add = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtOtherConfigName) || string.IsNullOrWhiteSpace(txtOtherConfigValue))
                {
                    ShowWarning("Configuration name or value is empty.");
                    return;
                }

                configuration.OtherDataBuildConfiguration[txtOtherConfigName] = txtOtherConfigValue;
                OtherConfigsList.Add(new Tuple<string, string>(txtOtherConfigName, txtOtherConfigValue));
            }));

            ButtonCommand_OtherConfig_Remove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (SelectedOtherConfig == null || string.IsNullOrWhiteSpace(SelectedOtherConfig.Item1))
                {
                    ShowWarning("Select a configuration before removing it.");
                    return;
                }

                configuration.OtherDataBuildConfiguration.Remove(SelectedOtherConfig.Item1);
                OtherConfigsList.Remove(SelectedOtherConfig);
            }));

            // Source File
            ButtonCommand_SourceFileAddPath = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAddSourcePath))
                {
                    ShowWarning("Source file path is empty.");
                    return;
                }

                CurrentConfig.SourceFiles.Add(txtAddSourcePath);
                sourceFilePathListCollection.Add(txtAddSourcePath);
            }));

            ButtonCommand_SourceFileRemovePath = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(selectedSourcePathListRemove))
                {
                    ShowWarning("Select a path before removing it.");
                    return;
                }

                CurrentConfig.SourceFiles.Remove(selectedSourcePathListRemove);
                sourceFilePathListCollection.Remove(selectedSourcePathListRemove);
            }));

            // Replace dub.json sourceFiles with the .d files listed in the active .visualdproj.
            ButtonCommand_SyncFromDproj = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await dubManager.SyncSourceFilesFromDprojAP();
                if (!result.Success)
                {
                    ShowWarning(result.ErrorMessage);
                    return;
                }

                ApplySyncResult(result);
                ShowInfo(result.Changed
                    ? $"sourceFiles synced from the project file: +{result.Added} added, -{result.Removed} removed, {result.Total} total."
                    : "sourceFiles already match the project file; dub.json was not changed.");
            }));

            // Build Options

            ButtonCommand_AddBuildOption = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (BuildOptionsList.Contains(SelectedBuildOptionComboBox))
                {
                    ShowWarning("This build option has already been added.");
                    return;
                }

                configuration.BuildOptions.Add(SelectedBuildOptionComboBox);

                BuildOptionsList.Add(SelectedBuildOptionComboBox);
            }));

            ButtonCommand_RemoveBuildOption = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                configuration.BuildOptions.Remove(SelectedBuildOption);
                BuildOptionsList.Remove(SelectedBuildOption);
            }));
        }
    }
}
