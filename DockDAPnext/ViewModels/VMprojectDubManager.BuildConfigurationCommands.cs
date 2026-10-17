using ReactiveUI;
using System;
using System.Reactive;

namespace DockDAPnext.ViewModels
{
    public partial class VMprojectDubManager
    {
        // Add/remove commands of the selected build configuration: D flags, L flags,
        // versions, other key/value settings, and build options.
        private void SetupBuildConfigurationFieldCommands()
        {
            // Target Path
            ButtonCommandApplayTargetPathBuildConfig = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                // Empty clears the override: dub then falls back to the package-level
                // targetPath (or its own default) for this configuration.
                configuration.TargetPath = string.IsNullOrWhiteSpace(TxtTargetPathBuildConfig)
                    ? null
                    : TxtTargetPathBuildConfig.Trim();
            }));

            // D Flags
            ButtonCommandDFlagAdd = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddDFlag))
                {
                    ShowWarning("D flag value is empty.");
                    return;
                }

                configuration.DFlags.Add(TxtAddDFlag);
                DFlagsList.Add(TxtAddDFlag);
            }));

            ButtonCommandDFlagRemove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedDFlag))
                {
                    ShowWarning("Select a D flag before removing it.");
                    return;
                }

                configuration.DFlags.Remove(SelectedDFlag);
                DFlagsList.Remove(SelectedDFlag);
            }));

            // L Flags
            ButtonCommandLFlagAdd = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddLFlag))
                {
                    ShowWarning("L flag value is empty.");
                    return;
                }

                configuration.LFlags.Add(TxtAddLFlag);
                LFlagsList.Add(TxtAddLFlag);
            }));

            ButtonCommandLFlagRemove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedLFlag))
                {
                    ShowWarning("Select an L flag before removing it.");
                    return;
                }

                configuration.LFlags.Remove(SelectedLFlag);
                LFlagsList.Remove(SelectedLFlag);
            }));

            // Versions
            ButtonCommandVersionAdd = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtAddVersion))
                {
                    ShowWarning("Version value is empty.");
                    return;
                }

                configuration.Versions.Add(TxtAddVersion);
                VersionsList.Add(TxtAddVersion);
            }));

            ButtonCommandVersionRemove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedVersion))
                {
                    ShowWarning("Select a version before removing it.");
                    return;
                }

                configuration.Versions.Remove(SelectedVersion);
                VersionsList.Remove(SelectedVersion);
            }));

            // Other Configs
            ButtonCommandOtherConfigAdd = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtOtherConfigName) || string.IsNullOrWhiteSpace(TxtOtherConfigValue))
                {
                    ShowWarning("Configuration name or value is empty.");
                    return;
                }

                configuration.OtherDataBuildConfiguration[TxtOtherConfigName] = TxtOtherConfigValue;
                OtherConfigsList.Add(new Tuple<string, string>(TxtOtherConfigName, TxtOtherConfigValue));
            }));

            ButtonCommandOtherConfigRemove = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (SelectedOtherConfig == null || string.IsNullOrWhiteSpace(SelectedOtherConfig.Item1))
                {
                    ShowWarning("Select a configuration before removing it.");
                    return;
                }

                configuration.OtherDataBuildConfiguration.Remove(SelectedOtherConfig.Item1);
                OtherConfigsList.Remove(SelectedOtherConfig);
            }));

            // Build Options
            ButtonCommandAddBuildOption = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                if (BuildOptionsList.Contains(SelectedBuildOptionComboBox))
                {
                    ShowWarning("This build option has already been added.");
                    return;
                }

                configuration.BuildOptions.Add(SelectedBuildOptionComboBox);

                BuildOptionsList.Add(SelectedBuildOptionComboBox);
            }));

            ButtonCommandRemoveBuildOption = Guard(ReactiveCommand.Create(() =>
            {
                if (!TryGetCurrentBuildConfiguration(out var configuration))
                {
                    return;
                }

                configuration.BuildOptions.Remove(SelectedBuildOption);
                BuildOptionsList.Remove(SelectedBuildOption);
            }));
        }
    }
}
