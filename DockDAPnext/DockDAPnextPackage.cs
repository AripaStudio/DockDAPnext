global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Reflection;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading;
using DockDAPnext.AppConfiguration;
using DockDAPnext.Service;
using ReactiveUI;

namespace DockDAPnext
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidDockDAPnextPackageString)]
    // NOTE: the tool window type referenced here must be the ToolWindowPane
    // (DockDAPwindow.Pane) - the same type exposed by DockDAPwindow.PaneType.
    // Community.VisualStudio.Toolkit matches ProvideToolWindowAttribute.ToolType
    // against BaseToolWindow<T>.PaneType; using typeof(DockDAPwindow) instead makes
    // BaseToolWindow<DockDAPwindow>.Initialize() throw
    // "requires a ProvideToolWindow attribute on the package".
    [ProvideToolWindow(typeof(DockDaPwindow.Pane)
    ,Style = VsDockStyle.Tabbed
    , Window = "3AE79031-E1BC-11D0-8F78-00A0C9110057"
    , Orientation = ToolWindowOrientation.Right)]
    public sealed class DockDaPnextPackage : ToolkitPackage
    {
        // devenv's probe path skips the extension folder, so resolve private assemblies manually
        static DockDaPnextPackage()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string dir = Path.GetDirectoryName(typeof(DockDaPnextPackage).Assembly.Location);
                string path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
        }


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // ReactiveUI ships no WPF platform module in this extension, so its default "main
            // thread" is the thread pool: every ReactiveCommand then reports CanExecute from a
            // worker thread and WPF throws Dispatcher.VerifyAccess, killing devenv.exe.
            RxSchedulers.MainThreadScheduler = DispatcherScheduler.Current;

            await base.InitializeAsync(cancellationToken, progress);

            ServiceLocator.Build();

            await this.RegisterCommandsAsync();

            // Registers every BaseToolWindow<T> in this assembly with the package.
            // Without this, BaseToolWindow<DockDAPwindow> stays uninitialized and
            // DockDAPwindow.ShowAsync() throws "has not been initialized".
            this.RegisterToolWindows();

            // Hooks VS project events so dub.json sourceFiles follows .dproj changes.
            // The package-owned JoinableTaskFactory keeps the fire-and-forget hooks tracked.
            DprojSyncTrigger.Initialize(JoinableTaskFactory);
        }

      
    }
}