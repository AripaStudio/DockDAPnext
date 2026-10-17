using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

namespace DockDAPnext.ViewModels
{
    public partial class VMprojectDubManager
    {
        #region ----- Authors Section ----
        public ReactiveCommand<Unit, Unit> ButtonCommandAuthorsAdd { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAuthorsRemove { get; private set; }

        private string _txtAddNewAuthors;

        public string TxtAddNewAuthors
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

        private string _txtNameProject;

        public string TxtNameProject
        {
            get => _txtNameProject;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtNameProject, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.Name = _txtNameProject;
                }
            }
        }

        private string _txtDescriptionProject;

        public string TxtDescriptionProject
        {
            get => _txtDescriptionProject;

            set
            {
                this.RaiseAndSetIfChanged(ref _txtDescriptionProject, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.Description = _txtDescriptionProject;
                }
            }
        }




        private string _txtLicenseProject;

        public string TxtLicenseProject
        {
            get => _txtLicenseProject;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtLicenseProject, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.License = _txtLicenseProject;
                }
            }
        }


        private string _txtTargetNameProject;

        public string TxtTargetNameProject
        {
            get => _txtTargetNameProject;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtTargetNameProject, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.TargetName = _txtTargetNameProject;
                }
            }
        }

        private string _txtTargetPathProject;

        // Package-level 'targetPath': the folder plain dub builds (no --config) put
        // their output into, relative to the dub.json folder. Empty writes nothing.
        public string TxtTargetPathProject
        {
            get => _txtTargetPathProject;
            set
            {
                this.RaiseAndSetIfChanged(ref _txtTargetPathProject, value);
                if (CurrentConfig != null)
                {
                    CurrentConfig.TargetPath = string.IsNullOrWhiteSpace(_txtTargetPathProject)
                        ? null
                        : _txtTargetPathProject.Trim();
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
        public ReactiveCommand<Unit, Unit> ButtonCommandAddDependency { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandRemoveSelectedDependency { get; private set; }


        private string _txtAddDependencyKey;

        public string TxtAddDependencyKey
        {
            get => _txtAddDependencyKey;
            set => this.RaiseAndSetIfChanged(ref _txtAddDependencyKey, value);
        }

        private string _txtAddDependencyValue;

        public string TxtAddDependencyValue
        {
            get => _txtAddDependencyValue;
            set => this.RaiseAndSetIfChanged(ref _txtAddDependencyValue, value);
        }

        private ObservableCollection<string> _dependenciesListCollection = new ObservableCollection<string>();

        public ObservableCollection<string> DependenciesListCollection
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
        #region sourceFile
        public ReactiveCommand<Unit, Unit> ButtonCommandSourceFileAddPath { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandSourceFileRemovePath { get; private set; }
        public ReactiveCommand<Unit, Unit> ButtonCommandSyncFromDproj { get; private set; }


        private string _txtAddSourcePath;

        public string TxtAddSourcePath
        {
            get => _txtAddSourcePath;
            set => this.RaiseAndSetIfChanged(ref _txtAddSourcePath, value);
        }

        private ObservableCollection<string> _sourceFilePathListCollection = new ObservableCollection<string>();
        public ObservableCollection<string> SourceFilePathListCollection
        {
            get => _sourceFilePathListCollection;
            set => this.RaiseAndSetIfChanged(ref _sourceFilePathListCollection, value);
        }

        private string _selectedSourcePathListRemove;

        public string SelectedSourcePathListRemove
        {
            get => _selectedSourcePathListRemove;
            set => this.RaiseAndSetIfChanged(ref _selectedSourcePathListRemove, value);
        }
        #endregion
    }
}
