using DockDAPnext.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using DockDAPnext.Service.InterFaces;
using MessageBox = System.Windows.Forms.MessageBox;

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

        private ObservableCollection<string> _authorsList;

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
                CurrentDubFileConfigAP.CurrentDubConfigAp.Name = _txtName_Project;
            }
        }

        private string _txtDescription_Project;

        public string txtDescription_Project
        {
            get => _txtDescription_Project;

            set
            {
                this.RaiseAndSetIfChanged(ref _txtDescription_Project, value);
                CurrentDubFileConfigAP.CurrentDubConfigAp.Description = _txtDescription_Project;
            }
        }




        private string _txtLicense_project;

        public string txtLicense_project
        {
            get => _txtLicense_project;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtLicense_project, value);
                CurrentDubFileConfigAP.CurrentDubConfigAp.License = _txtLicense_project;
            }
        }


        private string _txtTargetName_project;

        public string txtTargetName_project
        {
            get => _txtTargetName_project;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtTargetName_project, value);
                CurrentDubFileConfigAP.CurrentDubConfigAp.TargetName = _txtTargetName_project;
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
                CurrentDubFileConfigAP.CurrentDubConfigAp.TargetType = _selectedTargetType.ToString();
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

        private ObservableCollection<string> _dependenciesListCollection;

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
                SetCurrentConfigIndex();
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


        private ObservableCollection<string> _nameConfigListCollection;

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

        private ObservableCollection<string> _dFlagsList;
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

        private ObservableCollection<string> _lFlagsList;
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

        private ObservableCollection<string> _versionsList;
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

        private ObservableCollection<Tuple<string, string>> _otherConfigsList;
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
        private ObservableCollection<BuildOptionTypes> _buildOptionsList;
        public ObservableCollection<BuildOptionTypes> BuildOptionsList
        {
            get => _buildOptionsList;
            set => this.RaiseAndSetIfChanged(ref _buildOptionsList, value);
        }

        private List<BuildOptionTypes> _buildOptionTypesComboBox;

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
        public ReactiveCommand<Unit, Unit> ButtonCommand_SourceFileAddPath;
        public ReactiveCommand<Unit, Unit> ButtonCommand_SourceFileRemovePath;


        private string _txtAddSourcePath;

        public string txtAddSourcePath
        {
            get => _txtAddSourcePath;
            set => this.RaiseAndSetIfChanged(ref _txtAddSourcePath, value);
        }

        private ObservableCollection<string> _sourceFilePathListCollection;
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


        private void SetCurrentConfigIndex()
        {
            var index = CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations
                .FindIndex(s => s.Name == txtNameBuildConfig);

            if (index == -1)
            {
                MessageBox.Show("Please first write the name of the list (BuildConfig) you are working on.");
                CurrentBuildConfigID = 0;
                return;
            }

            CurrentBuildConfigID = index;
        }

        private int CurrentBuildConfigID = 0;

        private IdubManagerService dubManager;
        public VMprojectDubManager(IdubManagerService dubManagerService)
        {
            dubManager = dubManagerService;


            //name Config - build configurations
            ButtonCommand_ApplayNameBuildConfigurations = ReactiveCommand.Create( () =>
            {
                SetCurrentConfigIndex();
                if (NameConfigListCollection.Any(s => s != txtNameBuildConfig))
                {
                    NameConfigListCollection.Add(txtNameBuildConfig);
                    CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].Name =
                        txtNameBuildConfig;
                }
                
            });

            ButtonCommand_DeleteNameConfigurations = ReactiveCommand.Create( () =>
            {
                if (string.IsNullOrWhiteSpace(SelectedDeletedNameConfigOfList))
                {
                    MessageBox.Show("No configuration name selected to delete");
                    return;
                }
                var selectedId = CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations
                    .FindIndex(s => s.Name == SelectedDeletedNameConfigOfList);
                if (selectedId == -1)
                {
                    MessageBox.Show("Configuration name not found");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations.RemoveAt(selectedId);
                NameConfigListCollection.Remove(SelectedDeletedNameConfigOfList);
            });


            ButtonCommand_AddNewNameBuildConfiguarations = ReactiveCommand.Create( () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].Name =
                    txtAddNewConfigurationsName;
                NameConfigListCollection.Add(txtAddNewConfigurationsName);
            });

            // Authors Section
            ButtonCommand_Authors_Add = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtAddNewAuthors))
                {
                    MessageBox.Show("The input is invalid or is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.Authors.Add(txtAddNewAuthors);
            });

            ButtonCommand_Authors_Remove = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(SelectedAuthor))
                {
                    MessageBox.Show("No author selected to remove");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.Authors.Remove(SelectedAuthor);
            });

            // Dependencies
            ButtonCommand_AddDependency = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtAddDependencyKey) || string.IsNullOrWhiteSpace(txtAddDependencyValue))
                {
                    MessageBox.Show("Dependency key or value is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.Dependencies[txtAddDependencyKey] = txtAddDependencyValue;
                dependenciesListCollection.Add($"{txtAddDependencyKey}: {txtAddDependencyValue}");
            });

            ButtonCommand_RemoveSelectedDependency = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(SelectedDependencyRemove))
                {
                    MessageBox.Show("No dependency selected to remove");
                    return;
                }
                var keyToRemove = SelectedDependencyRemove.Split(':')[0].Trim();
                CurrentDubFileConfigAP.CurrentDubConfigAp.Dependencies.Remove(keyToRemove);
                dependenciesListCollection.Remove(SelectedDependencyRemove);
            });

            // D Flags
            ButtonCommand_DFlag_Add = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtAddDFlag))
                {
                    MessageBox.Show("D Flag input is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].DFlags.Add(txtAddDFlag);
                DFlagsList.Add(txtAddDFlag);
            });

            ButtonCommand_DFlag_Remove = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(SelectedDFlag))
                {
                    MessageBox.Show("No D Flag selected to remove");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].DFlags.Remove(SelectedDFlag);
                DFlagsList.Remove(SelectedDFlag);
            });

            // L Flags
            ButtonCommand_LFlag_Add = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtAddLFlag))
                {
                    MessageBox.Show("L Flag input is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].LFlags.Add(txtAddLFlag);
                LFlagsList.Add(txtAddLFlag);
            });

            ButtonCommand_LFlag_Remove = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(SelectedLFlag))
                {
                    MessageBox.Show("No L Flag selected to remove");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].LFlags.Remove(SelectedLFlag);
                LFlagsList.Remove(SelectedLFlag);
            });

            // Versions
            ButtonCommand_Version_Add = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtAddVersion))
                {
                    MessageBox.Show("Version input is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].Versions.Add(txtAddVersion);
                VersionsList.Add(txtAddVersion);
            });

            ButtonCommand_Version_Remove = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(SelectedVersion))
                {
                    MessageBox.Show("No version selected to remove");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].Versions.Remove(SelectedVersion);
                VersionsList.Remove(SelectedVersion);
            });

            // Other Configs
            ButtonCommand_OtherConfig_Add = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtOtherConfigName) || string.IsNullOrWhiteSpace(txtOtherConfigValue))
                {
                    MessageBox.Show("Config name or value is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].OtherDataBuildConfiguration[txtOtherConfigName] = txtOtherConfigValue;
                OtherConfigsList.Add(new Tuple<string, string>(txtOtherConfigName, txtOtherConfigValue));
            });

            ButtonCommand_OtherConfig_Remove = ReactiveCommand.Create(() =>
            {
                if (SelectedOtherConfig == null || string.IsNullOrWhiteSpace(SelectedOtherConfig.Item1))
                {
                    MessageBox.Show("No configuration selected to remove");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].OtherDataBuildConfiguration.Remove(SelectedOtherConfig.Item1);
                OtherConfigsList.Remove(SelectedOtherConfig);
            });

            // Source File
            ButtonCommand_SourceFileAddPath = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(txtAddSourcePath))
                {
                    MessageBox.Show("Source file path is empty");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.SourceFiles.Add(txtAddSourcePath);
                sourceFilePathListCollection.Add(txtAddSourcePath);
            });

            ButtonCommand_SourceFileRemovePath = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrWhiteSpace(selectedSourcePathListRemove))
                {
                    MessageBox.Show("No source file path selected to remove");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.SourceFiles.Remove(selectedSourcePathListRemove);
                sourceFilePathListCollection.Remove(selectedSourcePathListRemove);
            });

            // Build Options

            ButtonCommand_AddBuildOption = ReactiveCommand.Create(() =>
            {
                if (BuildOptionsList.Contains(SelectedBuildOption))
                {
                    MessageBox.Show("This build option is already added.");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].BuildOptions.Add(SelectedBuildOptionComboBox);
                
                BuildOptionsList.Add(SelectedBuildOptionComboBox);
            });

            ButtonCommand_RemoveBuildOption = ReactiveCommand.Create(() =>
            {
                if (SelectedBuildOption == null)
                {
                    MessageBox.Show("No build option selected to remove.");
                    return;
                }

                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].BuildOptions
                    .Remove(SelectedBuildOption);
                BuildOptionsList.Remove(SelectedBuildOption);
            });
        }
    }
}
