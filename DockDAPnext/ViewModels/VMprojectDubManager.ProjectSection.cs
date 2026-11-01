using DockDAPnext.Service.Models;
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

        // Unsupported is listed so a loaded entry can still be shown as what it is.
        public List<DependencyKind> DependencyKindList { get; } = new List<DependencyKind>
        {
            DependencyKind.Registry,
            DependencyKind.Git,
            DependencyKind.Path,
            DependencyKind.Unsupported
        };

        private DependencyKind _selectedDependencyKind = DependencyKind.Registry;

        // The editor swaps Repository/Path in and out of view and shows the JSON shape
        // the current fields will produce, so each dub form is entered in its own way.
        public DependencyKind SelectedDependencyKind
        {
            get => _selectedDependencyKind;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDependencyKind, value);
                this.RaisePropertyChanged(nameof(IsDependencyGit));
                this.RaisePropertyChanged(nameof(IsDependencyPath));
                this.RaisePropertyChanged(nameof(DependencyShapeHint));
            }
        }

        public bool IsDependencyGit => _selectedDependencyKind == DependencyKind.Git;

        public bool IsDependencyPath => _selectedDependencyKind == DependencyKind.Path;

        public string DependencyShapeHint
        {
            get
            {
                switch (_selectedDependencyKind)
                {
                    case DependencyKind.Git:
                        return "\"package\": { \"repository\": \"git+https://…\", \"version\": \"v1.0.0\" }";
                    case DependencyKind.Path:
                        return "\"package\": { \"path\": \"assets/library\" }";
                    case DependencyKind.Unsupported:
                        return "Unsupported shape. Choose Registry, Git or Path to replace this entry.";
                    default:
                        return "\"package\": \"~>1.0.0\"";
                }
            }
        }

        private string _txtDependencyName;

        public string TxtDependencyName
        {
            get => _txtDependencyName;
            set => this.RaiseAndSetIfChanged(ref _txtDependencyName, value);
        }

        private string _txtDependencyVersion;

        public string TxtDependencyVersion
        {
            get => _txtDependencyVersion;
            set => this.RaiseAndSetIfChanged(ref _txtDependencyVersion, value);
        }

        private string _txtDependencyRepository;

        public string TxtDependencyRepository
        {
            get => _txtDependencyRepository;
            set => this.RaiseAndSetIfChanged(ref _txtDependencyRepository, value);
        }

        private string _txtDependencyPath;

        public string TxtDependencyPath
        {
            get => _txtDependencyPath;
            set => this.RaiseAndSetIfChanged(ref _txtDependencyPath, value);
        }

        private ObservableCollection<DubDependencyInfo> _dependenciesListCollection = new ObservableCollection<DubDependencyInfo>();

        public ObservableCollection<DubDependencyInfo> DependenciesListCollection
        {
            get => _dependenciesListCollection;
            set => this.RaiseAndSetIfChanged(ref _dependenciesListCollection, value);
        }

        private DubDependencyInfo _selectedDependencyRemove;

        // Selecting a row loads it into the editor, which is how its kind becomes visible.
        public DubDependencyInfo SelectedDependencyRemove
        {
            get => _selectedDependencyRemove;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDependencyRemove, value);
                LoadDependencyEditor(value);
            }
        }

        private void LoadDependencyEditor(DubDependencyInfo dependency)
        {
            if (dependency == null)
            {
                ResetDependencyEditor();
                return;
            }

            SelectedDependencyKind = dependency.Kind;
            TxtDependencyName = dependency.Name;
            TxtDependencyVersion = dependency.Version;
            TxtDependencyRepository = dependency.Repository;
            TxtDependencyPath = dependency.Path;
        }

        private void ResetDependencyEditor()
        {
            SelectedDependencyKind = DependencyKind.Registry;
            TxtDependencyName = string.Empty;
            TxtDependencyVersion = string.Empty;
            TxtDependencyRepository = string.Empty;
            TxtDependencyPath = string.Empty;
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
