global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using DockDAPnext.AppConfiguration;
using Microsoft.VisualStudio;
using MessageBox = System.Windows.Forms.MessageBox;

namespace DockDAPnext
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("VSCommandTable.ctmenu", 1)]
    [Guid(PackageGuids.guidDockDAPnextPackageString)]
    [ProvideToolWindow(typeof(DockDAPwindow)
    ,Style = VsDockStyle.Tabbed
    , Window = "3AE79031-E1BC-11D0-8F78-00A0C9110057"
    , Orientation = ToolWindowOrientation.Right)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string , PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class DockDAPnextPackage : ToolkitPackage
    {


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            MessageBox.Show("DockDAP is Run");

            await base.InitializeAsync(cancellationToken, progress);


            ServiceLocator.Build();

            

            await this.RegisterCommandsAsync();

            

        }

      
    }
}