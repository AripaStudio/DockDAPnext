using DockDAPnext.Service;
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

                var key = TxtAddDependencyKey.Trim();
                var value = TxtAddDependencyValue.Trim();
                var existingKey = CurrentConfig.Dependencies.Keys.FirstOrDefault(
                    k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                var actualKey = existingKey ?? key;

                CurrentConfig.Dependencies[actualKey] = value;

                // Replacing the row keeps the list in step with the dictionary; appending
                // it again used to show a second line for a package that has one entry.
                var oldLine = DependenciesListCollection.FirstOrDefault(d => string.Equals(
                    DependencyKeyOf(d), actualKey, StringComparison.OrdinalIgnoreCase));
                if (oldLine == null)
                {
                    DependenciesListCollection.Add(DependencyLine(actualKey, value));
                }
                else
                {
                    DependenciesListCollection[DependenciesListCollection.IndexOf(oldLine)] =
                        DependencyLine(actualKey, value);
                }
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

                var keyToRemove = DependencyKeyOf(SelectedDependencyRemove);
                var existingKey = CurrentConfig.Dependencies.Keys.FirstOrDefault(
                    k => string.Equals(k, keyToRemove, StringComparison.OrdinalIgnoreCase));
                if (existingKey != null)
                {
                    CurrentConfig.Dependencies.Remove(existingKey);
                }

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

        // One display format for a dependency row, so a lookup can find the row it must replace.
        private static string DependencyLine(string key, string value) => key + ": " + value;

        private static string DependencyKeyOf(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }

            var separator = line.IndexOf(": ", StringComparison.Ordinal);
            return (separator < 0 ? line : line.Substring(0, separator)).Trim();
        }
    }
}
