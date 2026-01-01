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

        //----- Authors Section ----
        public ReactiveCommand<Unit, Unit> ButtonCommand_Authors_Add { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_Authors_Rrmove { get; }

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


        //----- Project Configuration ----
        private string _txtName_Project;

        public string txtName_Project
        {
            get => _txtName_Project;
            set => this.RaiseAndSetIfChanged(ref _txtName_Project, value);
        }

        private string _txtDescription_Project;

        public string txtDescription_Project
        {
            get => _txtDescription_Project;
            set => this.RaiseAndSetIfChanged(ref _txtDescription_Project, value);
        }


        private string _txtLicense_project;

        public string txtLicense_project
        {
            get => _txtLicense_project;
            set => this.RaiseAndSetIfChanged(ref _txtLicense_project, value);
        }


        private string _txtTargetName_project;

        public string txtTargetName_project
        {
            get => _txtTargetName_project;
            set => this.RaiseAndSetIfChanged(ref _txtTargetName_project, value);
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
            set => this.RaiseAndSetIfChanged(ref _selectedTargetType, value);
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



        //----- Dependencies -----
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



        //build Configuration 

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



        //sourceFile
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

        private void SetCurrentConfigIndex()
        {
            var index = CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations
                .FindIndex(s => s.Name == txtNameBuildConfig);

            if (index == -1)
            {
                MessageBox.Show("Please first write the name of the list (BuildConfig) you are working on.");
            }

            CurrentBuildConfigID = index;
        }

        private int CurrentBuildConfigID = 0;

        private IdubManagerService dubManager;
        public VMprojectDubManager(IdubManagerService dubManagerService)
        {
            dubManager = dubManagerService;

            // Authors Section
            ButtonCommand_Authors_Add = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.Authors.Add(txtAddNewAuthors);
            });

            ButtonCommand_Authors_Rrmove = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.Authors.Remove(SelectedAuthor);
            });

            // Dependencies
            ButtonCommand_AddDependency = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.Dependencies[txtAddDependencyKey] = txtAddDependencyValue;
                dependenciesListCollection.Add($"{txtAddDependencyKey}: {txtAddDependencyValue}");
            });

            ButtonCommand_RemoveSelectedDependency = ReactiveCommand.CreateFromTask(async () =>
            {
                var keyToRemove = SelectedDependencyRemove.Split(':')[0].Trim();
                CurrentDubFileConfigAP.CurrentDubConfigAp.Dependencies.Remove(keyToRemove);
                dependenciesListCollection.Remove(SelectedDependencyRemove);
            });

            // D Flags
            ButtonCommand_DFlag_Add = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].DFlags.Add(SelectedDFlag);
                DFlagsList.Add(txtAddDFlag);

            });

            ButtonCommand_DFlag_Remove = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].DFlags.Remove(SelectedDFlag);
                DFlagsList.Remove(SelectedDFlag);

            });

            // L Flags
            ButtonCommand_LFlag_Add = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].LFlags.Add(txtAddLFlag);
                LFlagsList.Add(txtAddLFlag);


            });

            ButtonCommand_LFlag_Remove = ReactiveCommand.CreateFromTask(async () =>
            {

                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].LFlags.Remove(SelectedLFlag);
                LFlagsList.Remove(SelectedLFlag);


            });

            // Versions
            ButtonCommand_Version_Add = ReactiveCommand.CreateFromTask(async () =>
            {

                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].Versions.Add(txtAddVersion);
                VersionsList.Add(txtAddVersion);


            });

            ButtonCommand_Version_Remove = ReactiveCommand.CreateFromTask(async () =>
            {

                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].Versions.Remove(SelectedVersion);
                VersionsList.Remove(SelectedVersion);


            });

            // Other Configs
            ButtonCommand_OtherConfig_Add = ReactiveCommand.CreateFromTask(async () =>
            {

                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].OtherDataBuildConfiguration[txtOtherConfigName] = txtOtherConfigValue;
                OtherConfigsList.Add(new Tuple<string, string>(txtOtherConfigName, txtOtherConfigValue));


            });

            ButtonCommand_OtherConfig_Remove = ReactiveCommand.CreateFromTask(async () =>
            {

                CurrentDubFileConfigAP.CurrentDubConfigAp.BuildConfigurations[CurrentBuildConfigID].OtherDataBuildConfiguration.Remove(SelectedOtherConfig.Item1);
                OtherConfigsList.Remove(SelectedOtherConfig);


            });

            // Source File
            ButtonCommand_SourceFileAddPath = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.SourceFiles.Add(txtAddSourcePath);
                sourceFilePathListCollection.Add(txtAddSourcePath);
            });

            ButtonCommand_SourceFileRemovePath = ReactiveCommand.CreateFromTask(async () =>
            {
                CurrentDubFileConfigAP.CurrentDubConfigAp.SourceFiles.Remove(selectedSourcePathListRemove);
                sourceFilePathListCollection.Remove(selectedSourcePathListRemove);
            });
        }
    }
}
