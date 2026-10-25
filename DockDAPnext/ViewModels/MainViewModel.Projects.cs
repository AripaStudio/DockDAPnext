using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DockDAPnext.Service;
using ReactiveUI;

namespace DockDAPnext.ViewModels
{
    // The solution-project selector: it lists the solution's D projects and loads the chosen one into every tab.
    public partial class MainViewModel
    {
        private SolutionDubProject _selectedSolutionProject;
        private string _selectedProjectStatus = "Reading the solution projects...";
        private bool _suppressProjectReload;

        // Only the newest selection may publish its result; two quick picks would otherwise race.
        private int _loadGeneration;

        public ObservableCollection<SolutionDubProject> SolutionProjects { get; } =
            new ObservableCollection<SolutionDubProject>();

        public SolutionDubProject SelectedSolutionProject
        {
            get => _selectedSolutionProject;
            set
            {
                if (ReferenceEquals(_selectedSolutionProject, value))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref _selectedSolutionProject, value);

                // Selecting only announces the choice: Refresh is the single load trigger.
                if (!_suppressProjectReload && value != null)
                {
                    SelectedProjectStatus = "Press Refresh to load " + value.DisplayName + ".";
                }
            }
        }

        public string SelectedProjectStatus
        {
            get => _selectedProjectStatus;
            private set => this.RaiseAndSetIfChanged(ref _selectedProjectStatus, value);
        }

        public ReactiveCommand<Unit, Unit> ButtonCommandRefreshProjects { get; private set; }

        private void InitializeSolutionProjects()
        {
            ButtonCommandRefreshProjects = Guard(ReactiveCommand.CreateFromTask(RefreshAndLoadSelectedProjectAsync));
        }

        // Refresh re-reads the solution and loads the selected project; the selection itself never loads.
        public async Task RefreshAndLoadSelectedProjectAsync()
        {
            await RefreshSolutionProjectsAsync();

            if (SelectedSolutionProject == null)
            {
                return;
            }

            await LoadProjectAsync(SelectedSolutionProject);
        }

        // Rebuilds the list only and keeps a matching choice; no project is loaded here.
        public async Task RefreshSolutionProjectsAsync()
        {
            try
            {
                var discovered = await Dubmanager.GetSolutionProjectsApAsync();
                var previous = SelectedSolutionProject ?? CurrentDubFileConfigAp.SelectedProject;

                _suppressProjectReload = true;
                SolutionProjects.Clear();
                foreach (var project in discovered)
                {
                    SolutionProjects.Add(project);
                }

                var target = previous == null
                    ? null
                    : discovered.FirstOrDefault(p => SolutionProjectsAp.SameDirectory(p.Directory, previous.Directory));

                if (target == null && discovered.Count == 0)
                {
                    SelectedSolutionProject = null;
                    _suppressProjectReload = false;

                    // The solution was closed or emptied: the tabs must not keep an orphaned file.
                    CurrentDubFileConfigAp.SelectedProject = null;
                    CurrentDubFileConfigAp.CurrentDubConfigAp = null;
                    VmProjectDubManager.ClearConfig();
                    SelectedProjectStatus = "This solution has no D project and no dub.json yet.";
                    return;
                }

                SelectedSolutionProject = target;
                _suppressProjectReload = false;

                if (target == null)
                {
                    SelectedProjectStatus = "Select a D project, then press Refresh.";
                }
                else if (CurrentDubFileConfigAp.CurrentDubConfigAp == null)
                {
                    SelectedProjectStatus = "Press Refresh to load " + target.DisplayName + ".";
                }
            }
            catch (Exception ex)
            {
                _suppressProjectReload = false;
                SelectedProjectStatus = "The solution projects could not be read: " + ex.Message;
            }
        }

        // The single load entry point: the Refresh command and the console probes both use it.
        public async Task LoadProjectAsync(SolutionDubProject project)
        {
            var generation = ++_loadGeneration;

            try
            {
                var result = await Dubmanager.SelectDubProjectApAsync(project);
                if (generation != _loadGeneration)
                {
                    return;
                }

                if (!result.IsSuccess)
                {
                    // The selector keeps the choice; the status line explains, no modal dialog.
                    ClearManagedProject(project, result.Message);
                    return;
                }

                CurrentDubFileConfigAp.CurrentDubConfigAp = result.Value;
                VmProjectDubManager.LoadFromConfig(result.Value);
                SelectedProjectStatus = "Managing " + project.Name + ": " + project.DubJsonPath;
                System.Diagnostics.Debug.WriteLine("[DOCKDAP] managing " + project.DubJsonPath);
            }
            catch (Exception ex)
            {
                if (generation == _loadGeneration)
                {
                    ClearManagedProject(project, ex.Message);
                }
            }
        }

        // Never keep the previous project's values: A's fields under B's name invite editing the wrong file.
        private void ClearManagedProject(SolutionDubProject project, string message)
        {
            CurrentDubFileConfigAp.CurrentDubConfigAp = null;
            VmProjectDubManager.ClearConfig();
            SelectedProjectStatus = project.Name + ": " + message;
        }
    }
}
