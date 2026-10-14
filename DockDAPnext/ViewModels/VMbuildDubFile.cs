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
        public ReactiveCommand<Unit, Unit> ButtonCommand_buildDebug { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_buildRelease { get; }

         public ReactiveCommand<Unit, Unit> ButtonCommand_buildDefault { get; }

        private string _buildStatusText = "Idle. Pick a configuration below to run 'dub build'.";

        // Bound by UCbuildDubFile: the visible run state the user asked for
        // (running / succeeded / failed / not started).
        public string BuildStatusText
        {
            get => _buildStatusText;
            set => this.RaiseAndSetIfChanged(ref _buildStatusText, value);
        }

        private bool _runAfterBuild;

        // Bound by the "Run after build" checkbox: when on, a successful build of an
        // executable launches the program without asking; when off, the user is asked.
        public bool RunAfterBuild
        {
            get => _runAfterBuild;
            set => this.RaiseAndSetIfChanged(ref _runAfterBuild, value);
        }

        private IdubManagerService apiDockManager;


        public VMbuildDubFile(IdubManagerService dubManager)
        {

            apiDockManager = dubManager;

            ButtonCommand_buildDebug = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_buildDebug");
                await RunBuildAsync("Debug", apiDockManager.BuildDebugDubAP);
            }));

            ButtonCommand_buildDefault = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_buildDefault");
                await RunBuildAsync("Default", apiDockManager.BuildDubAP);
            }));

            ButtonCommand_buildRelease = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_buildRelease");
                await RunBuildAsync("Release", apiDockManager.BuildReleaseDubAP);
            }));

            DiagLog.Log("VMbuildDubFile: ctor done, debugNull=" + (ButtonCommand_buildDebug == null)
                + " releaseNull=" + (ButtonCommand_buildRelease == null)
                + " defaultNull=" + (ButtonCommand_buildDefault == null));
        }

        // After a successful build the user either opted in via the checkbox or gets
        // asked; only an executable targetType has a program worth launching.
        private async Task OfferRunAsync(string label, DubBuildResult result)
        {
            var targetType = (CurrentConfig?.TargetType ?? "executable").Trim().ToLowerInvariant();
            if (targetType != "executable")
            {
                ShowInfo($"{label} build finished successfully (exit code 0).\n\nThe target is a library ('{CurrentConfig.TargetType}'), so there is no program to run.");
                return;
            }

            var shouldRun = RunAfterBuild
                || AskYesNo($"{label} build finished successfully (exit code 0).\n\nRun the program now?");

            if (!shouldRun)
            {
                ShowInfo($"{label} build finished successfully (exit code 0).");
                return;
            }

            var run = await apiDockManager.RunBuiltExecutableAP(result.WorkingDirectory);

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
            if (!EnsureConfigLoaded())
            {
                return;
            }

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
    }
}
