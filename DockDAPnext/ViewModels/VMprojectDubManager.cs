using DockDAPnext.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

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
            set
            {
                this.RaiseAndSetIfChanged(ref _txtAddNewAuthors, value);
            }
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
            
        public List<TargetTypeEnum> TargetTypeListComboBox { get;  } = new List<TargetTypeEnum>()
        {
            TargetTypeEnum.executable,
            TargetTypeEnum.sourceLibrary,
            TargetTypeEnum.library,
            TargetTypeEnum.dynamicLibrary,
            TargetTypeEnum.staticLibrary,
            TargetTypeEnum.autodetect,
            TargetTypeEnum.none
        };



        //Dependencies
        //build
        //sourceFile

    }
}
