using DockDAPnext.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DockDAPnext.Service.InterFaces;

namespace DockDAPnext.ViewModels
{
    // One ViewModel for the whole 'dub.json manager' tab. It is split by section into
    // partial files so each piece stays reviewable:
    //   VMprojectDubManager.ProjectSection.cs            - authors, project fields, dependencies, source files
    //   VMprojectDubManager.BuildConfigurationProperties.cs - the selected build configuration's UI state
    //   VMprojectDubManager.Commands.cs                  - add/remove commands of the project sections
    //   VMprojectDubManager.BuildConfigurationCommands.cs - add/remove commands of the build configuration
    public partial class VMprojectDubManager : BaseViewModel
    {
        private int _currentBuildConfigId = 0;

        private IDubManagerService _dubManager;

        public VMprojectDubManager(IDubManagerService dubManagerService)
        {
            _dubManager = dubManagerService;

            SetupBuildConfigurationNameCommands();
            SetupProjectSectionCommands();
            SetupBuildConfigurationFieldCommands();
        }

        // Silent: this runs on every keystroke of the build-config name box, so a MessageBox
        // here would interrupt typing; failures are reported by the buttons instead.
        private void UpdateCurrentConfigIndex()
        {
            var configurations = CurrentConfig?.BuildConfigurations;
            if (configurations == null)
            {
                return;
            }

            var index = configurations.FindIndex(s => s.Name == TxtNameBuildConfig);
            if (index >= 0)
            {
                _currentBuildConfigId = index;
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

            if (_currentBuildConfigId < 0 || _currentBuildConfigId >= configurations.Count)
            {
                _currentBuildConfigId = 0;
            }

            configuration = configurations[_currentBuildConfigId];
            return true;
        }

        // Brings the UI in sync with a freshly created or loaded dub.json, and repairs
        // collections that the JSON may have left null.
        public void LoadFromConfig(DubConfigAp config)
        {
            if (config == null)
            {
                return;
            }

            Normalize(config);

            TxtNameProject = config.Name;
            TxtDescriptionProject = config.Description;
            TxtLicenseProject = config.License;
            TxtTargetNameProject = config.TargetName;
            TxtTargetPathProject = config.TargetPath;

            if (Enum.TryParse(config.TargetType, out TargetTypeEnum targetType))
            {
                SelectedTargetType = targetType;
            }

            AuthorsList = new ObservableCollection<string>(config.Authors);
            DependenciesListCollection = new ObservableCollection<string>(
                config.Dependencies.Select(d => $"{d.Key}: {d.Value}"));
            SourceFilePathListCollection = new ObservableCollection<string>(config.SourceFiles);
            NameConfigListCollection = new ObservableCollection<string>(
                config.BuildConfigurations.Select(c => c.Name));

            _currentBuildConfigId = 0;
            LoadCurrentBuildConfigurationLists();
        }

        // Empties every field after the selector moved to a project whose dub.json did not load.
        public void ClearConfig()
        {
            LoadFromConfig(new DubConfigAp
            {
                TargetType = string.Empty,
            });

            // Enum.TryParse leaves the combo on the PREVIOUS project's type; the config is
            // already null here, so the setter only updates the display.
            SelectedTargetType = TargetTypeEnum.autodetect;
        }

        private void LoadCurrentBuildConfigurationLists()
        {
            var configuration = CurrentConfig?.BuildConfigurations?.ElementAtOrDefault(_currentBuildConfigId);

            // Keep the two single-value boxes in sync with the selected configuration so
            // the fields below always show the configuration the user is about to edit.
            TxtNameBuildConfig = configuration?.Name ?? string.Empty;
            TxtTargetPathBuildConfig = configuration?.TargetPath ?? string.Empty;

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

        private static void Normalize(DubConfigAp config)
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

            CurrentDubFileConfigAp.CurrentDubConfigAp = result.Config;
            SourceFilePathListCollection = new ObservableCollection<string>(result.Config.SourceFiles);
        }
    }
}
