using DockDAPnext.Service;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace DockDAPnext.ViewModels
{
    public partial class VMprojectDubManager
    {
        // Build-config name commands: apply/rename the selected configuration,
        // add a new one, delete one.
        private void SetupBuildConfigurationNameCommands()
        {
            ButtonCommandApplayNameBuildConfigurations = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded() || string.IsNullOrWhiteSpace(TxtNameBuildConfig))
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
                configurations[_currentBuildConfigId].Name = TxtNameBuildConfig;
                NameConfigListCollection = new ObservableCollection<string>(configurations.Select(c => c.Name));
            }));

            ButtonCommandDeleteNameConfigurations = Guard(ReactiveCommand.Create(() =>
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
                _currentBuildConfigId = 0;
                LoadCurrentBuildConfigurationLists();
            }));


            ButtonCommandAddNewNameBuildConfiguarations = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddNewConfigurationsName))
                {
                    ShowWarning("Build configuration name is empty.");
                    return;
                }

                var configurations = CurrentConfig.BuildConfigurations;
                if (configurations.Any(s => s.Name == TxtAddNewConfigurationsName))
                {
                    ShowWarning("This name has already been added.");
                    return;
                }

                configurations.Add(new BuildConfiguration { Name = TxtAddNewConfigurationsName });
                NameConfigListCollection.Add(TxtAddNewConfigurationsName);
                _currentBuildConfigId = configurations.Count - 1;
                LoadCurrentBuildConfigurationLists();
            }));
        }

        // Add/remove commands of the project sections: authors, dependencies,
        // source files, and the .dproj sourceFiles sync.
        private void SetupProjectSectionCommands()
        {
            // Authors Section
            ButtonCommandAuthorsAdd = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddNewAuthors))
                {
                    ShowWarning("Author name is empty.");
                    return;
                }

                CurrentConfig.Authors.Add(TxtAddNewAuthors);
                AuthorsList.Add(TxtAddNewAuthors);
            }));

            ButtonCommandAuthorsRemove = Guard(ReactiveCommand.Create(() =>
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
            ButtonCommandAddDependency = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddDependencyKey) || string.IsNullOrWhiteSpace(TxtAddDependencyValue))
                {
                    ShowWarning("Dependency name or value is empty.");
                    return;
                }

                CurrentConfig.Dependencies[TxtAddDependencyKey] = TxtAddDependencyValue;
                DependenciesListCollection.Add($"{TxtAddDependencyKey}: {TxtAddDependencyValue}");
            }));

            ButtonCommandRemoveSelectedDependency = Guard(ReactiveCommand.Create(() =>
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
                DependenciesListCollection.Remove(SelectedDependencyRemove);
            }));

            // Source File
            ButtonCommandSourceFileAddPath = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddSourcePath))
                {
                    ShowWarning("Source file path is empty.");
                    return;
                }

                CurrentConfig.SourceFiles.Add(TxtAddSourcePath);
                SourceFilePathListCollection.Add(TxtAddSourcePath);
            }));

            ButtonCommandSourceFileRemovePath = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedSourcePathListRemove))
                {
                    ShowWarning("Select a path before removing it.");
                    return;
                }

                CurrentConfig.SourceFiles.Remove(SelectedSourcePathListRemove);
                SourceFilePathListCollection.Remove(SelectedSourcePathListRemove);
            }));

            // Replace dub.json sourceFiles with the .d files listed in the active .visualdproj.
            ButtonCommandSyncFromDproj = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await _dubManager.SyncSourceFilesFromDprojApAsync();
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
        }
    }
}
