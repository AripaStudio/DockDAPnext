using DockDAPnext.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

namespace DockDAPnext.ViewModels
{
    public partial class VMprojectDubManager
    {
        #region build Configuration

        //----- Name Config ----
        private string _txtNameBuildConfig;

        public string TxtNameBuildConfig
        {
            get => _txtNameBuildConfig;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtNameBuildConfig, value);
                UpdateCurrentConfigIndex();
            }
        }

        public ReactiveCommand<Unit, Unit> ButtonCommandApplayNameBuildConfigurations { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAddNewNameBuildConfiguarations { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandDeleteNameConfigurations { get; private set; }

        private string _txtAddNewConfigurationsName;

        public string TxtAddNewConfigurationsName
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

        // Picking a name in the list switches the editor (name, target path, flags...)
        // to that configuration; silent on purpose - selection fires on every click.
        public string SelectedDeletedNameConfigOfList
        {
            get => _selectedDeletedNameConfigOfList;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDeletedNameConfigOfList, value);
                if (string.IsNullOrWhiteSpace(value) || CurrentConfig?.BuildConfigurations == null)
                {
                    return;
                }

                var index = CurrentConfig.BuildConfigurations.FindIndex(c => c?.Name == value);
                if (index >= 0)
                {
                    _currentBuildConfigId = index;
                    LoadCurrentBuildConfigurationLists();
                }
            }
        }

        //----- Target Path -----
        public ReactiveCommand<Unit, Unit> ButtonCommandApplayTargetPathBuildConfig { get; private set; }

        private string _txtTargetPathBuildConfig;

        // Per-configuration 'targetPath': where THIS configuration puts its output
        // (debug -> Output/debug, release -> Output/release), relative to dub.json.
        public string TxtTargetPathBuildConfig
        {
            get => _txtTargetPathBuildConfig;
            set => this.RaiseAndSetIfChanged(ref _txtTargetPathBuildConfig, value);
        }




        //----- D Flags -----
        public ReactiveCommand<Unit, Unit> ButtonCommandDFlagAdd { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandDFlagRemove { get; private set; }

        private string _txtAddDFlag;
        public string TxtAddDFlag
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
        public ReactiveCommand<Unit, Unit> ButtonCommandLFlagAdd { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandLFlagRemove { get; private set; }

        private string _txtAddLFlag;
        public string TxtAddLFlag
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
        public ReactiveCommand<Unit, Unit> ButtonCommandVersionAdd { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandVersionRemove { get; private set; }

        private string _txtAddVersion;
        public string TxtAddVersion
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
        public ReactiveCommand<Unit, Unit> ButtonCommandOtherConfigAdd { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandOtherConfigRemove { get; private set; }

        private string _txtOtherConfigName;
        public string TxtOtherConfigName
        {
            get => _txtOtherConfigName;
            set => this.RaiseAndSetIfChanged(ref _txtOtherConfigName, value);
        }

        private string _txtOtherConfigValue;
        public string TxtOtherConfigValue
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

        public ReactiveCommand<Unit, Unit> ButtonCommandAddBuildOption { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandRemoveBuildOption { get; private set; }

        #endregion
    }
}
