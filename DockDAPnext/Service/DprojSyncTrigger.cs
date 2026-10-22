using DockDAPnext.AppConfiguration;
using DockDAPnext.Service.InterFaces;
using DockDAPnext.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DockDAPnext.Service
{
    // Bridges VS project events to the dub.json source sync, only after a file was loaded this session.
    public static class DprojSyncTrigger
    {
        private static bool _initialized;

        // RunAsync results are kept in fields so the loops stay tracked/joinable (VSSDK007).
        private static JoinableTask _eventHookTask;
        private static JoinableTask _syncLoopTask;

        // Package-owned JoinableTasks are tracked for shutdown, so fire-and-forget stays legal (VSSDK007).
        private static JoinableTaskFactory _factory;

        // One loop at a time; a change that arrives during a run is coalesced into one more run.
        private static readonly object SyncGate = new object();
        private static bool _syncRunning;
        private static bool _syncRequested;

        // Kept in fields so they outlive the hook (Toolkit defines same-named types).
        private static EnvDTE.DocumentEvents _documentEvents;
        private static EnvDTE.ProjectItemsEvents _projectItemsEvents;

        public static void Initialize(JoinableTaskFactory factory)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _factory = factory ?? ThreadHelper.JoinableTaskFactory;

            _eventHookTask = _factory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                    if (dte == null)
                    {
                        Debug.WriteLine("[DOCKDAP] auto-sync hook: DTE unavailable");
                        return;
                    }

                    _documentEvents = dte.Events.DocumentEvents;
                    _documentEvents.DocumentSaved += OnDocumentSaved;

                    _projectItemsEvents = ((Events2)dte.Events).ProjectItemsEvents;
                    _projectItemsEvents.ItemAdded += OnItemAdded;
                    _projectItemsEvents.ItemRemoved += OnItemRemoved;
                    _projectItemsEvents.ItemRenamed += OnItemRenamed;

                    Debug.WriteLine("[DOCKDAP] auto-sync hook installed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DOCKDAP] auto-sync hook failed: {ex.Message}");
                }
            });
        }

        private static void OnDocumentSaved(EnvDTE.Document document)
        {
            var path = document?.FullName;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!SolutionProjectsAp.IsDProject(path))
            {
                return;
            }

            if (!BelongsToSelectedProject(path))
            {
                Debug.WriteLine("[DOCKDAP] auto-sync skipped (other project): " + path);
                return;
            }

            ScheduleSync("project save");
        }

        private static void OnItemAdded(EnvDTE.ProjectItem item) => ScheduleForDSource(item, "item added");

        private static void OnItemRemoved(EnvDTE.ProjectItem item) => ScheduleForDSource(item, "item removed");

        private static void OnItemRenamed(EnvDTE.ProjectItem item, string oldName) => ScheduleForDSource(item, "item rename");

        private static void ScheduleForDSource(EnvDTE.ProjectItem item, string reason)
        {
            var name = item?.Name;
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!string.Equals(Path.GetExtension(name), ".d", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var projectPath = GetContainingProjectPath(item);
            if (projectPath != null && !BelongsToSelectedProject(projectPath))
            {
                Debug.WriteLine("[DOCKDAP] auto-sync skipped (other project): " + projectPath);
                return;
            }

            ScheduleSync(reason);
        }

        // The selector wins: another project's event must not write the selected project's dub.json.
        private static bool BelongsToSelectedProject(string path)
        {
            var selected = CurrentDubFileConfigAp.SelectedProject;
            if (selected == null || string.IsNullOrEmpty(selected.Directory))
            {
                return true;
            }

            return SolutionProjectsAp.IsInsideDirectory(path, selected.Directory);
        }

        private static string GetContainingProjectPath(EnvDTE.ProjectItem item)
        {
            try
            {
                return item?.ContainingProject?.FullName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DOCKDAP] project lookup failed: " + ex.Message);
                return null;
            }
        }

        private static void ScheduleSync(string reason)
        {
            if (CurrentDubFileConfigAp.CurrentDubConfigAp == null)
            {
                return;
            }

            lock (SyncGate)
            {
                _syncRequested = true;
                if (_syncRunning)
                {
                    return;
                }

                _syncRunning = true;
            }

            _syncLoopTask = _factory.RunAsync(async () =>
            {
                try
                {
                    while (true)
                    {
                        lock (SyncGate)
                        {
                            _syncRequested = false;
                        }

                        await RunOnceAsync(reason);

                        // Checked under the lock: a request landing during the run is never dropped.
                        lock (SyncGate)
                        {
                            if (!_syncRequested)
                            {
                                _syncRunning = false;
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DOCKDAP] auto-sync failed ({reason}): {ex.Message}");
                    lock (SyncGate)
                    {
                        _syncRunning = false;
                        _syncRequested = false;
                    }
                }
            });
        }

        private static async Task RunOnceAsync(string reason)
        {
            await TaskScheduler.Default;

            var provider = ServiceLocator.Provider;
            var dubService = provider?.GetService<IDubManagerService>();
            if (dubService == null)
            {
                return;
            }

            var result = await dubService.SyncSourceFilesFromDprojApAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!result.Success)
            {
                Debug.WriteLine($"[DOCKDAP] auto-sync skipped ({reason}): {result.ErrorMessage}");
                return;
            }

            provider.GetService<VMprojectDubManager>()?.ApplySyncResult(result);
            Debug.WriteLine(result.Changed
                ? $"[DOCKDAP] auto-sync ({reason}): +{result.Added} -{result.Removed}, {result.Total} source files"
                : $"[DOCKDAP] auto-sync ({reason}): already in sync");
        }
    }
}
