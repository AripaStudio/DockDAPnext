global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using DockDAPnext.AppConfiguration;

namespace DockDAPnext
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidDockDAPnextPackageString)]
    [ProvideToolWindow(typeof(DockDAPwindow)
    ,Style = VsDockStyle.Tabbed
    , Window = "3AE79031-E1BC-11D0-8F78-00A0C9110057"
    , Orientation = ToolWindowOrientation.Right)]
    public sealed class DockDAPnextPackage : ToolkitPackage
    {
        // devenv's probe path skips the extension folder, so resolve private assemblies manually
        static DockDAPnextPackage()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string dir = Path.GetDirectoryName(typeof(DockDAPnextPackage).Assembly.Location);
                string path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
        }


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await base.InitializeAsync(cancellationToken, progress);


            ServiceLocator.Build();

            

            await this.RegisterCommandsAsync();

            

        }

      
    }
}