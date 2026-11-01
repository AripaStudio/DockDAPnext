using DockDAPnext.Service;
using DockDAPnext.Service.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
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

                configurations.Add(CreateConfiguration(TxtAddNewConfigurationsName));
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

                var author = TxtAddNewAuthors.Trim();
                if (CurrentConfig.Authors.Any(a => string.Equals(a, author, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowWarning($"'{author}' is already listed as an author.");
                    return;
                }

                CurrentConfig.Authors.Add(author);
                AuthorsList.Add(author);
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

            // Dependencies: one upsert that covers all three dub shapes (registry / git / path).
            ButtonCommandAddDependency = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                var name = (TxtDependencyName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowWarning("Dependency name is empty.");
                    return;
                }

                var dependency = new DubDependencyInfo
                {
                    Name = name,
                    Kind = SelectedDependencyKind,
                    Version = CleanDependencyField(TxtDependencyVersion),
                    Repository = CleanDependencyField(TxtDependencyRepository),
                    Path = CleanDependencyField(TxtDependencyPath),
                };

                var token = DubManagerAp.CreateDependencyToken(dependency);
                if (!token.IsSuccess)
                {
                    ShowWarning(token.Message);
                    return;
                }

                var existingKey = CurrentConfig.Dependencies.Keys.FirstOrDefault(
                    k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
                var actualKey = existingKey ?? name;
                CurrentConfig.Dependencies[actualKey] = token.Value;

                // Replacing the row keeps the list in step with the dictionary.
                var row = dependency with { Name = actualKey };
                var oldRow = DependenciesListCollection.FirstOrDefault(d => string.Equals(
                    d.Name, actualKey, StringComparison.OrdinalIgnoreCase));
                if (oldRow == null)
                {
                    DependenciesListCollection.Add(row);
                }
                else
                {
                    DependenciesListCollection[DependenciesListCollection.IndexOf(oldRow)] = row;
                }

                SelectedDependencyRemove = row;
            }));

            ButtonCommandRemoveSelectedDependency = Guard(ReactiveCommand.Create(() =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                if (SelectedDependencyRemove == null)
                {
                    ShowWarning("Select a dependency before removing it.");
                    return;
                }

                var name = SelectedDependencyRemove.Name;
                var existingKey = CurrentConfig.Dependencies.Keys.FirstOrDefault(
                    k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
                if (existingKey != null)
                {
                    CurrentConfig.Dependencies.Remove(existingKey);
                }

                DependenciesListCollection.Remove(SelectedDependencyRemove);
                SelectedDependencyRemove = null;
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

                var sourcePath = TxtAddSourcePath.Trim();
                if (CurrentConfig.SourceFiles.Any(p => string.Equals(p?.Trim(), sourcePath, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowWarning($"'{sourcePath}' is already in sourceFiles.");
                    return;
                }

                CurrentConfig.SourceFiles.Add(sourcePath);
                SourceFilePathListCollection.Add(sourcePath);
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

            // Replace dub.json sourceFiles with the .d files listed in the selected .visualdproj.
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

        // A configuration this extension creates carries ready-to-use lists: files loaded
        // from disk are normalized in LoadFromConfig, but a brand-new entry must not hand
        // the flag tabs a null list (adding a configuration used to throw).
        public static BuildConfiguration CreateConfiguration(string name) => new BuildConfiguration
        {
            Name = name,
            DFlags = new List<string>(),
            LFlags = new List<string>(),
            Versions = new List<string>(),
            BuildOptions = new List<BuildOptionTypes>(),
        };

        // An empty editor box means "no value", not an empty string in dub.json.
        private static string CleanDependencyField(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
