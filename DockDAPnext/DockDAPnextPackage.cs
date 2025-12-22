global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using DockDAPnext.AppConfiguration;
using Microsoft.Extensions.Hosting;

namespace DockDAPnext
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.DockDAPnextString)]
    [ProvideToolWindow(typeof(DockDAPwindow)
    ,Style = VsDockStyle.Tabbed
    , Orientation = ToolWindowOrientation.Right)]
    public sealed class DockDAPnextPackage : ToolkitPackage
    {

        public static IHost appHost { get; set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            appHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.ConfigurationServices();
                }).Build();

            await appHost.StartAsync(cancellationToken);

            

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (appHost != null)
                {
                    appHost.StopAsync().GetAwaiter().GetResult();
                    appHost.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}