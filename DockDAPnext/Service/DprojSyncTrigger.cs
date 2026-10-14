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
using System.Threading;
using System.Threading.Tasks;

namespace DockDAPnext.Service
{
    // Bridges VS project events to the dub.json source sync: file add/remove/rename and
    // project save trigger an automatic sync (only after a dub.json was loaded this session,
    // so the tool never rewrites a file it does not own yet).
    public static class DprojSyncTrigger
    {
        private static bool _initialized;

        // 0 = idle, 1 = sync running, 2 = another change arrived while running (re-run).
        private static int _state;

        // COM event sources are kept in fields so they live as long as the package.
        // Fully qualified: Community.VisualStudio.Toolkit defines same-named types.
        private static EnvDTE.DocumentEvents _documentEvents;
        private static EnvDTE.ProjectItemsEvents _projectItemsEvents;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
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

            var extension = Path.GetExtension(path);
            if (IsDproj(extension))
            {
                ScheduleSync("project save");
            }
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

            if (string.Equals(Path.GetExtension(name), ".d", StringComparison.OrdinalIgnoreCase))
            {
                ScheduleSync(reason);
            }
        }

        private static bool IsDproj(string extension) =>
            string.Equals(extension, ".visualdproj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".dproj", StringComparison.OrdinalIgnoreCase);

        private static void ScheduleSync(string reason)
        {
            if (CurrentDubFileConfigAP.CurrentDubConfigAp == null)
            {
                return;
            }

            // A burst of events (multi-file add) marks state 2 so the loop runs once more
            // after the current pass and sees the final file list.
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            {
                Interlocked.CompareExchange(ref _state, 2, 1);
                return;
            }

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    while (true)
                    {
                        await RunOnceAsync(reason);

                        // 1 (nothing new) -> 0: done. 2 (new request) stays 2: loop again.
                        if (Interlocked.CompareExchange(ref _state, 0, 1) == 1)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DOCKDAP] auto-sync failed ({reason}): {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _state, 0);
                }
            });
        }

        private static async Task RunOnceAsync(string reason)
        {
            await TaskScheduler.Default;

            var provider = ServiceLocator.Provider;
            var dubService = provider?.GetService<IdubManagerService>();
            if (dubService == null)
            {
                return;
            }

            var result = await dubService.SyncSourceFilesFromDprojAP();

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
