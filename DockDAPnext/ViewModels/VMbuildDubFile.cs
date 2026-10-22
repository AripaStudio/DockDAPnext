using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DockDAPnext.ViewModels
{
    public class VMbuildDubFile : BaseViewModel
    {
        public ReactiveCommand<Unit, Unit> ButtonCommandBuildDebug { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommandBuildRelease { get; }

         public ReactiveCommand<Unit, Unit> ButtonCommandBuildDefault { get; }

        private string _buildStatusText = "Idle. Pick a configuration below to run 'dub build'.";

        // Bound by UCbuildDubFile: the visible run state the user asked for
        // (running / succeeded / failed / not started).
        public string BuildStatusText
        {
            get => _buildStatusText;
            set => this.RaiseAndSetIfChanged(ref _buildStatusText, value);
        }

        private bool _isBuilding;

        // Bound by the three build buttons: one dub build at a time in one folder, or two
        // processes would interleave their output and fight over the same object files.
        public bool IsBuilding
        {
            get => _isBuilding;
            private set
            {
                this.RaiseAndSetIfChanged(ref _isBuilding, value);
                this.RaisePropertyChanged(nameof(CanStartBuild));
            }
        }

        public bool CanStartBuild => !_isBuilding;

        private bool _runAfterBuild;

        // Bound by the "Run after build" checkbox: when on, a successful build of an
        // executable launches the program without asking; when off, the user is asked.
        public bool RunAfterBuild
        {
            get => _runAfterBuild;
            set => this.RaiseAndSetIfChanged(ref _runAfterBuild, value);
        }

        private IDubManagerService _apiDockManager;


        public VMbuildDubFile(IDubManagerService dubManager)
        {

            _apiDockManager = dubManager;

            ButtonCommandBuildDebug = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                await RunBuildAsync("Debug", _apiDockManager.BuildDebugDubApAsync);
            }));

            ButtonCommandBuildDefault = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                await RunBuildAsync("Default", _apiDockManager.BuildDubApAsync);
            }));

            ButtonCommandBuildRelease = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                await RunBuildAsync("Release", _apiDockManager.BuildReleaseDubApAsync);
            }));

        }

        // After a successful build the user either opted in via the checkbox or gets
        // asked; only an executable targetType has a program worth launching.
        private async Task OfferRunAsync(string label, DubBuildResult result)
        {
            // The build ran against the on-disk dub.json: prefer its targetType over the
            // in-memory twin, which can be stale (edited file, another project selected).
            var builtTargetType = result.TargetType ?? CurrentConfig?.TargetType;
            var targetType = (builtTargetType ?? "executable").Trim().ToLowerInvariant();
            if (targetType != "executable")
            {
                ShowInfo($"{label} build finished successfully (exit code 0).\n\nThe target is a library ('{builtTargetType}'), so there is no program to run.");
                return;
            }

            var shouldRun = RunAfterBuild
                || AskYesNo($"{label} build finished successfully (exit code 0).\n\nRun the program now?");

            if (!shouldRun)
            {
                ShowInfo($"{label} build finished successfully (exit code 0).");
                return;
            }

            var run = await _apiDockManager.RunBuiltExecutableApAsync(result.WorkingDirectory, result.OutputDirectory);

            if (run.Started)
            {
                BuildStatusText = $"{label} build finished (exit code 0). Running: {run.ExecutablePath}";
            }
            else
            {
                BuildStatusText = $"{label} build finished, but the program was not run: {run.FailureReason}";
                ShowWarning(run.FailureReason);
            }
        }

        private async Task RunBuildAsync(string label, Func<Task<DubBuildResult>> build)
        {
            if (IsBuilding)
            {
                BuildStatusText = $"{label} build not started: another build is still running.";
                return;
            }

            if (!EnsureConfigLoaded())
            {
                return;
            }

            IsBuilding = true;
            try
            {
                BuildStatusText = $"{label} build running...";
                var result = await build();

                if (result == null)
                {
                    BuildStatusText = $"{label} build: the service returned no result.";
                    return;
                }

                if (result.Success)
                {
                    BuildStatusText = $"{label} build finished (exit code 0).";
                    await OfferRunAsync(label, result);
                }
                else if (!result.Started)
                {
                    BuildStatusText = $"{label} build not started: {result.FailureReason}";
                    ShowWarning($"{label} build was not started: {result.FailureReason}");
                }
                else
                {
                    BuildStatusText = $"{label} build failed (exit code {result.ExitCode}) - see the 'Dub Manager' Output pane.";
                    ShowWarning($"{label} build failed with exit code {result.ExitCode}. Details are in the 'Dub Manager' Output pane.");
                }
            }
            finally
            {
                IsBuilding = false;
            }
        }
    }
}
